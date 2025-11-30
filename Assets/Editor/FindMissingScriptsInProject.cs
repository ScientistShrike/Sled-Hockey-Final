using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class FindMissingScriptsInProject : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts in Project")]
    public static void ShowWindow()
    {
        GetWindow<FindMissingScriptsInProject>("FindMissingScripts");
    }

    private Vector2 scrollPos;
    private List<string> results = new List<string>();

    private void OnGUI()
    {
        if (GUILayout.Button("Scan Open Scenes and Prefabs for Missing Scripts"))
        {
            results.Clear();
            ScanOpenScenes();
            ScanPrefabs();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Remove Missing Components From Prefabs (Confirm)"))
        {
            if (EditorUtility.DisplayDialog("Confirm Remove", "Are you sure you want to remove missing components from all prefabs? This action cannot be undone.", "Yes", "No"))
            {
                RemoveMissingComponentsFromPrefabs();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                results.Add("Completed prefab cleanup - saved and refreshed.");
            }
        }
        if (GUILayout.Button("Remove Missing Components From Open Scenes (Confirm)"))
        {
            if (EditorUtility.DisplayDialog("Confirm Remove", "Are you sure you want to remove missing components from the open scenes? This action cannot be undone.", "Yes", "No"))
            {
                RemoveMissingComponentsFromScenes();
                EditorSceneManager.MarkAllScenesDirty();
                AssetDatabase.SaveAssets();
                results.Add("Completed scene cleanup - marked scenes dirty and saved assets.");
            }
        }
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var s in results)
        {
            EditorGUILayout.SelectableLabel(s, GUILayout.Height(18));
        }
        EditorGUILayout.EndScrollView();
    }

    private void ScanOpenScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            var rootObjs = scene.GetRootGameObjects();
            foreach (var root in rootObjs)
                ScanGameObject(root, scene.name);
        }
    }

    private void ScanPrefabs()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            ScanGameObject(prefab, path);
        }
    }

    private void RemoveMissingComponentsFromPrefabs()
    {
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
            bool removedAny = false;
            foreach (Transform t in prefabInstance.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                // Remove any missing scripts on this GameObject
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go) > 0)
                {
                    removedAny = true;
                }
            }

            if (removedAny)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                results.Add($"Removed missing components from prefab: {path}");
            }
            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }
    }

    private void RemoveMissingComponentsFromScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var rootObjs = scene.GetRootGameObjects();
            bool sceneModified = false;
            foreach (var root in rootObjs)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go) > 0)
                    {
                        sceneModified = true;
                    }
                }
            }
            if (sceneModified)
            {
                results.Add($"Removed missing components from scene: {scene.name}");
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
        // We don't auto-save or close scenes; user can manually save scenes from File -> Save Scenes
    }

    private void ScanGameObject(GameObject go, string location)
    {
        var comps = go.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
            {
                string s = $"Missing script on GameObject '{go.name}' at '{location}'";
                results.Add(s);
            }
        }

        // Recurse children if any
        for (int i = 0; i < go.transform.childCount; i++)
        {
            ScanGameObject(go.transform.GetChild(i).gameObject, location);
        }
    }
}
