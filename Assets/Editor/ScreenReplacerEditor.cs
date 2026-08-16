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

            bool needsWork = name == "Level1" || name == "Level2" || name == "Infinite"
                          || name.StartsWith("MiniGolf-Level") || name == "Game";
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
        bool changed = DestroyAll(ct, "PauseScreen")
                     | DestroyAll(ct, "SettingScreen")
                     | DestroyAll(ct, "GameOverScreen");

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
            WirePauseScreenButtons(pauseInst, pauseManager);
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
            WireSettingScreenButtons(settingInst);
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
            WireGameOverButton(gameOverInst, "StartButton", gameManager, "Retry");
            WireGameOverButton(gameOverInst, "BackToMenuButton", gameManager, "LoadMainMenu");
            Log("  Added GameOver");
            changed = true;
        }

        AddScreenNav(pauseInst, "ArrowIndicator?");
        AddScreenNav(settingInst, "ArrowIndicator?");
        AddScreenNav(gameOverInst, "ArrowIndicator");
        return changed;
    }

    private bool ProcessSnakeInfinite(Canvas canvas)
    {
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null) return false;

        Transform ct = canvas.transform;
        bool changed = DestroyAll(ct, "GameOverScreen");

        var pfb = LoadPrefab("GameOver");
        if (pfb != null)
        {
            var inst = PrefabUtility.InstantiatePrefab(pfb, ct) as GameObject;
            inst.name = "GameOverScreen";
            inst.SetActive(false);
            SetPrivateField(gameManager, "gameOverScreen", inst);
            var scoreText = inst.GetComponentInChildren<TextMeshProUGUI>(true);
            SetPrivateField(gameManager, "finalScoreText", scoreText);
            WireGameOverButton(inst, "StartButton", gameManager, "Retry");
            WireGameOverButton(inst, "BackToMenuButton", gameManager, "LoadMainMenu");
            Log("  Added GameOver");
            changed = true;
            AddScreenNav(inst, "ArrowIndicator");
        }
        return changed;
    }

    private bool ProcessMiniGolf(Canvas canvas, string sceneName)
    {
        var levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null) return false;

        AddPauseSystemIfMissing();

        Transform ct = canvas.transform;
        bool changed = DestroyAll(ct, "PauseMenu")
                     | DestroyAll(ct, "PauseScreen")
                     | DestroyAll(ct, "SettingScreen")
                     | DestroyAll(ct, "GameOverUI");

        var pausePfb = LoadPrefab("PauseScreen");
        var settingPfb = LoadPrefab("SettingScreen");
        var gameOverPfb = LoadPrefab("GameOver");

        GameObject pauseInst = null, settingInst = null, gameOverInst = null;

        var pm = FindObjectOfType<PauseManager>();
        var sm = FindObjectOfType<SettingsManager>();

        if (pausePfb != null)
        {
            pauseInst = PrefabUtility.InstantiatePrefab(pausePfb, ct) as GameObject;
            pauseInst.name = "PauseScreen";
            pauseInst.SetActive(false);
            SetPrivateField(levelManager, "pauseMenuUI", pauseInst);
            if (pm != null)
            {
                SetPrivateField(pm, "pauseMenu", pauseInst);
                WirePauseScreenButtons(pauseInst, pm);
            }
            Log("  Added PauseScreen");
            changed = true;
        }
        if (settingPfb != null)
        {
            settingInst = PrefabUtility.InstantiatePrefab(settingPfb, ct) as GameObject;
            settingInst.name = "SettingScreen";
            settingInst.SetActive(false);
            if (pm != null) SetPrivateField(pm, "settingsUI", settingInst);
            WireSettingsManager(settingInst);
            WireSettingScreenButtons(settingInst);
            Log("  Added SettingScreen");
            changed = true;
        }
        if (gameOverPfb != null)
        {
            gameOverInst = PrefabUtility.InstantiatePrefab(gameOverPfb, ct) as GameObject;
            gameOverInst.name = "GameOverUI";
            gameOverInst.SetActive(false);
            SetPrivateField(levelManager, "GameOverUI", gameOverInst);
            WireGameOverButton(gameOverInst, "StartButton", levelManager, "ReplayButtonHandler");
            WireGameOverButton(gameOverInst, "BackToMenuButton", levelManager, "BackToMenuButtonHandler");
            Log("  Added GameOver");
            changed = true;
        }

        AddScreenNav(pauseInst, "ArrowIndicator?");
        AddScreenNav(settingInst, "ArrowIndicator?");
        AddScreenNav(gameOverInst, "ArrowIndicator");
        return changed;
    }

    private bool ProcessTopDownShooter(Canvas canvas)
    {
        var slm = FindObjectOfType<ShooterLevelManager>();
        if (slm == null) return false;

        AddPauseSystemIfMissing();

        Transform ct = canvas.transform;
        bool changed = DestroyAll(ct, "pauseMenu")
                     | DestroyAll(ct, "deathScreen")
                     | DestroyAll(ct, "SettingScreen");

        var pausePfb = LoadPrefab("PauseScreen");
        var settingPfb = LoadPrefab("SettingScreen");
        var gameOverPfb = LoadPrefab("GameOver");

        GameObject pauseInst = null, settingInst = null, gameOverInst = null;

        var pm = FindObjectOfType<PauseManager>();

        if (pausePfb != null)
        {
            pauseInst = PrefabUtility.InstantiatePrefab(pausePfb, ct) as GameObject;
            pauseInst.name = "pauseMenu";
            pauseInst.SetActive(false);
            slm.pauseMenu = pauseInst;
            if (pm != null)
            {
                SetPrivateField(pm, "pauseMenu", pauseInst);
                WirePauseScreenButtons(pauseInst, pm);
            }
            Log("  Added PauseScreen");
            changed = true;
        }
        if (settingPfb != null)
        {
            settingInst = PrefabUtility.InstantiatePrefab(settingPfb, ct) as GameObject;
            settingInst.name = "SettingScreen";
            settingInst.SetActive(false);
            if (pm != null) SetPrivateField(pm, "settingsUI", settingInst);
            WireSettingsManager(settingInst);
            WireSettingScreenButtons(settingInst);
            Log("  Added SettingScreen");
            changed = true;
        }
        if (gameOverPfb != null)
        {
            gameOverInst = PrefabUtility.InstantiatePrefab(gameOverPfb, ct) as GameObject;
            gameOverInst.name = "deathScreen";
            gameOverInst.SetActive(false);
            slm.deathScreen = gameOverInst;
            WireTDSDeathTexts(slm, gameOverInst);
            WireGameOverButton(gameOverInst, "StartButton", slm, "ReplayGame");
            WireGameOverButton(gameOverInst, "BackToMenuButton", slm, "BackToMenu");
            Log("  Added GameOver");
            changed = true;
        }

        AddScreenNav(pauseInst, "ArrowIndicator?");
        AddScreenNav(settingInst, "ArrowIndicator?");
        AddScreenNav(gameOverInst, "ArrowIndicator");
        return changed;
    }

    // ── Button wiring helpers ──────────────────────────────────────────

    private void WirePauseScreenButtons(GameObject pauseInst, PauseManager pm)
    {
        if (pauseInst == null || pm == null) return;

        var buttons = pauseInst.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string method = null;
            switch (btn.name)
            {
                case "Continue": method = "ContinueButton"; break;
                case "Restart":  method = "RestartGame";    break;
                case "SettingButton": method = "ShowSettings"; break;
                case "MenuButton":    method = "LoadMainMenu"; break;
            }
            if (method != null)
                SetOnClickTarget(btn, pm, method);
        }
        Log($"  Wired PauseScreen buttons -> PauseManager");
    }

    private void WireSettingScreenButtons(GameObject settingInst)
    {
        if (settingInst == null) return;

        var sm = FindObjectOfType<SettingsManager>();
        var pm = FindObjectOfType<PauseManager>();

        var buttons = settingInst.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name == "Save" && sm != null)
                SetOnClickTarget(btn, sm, "Save");
            else if (btn.name == "MenuButton" && sm != null)
                SetOnClickTarget(btn, sm, "Back");
            else if (btn.name == "MenuButton (1)" && pm != null)
                SetOnClickTarget(btn, pm, "LoadMainMenu");
        }
        Log($"  Wired SettingScreen buttons -> SettingsManager / PauseManager");
    }

    private void WireGameOverButton(GameObject gameOverRoot, string buttonName, Object target, string methodName)
    {
        if (gameOverRoot == null || target == null) return;

        var buttons = gameOverRoot.GetComponentsInChildren<Button>(true);
        Button targetButton = null;
        foreach (var b in buttons)
            if (b.name == buttonName) { targetButton = b; break; }

        if (targetButton == null) { Log($"    Button '{buttonName}' not found on GameOver"); return; }

        SetOnClickTarget(targetButton, target, methodName);
        Log($"    Wired GameOver '{buttonName}' -> {target.GetType().Name}.{methodName}");
    }

    private void SetOnClickTarget(Button btn, Object target, string methodName)
    {
        SerializedObject so = new SerializedObject(btn);
        SerializedProperty onClickProp = so.FindProperty("m_OnClick");
        if (onClickProp == null) return;

        SerializedProperty calls = onClickProp.FindPropertyRelative("m_PersistentCalls")
                                            .FindPropertyRelative("m_Calls");

        calls.ClearArray();

        // Pre-fill with the correct method name from the prefab's existing data
        calls.arraySize = 1;
        SerializedProperty call = calls.GetArrayElementAtIndex(0);

        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
            target.GetType().FullName + ", Assembly-CSharp";
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").intValue = 1;

        var args = call.FindPropertyRelative("m_Arguments");
        args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = null;
        args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "UnityEngine.Object, UnityEngine";
        args.FindPropertyRelative("m_IntArgument").intValue = 0;
        args.FindPropertyRelative("m_FloatArgument").floatValue = 0;
        args.FindPropertyRelative("m_StringArgument").stringValue = "";
        args.FindPropertyRelative("m_BoolArgument").boolValue = false;
        call.FindPropertyRelative("m_CallState").intValue = 2;

        so.ApplyModifiedProperties();
    }

    // ── Manager ref wiring ─────────────────────────────────────────────

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

    private void WireTDSDeathTexts(ShooterLevelManager slm, GameObject death)
    {
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

    // ── Screen / GameObject helpers ────────────────────────────────────

    private bool DestroyAll(Transform parent, string name)
    {
        bool found = false;
        var allChildren = parent.GetComponentsInChildren<Transform>(true);
        foreach (var child in allChildren)
        {
            if (child != parent && child.name == name)
            {
                DestroyImmediate(child.gameObject);
                Log($"  Removed old {name}");
                found = true;
            }
        }
        return found;
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

    // ── Reflection helpers ─────────────────────────────────────────────

    private void SetPrivateField(Object obj, string fieldName, Object value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
    }

    private Object GetPrivateField(Object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field != null) return (Object)field.GetValue(obj);
        return null;
    }
}
