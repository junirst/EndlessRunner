using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private RectTransform snakeContainer;
    [SerializeField] private Image segmentPrefab;
    [SerializeField] private int segmentCount = 12;
    [SerializeField] private float segmentSize = 16f;
    [SerializeField] private float amplitude = 20f;
    [SerializeField] private float frequency = 2f;
    [SerializeField] private float speed = 100f;
    [SerializeField] private Color snakeColor = new Color(0.8679245f, 0.5199359f, 0.5663873f);
    [SerializeField] private float minDisplayTime = 1.5f;

    private readonly List<RectTransform> segments = new List<RectTransform>();
    private float offset;

    public void Configure(CanvasGroup cg, RectTransform snakeRT)
    {
        overlay = cg;
        snakeContainer = snakeRT;
        Awake();
    }

    private void Awake()
    {
        if (overlay != null)
        {
            overlay.alpha = 0f;
            overlay.gameObject.SetActive(true);
            overlay.blocksRaycasts = false;
        }
    }

    private void GenerateSnake()
    {
        if (snakeContainer == null) return;

        for (int i = 0; i < segmentCount; i++)
        {
            Image seg;
            if (segmentPrefab != null)
            {
                seg = Instantiate(segmentPrefab, snakeContainer);
            }
            else
            {
                GameObject go = new GameObject($"SnakeSeg_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(snakeContainer, false);
                seg = go.GetComponent<Image>();
            }
            seg.color = snakeColor;
            seg.raycastTarget = false;
            RectTransform rt = seg.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(segmentSize, segmentSize);
            segments.Add(rt);
        }
    }

    public void ShowAndLoad(string sceneName)
    {
        GenerateSnake();
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        float fadeDuration = 0.3f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (overlay != null)
                overlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        if (overlay != null)
        {
            overlay.alpha = 1f;
            overlay.blocksRaycasts = true;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float loadStart = Time.unscaledTime;
        while (!op.isDone)
        {
            UpdateSnakeAnimation(Time.unscaledDeltaTime);

            if (op.progress >= 0.9f && Time.unscaledTime - loadStart >= minDisplayTime)
            {
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void UpdateSnakeAnimation(float dt)
    {
        if (segments.Count == 0) return;

        offset += dt * speed;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null) continue;

            float t = (float)i / segments.Count;
            float x = -i * (segmentSize + 2f) + offset;
            float y = Mathf.Sin(t * Mathf.PI * 2f * frequency + offset * 0.05f) * amplitude;

            segments[i].anchoredPosition = new Vector2(x % (segmentCount * (segmentSize + 2f)), y);

            float alpha = 1f - t * 0.5f;
            Color c = snakeColor;
            c.a = Mathf.Clamp01(alpha);
            segments[i].GetComponent<Image>().color = c;
        }
    }
}
