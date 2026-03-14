#pragma once

// BLE4Unity.h — Windows BLE DLL for Unity (Editor + Standalone)
// Wraps the WinRT BLE API into a plain C DLL usable via P/Invoke.

#include <cstdint>
#include <wchar.h>

struct DeviceUpdate {
	wchar_t id[100];
	bool isConnectable = false;
	bool isConnectableUpdated = false;
	wchar_t name[50];
	bool nameUpdated = false;
};

struct Service {
	wchar_t uuid[100];
};

struct Characteristic {
	wchar_t uuid[100];
	wchar_t userDescription[100];
};

struct BLEData {
	uint8_t buf[512];
	uint16_t size;
	wchar_t deviceId[256];
	wchar_t serviceUuid[256];
	wchar_t characteristicUuid[256];
};

struct ErrorMessage {
	wchar_t msg[1024];
};

enum class ScanStatus { PROCESSING, AVAILABLE, FINISHED };

// Callback type for device disconnection events (called from WinRT thread)
typedef void (*DeviceDisconnectedCallback)(const wchar_t* deviceId);

extern "C" {

	__declspec(dllexport) void StartDeviceScan();

	__declspec(dllexport) ScanStatus PollDevice(DeviceUpdate* device, bool block);

	__declspec(dllexport) void StopDeviceScan();

	__declspec(dllexport) void ScanServices(wchar_t* deviceId);

	__declspec(dllexport) ScanStatus PollService(Service* service, bool block);

	__declspec(dllexport) void ScanCharacteristics(wchar_t* deviceId, wchar_t* serviceId);

	__declspec(dllexport) ScanStatus PollCharacteristic(Characteristic* characteristic, bool block);

	__declspec(dllexport) bool SubscribeCharacteristic(wchar_t* deviceId, wchar_t* serviceId, wchar_t* characteristicId, bool block);

	__declspec(dllexport) bool PollData(BLEData* data, bool block);

	__declspec(dllexport) bool SendData(BLEData* data, bool block);

	__declspec(dllexport) void Quit();

	__declspec(dllexport) void GetError(ErrorMessage* buf);

	// Disconnect a single device: unsubscribe all notifications, close session, dispose device
	__declspec(dllexport) void DisconnectDevice(wchar_t* deviceId);

	// Check whether a device is currently connected
	__declspec(dllexport) bool IsConnected(wchar_t* deviceId);

	// One-shot read of a characteristic value (blocking or non-blocking)
	__declspec(dllexport) bool ReadCharacteristic(wchar_t* deviceId, wchar_t* serviceId, wchar_t* characteristicId, BLEData* result, bool block);

	// Register a callback invoked when any device disconnects unexpectedly
	__declspec(dllexport) void RegisterDisconnectedCallback(DeviceDisconnectedCallback callback);
}
