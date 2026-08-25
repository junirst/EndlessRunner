using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadePanelController : MonoBehaviour
{
    private const string PauseButtonCanvasName = "Match 3 Pause Button Canvas";
    private const string FadePanelPath = "Top UI/Fade Panel";
    private const string GoalPanelPath = FadePanelPath + "/Panel";
    private const string OkayButtonPath = GoalPanelPath + "/OK Button";
    private const float IntroAnimationDuration = 0.6f;
    private static readonly Vector3 EnlargedGoalPanelScale = new Vector3(0.7299449f, 0.47283104f, 0.466698f);

    public Animator panelAnim;
    public Animator gameInforAnim;

    /// <summary>
    /// Dismisses the goal introduction and unlocks the match-three interface.
    /// </summary>
    public void Okay()
    {
        if (panelAnim != null)
        {
            panelAnim.SetBool("Out", true);
        }

        if (gameInforAnim != null)
        {
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
        ResolveAnimationReferences();
        WireOkayButton();
        DisableTransparentRootRaycast();
        ApplyGoalPanelScale();
        SetPauseButtonVisible(false);
    }

    private void WireOkayButton()
    {
        GameObject okayButtonObject = GameObject.Find(OkayButtonPath);
        if (okayButtonObject == null)
        {
            Transform okayButtonTransform = transform.Find("Panel/OK Button");
            if (okayButtonTransform == null)
            {
                okayButtonTransform = transform.Find("Panel/Okay Button");
            }

            if (okayButtonTransform != null)
            {
                okayButtonObject = okayButtonTransform.gameObject;
            }
        }

        if (okayButtonObject == null)
        {
            return;
        }

        UnityEngine.UI.Button okayButton = okayButtonObject.GetComponent<UnityEngine.UI.Button>();
        if (okayButton == null)
        {
            return;
        }

        okayButton.interactable = true;
        okayButton.onClick.RemoveListener(Okay);
        okayButton.onClick.AddListener(Okay);
    }

    private void ResolveAnimationReferences()
    {
        if (panelAnim == null)
        {
            GameObject fadePanel = GameObject.Find(FadePanelPath);
            if (fadePanel != null)
            {
                panelAnim = fadePanel.GetComponent<Animator>();
            }
        }

        if (panelAnim == null)
        {
            panelAnim = GetComponent<Animator>();
        }

        if (gameInforAnim == null)
        {
            GameObject goalPanel = GameObject.Find(GoalPanelPath);
            if (goalPanel != null)
            {
                gameInforAnim = goalPanel.GetComponent<Animator>();
            }
        }

        if (gameInforAnim == null)
        {
            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                gameInforAnim = panel.GetComponent<Animator>();
            }
        }
    }

    private void DisableTransparentRootRaycast()
    {
        GameObject fadePanel = GameObject.Find(FadePanelPath);
        UnityEngine.UI.Image rootImage = fadePanel != null
            ? fadePanel.GetComponent<UnityEngine.UI.Image>()
            : GetComponent<UnityEngine.UI.Image>();
        if (rootImage != null)
        {
            rootImage.raycastTarget = false;
        }
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
        GameObject goalPanel = GameObject.Find(GoalPanelPath);
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
