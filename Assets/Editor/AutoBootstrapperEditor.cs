using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector for AutoBootstrapper with visual Test Mode indicator
/// </summary>
[CustomEditor(typeof(AutoBootstrapper))]
public class AutoBootstrapperEditor : Editor
{
    private GUIStyle headerStyle;
    private GUIStyle warningBoxStyle;
    private GUIStyle infoBoxStyle;

    public override void OnInspectorGUI()
    {
        AutoBootstrapper bootstrapper = (AutoBootstrapper)target;

        // Initialize styles
        InitializeStyles();

        // Header
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quest House Design - Auto Bootstrapper", headerStyle);
        EditorGUILayout.Space(10);

        // Test Mode toggle with visual indicator
        EditorGUI.BeginChangeCheck();
        bool testMode = EditorGUILayout.Toggle(new GUIContent(
            "Test Mode (Simple UI)",
            "Enable to test UI without 3D visualizations (faster iteration)"
        ), bootstrapper.testModeSimpleUI);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(bootstrapper, "Toggle Test Mode");
            bootstrapper.testModeSimpleUI = testMode;
            EditorUtility.SetDirty(bootstrapper);
        }

        EditorGUILayout.Space(10);

        // Visual indicator
        if (bootstrapper.testModeSimpleUI)
        {
            EditorGUILayout.BeginVertical(warningBoxStyle);
            EditorGUILayout.LabelField("? TEST MODE ACTIVE", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("• UI only - no visualizations");
            EditorGUILayout.LabelField("• DollHouse: DISABLED");
            EditorGUILayout.LabelField("• InRoom view: DISABLED");
            EditorGUILayout.LabelField("• Faster build & iteration");
            EditorGUILayout.Space(5);
            
            string apkName = PlayerSettings.productName + "_TestMode.apk";
            EditorGUILayout.LabelField($"APK Name: {apkName}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.BeginVertical(infoBoxStyle);
            EditorGUILayout.LabelField("? PRODUCTION MODE", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("• Full functionality");
            EditorGUILayout.LabelField("• DollHouse: ENABLED");
            EditorGUILayout.LabelField("• InRoom view: ENABLED");
            EditorGUILayout.LabelField("• All visualizations active");
            EditorGUILayout.Space(5);
            
            string apkName = PlayerSettings.productName + ".apk";
            EditorGUILayout.LabelField($"APK Name: {apkName}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // Build info section
        if (GUILayout.Button("Open Build Folder"))
        {
            QuestHouseBuildMenu.OpenBuildFolder();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Test Mode affects APK naming:\n" +
            "• Test Mode ON ? ProductName_TestMode.apk\n" +
            "• Test Mode OFF ? ProductName.apk",
            MessageType.Info
        );
    }

    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (warningBoxStyle == null)
        {
            warningBoxStyle = new GUIStyle(EditorStyles.helpBox);
            warningBoxStyle.normal.textColor = new Color(1f, 0.6f, 0f); // Orange
            warningBoxStyle.padding = new RectOffset(10, 10, 10, 10);
        }

        if (infoBoxStyle == null)
        {
            infoBoxStyle = new GUIStyle(EditorStyles.helpBox);
            infoBoxStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f); // Green
            infoBoxStyle.padding = new RectOffset(10, 10, 10, 10);
        }
    }
}
