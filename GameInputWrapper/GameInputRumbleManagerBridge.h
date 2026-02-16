#pragma once

extern "C" {
    void* GameInputRumbleManager_Create();
    void GameInputRumbleManager_Destroy(void* p);
    bool GameInputRumbleManager_Initialize(void* p);
    void GameInputRumbleManager_Shutdown(void* p);
    bool GameInputRumbleManager_SetRumble(void* p, float low, float high);
    int GameInputRumbleManager_GetDeviceCount(void* p);
    bool GameInputRumbleManager_GetExclusiveAccessAcquired(void* p);
    bool GameInputRumbleManager_InitializeForElite(void* p);
}
