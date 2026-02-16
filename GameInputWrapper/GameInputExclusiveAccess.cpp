// Uses GameInput v0 interface for AcquireExclusiveRawDeviceAccess / ReleaseExclusiveRawDeviceAccess.
// v3 IGameInputDevice does not expose these; the v0 interface does. We QI for v0 and call through it.

#include "GameInputExclusiveAccess.h"

// Include v0 header - it has the extended IGameInputDevice with AcquireExclusiveRawDeviceAccess.
// Path is relative to the GameInput native include root (packages/.../native/include).
#include "v0/GameInput.h"

namespace GameInputWrapper {

bool AcquireExclusiveRawDeviceAccess(IUnknown* device, uint64_t timeoutInMicroseconds)
{
    if (!device) return false;
    IGameInputDevice* pV0 = nullptr;
    HRESULT hr = device->QueryInterface(__uuidof(IGameInputDevice), reinterpret_cast<void**>(&pV0));
    if (FAILED(hr) || !pV0) return false;
    bool ok = pV0->AcquireExclusiveRawDeviceAccess(timeoutInMicroseconds);
    pV0->Release();
    return ok;
}

void ReleaseExclusiveRawDeviceAccess(IUnknown* device)
{
    if (!device) return;
    IGameInputDevice* pV0 = nullptr;
    HRESULT hr = device->QueryInterface(__uuidof(IGameInputDevice), reinterpret_cast<void**>(&pV0));
    if (FAILED(hr) || !pV0) return;
    pV0->ReleaseExclusiveRawDeviceAccess();
    pV0->Release();
}

}
