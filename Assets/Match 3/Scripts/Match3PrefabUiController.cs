using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Match3PrefabUiController : MonoBehaviour
{
    private const int TotalLevels = 6;
    private const int PrefabCanvasSortingOrder = 151;
    private const string PauseCanvasName = "Match 3 Pause Canvas";
    private const string PauseScreenResourcePath = "Match3UI/PauseScreen";
    private const string SettingScreenResourcePath = "Match3UI/SettingScreen";
    private const string PauseSystemResourcePath = "Match3UI/PauseSystem";
    private const string MenuScenePath = "Assets/Match 3/Scenes/Menu.unity";

    private GameObject pauseScreenInstance;
    private GameObject settingScreenInstance;
    private GameObject pauseSystemInstance;
    private Match3PauseController pauseController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsMatch3GameplayScene(scene.name) || FindObjectOfType<Match3PrefabUiController>() != null)
        {
            return;
        }

        Board sceneBoard = FindObjectOfType<Board>();
        if (sceneBoard != null)
        {
            sceneBoard.gameObject.AddComponent<Match3PrefabUiController>();
        }
    }

    private static bool IsMatch3GameplayScene(string sceneName)
    {
        if (sceneName == "Main")
        {
            return true;
        }

        return sceneName.StartsWith("Level") && int.TryParse(sceneName.Substring("Level".Length), out int levelNumber) && levelNumber >= 1 && levelNumber <= TotalLevels;
    }

    /// <summary>Connects the prefab UI adapter to the active Match 3 pause controller.</summary>
    public void Initialize(Match3PauseController controller)
    {
        pauseController = controller;
        WirePauseScreen();
    }

    private void Start()
    {
        pauseController = FindObjectOfType<Match3PauseController>();
        BuildPrefabUi();
    }

    private void BuildPrefabUi()
    {
        Canvas pauseCanvas = FindPauseCanvas();
        if (pauseCanvas == null)
        {
            return;
        }

        pauseSystemInstance = InstantiatePrefab(PauseSystemResourcePath, null, "Match 3 Pause System");
        RemoveForeignPauseComponents(pauseSystemInstance);

        pauseScreenInstance = InstantiatePrefab(PauseScreenResourcePath, pauseCanvas.transform, "Match 3 Pause Screen");
        settingScreenInstance = InstantiatePrefab(SettingScreenResourcePath, pauseCanvas.transform, "Match 3 Setting Screen");

        ConfigurePrefabCanvas(pauseScreenInstance);
        ConfigurePrefabCanvas(settingScreenInstance);
        WirePauseScreen();
        WireSettingScreen();

        pauseScreenInstance.SetActive(false);
        settingScreenInstance.SetActive(false);
    }

    private Canvas FindPauseCanvas()
    {
        GameObject pauseCanvasObject = GameObject.Find(PauseCanvasName);
        return pauseCanvasObject != null ? pauseCanvasObject.GetComponent<Canvas>() : null;
    }

    private GameObject InstantiatePrefab(string resourcePath, Transform parent, string instanceName)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("Match3 prefab not found at Resources/" + resourcePath);
            return null;
        }

        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = instanceName;
        return instance;
    }

    private void RemoveForeignPauseComponents(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        PauseManager pauseManager = instance.GetComponent<PauseManager>();
        if (pauseManager != null)
        {
            Destroy(pauseManager);
        }

        SettingsManager settingsManager = instance.GetComponent<SettingsManager>();
        if (settingsManager != null)
        {
            Destroy(settingsManager);
        }

        SnakeAudioManager snakeAudioManager = instance.GetComponent<SnakeAudioManager>();
        if (snakeAudioManager != null)
        {
            Destroy(snakeAudioManager);
        }
    }

    private void ConfigurePrefabCanvas(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Canvas canvas = instance.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = PrefabCanvasSortingOrder;
        }

        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null && instance.transform.parent != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }

    private void WirePauseScreen()
    {
        if (pauseScreenInstance == null)
        {
            return;
        }

        WireButton(FindButton(pauseScreenInstance.transform, "Continue"), ResumeGame);
        WireButton(FindButton(pauseScreenInstance.transform, "Restart"), RestartLevel);
        WireButton(FindButton(pauseScreenInstance.transform, "SettingButton"), OpenSettings);
        WireButton(FindButton(pauseScreenInstance.transform, "MenuButton"), OpenMainMenu);
    }

    private void WireSettingScreen()
    {
        if (settingScreenInstance == null)
        {
            return;
        }

        WireButton(FindButton(settingScreenInstance.transform, "Back"), CloseSettings);
        WireButton(FindButton(settingScreenInstance.transform, "Save"), SaveSettings);
        WireButton(FindButton(settingScreenInstance.transform, "MenuButton"), OpenMainMenu);
    }

    private Button FindButton(Transform root, string objectName)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == objectName)
            {
                return button;
            }
        }

        return null;
    }

    private void WireButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void ShowPauseScreen()
    {
        if (pauseScreenInstance == null)
        {
            return;
        }

        if (pauseController == null)
        {
            pauseController = FindObjectOfType<Match3PauseController>();
        }

        GameObject oldPausePanel = FindSceneObject("Match 3 Pause Canvas/Pause Panel");
        if (oldPausePanel != null)
        {
            oldPausePanel.SetActive(false);
        }

        settingScreenInstance.SetActive(false);
        pauseScreenInstance.SetActive(true);
    }

    public void HidePauseScreen()
    {
        if (pauseScreenInstance != null)
        {
            pauseScreenInstance.SetActive(false);
        }

        if (settingScreenInstance != null)
        {
            settingScreenInstance.SetActive(false);
        }
    }

    private void OpenSettings()
    {
        if (pauseScreenInstance != null)
        {
            pauseScreenInstance.SetActive(false);
        }

        if (settingScreenInstance != null)
        {
            settingScreenInstance.SetActive(true);
        }
    }

    private void CloseSettings()
    {
        if (settingScreenInstance != null)
        {
            settingScreenInstance.SetActive(false);
        }

        if (pauseScreenInstance != null)
        {
            pauseScreenInstance.SetActive(true);
        }
    }

    private void SaveSettings()
    {
        PlayerPrefs.Save();
        CloseSettings();
    }

    private void ResumeGame()
    {
        if (pauseController != null)
        {
            pauseController.ResumeGame();
        }
    }

    private void RestartLevel()
    {
        if (pauseController != null)
        {
            pauseController.RestartLevel();
        }
    }

    private void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScenePath);
    }

    private GameObject FindSceneObject(string path)
    {
        string[] parts = path.Split('/');
        GameObject current = GameObject.Find(parts[0]);
        for (int i = 1; i < parts.Length && current != null; i++)
        {
            Transform child = current.transform.Find(parts[i]);
            current = child != null ? child.gameObject : null;
        }

        return current;
    }
}
