using UnityEditor;
using UnityEngine;

public class ExporterSetup
{
    [MenuItem("Tools/QuestHouseDesign/Apply Player Settings")]
    public static void ApplyPlayerSettings()
    {
        // Android settings
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.veksco.questhousedesign");
        // set company and product names
        PlayerSettings.companyName = "VeksCo";
        PlayerSettings.productName = "QuestHouseDesign";
        EditorUtility.DisplayDialog("QuestHouseDesign", "Player settings applied. Please verify in Project Settings.", "OK");
    }
}
