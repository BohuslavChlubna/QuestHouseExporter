Quest Exporter

This Unity project contains runtime exporter for Oculus Quest spatial scans.

How to use
1. In Unity, open Tools ? QuestExporter ? Run Full Setup. This creates a scene `Assets/Scenes/ExporterScene.unity` and applies recommended PlayerSettings.
2. Build for Android (Quest): ensure XR Plugin Management configured (OpenXR or Oculus).
3. Install APK to headset (USB + ADB). Launch app.
4. Use on-screen UI (Export button) to export rooms. Files saved to `Application.persistentDataPath/QuestHouseExport`.
5. Pull files via ADB: `adb pull /sdcard/Android/data/com.yourcompany.questexport/files/QuestHouseExport ./QuestExport`.

Notes
- GLB exporter is basic and includes positions, normals, UVs and a simple material. For advanced export replace the shim with Siccity GLTFUtility.
- HTTP server is optional and disabled by default.
