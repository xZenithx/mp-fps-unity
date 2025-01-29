using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(MapManager))]
public class MapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapManager mapManager = (MapManager)target;

        // Button to auto-populate scenes from Scenes/Maps/
        if (GUILayout.Button("Populate Maps from Scenes/Maps"))
        {
            PopulateMaps(mapManager);
        }
    }

    private void PopulateMaps(MapManager mapManager)
    {
        // Path to your Scenes/Maps folder
        string mapsFolderPath = "Assets/Scenes/Maps/";
        
        // Get all .unity files in the folder
        List<string> sceneNames = new List<string>();
        DirectoryInfo dir = new DirectoryInfo(mapsFolderPath);
        FileInfo[] files = dir.GetFiles("*.unity");

        foreach (FileInfo file in files)
        {
            string sceneName = Path.GetFileNameWithoutExtension(file.Name);
            sceneNames.Add(sceneName);
        }

        // Assign to Maps array
        mapManager.Maps = sceneNames.ToArray();
        EditorUtility.SetDirty(mapManager); // Save changes

        // Automatically add scenes to Build Settings
        AddScenesToBuildSettings(mapsFolderPath);
    }

    private void AddScenesToBuildSettings(string folderPath)
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"Added {buildScenes.Count} scenes to Build Settings.");
    }
}