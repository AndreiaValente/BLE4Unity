// BLE4Unity.cs — C# wrapper for BLE4Unity.dll
// Drop this into your Unity project alongside the DLL in Assets/Plugins/x86_64/
//
// Usage:
//   BLE4Unity.StartDeviceScan();
//   BLE4Unity.SubscribeCharacteristic(id, service, char, true);
//   BLE4Unity.DisconnectDevice(id);

using System;
using System.Runtime.InteropServices;

public static class BLE4Unity
{
    private const string DLL = "BLE4Unity";

    // =========================================================================
    // Structs (must match include/BLE4Unity.h layout exactly)
    // =========================================================================

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DeviceUpdate
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string id;
        [MarshalAs(UnmanagedType.I1)]
        public bool isConnectable;
        [MarshalAs(UnmanagedType.I1)]
        public bool isConnectableUpdated;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string name;
        [MarshalAs(UnmanagedType.I1)]
        public bool nameUpdated;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Service
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string uuid;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Characteristic
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string uuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string userDescription;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BLEData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] buf;
        public ushort size;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string deviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string serviceUuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string characteristicUuid;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ErrorMessage
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
        public string msg;
    }

    public enum ScanStatus { PROCESSING, AVAILABLE, FINISHED }

    // =========================================================================
    // Scanning
    // =========================================================================

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void StartDeviceScan();

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern ScanStatus PollDevice(ref DeviceUpdate device,
        [MarshalAs(UnmanagedType.I1)] bool block);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void StopDeviceScan();

    // =========================================================================
    // Service & characteristic discovery
    // =========================================================================

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ScanServices(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern ScanStatus PollService(ref Service service,
        [MarshalAs(UnmanagedType.I1)] bool block);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ScanCharacteristics(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        [MarshalAs(UnmanagedType.LPWStr)] string serviceId);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern ScanStatus PollCharacteristic(ref Characteristic characteristic,
        [MarshalAs(UnmanagedType.I1)] bool block);

    // =========================================================================
    // Subscribe, read, write
    // =========================================================================

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SubscribeCharacteristic(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        [MarshalAs(UnmanagedType.LPWStr)] string serviceId,
        [MarshalAs(UnmanagedType.LPWStr)] string characteristicId,
        [MarshalAs(UnmanagedType.I1)] bool block);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool PollData(ref BLEData data,
        [MarshalAs(UnmanagedType.I1)] bool block);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SendData(ref BLEData data,
        [MarshalAs(UnmanagedType.I1)] bool block);

    /// <summary>
    /// One-shot read of a characteristic value. Populates the BLEData struct.
    /// Useful for reading battery level, device info, sensor settings, etc.
    /// </summary>
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool ReadCharacteristic(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        [MarshalAs(UnmanagedType.LPWStr)] string serviceId,
        [MarshalAs(UnmanagedType.LPWStr)] string characteristicId,
        ref BLEData result,
        [MarshalAs(UnmanagedType.I1)] bool block);

    // =========================================================================
    // Connection management
    // =========================================================================

    /// <summary>
    /// Disconnect a single device cleanly: unsubscribes all notifications,
    /// closes the GattSession, disposes the BluetoothLEDevice, removes from cache.
    /// The device's BLE connection slot is freed immediately.
    /// </summary>
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisconnectDevice(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    /// <summary>
    /// Returns true if the device is cached and its WinRT ConnectionStatus is Connected.
    /// </summary>
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool IsConnected(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    /// <summary>
    /// Register a callback invoked when any cached device disconnects unexpectedly.
    /// WARNING: Fires on a WinRT background thread, NOT the Unity main thread.
    /// Use a ConcurrentQueue or similar to marshal back to Update().
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public delegate void DeviceDisconnectedCallback(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void RegisterDisconnectedCallback(
        DeviceDisconnectedCallback callback);

    // =========================================================================
    // Lifecycle
    // =========================================================================

    /// <summary>
    /// Full teardown: disconnects all devices, clears all queues, stops scanning.
    /// Call only on application exit or when you need to reset everything.
    /// For disconnecting a single device, use DisconnectDevice() instead.
    /// </summary>
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Quit();

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetError(ref ErrorMessage buf);

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Retrieves the last error message from the DLL.
    /// </summary>
    public static string GetLastError()
    {
        var err = new ErrorMessage();
        GetError(ref err);
        return err.msg;
    }

    /// <summary>
    /// Non-blocking PollData that returns a nullable BLEData.
    /// Returns null if no data is available.
    /// </summary>
    public static BLEData? Poll()
    {
        var data = new BLEData();
        bool result = PollData(ref data, false);
        if (result && data.size > 0)
            return data;
        return null;
    }

    /// <summary>
    /// Build a BLEData packet and write it to a characteristic.
    /// </summary>
    public static bool Write(string deviceId, string serviceUuid, string charUuid, byte[] data, bool block)
    {
        if (data == null || data.Length > 512)
            return false;

        var pkg = new BLEData();
        pkg.buf = new byte[512];
        Array.Copy(data, pkg.buf, data.Length);
        pkg.size = (ushort)data.Length;
        pkg.deviceId = deviceId;
        pkg.serviceUuid = serviceUuid;
        pkg.characteristicUuid = charUuid;

        return SendData(ref pkg, block);
    }
}
