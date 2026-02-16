#pragma once

#include <stdint.h>
#include <unknwn.h>

namespace GameInputWrapper {

/// <summary>
/// Acquires exclusive raw device access for the given device (IUnknown* / IGameInputDevice*).
/// Gives exclusive output priority so rumble commands override other apps (e.g. Elite Dangerous via DirectInput).
/// Reference-counted: call ReleaseExclusiveRawDeviceAccess once per successful Acquire.
/// </summary>
/// <param name="device">IGameInputDevice as IUnknown* (v3 or v0)</param>
/// <param name="timeoutInMicroseconds">Timeout in microseconds (e.g. 1000000 = 1 second)</param>
/// <returns>true if exclusive access was acquired</returns>
bool AcquireExclusiveRawDeviceAccess(IUnknown* device, uint64_t timeoutInMicroseconds);

/// <summary>
/// Releases exclusive raw device access. Call once per successful AcquireExclusiveRawDeviceAccess.
/// </summary>
void ReleaseExclusiveRawDeviceAccess(IUnknown* device);

}
