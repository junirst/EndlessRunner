using UnityEngine;
using UnityEngine.UI;

public class LevelStarRating : MonoBehaviour
{
    private const int MinStars = 1;
    private const int MaxStars = 3;

    [Header("Star Thresholds")]
    [SerializeField] private int threeStarMaxStrokes = 1;
    [SerializeField] private int twoStarMaxStrokes = 2;

    [Header("Star Images")]
    [SerializeField] private Transform starParent;
    [SerializeField] private GameObject firstStar;
    [SerializeField] private GameObject secondStar;
    [SerializeField] private GameObject thirdStar;

    private void OnValidate()
    {
        if (threeStarMaxStrokes < 1)
        {
            threeStarMaxStrokes = 1;
        }

        if (twoStarMaxStrokes < threeStarMaxStrokes)
        {
            twoStarMaxStrokes = threeStarMaxStrokes;
        }
    }

    private void Awake()
    {
        AttachStarsToParent();
    }

    public int GetStarRating(int strokes)
    {
        if (strokes <= threeStarMaxStrokes)
        {
            return 3;
        }

        if (strokes <= twoStarMaxStrokes)
        {
            return 2;
        }

        return MinStars;
    }

    public string GetCompletionHintText(int strokes)
    {
        int starRating = GetStarRating(strokes);
        if (starRating >= MaxStars)
        {
            return string.Empty;
        }

        if (starRating >= 2)
        {
            return BuildThresholdLine(3, threeStarMaxStrokes);
        }

        return BuildThresholdLine(2, twoStarMaxStrokes);
    }

    public void SetStarDisplay(int stars)
    {
        AttachStarsToParent();

        if (firstStar != null)
        {
            firstStar.SetActive(stars >= 1);
        }

        if (secondStar != null)
        {
            secondStar.SetActive(stars >= 2);
        }

        if (thirdStar != null)
        {
            thirdStar.SetActive(stars >= 3);
        }
    }

    public void SetStarDisplayFromStrokes(int strokes)
    {
        SetStarDisplay(GetStarRating(strokes));
    }

    private void AttachStarsToParent()
    {
        if (starParent == null)
        {
            return;
        }

        DisableRaycastBlocking(starParent.gameObject);

        AttachStar(firstStar);
        AttachStar(secondStar);
        AttachStar(thirdStar);
    }

    private void AttachStar(GameObject star)
    {
        if (star == null)
        {
            return;
        }

        if (star.transform.parent == starParent)
        {
            return;
        }

        star.transform.SetParent(starParent, false);
        DisableRaycastBlocking(star);
    }

    private void DisableRaycastBlocking(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    private string BuildThresholdLine(int stars, int maxStrokes)
    {
        string strokeWord = maxStrokes == 1 ? "stroke" : "strokes";

        return maxStrokes + " " + strokeWord + " or fewer to get " + stars + " stars";
    }
}