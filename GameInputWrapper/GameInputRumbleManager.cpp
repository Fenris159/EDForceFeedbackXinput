#include "GameInputRumbleManager.h"
#include "GameInputRumbleManagerBridge.h"

using namespace GameInputWrapper;

GameInputRumbleManager::GameInputRumbleManager()
    : m_native(GameInputRumbleManager_Create())
{
}

GameInputRumbleManager::~GameInputRumbleManager()
{
    this->!GameInputRumbleManager();
}

GameInputRumbleManager::!GameInputRumbleManager()
{
    if (m_native)
    {
        GameInputRumbleManager_Destroy(m_native);
        m_native = nullptr;
    }
}

bool GameInputRumbleManager::Initialize()
{
    if (!m_native) return false;
    return GameInputRumbleManager_Initialize(m_native);
}

void GameInputRumbleManager::Shutdown()
{
    if (m_native)
        GameInputRumbleManager_Shutdown(m_native);
}

bool GameInputRumbleManager::SetRumble(float lowFrequency, float highFrequency)
{
    if (!m_native) return false;
    return GameInputRumbleManager_SetRumble(m_native, lowFrequency, highFrequency);
}

int GameInputRumbleManager::DeviceCount::get()
{
    if (!m_native) return 0;
    return GameInputRumbleManager_GetDeviceCount(m_native);
}

bool GameInputRumbleManager::ExclusiveAccessAcquired::get()
{
    if (!m_native) return false;
    return GameInputRumbleManager_GetExclusiveAccessAcquired(m_native);
}

bool GameInputRumbleManager::InitializeForEliteController()
{
    if (!m_native) return false;
    return GameInputRumbleManager_InitializeForElite(m_native);
}
