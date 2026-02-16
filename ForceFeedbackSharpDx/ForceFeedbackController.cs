using Microsoft.Extensions.Logging;
using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ForceFeedbackSharpDx
{
    public class ForceFeedbackController : IForceFeedbackDevice
    {
        private const uint WINDOW_HANDLE_ERROR = 0x80070006;

        private Joystick joystick = null;
        private DirectInput directInput = null;
        private readonly Dictionary<string, EffectInfo> knownEffects = new Dictionary<string, EffectInfo>();
        private readonly Dictionary<string, List<EffectFile>> fileEffects = new Dictionary<string, List<EffectFile>>();
        public ILogger Logger { get; set; }

        public String StatusText;

        public string GetName() => StatusText ?? "Force Feedback Device";

        public bool Initialize(
            string productGuid,
            string productName,
            string forceFilesFolder,
            bool autoCenter,
            int forceFeedbackGain,
            int deadzone = 0,
            int saturation = 10000)
        {
            // Initialize DirectInput
            if (directInput != null)
            {
                directInput.Dispose();
                directInput = null;
            }

            directInput = new DirectInput();

            var product = new Guid(productGuid);

            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;
            var joystickName = String.Empty;

            var directInputDevices = new List<DeviceInstance>();

            directInputDevices.AddRange(directInput.GetDevices());

            // Filtered
            //directInputDevices.AddRange(directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices));
            //directInputDevices.AddRange(directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices));
            //directInputDevices.AddRange(directInput.GetDevices(DeviceType.Driving, DeviceEnumerationFlags.AllDevices));

            foreach (var deviceInstance in directInputDevices)
            {
                Logger?.LogDebug($"DeviceName: {deviceInstance.ProductName}: ProductGuid {deviceInstance.ProductGuid}");

                if (deviceInstance.ProductGuid == product || deviceInstance.ProductName == productName)
                {
                    joystickGuid = deviceInstance.InstanceGuid;
                    joystickName = deviceInstance.ProductName;
                    break;
                }
            }

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                Logger?.LogDebug("No matching Joystick/Gamepad/Wheel found. {deviceInstance.ProductName} {productGuid}");
                return false;
            }

            // Instantiate the joystick
            joystick = new Joystick(directInput, joystickGuid);

            Logger?.LogDebug("Found Joystick/Gamepad {0}", joystickName);

            // Query all suported ForceFeedback effects
            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
            {
                knownEffects.Add(effectInfo.Name, effectInfo);
                Logger?.LogDebug("Effect available {0}", effectInfo.Name);
            }

            // Load all of the effect files
            var forcesFolder = new DirectoryInfo(forceFilesFolder);

            foreach (var file in forcesFolder.GetFiles("*.ffe"))
            {
                var effectsFromFile = joystick.GetEffectsInFile(file.FullName, EffectFileFlags.ModidyIfNeeded);
                fileEffects.Add(file.Name.Trim().ToLower(), new List<EffectFile>(effectsFromFile));
                Logger?.LogDebug($"File Effect available {file.Name}");
            }

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // DirectX requires a window handle to set the CooperativeLevel
            var handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            try
            {
                // Exclusive is required to control the forces
                joystick.SetCooperativeLevel(handle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            }
            catch (SharpDX.SharpDXException ex) when ((uint)ex.HResult == WINDOW_HANDLE_ERROR)
            {
                Logger?.LogDebug(" Unable to access window handle.  Do not run EDForceFeedback.exe in a console window.");
                StatusText = "Unable to access window handle";
                return false;
            }

            try
            {
                // Autocenter: For the MSFF2 joystick this value is a spring force that plays in the background.
                // If all effects are stopped. The spring will stop too.  Reset will turn it back on.
                joystick.Properties.AutoCenter = autoCenter;
            }
            catch (SharpDXException)
            {
                // Some devices do not support this setting.
            }

            // Acquire the joystick
            joystick.Acquire();

            SetActuators(true);

            joystick.Properties.DeadZone = deadzone;
            joystick.Properties.Saturation = saturation;
            joystick.Properties.ForceFeedbackGain = forceFeedbackGain;

            StatusText = $"{joystickName}: Aquired";

            return true;
        }

        private void PlayEffect(Effect effect)
        {
            // See if our effects are playing.  If not play them
            if (effect.Status != EffectStatus.Playing)
            {
                Logger?.LogDebug($"Effect {effect.Guid} starting");
                effect.Start(1, EffectPlayFlags.NoDownload);
            }
        }

        public void PlayEffects(IEnumerable<Effect> effects)
        {
            foreach (var effect in effects)
                PlayEffect(effect);
        }

        /// <summary>
        /// Stop any effects that were started from the PlayFileEffect() method.
        /// </summary>
        /// <param name="effects"></param>
        public void StopEffects(IEnumerable<Effect> effects, bool dispose = true)
        {
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                try { effect.Stop(); }
                catch (SharpDXException) { }
            }

            if (dispose)
            {
                foreach (var effect in effects)
                    effect?.Dispose();
            }

            //Reset();  // This will kill all effects
        }

        /// <summary>
        /// Stop all effects. Reset to Default. Note: This reenables the center spring if autocenter is set.
        /// </summary>
        public void Reset()
        {
            joystick.SendForceFeedbackCommand(ForceFeedbackCommand.Reset);
        }

        /// <summary>
        /// Stop all effects.  Note: This will disable the autocenter effect.  To reenable call Reset().
        /// </summary>
        public void StopAllEffects()
        {
            joystick.SendForceFeedbackCommand(ForceFeedbackCommand.StopAll);
        }

        /// <summary>
        /// Set the actuators on/off
        /// </summary>
        public void SetActuators(bool on = true)
        {
            try
            {
                if (on)
                    joystick.SendForceFeedbackCommand(ForceFeedbackCommand.SetActuatorsOn);
                else
                    joystick.SendForceFeedbackCommand(ForceFeedbackCommand.SetActuatorsOff);
            }
            catch(Exception ex)
            {
                Logger?.LogError($"Exception {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the effects in the effect file.  Caller must dispose of the object when completed.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<Effect> GetEffectFromFile(string name)
        {
            try
            {
                var fileEffect = fileEffects[name.Trim().ToLower()];

                // Create a new List<> of effects
                return fileEffect.ConvertAll(x => new Effect(joystick, x.Guid, x.Parameters));
            }
            catch (KeyNotFoundException ex)
            {
                Logger?.LogError($"GetEffectFromFile Key: '{name}' Not Found Exception", ex);
            }
            catch (Exception ex)
            {
                Logger?.LogError("GetEffectFromFile Exception", ex);
            }
            return null;
        }

        /// <summary>
        /// Copy the effect file and begin playing it.  If the duration is greater than zero the effect will play for that many milliseconds
        /// and then be stopped.
        /// </summary>
        /// <param name="name">Effect file name</param>
        /// <param name="duration">Zero and below will play until stopped.  Above zero will play for that many milliseconds.  Default: 250.</param>
        /// <summary>Maps event-specific .ffe names to fallback base .ffe when the specific file does not exist (MSFFB2).</summary>
        private static readonly Dictionary<string, string> EventFfeFallbacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "status_docked_true.ffe", "dock.ffe" }, { "status_docked_false.ffe", "dock.ffe" },
            { "status_landed_true.ffe", "hardpoints.ffe" }, { "status_landed_false.ffe", "hardpoints.ffe" },
            { "status_gear_true.ffe", "gear.ffe" }, { "status_gear_false.ffe", "gear.ffe" },
            { "status_shields_true.ffe", "vibrate.ffe" }, { "status_shields_false.ffe", "vibrate.ffe" },
            { "status_supercruise_true.ffe", "supercruise.ffe" }, { "status_supercruise_false.ffe", "supercruise.ffe" },
            { "status_flightassist_true.ffe", "vibrate.ffe" }, { "status_flightassist_false.ffe", "vibrate.ffe" },
            { "status_hardpoints_true.ffe", "hardpoints.ffe" }, { "status_hardpoints_false.ffe", "hardpoints.ffe" },
            { "status_winging_true.ffe", "vibrate.ffe" }, { "status_winging_false.ffe", "vibrate.ffe" },
            { "status_lights_true.ffe", "vibrate.ffe" }, { "status_lights_false.ffe", "vibrate.ffe" },
            { "status_cargoscoop_true.ffe", "cargo.ffe" }, { "status_cargoscoop_false.ffe", "cargo.ffe" },
            { "status_silentrunning_true.ffe", "vibrate.ffe" }, { "status_silentrunning_false.ffe", "vibrate.ffe" },
            { "status_scooping_true.ffe", "vibrate.ffe" }, { "status_scooping_false.ffe", "vibrate.ffe" },
            { "status_srvhandbreak_true.ffe", "vibrate.ffe" }, { "status_srvhandbreak_false.ffe", "vibrate.ffe" },
            { "status_srvturrent_true.ffe", "vibrate.ffe" }, { "status_srvturrent_false.ffe", "vibrate.ffe" },
            { "status_srvnearship_true.ffe", "vibrate.ffe" }, { "status_srvnearship_false.ffe", "vibrate.ffe" },
            { "status_srvdriveassist_true.ffe", "vibrate.ffe" }, { "status_srvdriveassist_false.ffe", "vibrate.ffe" },
            { "status_masslocked_true.ffe", "vibrate.ffe" }, { "status_masslocked_false.ffe", "vibrate.ffe" },
            { "status_fsdcharging_true.ffe", "vibrate.ffe" }, { "status_fsdcharging_false.ffe", "vibrate.ffe" },
            { "status_fsdcooldown_true.ffe", "vibrate.ffe" }, { "status_fsdcooldown_false.ffe", "vibrate.ffe" },
            { "status_lowfuel_true.ffe", "vibrateside.ffe" }, { "status_lowfuel_false.ffe", "vibrateside.ffe" },
            { "status_overheating_true.ffe", "vibrateside.ffe" }, { "status_overheating_false.ffe", "vibrateside.ffe" },
            { "docked.ffe", "dock.ffe" }, { "undocked.ffe", "dock.ffe" },
            { "touchdown.ffe", "landed.ffe" }, { "liftoff.ffe", "landed.ffe" },
            { "supercruiseentry.ffe", "supercruise.ffe" }, { "supercruiseexit.ffe", "supercruise.ffe" },
            { "fsdjump.ffe", "vibrate.ffe" }, { "startjump.ffe", "vibrate.ffe" },
            { "shieldstate.ffe", "vibrate.ffe" }, { "cockpitbreached.ffe", "vibrate.ffe" },
            { "heatdamage.ffe", "vibrateside.ffe" }, { "heatwarning.ffe", "vibrateside.ffe" },
            { "launchfighter.ffe", "vibrate.ffe" }, { "dockfighter.ffe", "vibrate.ffe" },
            { "approachsettlement.ffe", "vibrate.ffe" }, { "leavebody.ffe", "vibrate.ffe" }, { "approachbody.ffe", "vibrate.ffe" },
            { "dockingrequested.ffe", "vibrate.ffe" }, { "dockinggranted.ffe", "vibrate.ffe" },
            { "dockingdenied.ffe", "vibrate.ffe" }, { "dockingcancelled.ffe", "vibrate.ffe" }, { "dockingtimeout.ffe", "vibrate.ffe" },
        };

        public void PlayFileEffect(string name, int duration = 250, double? leftMotorOverride = null, double? rightMotorOverride = null, bool pulse = false, int pulseAmount = 0)
        {
            var key = name.Trim().ToLowerInvariant();
            if (!key.EndsWith(".ffe")) key += ".ffe";

            if (!fileEffects.ContainsKey(key) && EventFfeFallbacks.TryGetValue(key, out var fallback))
            {
                key = fallback;
                Logger?.LogDebug($"Event-specific .ffe not found, using fallback: {fallback}");
            }

            try
            {
                var fileEffect = fileEffects[key];

                // Create a new List<> of effects
                var forceEffects = fileEffect.ConvertAll(x => new Effect(joystick, x.Guid, x.Parameters));

                _ = Task.Run(async () =>
                {
                    PlayEffects(forceEffects);
                    if (duration > 0)
                    {
                        await Task.Delay(duration).ConfigureAwait(false);
                        StopEffects(forceEffects);
                    }
                    else
                    {
                        // Wait a moment, effects don't seem to play if we exit fast
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                }).ContinueWith(t =>
                {
                    if (t.IsCanceled) Logger?.LogDebug($"Effect {name} cancelled");
                    else if (t.IsFaulted) Logger?.LogDebug($"Effect {name} Exception {t.Exception.InnerException?.Message}");
                    else Logger?.LogDebug($"Effect {name} complete");
                });
            }
            catch(KeyNotFoundException ex)
            {
                Logger?.LogError($"PlayFileEffect Key: '{name}' Not Found Exception", ex);
            }
            catch (Exception ex)
            {
                Logger?.LogError("PlayFileEffect Exception", ex);
            }
        }

        public void Dispose()
        {
            joystick?.Unacquire();
            joystick?.Dispose();
            joystick = null;
            directInput?.Dispose();
            directInput = null;
        }
    }
}
