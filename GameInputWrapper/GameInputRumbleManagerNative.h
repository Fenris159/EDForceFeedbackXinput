#pragma once

#include <vector>
#include <GameInput.h>

using namespace GameInput::v3;

namespace GameInputWrapper {

    class GameInputRumbleManagerNative
    {
    public:
        GameInputRumbleManagerNative();
        ~GameInputRumbleManagerNative();

        bool Initialize();
        void Shutdown();
        bool SetRumble(float lowFrequency, float highFrequency);
        int GetDeviceCount() const;
        bool GetExclusiveAccessAcquired() const;
        bool InitializeForEliteController();

    private:
        IGameInput* m_pGameInput;
        std::vector<IGameInputDevice*> m_devices;
        GameInputCallbackToken m_callbackToken;
        bool m_exclusiveAccessAcquired;

        static void CALLBACK DeviceCallback(
            GameInputCallbackToken callbackToken,
            void* context,
            IGameInputDevice* device,
            uint64_t timestamp,
            GameInputDeviceStatus currentStatus,
            GameInputDeviceStatus previousStatus);
    };
}
