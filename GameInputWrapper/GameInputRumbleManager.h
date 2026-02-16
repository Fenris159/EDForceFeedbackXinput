#pragma once

using namespace System;

namespace GameInputWrapper {

    /// <summary>
    /// Manages rumble for Xbox gamepads via Microsoft.GameInput.
    /// Targets ALL detected gamepad devices when SetRumble is called.
    /// </summary>
    public ref class GameInputRumbleManager
    {
    public:
        GameInputRumbleManager();
        ~GameInputRumbleManager();
        !GameInputRumbleManager();

        bool Initialize();
        void Shutdown();
        bool SetRumble(float lowFrequency, float highFrequency);
        property int DeviceCount { int get(); }
        property bool ExclusiveAccessAcquired { bool get(); }
        bool InitializeForEliteController();

    private:
        void* m_native;  // GameInputRumbleManagerNative*
    };
}
