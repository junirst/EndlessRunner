using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager main;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI strokeUI;
    [Space(10)]
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private TextMeshProUGUI levelCompleteStrokeUI;
    [Space(10)]
    [SerializeField] private GameObject GameOverUI;

    [Header("Attributes")]
    [SerializeField] private int maxStrokes;

    private int strokes;
    [HideInInspector] public bool outOfStrokes;
    [HideInInspector] public bool levelCompleted;

    private void Awake()
    {
        main = this;
    }

    private void Start()
    {
        updateStrokeUI();
    }

    public void IncreaseStroke()
    {
        strokes++;
        updateStrokeUI();

        if (strokes >= maxStrokes)
        {
            outOfStrokes = true;
        }
    }

    public void LevelComplete()
    {
        levelCompleted = true;
        
        levelCompleteStrokeUI.text = strokes > 1 ? "You putted in " + strokes + " strokes" : "You got a hole in one!";

        levelCompleteUI.SetActive(true);
    }

    public void GameOver()
    {
        GameOverUI.SetActive(true);
    }

    private void updateStrokeUI()
    {
        strokeUI.text = strokes + "/" + maxStrokes;
    }
}
