using ForceFeedbackSharpDx;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Journals
{
    public class Client : IDisposable
    {
        private EliteAPI.EliteDangerousApi eliteAPI;
        private bool _disposed;
        private ILogger logger;

        private readonly List<DeviceEvents> Devices = new List<DeviceEvents>();

        // Cached Status.json values for change detection (Status.* synthetic events don't include values)
        private long _lastStatusFlags = -1;
        private bool? _lastStatusGear;

        /// <summary>
        /// Initialize with optional API instance for testing. When <paramref name="testApi"/> is non-null,
        /// that instance is used and Start() is not called (caller can invoke events via <see cref="SimulateEvent"/>).
        /// </summary>
        public Task Initialize(Settings settings, EliteAPI.EliteDangerousApi testApi = null)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "hh:mm:ss "; }).SetMinimumLevel(LogLevel.Debug);
            });
            logger = loggerFactory.CreateLogger<Client>();

            var xinputIndicesAdded = new HashSet<int>();
            var defaultXInputEvents = GetDefaultXInputEventConfig();

            foreach (var device in settings.Devices ?? new List<Device>())
            {
                if (device.XInput == true)
                {
                    var userIndex = device.UserIndex ?? -1;
                    if (userIndex >= 0)
                    {
                        TryAddXInputDevice(userIndex, device, defaultXInputEvents, xinputIndicesAdded, settings, requireConnected: true);
                    }
                    continue;
                }

                var ffDevice = new ForceFeedbackController() { Logger = logger };
                if (ffDevice.Initialize(
                        device.ProductGuid,
                        device.ProductName,
                        @".\Forces",
                        device.AutoCenter ?? true,
                        device.ForceFeedbackGain ?? 10000) == false)
                {
                    logger.LogError($"Device Initialization failed: {device.ProductGuid}: {device.ProductName}");
                    continue;
                }

                var deviceEvents = new DeviceEvents
                {
                    EventSettings = (device.StatusEvents ?? new List<EventConfiguration>()).ToDictionary(v => v.Event, v => v),
                    Device = ffDevice
                };

                Devices.Add(deviceEvents);
            }

            // Auto-add all XInput slots 0-3 and broadcast rumble to each (covers virtual controller setups)
            // Use the first XInput device config with UserIndex -1 (auto-detect) from settings, if any
            var xinputAutoConfig = (settings?.Devices ?? new List<Device>())
                .FirstOrDefault(d => d?.XInput == true && (d.UserIndex == null || d.UserIndex < 0));
            var eventsForAuto = (xinputAutoConfig?.StatusEvents?.Count > 0) ? xinputAutoConfig.StatusEvents : defaultXInputEvents;

            for (int i = 0; i <= 3; i++)
            {
                if (!xinputIndicesAdded.Contains(i))
                {
                    TryAddXInputDevice(i, xinputAutoConfig, eventsForAuto, xinputIndicesAdded, settings, requireConnected: false);
                }
            }

            eliteAPI = testApi ?? new EliteAPI.EliteDangerousApi();

            // Journal events -> Status mappings (v5 uses EliteAPI.Events.Game types)
            eliteAPI.On<EliteAPI.Events.Game.DockedEvent>(e => FindEffect("Status.Docked:True"));
            eliteAPI.On<EliteAPI.Events.Game.UndockedEvent>(e => FindEffect("Status.Docked:False"));
            eliteAPI.On<EliteAPI.Events.Game.TouchdownEvent>(e => FindEffect("Status.Landed:True"));
            eliteAPI.On<EliteAPI.Events.Game.LiftoffEvent>(e => FindEffect("Status.Landed:False"));

            // All typed events - use event name as key for user config (e.g. "Docked", "LoadGame")
            eliteAPI.OnAll(e => Events_AllEvent(e));

            // Status.json: parse Flags and emit Status.Gear:True etc. Do NOT call FindEffect for Journal events here -
            // OnAll/typed handlers already do that; calling it here would cause duplicate rumble.
            eliteAPI.OnAllJson(arg =>
            {
                var (eventName, json) = arg;
                if (eventName == "Status")
                    EmitStatusEventsFromJson(json);
            });

            if (testApi == null)
                eliteAPI.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// For testing: invoke a single event by JSON. Only valid when initialized with a test API (no file watchers).
        /// </summary>
        public void SimulateEvent(string json)
        {
            if (json == null) return;
            eliteAPI?.Invoke(json);
        }

        /// <summary>For testing: fire a single event key directly (e.g. "Status.Hardpoints:False"). Plays exactly one event as configured.</summary>
        public void TriggerEvent(string eventKey)
        {
            if (!string.IsNullOrEmpty(eventKey))
                FindEffect(eventKey);
        }

        /// <summary>Stop all rumble on XInput devices immediately. Use when rumble gets stuck.</summary>
        public void StopAllRumble()
        {
            foreach (var device in Devices)
            {
                if (device.Device is XInputRumbleDevice xinput)
                    xinput.StopRumble();
            }
        }

        private void EmitStatusEventsFromJson(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                var flagsToken = obj["Flags"];
                var gearToken = obj["Gear"];

                if (flagsToken != null && flagsToken.Type == JTokenType.Integer)
                {
                    var flags = flagsToken.Value<long>();
                    if (_lastStatusFlags >= 0)
                    {
                        EmitChangedStatusFlags(flags, obj);
                    }
                    _lastStatusFlags = flags;
                }

                if (gearToken != null)
                {
                    var gear = gearToken.Value<bool>();
                    if (_lastStatusGear.HasValue && _lastStatusGear.Value != gear)
                    {
                        FindEffect($"Status.Gear:{gear}");
                    }
                    _lastStatusGear = gear;
                }

                // Landed: skip - handled by TouchdownEvent/LiftoffEvent typed handlers
            }
            catch (Exception ex)
            {
                logger.LogDebug("Failed to parse Status json: {Ex}", ex.Message);
            }
        }

        /// <summary>Emit FindEffect for each changed flag. Skip Docked/Landed (handled by typed handlers). Skip Gear/Landed when obj has explicit tokens.</summary>
        private void EmitChangedStatusFlags(long flags, JObject obj)
        {
            long prev = _lastStatusFlags;
            var hasGearToken = obj?["Gear"] != null;
            // Docked and Landed are handled by typed handlers (DockedEvent, UndockedEvent, TouchdownEvent, LiftoffEvent) - skip to avoid duplicates
            var statusFields = new[] {
                (1L << 0, "Docked", true),
                (1L << 1, "Landed", true),
                (1L << 2, "Gear", true),
                (1L << 3, "Shields", false),
                (1L << 4, "Supercruise", false),
                (1L << 5, "FlightAssist", false),
                (1L << 6, "Hardpoints", false),
                (1L << 7, "Winging", false),
                (1L << 8, "Lights", false),
                (1L << 9, "CargoScoop", false),
                (1L << 10, "SilentRunning", false),
                (1L << 11, "Scooping", false),
                (1L << 12, "SrvHandbreak", false),
                (1L << 13, "SrvTurrent", false),
                (1L << 14, "SrvNearShip", false),
                (1L << 15, "SrvDriveAssist", false),
                (1L << 16, "MassLocked", false),
                (1L << 17, "FsdCharging", false),
                (1L << 18, "FsdCooldown", false),
                (1L << 19, "LowFuel", false),
                (1L << 20, "Overheating", false),
            };
            foreach (var (mask, name, skipWhenTokenPresent) in statusFields)
            {
                if (name == "Docked" || name == "Landed")
                    continue; // Handled by typed handlers (DockedEvent, UndockedEvent, TouchdownEvent, LiftoffEvent)
                if (name == "Supercruise")
                    continue; // Handled by Journal events SupercruiseEntry/SupercruiseExit (more responsive)
                if (skipWhenTokenPresent && name == "Gear" && hasGearToken)
                    continue; // Gear handled by explicit token when present
                bool curr = (flags & mask) != 0;
                bool prevVal = (prev & mask) != 0;
                if (curr != prevVal)
                {
                    FindEffect($"Status.{name}:{curr}");
                }
            }
        }

        /// <summary>Journal event names we handle elsewhere. Skip FindEffect for these to avoid duplicate vibrations.</summary>
        /// <summary>Status: we parse Flags and emit Status.Gear:True etc. via EmitStatusEventsFromJson. Docked/Undocked/Touchdown/Liftoff: handled by typed handlers. HeatWarning/HeatDamage: overlap with Status.Overheating when overheating.</summary>
        private static readonly HashSet<string> StatusHandledEventNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Docked", "Undocked", "Touchdown", "Liftoff", "Status", "HeatWarning", "HeatDamage", "ShieldState" };

        private void Events_AllEvent(EliteAPI.Events.IEvent e)
        {
            var eventKey = e?.Event ?? e?.GetType().Name ?? "Unknown";
            if (StatusHandledEventNames.Contains(eventKey))
                return; // Already fired by typed handler (e.g. DockedEvent -> Status.Docked:True)
            FindEffect(eventKey);
        }

        private static List<EventConfiguration> GetDefaultXInputEventConfig()
        {
            var list = new List<EventConfiguration>();
            void Add(string evt, int duration)
            {
                list.Add(new EventConfiguration { Event = evt, ForceFile = EventConfiguration.EventToFfeName(evt), Duration = duration });
            }
            // Status events (Status.json flags)
            Add("Status.Docked:True", 2000); Add("Status.Docked:False", 2000);
            Add("Status.Landed:True", 1500); Add("Status.Landed:False", 1500);
            Add("Status.Gear:True", 3000); Add("Status.Gear:False", 3000);
            Add("Status.Shields:True", 250); Add("Status.Shields:False", 250);
            // Status.Supercruise skipped - use SupercruiseEntry/SupercruiseExit (Journal)
            Add("Status.FlightAssist:True", 250); Add("Status.FlightAssist:False", 250);
            Add("Status.Hardpoints:True", 2000); Add("Status.Hardpoints:False", 2000);
            Add("Status.Winging:True", 250); Add("Status.Winging:False", 250);
            Add("Status.Lights:True", 250); Add("Status.Lights:False", 250);
            Add("Status.CargoScoop:True", 2000); Add("Status.CargoScoop:False", 2000);
            Add("Status.SilentRunning:True", 250); Add("Status.SilentRunning:False", 250);
            Add("Status.Scooping:True", 500); Add("Status.Scooping:False", 500);
            Add("Status.SrvHandbreak:True", 250); Add("Status.SrvHandbreak:False", 250);
            Add("Status.SrvTurrent:True", 250); Add("Status.SrvTurrent:False", 250);
            Add("Status.SrvNearShip:True", 250); Add("Status.SrvNearShip:False", 250);
            Add("Status.SrvDriveAssist:True", 250); Add("Status.SrvDriveAssist:False", 250);
            Add("Status.MassLocked:True", 500); Add("Status.MassLocked:False", 500);
            Add("Status.FsdCharging:True", 500); Add("Status.FsdCharging:False", 500);
            Add("Status.FsdCooldown:True", 500); Add("Status.FsdCooldown:False", 500);
            Add("Status.LowFuel:True", 500); Add("Status.LowFuel:False", 500);
            Add("Status.Overheating:True", 250); Add("Status.Overheating:False", 250);
            // Journal events (Docked/Undocked/Touchdown/Liftoff handled via Status.* above)
            Add("SupercruiseEntry", 1000); Add("SupercruiseExit", 1000);
            Add("FSDJump", 1500); Add("StartJump", 500);
            Add("HullDamage", 400); Add("UnderAttack", 400);
            // ShieldState, HeatDamage, HeatWarning: suppressed (use Status.Shields/Status.Overheating)
            Add("Interdicted", 800); Add("Interdiction", 500); Add("EscapeInterdiction", 500);
            Add("Died", 2000); Add("CockpitBreached", 1000);
            Add("LaunchSRV", 2000); Add("DockSRV", 2000);
            Add("LaunchFighter", 1500); Add("DockFighter", 1500);
            Add("FuelScoop", 500);
            Add("ApproachSettlement", 250); Add("LeaveBody", 250); Add("ApproachBody", 250);
            Add("DockingRequested", 250); Add("DockingGranted", 250);
            Add("DockingDenied", 500); Add("DockingCancelled", 250); Add("DockingTimeout", 500);
            return list;
        }

        private static Dictionary<string, XInputRumbleDevice.RumbleEventConfig> BuildForceFileRumbleOverrides(Dictionary<string, ForceFileRumbleEntry> forceFileRumble)
        {
            var result = new Dictionary<string, XInputRumbleDevice.RumbleEventConfig>();
            if (forceFileRumble == null) return result;
            foreach (var kv in forceFileRumble)
            {
                var key = (kv.Key ?? "").Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(key)) continue;
                if (!key.EndsWith(".ffe", StringComparison.OrdinalIgnoreCase))
                    key += ".ffe";
                result[key] = new XInputRumbleDevice.RumbleEventConfig
                {
                    LeftMotor = Math.Max(0, Math.Min(1.0, kv.Value?.Left ?? 0.5)),
                    RightMotor = Math.Max(0, Math.Min(1.0, kv.Value?.Right ?? 0.5)),
                    Duration = 0 // use event duration
                };
            }
            return result;
        }

        private void TryAddXInputDevice(int userIndex, Device deviceConfig, List<EventConfiguration> defaultEvents, HashSet<int> xinputIndicesAdded, Settings settings, bool requireConnected = true)
        {
            try
            {
                var rumbleGain = deviceConfig?.RumbleGain ?? 1.0;
                var customRumble = BuildForceFileRumbleOverrides(settings?.ForceFileRumble);
                var statusEvents = (deviceConfig?.StatusEvents?.Count > 0) ? deviceConfig.StatusEvents : defaultEvents;

                var xinputDevice = new XInputRumbleDevice(userIndex, logger, rumbleGain, customRumble);
                if (requireConnected && !xinputDevice.IsConnected)
                    return;

                var deviceEvents = new DeviceEvents
                {
                    EventSettings = statusEvents.ToDictionary(v => v.Event, v => v),
                    Device = xinputDevice
                };

                Devices.Add(deviceEvents);
                xinputIndicesAdded.Add(userIndex);
                var exclusiveAccess = xinputDevice.GetExclusiveAccessAcquired();
                var exclusiveStr = exclusiveAccess.HasValue
                    ? $" | AcquireExclusiveRawDeviceAccess: {exclusiveAccess.Value}"
                    : "";
                logger.LogInformation("Detected Xbox controller at index {0}: {1} (via {2}){3}", userIndex, xinputDevice.GetName(), xinputDevice.GetBackendName(), exclusiveStr);
                // Startup rumble test - if you feel this, the correct controller is receiving rumble
                xinputDevice.PlayFileEffect("Vibrate.ffe", 400);
            }
            catch (Exception ex)
            {
                logger.LogDebug("XInput index {0} not available: {1}", userIndex, ex.Message);
            }
        }

        private void FindEffect(string eventKey)
        {
            logger.LogDebug($"Event {eventKey}");

            foreach (var device in Devices)
            {
                if (device.EventSettings.ContainsKey(eventKey))
                {
                    var eventConfig = device.EventSettings[eventKey];
                    device.Device?.PlayFileEffect(eventConfig.ForceFile, eventConfig.Duration, eventConfig.LeftMotor, eventConfig.RightMotor, eventConfig.Pulse, eventConfig.PulseAmount);
                }
            }
        }

        /// <summary>Releases all devices (raw HID, SharpDX XInput, DirectInput). Stops rumble and frees resources.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            foreach (var deviceEvents in Devices)
            {
                try { deviceEvents.Device?.Dispose(); } catch { }
            }
            Devices.Clear();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
