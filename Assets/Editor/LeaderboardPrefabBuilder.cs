using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LeaderboardPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Leaderboard.prefab";

    [MenuItem("Tools/Leaderboard/Create Leaderboard Prefab")]
    public static void CreatePrefab()
    {
        GameObject prefab = GetOrCreatePrefab();
        if (prefab == null) return;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"LeaderboardPrefabBuilder: prefab ready at {PrefabPath}");
    }

    /// <summary>
    /// Adds a persistent LeaderboardUI instance under the current scene's
    /// Canvas so it shows in the hierarchy during edit mode (no Play mode).
    /// It is left inactive so it never appears over the game unless enabled.
    /// </summary>
    [MenuItem("Tools/UI/Add Leaderboard UI to Current Scene")]
    public static void AddToScene()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("LeaderboardPrefabBuilder: no Canvas in the current scene. Nothing to do.");
            return;
        }

        GameObject prefab = GetOrCreatePrefab();
        if (prefab == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        instance.name = "LeaderboardUI";
        instance.SetActive(false);
        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Add Leaderboard UI");

        LeaderboardManager manager = Object.FindObjectOfType<LeaderboardManager>();
        if (manager != null)
            manager.SetLeaderboardPrefab(prefab);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"LeaderboardPrefabBuilder: Leaderboard UI added under {canvas.name} (inactive, editable).");
    }

    private static GameObject GetOrCreatePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null) return prefab;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("LeaderboardPrefabBuilder: no Canvas found in the current scene; cannot build prefab.");
            return null;
        }

        GameObject built = LeaderboardUI.CreateProceduralPanel(canvas);
        if (built == null)
        {
            Debug.LogError("LeaderboardPrefabBuilder: failed to build the leaderboard panel.");
            return null;
        }

        built.transform.SetParent(null, false);
        built.SetActive(true);

        string dir = Path.GetDirectoryName(PrefabPath);
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(dir));

        prefab = PrefabUtility.SaveAsPrefabAsset(built, PrefabPath);
        Object.DestroyImmediate(built);
        return prefab;
    }
}