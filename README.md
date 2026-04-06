# BLE4Unity

Plug-and-play Bluetooth Low Energy for Unity on Windows. Drop three files into your project and start scanning, connecting, and streaming data from any BLE device. Works out of the box with zero setup.

Runs in the Unity Editor (no play mode required) and Windows Standalone builds. Supports multiple simultaneous GATT service subscriptions, per-device disconnect, characteristic reads and writes, and connection status monitoring.

## Quick start

### Option A: Import the Unity package

1. Download `BLE4Unity.unitypackage` from the `unity/` folder (or from Releases)
2. In Unity, go to **Assets → Import Package → Custom Package...** and select it
3. Import all files — they'll be placed in `Assets/BLE4Unity/`
4. Go to **Tools → BLE Scan Test** to verify your Bluetooth adapter is working

### Option B: Copy files manually

1. Create a folder `Assets/BLE4Unity/` in your Unity project
2. Copy these three files from the `unity/` folder into it:
   - `BLE4Unity.dll`
   - `BLE4Unity.cs`
   - `BLEScanWindow.cs`
3. Click on `BLE4Unity.dll` in Unity's Project panel → in the Inspector, under Platform settings, make sure only **x86_64** is checked → click **Apply**
4. Go to **Tools → BLE Scan Test** to verify everything is working

### Testing the setup

**Tools → BLE Scan Test** opens an Editor window that scans for nearby BLE devices and lists them by name. If devices appear, the DLL is loaded and your Bluetooth adapter is working. This window is just a diagnostic tool — you don't need it in your actual project.

## Using BLE4Unity in your scripts

All BLE operations go through the static `BLE4Unity` class. There are no MonoBehaviours to attach or instances to create. Call the functions directly from any script.

### Scan for devices

```csharp
BLE4Unity.StartDeviceScan();

// In Update() or EditorApplication.update, poll for results:
var device = new BLE4Unity.DeviceUpdate();
var status = BLE4Unity.PollDevice(ref device, false);

if (status == BLE4Unity.ScanStatus.AVAILABLE)
    Debug.Log($"Found: {device.name} ({device.id})");

// When done:
BLE4Unity.StopDeviceScan();
```

### Subscribe to notifications

```csharp
// Subscribe to a characteristic — pass the device ID, service UUID, and characteristic UUID
bool ok = BLE4Unity.SubscribeCharacteristic(deviceId, serviceUuid, charUuid, block: true);

// You can subscribe to characteristics on different services — they all work simultaneously
BLE4Unity.SubscribeCharacteristic(deviceId, serviceA, charA, true);
BLE4Unity.SubscribeCharacteristic(deviceId, serviceB, charB, true);
```

### Receive data

```csharp
// Poll in Update() — all subscribed characteristics feed into the same queue
var data = new BLE4Unity.BLEData();
while (BLE4Unity.PollData(ref data, false))
{
    // Use data.characteristicUuid to tell which characteristic sent this
    byte[] payload = new byte[data.size];
    Array.Copy(data.buf, payload, data.size);
}
```

### Write a command

```csharp
var cmd = new BLE4Unity.BLEData();
cmd.buf = new byte[512];
cmd.deviceId = deviceId;
cmd.serviceUuid = serviceUuid;
cmd.characteristicUuid = charUuid;

byte[] command = new byte[] { 0x02, 0x00 };
Array.Copy(command, cmd.buf, command.Length);
cmd.size = (ushort)command.Length;

BLE4Unity.SendData(ref cmd, block: true);
```

### Read a characteristic

```csharp
var result = new BLE4Unity.BLEData();
result.buf = new byte[512];
if (BLE4Unity.ReadCharacteristic(deviceId, serviceUuid, charUuid, ref result, block: true))
{
    // result.buf[0..result.size-1] contains the value
}
```

### Disconnect

```csharp
// Disconnect one device — frees the BLE slot immediately
BLE4Unity.DisconnectDevice(deviceId);

// Check connection status
bool connected = BLE4Unity.IsConnected(deviceId);

// Full teardown (all devices) — call on application exit
BLE4Unity.Quit();
```

### Disconnect callback

```csharp
// Keep a static reference to prevent GC of the delegate
private static BLE4Unity.DeviceDisconnectedCallback _onDisconnect;

void OnEnable()
{
    _onDisconnect = OnDeviceDisconnected;
    BLE4Unity.RegisterDisconnectedCallback(_onDisconnect);
}

void OnDeviceDisconnected(string deviceId)
{
    // This fires on a background thread — do NOT call Unity API here.
    // Use a ConcurrentQueue to marshal to the main thread.
    Debug.Log($"Device lost: {deviceId}");
}
```

### Error handling

```csharp
string error = BLE4Unity.GetLastError();
if (error != "Ok")
    Debug.LogWarning($"BLE error: {error}");
```

### Threading

All DLL functions can be called from any thread. For the polling pattern (`PollDevice`, `PollData`, etc.), call with `block: false` from `Update()` or `EditorApplication.update`, or call with `block: true` from a background thread.

The `DeviceDisconnectedCallback` fires on a WinRT background thread. Do not call Unity API from it directly.

### GATT service caching

When you call `SubscribeCharacteristic`, `SendData`, or `ReadCharacteristic`, the DLL internally looks up the service and characteristic on the device. For previously paired or well-known devices (like a Polar H10), this lookup works immediately because Windows keeps their GATT tables cached.

For custom or unpaired BLE peripherals (like an ESP32), the lookup can fail with "No service found" because Windows hasn't discovered the device's services yet. To fix this, call `ScanServices` and wait for it to finish **before** subscribing. This forces a full GATT discovery that populates the DLL's internal cache:

```csharp
// On a background thread:

// 1. Discover services (populates the cache)
BLE4Unity.ScanServices(deviceId);
var service = new BLE4Unity.Service();
while (BLE4Unity.PollService(ref service, true) != BLE4Unity.ScanStatus.FINISHED) { }

// 2. Optionally discover characteristics
BLE4Unity.ScanCharacteristics(deviceId, serviceUuid);
var chr = new BLE4Unity.Characteristic();
while (BLE4Unity.PollCharacteristic(ref chr, true) != BLE4Unity.ScanStatus.FINISHED) { }

// 3. Now subscribe — the service/characteristic are cached and will be found
bool ok = BLE4Unity.SubscribeCharacteristic(deviceId, serviceUuid, charUuid, true);
```

All three steps should run on the **same background thread** to avoid race conditions with the DLL's internal WinRT coroutines.

## API reference

| Function | Description |
|----------|-------------|
| `StartDeviceScan()` | Begin scanning for BLE devices |
| `PollDevice(ref device, block)` | Get next discovered device |
| `StopDeviceScan()` | Stop scanning |
| `ScanServices(deviceId)` | Discover services on a device |
| `PollService(ref service, block)` | Get next discovered service |
| `ScanCharacteristics(deviceId, serviceId)` | Discover characteristics on a service |
| `PollCharacteristic(ref char, block)` | Get next discovered characteristic |
| `SubscribeCharacteristic(deviceId, serviceId, charId, block)` | Subscribe to notifications |
| `PollData(ref data, block)` | Get next notification payload |
| `SendData(ref data, block)` | Write a value to a characteristic |
| `ReadCharacteristic(deviceId, serviceId, charId, ref data, block)` | One-shot read |
| `DisconnectDevice(deviceId)` | Disconnect one device cleanly |
| `IsConnected(deviceId)` | Check if a device is connected |
| `RegisterDisconnectedCallback(callback)` | Get notified on unexpected disconnects |
| `Quit()` | Disconnect all devices and reset |
| `GetError(ref msg)` | Get last error message |

## Compiling the DLL yourself

The `unity/` folder includes a prebuilt `BLE4Unity.dll`. If you need to modify the source or rebuild it yourself, follow these steps.

### Requirements

- Visual Studio 2022
- Workloads: **Desktop development with C++** and **Universal Windows Platform development**
- A Windows 10 or 11 SDK (any recent version)

### Steps

1. Open Visual Studio → **Create a new project** → search "DLL" → pick **Dynamic-Link Library (DLL)** (C++, Windows, Library) → name it `BLE4Unity` → Create

2. Delete all auto-generated files from the project: `dllmain.cpp`, `framework.h`, `pch.h`, `pch.cpp` (right-click each in Solution Explorer → Remove → Delete)

3. Copy the `dll-source/include/` and `dll-source/src/` folders into your project folder (next to the `.vcxproj` file)

4. Add the source files to the project: right-click project → **Add → Existing Item** → select `src/BLE4Unity.cpp`, then again for `include/BLE4Unity.h`, `include/stdafx.h`, `include/targetver.h`

5. Install the NuGet package: right-click project → **Manage NuGet Packages** → Browse → search `Microsoft.Windows.CppWinRT` → Install

6. Open project Properties (right-click project → **Properties**). Set the dropdowns to **Release | x64**, then configure:

   - **C/C++ → General → Additional Include Directories**: add `$(ProjectDir)include`
   - **C/C++ → Precompiled Headers → Precompiled Header**: set to `Not Using Precompiled Headers`
   - **C/C++ → Language → C++ Language Standard**: set to `ISO C++20 Standard (/std:c++20)`
   - **General → Configuration Type**: verify it says `Dynamic Library (.dll)`

7. If the precompiled header setting doesn't stick (you get errors about `pch.h`): right-click project → **Unload Project** → right-click → **Edit .vcxproj** → find any `<PrecompiledHeader>Use</PrecompiledHeader>` entries and change them to `<PrecompiledHeader>NotUsing</PrecompiledHeader>` → also replace any `pch.h` references with `stdafx.h` → Save → right-click → **Reload Project**

8. Build: set toolbar to **Release | x64** → press **Ctrl+B**

9. The output is at `x64/Release/BLE4Unity.dll`. Copy it to `Assets/BLE4Unity/` in your Unity project.

## License

WTFPL — do whatever you want.