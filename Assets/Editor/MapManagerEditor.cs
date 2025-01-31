using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        // Get existing build scenes
        List<EditorBuildSettingsScene> buildScenes = 
            new(EditorBuildSettings.scenes);

        // Track paths to avoid duplicates
        HashSet<string> existingPaths = new(
            buildScenes.Select(s => s.path)
        );

        // Find new scenes to add
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
        int addedCount = 0;

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            
            if (!existingPaths.Contains(scenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                addedCount++;
                existingPaths.Add(scenePath);
            }
        }

        // Update build settings
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"Added {addedCount} new scenes. Total in build: {buildScenes.Count}");
    }
}