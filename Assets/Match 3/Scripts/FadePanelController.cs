using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadePanelController : MonoBehaviour
{
    private const string PauseButtonCanvasName = "Match 3 Pause Button Canvas";
    private const float IntroAnimationDuration = 0.6f;
    private static readonly Vector3 EnlargedGoalPanelScale = new Vector3(0.7299449f, 0.47283104f, 0.466698f);

    public Animator panelAnim;
    public Animator gameInforAnim;

    public void Okay()
    {
        if (panelAnim != null && gameInforAnim != null)
        {
            panelAnim.SetBool("Out", true);
            gameInforAnim.SetBool("Out", true);
        }

        Match3ResultPanel resultPanel = FindObjectOfType<Match3ResultPanel>();
        if (resultPanel != null)
        {
            resultPanel.SetIntroDismissed();
        }

        StartCoroutine(ShowPauseButtonAfterIntro());
    }

    private void Start()
    {
        ApplyGoalPanelScale();
        SetPauseButtonVisible(false);
    }

    private IEnumerator ShowPauseButtonAfterIntro()
    {
        yield return new WaitForSecondsRealtime(IntroAnimationDuration);
        SetPauseButtonVisible(true);
    }

    private void SetPauseButtonVisible(bool visible)
    {
        Match3PauseController pauseController = FindObjectOfType<Match3PauseController>();
        if (pauseController != null)
        {
            pauseController.SetPauseButtonVisible(visible);
            return;
        }

        GameObject pauseButtonCanvas = GameObject.Find(PauseButtonCanvasName);
        if (pauseButtonCanvas != null)
        {
            pauseButtonCanvas.SetActive(visible);
        }
    }

    private void ApplyGoalPanelScale()
    {
        GameObject goalPanel = GameObject.Find("Top UI/Fade Panel/Panel");
        if (goalPanel == null)
        {
            return;
        }

        RectTransform panelRect = goalPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.localScale = EnlargedGoalPanelScale;
        }
    }
}
