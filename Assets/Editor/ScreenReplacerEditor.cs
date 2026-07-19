using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScreenReplacerEditor : EditorWindow
{
    private Vector2 scrollPos;
    private string log;

    [MenuItem("Tools/Screen Replacer")]
    public static void ShowWindow()
    {
        GetWindow<ScreenReplacerEditor>("Screen Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace old embedded screens with shared prefab instances", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Replace Screens in All Scenes", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Confirm",
                    "This will modify scene files. Make sure you have a backup. Continue?",
                    "Yes", "Cancel"))
            {
                log = "";
                ReplaceAll();
            }
        }

        GUILayout.Space(10);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Log(string msg)
    {
        log += msg + "\n";
        Debug.Log(msg);
    }

    private void ReplaceAll()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            bool needsWork = false;
            foreach (var kw in new[] { "Level1", "Level2", "Infinite", "MiniGolf-Level", "Game" })
            {
                if (kw == "Game" && name == "Game") { needsWork = true; break; }
                if (name == kw) { needsWork = true; break; }
                if (name.StartsWith("MiniGolf-Level")) { needsWork = true; break; }
            }
            if (!needsWork) continue;

            Log($"Processing: {path}");
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = ProcessScene(name);
            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
                Log($"  Saved: {path}");
            }
            else Log($"  No changes.");
        }

        AssetDatabase.Refresh();
        Log("Done!");
    }

    private bool ProcessScene(string sceneName)
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { Log("  No Canvas found."); return false; }

        if (sceneName == "Level1" || sceneName == "Level2") return ProcessSnakeLevel(canvas);
        if (sceneName == "Infinite") return ProcessSnakeInfinite(canvas);
        if (sceneName.StartsWith("MiniGolf-Level")) return ProcessMiniGolf(canvas, sceneName);
        if (sceneName == "Game") return ProcessTopDownShooter(canvas);
        return false;
    }

    private bool ProcessSnakeLevel(Canvas canvas)
    {
        var pauseManager = FindObjectOfType<PauseManager>();
        var gameManager = FindObjectOfType<GameManager>();
        if (pauseManager == null || gameManager == null) return false;

        Transform ct = canvas.transform;
        bool changed = DestroyOld(ct, "PauseScreen")
                     | DestroyOld(ct, "SettingScreen")
                     | DestroyOld(ct, "GameOverScreen");

        var pausePfb = LoadPrefab("PauseScreen");
        var settingPfb = LoadPrefab("SettingScreen");
        var gameOverPfb = LoadPrefab("GameOver");

        GameObject pauseInst = null, settingInst = null, gameOverInst = null;

        if (pausePfb != null)
        {
            pauseInst = PrefabUtility.InstantiatePrefab(pausePfb, ct) as GameObject;
            pauseInst.name = "PauseScreen";
            pauseInst.SetActive(false);
            SetPrivateField(pauseManager, "pauseMenu", pauseInst);
            Log("  Added PauseScreen");
            changed = true;
        }
        if (settingPfb != null)
        {
            settingInst = PrefabUtility.InstantiatePrefab(settingPfb, ct) as GameObject;
            settingInst.name = "SettingScreen";
            settingInst.SetActive(false);
            SetPrivateField(pauseManager, "settingsUI", settingInst);
            WireSettingsManager(settingInst);
            Log("  Added SettingScreen");
            changed = true;
        }
        if (gameOverPfb != null)
        {
            gameOverInst = PrefabUtility.InstantiatePrefab(gameOverPfb, ct) as GameObject;
            gameOverInst.name = "GameOverScreen";
            gameOverInst.SetActive(false);
            SetPrivateField(gameManager, "gameOverScreen", gameOverInst);
            var scoreText = gameOverInst.GetComponentInChildren<TextMeshProUGUI>(true);
            SetPrivateField(gameManager, "finalScoreText", scoreText);
            Log("  Added GameOver");
            changed = true;
        }

        AddScreenNav(pauseInst, "ArrowIndicator?");
        AddScreenNav(settingInst, "ArrowIndicator?");
        AddScreenNav(gameOverInst, "ArrowIndicator");

        WireGameOverButton(gameOverInst, "StartButton", gameManager, "Retry");
        WireGameOverButton(gameOverInst, "BackToMenuButton", gameManager, "LoadMainMenu");
        return changed;
    }

    private bool ProcessSnakeInfinite(Canvas canvas)
    {
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null) return false;

        Transform ct = canvas.transform;
        bool changed = DestroyOld(ct, "GameOverScreen");

        var pfb = LoadPrefab("GameOver");
        if (pfb != null)
        {
            var inst = PrefabUtility.InstantiatePrefab(pfb, ct) as GameObject;
            inst.name = "GameOverScreen";
            inst.SetActive(false);
            SetPrivateField(gameManager, "gameOverScreen", inst);
            var scoreText = inst.GetComponentInChildren<TextMeshProUGUI>(true);
            SetPrivateField(gameManager, "finalScoreText", scoreText);
            Log("  Added GameOver");
            changed = true;
            AddScreenNav(inst, "ArrowIndicator");
            WireGameOverButton(inst, "StartButton", gameManager, "Retry");
            WireGameOverButton(inst, "BackToMenuButton", gameManager, "LoadMainMenu");
        }
        return changed;
    }

    private bool ProcessMiniGolf(Canvas canvas, string sceneName)
    {
        var levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null) return false;

        AddPauseSystemIfMissing();

        Transform ct = canvas.transform;
        bool changed = DestroyOld(ct, "pauseMenuUI")
                     | DestroyOld(ct, "GameOverUI");

        var pausePfb = LoadPrefab("PauseScreen");
        var settingPfb = LoadPrefab("SettingScreen");
        var gameOverPfb = LoadPrefab("GameOver");

        GameObject pauseInst = null, settingInst = null, gameOverInst = null;

        if (pausePfb != null)
        {
            pauseInst = PrefabUtility.InstantiatePrefab(pausePfb, ct) as GameObject;
            pauseInst.name = "PauseScreen";
            pauseInst.SetActive(false);
            SetPrivateField(levelManager, "pauseMenuUI", pauseInst);
            Log("  Added PauseScreen");
            changed = true;
        }
        if (settingPfb != null)
        {
            settingInst = PrefabUtility.InstantiatePrefab(settingPfb, ct) as GameObject;
            settingInst.name = "SettingScreen";
            settingInst.SetActive(false);
            WireSettingsManager(settingInst);
            Log("  Added SettingScreen");
            changed = true;
        }
        if (gameOverPfb != null)
        {
            gameOverInst = PrefabUtility.InstantiatePrefab(gameOverPfb, ct) as GameObject;
            gameOverInst.name = "GameOverUI";
            gameOverInst.SetActive(false);
            SetPrivateField(levelManager, "GameOverUI", gameOverInst);
            Log("  Added GameOver");
            changed = true;
        }

        AddScreenNav(pauseInst, "ArrowIndicator?");
        AddScreenNav(settingInst, "ArrowIndicator?");
        AddScreenNav(gameOverInst, "ArrowIndicator");

        WireGameOverButton(gameOverInst, "StartButton", levelManager, "ReplayButtonHandler");
        WireGameOverButton(gameOverInst, "BackToMenuButton", levelManager, "BackToMenuButtonHandler");
        WirePauseManagerRefs();
        return changed;
    }

    private bool ProcessTopDownShooter(Canvas canvas)
    {
        var slm = FindObjectOfType<ShooterLevelManager>();
        if (slm == null) return false;

        AddPauseSystemIfMissing();

        Transform ct = canvas.transform;
        bool changed = DestroyOld(ct, "pauseMenu")
                     | DestroyOld(ct, "deathScreen");

        var pausePfb = LoadPrefab("PauseScreen");
        var settingPfb = LoadPrefab("SettingScreen");
        var gameOverPfb = LoadPrefab("GameOver");

        GameObject pauseInst = null, settingInst = null, gameOverInst = null;

        if (pausePfb != null)
        {
            pauseInst = PrefabUtility.InstantiatePrefab(pausePfb, ct) as GameObject;
            pauseInst.name = "pauseMenu";
            pauseInst.SetActive(false);
            slm.pauseMenu = pauseInst;
            Log("  Added PauseScreen");
            changed = true;
        }
        if (settingPfb != null)
        {
            settingInst = PrefabUtility.InstantiatePrefab(settingPfb, ct) as GameObject;
            settingInst.name = "SettingScreen";
            settingInst.SetActive(false);
            WireSettingsManager(settingInst);
            Log("  Added SettingScreen");
            changed = true;
        }
        if (gameOverPfb != null)
        {
            gameOverInst = PrefabUtility.InstantiatePrefab(gameOverPfb, ct) as GameObject;
            gameOverInst.name = "deathScreen";
            gameOverInst.SetActive(false);
            slm.deathScreen = gameOverInst;
            WireTDSDeathTexts(slm);
            Log("  Added GameOver");
            changed = true;
        }

        AddScreenNav(pauseInst, "ArrowIndicator?");
        AddScreenNav(settingInst, "ArrowIndicator?");
        AddScreenNav(gameOverInst, "ArrowIndicator");
        WireGameOverButton(gameOverInst, "StartButton", slm, "ReplayGame");
        WireGameOverButton(gameOverInst, "BackToMenuButton", slm, "BackToMenu");
        WirePauseManagerRefs();
        return changed;
    }

    private void WireSettingsManager(GameObject settingScreen)
    {
        var sm = FindObjectOfType<SettingsManager>();
        if (sm == null) return;

        var dropdowns = settingScreen.GetComponentsInChildren<TMP_Dropdown>(true);
        var sliders = settingScreen.GetComponentsInChildren<Slider>(true);
        foreach (var d in dropdowns)
        {
            if (d.name.Contains("Mode") || d.name.Contains("Fullscreen"))
                SetPrivateField(sm, "screenModeDropdown", d);
            else if (d.name.Contains("Resolution"))
                SetPrivateField(sm, "resolutionDropdown", d);
        }
        foreach (var s in sliders)
        {
            if (s.name.Contains("BackgroundMusic") || s.name.Contains("BGM"))
                SetPrivateField(sm, "bgmSlider", s);
            else if (s.name.Contains("SFX") || s.name.Contains("Sfx"))
                SetPrivateField(sm, "sfxSlider", s);
        }
    }

    private void WirePauseManagerRefs()
    {
        var pm = FindObjectOfType<PauseManager>();
        if (pm == null) return;
        var pause = GameObject.Find("PauseScreen");
        var setting = GameObject.Find("SettingScreen");
        if (pause != null) SetPrivateField(pm, "pauseMenu", pause);
        if (setting != null) SetPrivateField(pm, "settingsUI", setting);
    }

    private void WireTDSDeathTexts(ShooterLevelManager slm)
    {
        var death = GameObject.Find("deathScreen");
        if (death == null) return;
        var allText = death.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in allText)
        {
            if (t.name == "Score") slm.scoreText = t;
            else if (t.name == "HighScore") slm.highscoreText = t;
        }
    }

    private void AddPauseSystemIfMissing()
    {
        if (FindObjectOfType<PauseManager>() != null) return;
        var pfb = LoadPrefab("PauseSystem");
        if (pfb != null)
        {
            PrefabUtility.InstantiatePrefab(pfb);
            Log("  Added PauseSystem");
        }
    }

    private bool DestroyOld(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                DestroyImmediate(child.gameObject);
                Log($"  Removed old {name}");
                return true;
            }
        }
        return false;
    }

    private GameObject LoadPrefab(string name)
    {
        string path = $"Assets/Prefabs/{name}.prefab";
        var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (pfb == null) Log($"  Prefab not found: {path}");
        return pfb;
    }

    private void AddScreenNav(GameObject go, string arrowName)
    {
        if (go == null) return;
        var nav = go.GetComponent<ScreenNav>();
        if (nav == null) nav = go.AddComponent<ScreenNav>();
        var field = typeof(ScreenNav).GetField("arrowChildName",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(nav, arrowName);
        Log($"  Added ScreenNav to {go.name}");
    }

    private void SetPrivateField(Object obj, string fieldName, Object value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
    }

    private void WireGameOverButton(GameObject gameOverRoot, string buttonName, Object target, string methodName)
    {
        if (gameOverRoot == null || target == null) return;

        // Find the button in GameOver prefab children
        var buttons = gameOverRoot.GetComponentsInChildren<Button>(true);
        Button targetButton = null;
        foreach (var b in buttons)
        {
            if (b.name == buttonName) { targetButton = b; break; }
        }
        if (targetButton == null) { Log($"    Button '{buttonName}' not found on GameOver"); return; }

        // Clear existing calls and add new one via SerializedObject
        SerializedObject so = new SerializedObject(targetButton);
        SerializedProperty onClickProp = so.FindProperty("m_OnClick");
        SerializedProperty persistentCalls = onClickProp.FindPropertyRelative("m_PersistentCalls");
        SerializedProperty calls = persistentCalls.FindPropertyRelative("m_Calls");

        calls.ClearArray();
        calls.arraySize = 1;
        SerializedProperty call = calls.GetArrayElementAtIndex(0);

        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().FullName + ", Assembly-CSharp";
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").intValue = 1;
        call.FindPropertyRelative("m_Arguments").FindPropertyRelative("m_ObjectArgument").objectReferenceValue = null;
        call.FindPropertyRelative("m_Arguments").FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Object, UnityEngine";
        call.FindPropertyRelative("m_Arguments").FindPropertyRelative("m_IntArgument").intValue = 0;
        call.FindPropertyRelative("m_Arguments").FindPropertyRelative("m_FloatArgument").floatValue = 0;
        call.FindPropertyRelative("m_Arguments").FindPropertyRelative("m_StringArgument").stringValue = "";
        call.FindPropertyRelative("m_Arguments").FindPropertyRelative("m_BoolArgument").boolValue = false;
        call.FindPropertyRelative("m_CallState").intValue = 2;

        so.ApplyModifiedProperties();
        Log($"    Wired GameOver '{buttonName}' -> {target.GetType().Name}.{methodName}");
    }
}
