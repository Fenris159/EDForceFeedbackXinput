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
    public class Client
    {
        private EliteAPI.EliteDangerousApi eliteAPI;
        private ILogger logger;

        private readonly List<DeviceEvents> Devices = new List<DeviceEvents>();

        // Cached Status.json values for change detection (Status.* synthetic events don't include values)
        private long _lastStatusFlags = -1;
        private bool? _lastStatusGear;
        private bool? _lastStatusLanded;

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
                if (device.XInput)
                {
                    var userIndex = device.UserIndex >= 0 ? device.UserIndex : -1;
                    if (userIndex >= 0)
                    {
                        TryAddXInputDevice(userIndex, device, defaultXInputEvents, xinputIndicesAdded);
                    }
                    continue;
                }

                var ffDevice = new ForceFeedbackController() { Logger = logger };
                if (ffDevice.Initialize(
                        device.ProductGuid,
                        device.ProductName,
                        @".\Forces",
                        device.AutoCenter,
                        device.ForceFeedbackGain) == false)
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

            for (int i = 0; i <= 3; i++)
            {
                if (!xinputIndicesAdded.Contains(i))
                {
                    TryAddXInputDevice(i, null, defaultXInputEvents, xinputIndicesAdded);
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

            // All events including Status synthetic - Status.X don't include value, so we also handle full Status
            eliteAPI.OnAllJson(arg =>
            {
                var (eventName, json) = arg;
                FindEffect(eventName);
                if (eventName == "Status")
                {
                    EmitStatusEventsFromJson(json);
                }
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

        private void EmitStatusEventsFromJson(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                var flagsToken = obj["Flags"];
                var gearToken = obj["Gear"];
                var landedToken = obj["Landed"];

                if (flagsToken != null && flagsToken.Type == JTokenType.Integer)
                {
                    var flags = flagsToken.Value<long>();
                    if (_lastStatusFlags >= 0)
                    {
                        EmitChangedStatusFlags(flags);
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

                if (landedToken != null)
                {
                    var landed = landedToken.Value<bool>();
                    if (_lastStatusLanded.HasValue && _lastStatusLanded.Value != landed)
                    {
                        FindEffect($"Status.Landed:{landed}");
                    }
                    _lastStatusLanded = landed;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug("Failed to parse Status json: {Ex}", ex.Message);
            }
        }

        private void EmitChangedStatusFlags(long flags)
        {
            long prev = _lastStatusFlags;
            var statusFields = new[] {
                (1L << 0, "Docked"),
                (1L << 1, "Landed"),
                (1L << 2, "Gear"),
                (1L << 3, "Shields"),
                (1L << 4, "Supercruise"),
                (1L << 5, "FlightAssist"),
                (1L << 6, "Hardpoints"),
                (1L << 7, "Winging"),
                (1L << 8, "Lights"),
                (1L << 9, "CargoScoop"),
                (1L << 10, "SilentRunning"),
                (1L << 11, "Scooping"),
                (1L << 12, "SrvHandbreak"),
                (1L << 13, "SrvTurrent"),
                (1L << 14, "SrvNearShip"),
                (1L << 15, "SrvDriveAssist"),
                (1L << 16, "MassLocked"),
                (1L << 17, "FsdCharging"),
                (1L << 18, "FsdCooldown"),
                (1L << 19, "LowFuel"),
                (1L << 20, "Overheating"),
            };
            foreach (var (mask, name) in statusFields)
            {
                bool curr = (flags & mask) != 0;
                bool prevVal = (prev & mask) != 0;
                if (curr != prevVal)
                {
                    FindEffect($"Status.{name}:{curr}");
                }
            }
        }

        private void Events_AllEvent(EliteAPI.Events.IEvent e)
        {
            var eventKey = e?.Event ?? e?.GetType().Name ?? "Unknown";
            FindEffect(eventKey);
        }

        private static List<EventConfiguration> GetDefaultXInputEventConfig()
        {
            return new List<EventConfiguration>
            {
                new EventConfiguration { Event = "Status.Docked:True", ForceFile = "Dock.ffe", Duration = 2000 },
                new EventConfiguration { Event = "Status.Docked:False", ForceFile = "Dock.ffe", Duration = 2000 },
                new EventConfiguration { Event = "Status.Gear:True", ForceFile = "Gear.ffe", Duration = 3000 },
                new EventConfiguration { Event = "Status.Gear:False", ForceFile = "Gear.ffe", Duration = 3000 },
                new EventConfiguration { Event = "Status.Lights:True", ForceFile = "Vibrate.ffe", Duration = 250 },
                new EventConfiguration { Event = "Status.Lights:False", ForceFile = "Vibrate.ffe", Duration = 250 },
                new EventConfiguration { Event = "Status.Hardpoints:True", ForceFile = "Hardpoints.ffe", Duration = 2000 },
                new EventConfiguration { Event = "Status.Hardpoints:False", ForceFile = "Hardpoints.ffe", Duration = 2000 },
                new EventConfiguration { Event = "Status.Landed:True", ForceFile = "Hardpoints.ffe", Duration = 1500 },
                new EventConfiguration { Event = "Status.Landed:False", ForceFile = "Hardpoints.ffe", Duration = 1500 },
                new EventConfiguration { Event = "Status.LowFuel:True", ForceFile = "VibrateSide.ffe", Duration = 500 },
                new EventConfiguration { Event = "Status.LowFuel:False", ForceFile = "VibrateSide.ffe", Duration = 500 },
                new EventConfiguration { Event = "Status.CargoScoop:True", ForceFile = "Cargo.ffe", Duration = 2000 },
                new EventConfiguration { Event = "Status.CargoScoop:False", ForceFile = "Cargo.ffe", Duration = 2000 },
                new EventConfiguration { Event = "Status.Overheating:True", ForceFile = "VibrateSide.ffe", Duration = 250 },
                new EventConfiguration { Event = "Status.Overheating:False", ForceFile = "VibrateSide.ffe", Duration = 250 },
            };
        }

        private void TryAddXInputDevice(int userIndex, Device deviceConfig, List<EventConfiguration> defaultEvents, HashSet<int> xinputIndicesAdded)
        {
            try
            {
                var rumbleGain = deviceConfig?.RumbleGain ?? 1.0;
                var customRumble = new Dictionary<string, XInputRumbleDevice.RumbleEventConfig>();
                var statusEvents = (deviceConfig?.StatusEvents?.Count > 0) ? deviceConfig.StatusEvents : defaultEvents;

                var xinputDevice = new XInputRumbleDevice(userIndex, logger, rumbleGain, customRumble);
                if (!xinputDevice.IsConnected)
                    return;

                var deviceEvents = new DeviceEvents
                {
                    EventSettings = statusEvents.ToDictionary(v => v.Event, v => v),
                    Device = xinputDevice
                };

                Devices.Add(deviceEvents);
                xinputIndicesAdded.Add(userIndex);
                logger.LogInformation("Detected Xbox controller at index {0}: {1}", userIndex, xinputDevice.GetName());
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
                    device.Device?.PlayFileEffect(eventConfig.ForceFile, eventConfig.Duration, eventConfig.LeftMotor, eventConfig.RightMotor);
                }
            }
        }
    }
}
