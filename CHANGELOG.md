# Changelog

## v1.0.0

Initial release.

- Device scanning with name and connectable status
- Service and characteristic discovery
- Characteristic subscription (notify) across multiple GATT services simultaneously
- Characteristic write
- Characteristic read (one-shot)
- Per-device disconnect with CCCD unsubscribe, GattSession close, and device disposal
- Connection status query
- Disconnect callback for unexpected drops
- Persistent connections via GattSession with MaintainConnection
- Thread-safe device cache
- Blocking and non-blocking modes for all operations
- C# P/Invoke wrapper for Unity
- BLE Scan Test EditorWindow for setup verification
- Prebuilt x64 DLL and Unity package
