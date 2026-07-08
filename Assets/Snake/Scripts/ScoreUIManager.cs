using UnityEngine;
using TMPro;

public class ScoreUIManager : MonoBehaviour
{
    public static ScoreUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetVisible(bool visible)
    {
        if (scoreText != null)
            scoreText.gameObject.SetActive(visible);
    }

    private void Update()
    {
        if (ScoreManager.Instance == null || scoreText == null) return;

        scoreText.text = $"Score: {ScoreManager.Instance.Score}";
    }
}
