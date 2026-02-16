#include "GameInputRumbleManagerBridge.h"
#include "GameInputRumbleManagerNative.h"
#include "GameInputExclusiveAccess.h"

using namespace GameInput::v3;

namespace {

    struct EnumerateContext {
        std::vector<IGameInputDevice*>* devices;
        bool preferElite;
    };
}

void CALLBACK GameInputWrapper::GameInputRumbleManagerNative::DeviceCallback(
    GameInputCallbackToken callbackToken,
    void* context,
    IGameInputDevice* device,
    uint64_t timestamp,
    GameInputDeviceStatus currentStatus,
    GameInputDeviceStatus previousStatus)
{
    (void)callbackToken;
    (void)timestamp;
    (void)previousStatus;

    if (!context || !device || !(currentStatus & GameInputDeviceConnected))
        return;

    auto* ctx = static_cast<EnumerateContext*>(context);
    const GameInputDeviceInfo* pInfo = nullptr;
    if (SUCCEEDED(device->GetDeviceInfo(&pInfo)) && pInfo)
    {
        const GameInputDeviceInfo& info = *pInfo;
        if (ctx->preferElite)
        {
            const uint16_t elitePids[] = { 0x0B00, 0x0B05, 0x0B22, 0x0B13 };
            bool isElite = (info.vendorId == 0x045E);
            if (isElite)
            {
                for (uint16_t pid : elitePids)
                {
                    if (info.productId == pid)
                    {
                        device->AddRef();
                        ctx->devices->push_back(device);
                        return;
                    }
                }
            }
            return;
        }
        else
        {
            device->AddRef();
            ctx->devices->push_back(device);
        }
    }
}

GameInputWrapper::GameInputRumbleManagerNative::GameInputRumbleManagerNative()
    : m_pGameInput(nullptr)
    , m_callbackToken(0)
    , m_exclusiveAccessAcquired(false)
{
}

GameInputWrapper::GameInputRumbleManagerNative::~GameInputRumbleManagerNative()
{
    Shutdown();
}

void GameInputWrapper::GameInputRumbleManagerNative::Shutdown()
{
    for (auto* p : m_devices)
    {
        if (p)
        {
            GameInputWrapper::ReleaseExclusiveRawDeviceAccess(p);
            p->Release();
        }
    }
    m_devices.clear();

    if (m_callbackToken != 0 && m_pGameInput)
    {
        m_pGameInput->UnregisterCallback(m_callbackToken);
        m_callbackToken = 0;
    }

    if (m_pGameInput)
    {
        m_pGameInput->Release();
        m_pGameInput = nullptr;
    }
}

bool GameInputWrapper::GameInputRumbleManagerNative::Initialize()
{
    if (FAILED(GameInputCreate(&m_pGameInput)))
        return false;

    m_devices.clear();
    EnumerateContext ctx = { &m_devices, false };

    HRESULT hr = m_pGameInput->RegisterDeviceCallback(
        nullptr,
        GameInputKindGamepad,
        GameInputDeviceConnected,
        GameInputBlockingEnumeration,
        &ctx,
        &DeviceCallback,
        &m_callbackToken);

    if (FAILED(hr) || m_callbackToken == 0)
    {
        if (m_pGameInput)
        {
            m_pGameInput->Release();
            m_pGameInput = nullptr;
        }
        return false;
    }

    m_pGameInput->UnregisterCallback(m_callbackToken);
    m_callbackToken = 0;

    // Acquire exclusive raw device access so rumble overrides Elite Dangerous (DirectInput)
    const uint64_t timeoutMicroseconds = 1000000;  // 1 second
    m_exclusiveAccessAcquired = false;
    for (auto* p : m_devices)
    {
        if (p && GameInputWrapper::AcquireExclusiveRawDeviceAccess(p, timeoutMicroseconds))
            m_exclusiveAccessAcquired = true;
    }

    return !m_devices.empty();
}

bool GameInputWrapper::GameInputRumbleManagerNative::InitializeForEliteController()
{
    if (!m_pGameInput)
        return false;

    m_devices.clear();
    EnumerateContext ctx = { &m_devices, true };

    m_pGameInput->RegisterDeviceCallback(
        nullptr,
        GameInputKindGamepad,
        GameInputDeviceConnected,
        GameInputBlockingEnumeration,
        &ctx,
        &DeviceCallback,
        &m_callbackToken);

    if (m_callbackToken != 0)
    {
        m_pGameInput->UnregisterCallback(m_callbackToken);
        m_callbackToken = 0;
    }

    if (!m_devices.empty())
    {
        const uint64_t timeoutMicroseconds = 1000000;
        m_exclusiveAccessAcquired = false;
        for (auto* p : m_devices)
        {
            if (p && GameInputWrapper::AcquireExclusiveRawDeviceAccess(p, timeoutMicroseconds))
                m_exclusiveAccessAcquired = true;
        }
        return true;
    }

    ctx.preferElite = false;
    m_pGameInput->RegisterDeviceCallback(
        nullptr,
        GameInputKindGamepad,
        GameInputDeviceConnected,
        GameInputBlockingEnumeration,
        &ctx,
        &DeviceCallback,
        &m_callbackToken);

    if (m_callbackToken != 0)
    {
        m_pGameInput->UnregisterCallback(m_callbackToken);
        m_callbackToken = 0;
    }

    if (!m_devices.empty())
    {
        const uint64_t timeoutMicroseconds = 1000000;
        m_exclusiveAccessAcquired = false;
        for (auto* p : m_devices)
        {
            if (p && GameInputWrapper::AcquireExclusiveRawDeviceAccess(p, timeoutMicroseconds))
                m_exclusiveAccessAcquired = true;
        }
    }

    return !m_devices.empty();
}

int GameInputWrapper::GameInputRumbleManagerNative::GetDeviceCount() const
{
    return static_cast<int>(m_devices.size());
}

bool GameInputWrapper::GameInputRumbleManagerNative::GetExclusiveAccessAcquired() const
{
    return m_exclusiveAccessAcquired;
}

bool GameInputWrapper::GameInputRumbleManagerNative::SetRumble(float lowFrequency, float highFrequency)
{
    if (m_devices.empty())
        return false;

    GameInputRumbleParams params = {};
    params.lowFrequency = (std::max)(0.0f, (std::min)(1.0f, lowFrequency));
    params.highFrequency = (std::max)(0.0f, (std::min)(1.0f, highFrequency));
    params.leftTrigger = 0.0f;
    params.rightTrigger = 0.0f;

    for (auto* p : m_devices)
    {
        if (p) p->SetRumbleState(&params);
    }
    return true;
}

// Bridge implementation
extern "C" {

void* GameInputRumbleManager_Create()
{
    return new GameInputWrapper::GameInputRumbleManagerNative();
}

void GameInputRumbleManager_Destroy(void* p)
{
    delete static_cast<GameInputWrapper::GameInputRumbleManagerNative*>(p);
}

bool GameInputRumbleManager_Initialize(void* p)
{
    return static_cast<GameInputWrapper::GameInputRumbleManagerNative*>(p)->Initialize();
}

void GameInputRumbleManager_Shutdown(void* p)
{
    static_cast<GameInputWrapper::GameInputRumbleManagerNative*>(p)->Shutdown();
}

bool GameInputRumbleManager_SetRumble(void* p, float low, float high)
{
    return static_cast<GameInputWrapper::GameInputRumbleManagerNative*>(p)->SetRumble(low, high);
}

int GameInputRumbleManager_GetDeviceCount(void* p)
{
    return static_cast<GameInputWrapper::GameInputRumbleManagerNative*>(p)->GetDeviceCount();
}

bool GameInputRumbleManager_GetExclusiveAccessAcquired(void* p)
{
    return static_cast<GameInputWrapper::GameInputRumbleManagerNative*>(p)->GetExclusiveAccessAcquired();
}

bool GameInputRumbleManager_InitializeForElite(void* p)
{
    return static_cast<GameInputWrapper::GameInputRumbleManagerNative*>(p)->InitializeForEliteController();
}

}
