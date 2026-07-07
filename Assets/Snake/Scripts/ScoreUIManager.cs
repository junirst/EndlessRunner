using UnityEngine;
using TMPro;

public class ScoreUIManager : MonoBehaviour
{
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI comboText;

    private void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ScoreUIManager: No Canvas found in scene!");
            return;
        }

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        scoreText = CreateText(canvas.transform, "ScoreText", "Score: 0", font,
            new Vector2(10, -10), TextAlignmentOptions.TopLeft, 24);
        comboText = CreateText(canvas.transform, "ComboText", "", font,
            Vector2.zero, TextAlignmentOptions.Center, 36);
        comboText.gameObject.SetActive(false);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, TMP_FontAsset font,
        Vector2 anchoredPos, TextAlignmentOptions alignment, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return tmp;
    }

    private void Update()
    {
        if (ScoreManager.Instance == null) return;

        scoreText.text = $"Score: {ScoreManager.Instance.Score}";

        if (ScoreManager.Instance.Combo > 1)
        {
            comboText.text = $"x{ScoreManager.Instance.Combo} Combo!";
            if (!comboText.gameObject.activeSelf)
                comboText.gameObject.SetActive(true);
        }
        else if (comboText.gameObject.activeSelf)
        {
            comboText.gameObject.SetActive(false);
        }
    }
}
