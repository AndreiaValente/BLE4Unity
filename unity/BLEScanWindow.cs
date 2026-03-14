using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BLEScanWindow : EditorWindow
{
    private struct FoundDevice
    {
        public string id;
        public string name;
        public bool connectable;
    }

    private List<FoundDevice> _devices = new List<FoundDevice>();
    private HashSet<string> _seenIds = new HashSet<string>();
    private bool _scanning = false;
    private Vector2 _scroll;

    [MenuItem("Tools/BLE Scan Test")]
    public static void ShowWindow()
    {
        GetWindow<BLEScanWindow>("BLE Scan Test");
    }

    private void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }

    private void EditorUpdate()
    {
        if (!_scanning)
            return;

        bool changed = false;
        var device = new BLE4Unity.DeviceUpdate();

        while (true)
        {
            var status = BLE4Unity.PollDevice(ref device, false);

            if (status == BLE4Unity.ScanStatus.AVAILABLE)
            {
                if (!string.IsNullOrEmpty(device.id) && !_seenIds.Contains(device.id) && device.nameUpdated && !string.IsNullOrEmpty(device.name))
                {
                    _seenIds.Add(device.id);
                    _devices.Add(new FoundDevice
                    {
                        id = device.id,
                        name = device.nameUpdated ? device.name : "",
                        connectable = device.isConnectableUpdated && device.isConnectable
                    });
                    changed = true;
                }
            }
            else if (status == BLE4Unity.ScanStatus.FINISHED)
            {
                _scanning = false;
                changed = true;
                break;
            }
            else
            {
                break;
            }
        }

        if (changed)
            Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (!_scanning)
            {
                if (GUILayout.Button("Start Scan", GUILayout.Height(30)))
                    StartScan();
            }
            else
            {
                if (GUILayout.Button("Stop Scan", GUILayout.Height(30)))
                    StopScan();
            }

            if (_devices.Count > 0)
            {
                if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    _devices.Clear();
                    _seenIds.Clear();
                }
            }
        }

        if (_scanning)
        {
            EditorGUILayout.HelpBox("Scanning for BLE devices...", MessageType.Info);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Devices found: {_devices.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        for (int i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string displayName = string.IsNullOrEmpty(device.name) ? "(unnamed)" : device.name;
                EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("ID", device.id, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Connectable", device.connectable ? "Yes" : "No");
            }
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void StartScan()
    {
        _devices.Clear();
        _seenIds.Clear();
        BLE4Unity.StartDeviceScan();
        _scanning = true;
    }

    private void StopScan()
    {
        BLE4Unity.StopDeviceScan();
        _scanning = false;
    }

    private void OnDestroy()
    {
        if (_scanning)
            BLE4Unity.StopDeviceScan();

        BLE4Unity.Quit();
    }
}