using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraScalar : MonoBehaviour
{
    private const string BackgroundCanvasName = "Background Canvas";
    private const float BackgroundCanvasPlaneDistance = 5f;

    private Board board;
    public float cameraOffset;
    public float aspectRatio = 0.625f;
    public float padding = 2;
    public float xOffset = -2.0f;
    public float yOffset = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureLoadedMatch3Scene()
    {
        GameObject backgroundCanvasObject = GameObject.Find(BackgroundCanvasName);
        GameObject cameraObject = GameObject.Find("Main Camera");
        Camera sceneCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : Camera.main;
        if (backgroundCanvasObject == null || sceneCamera == null)
        {
            return;
        }

        Canvas backgroundCanvas = backgroundCanvasObject.GetComponent<Canvas>();
        if (backgroundCanvas == null)
        {
            return;
        }

        backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        backgroundCanvas.worldCamera = sceneCamera;
        backgroundCanvas.planeDistance = BackgroundCanvasPlaneDistance;
        backgroundCanvas.overrideSorting = true;
        backgroundCanvas.sortingOrder = -3;

        Graphic[] backgroundGraphics = backgroundCanvasObject.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic backgroundGraphic in backgroundGraphics)
        {
            backgroundGraphic.raycastTarget = false;
        }
    }

    // Use this for initialization
    void Start()
    {
        ConfigureBackgroundCanvas();
        board = FindObjectOfType<Board>();
        if (board != null)
        {
            RepositionCamera(board.width - 1, board.height - 1);
        }
    }

    private void ConfigureBackgroundCanvas()
    {
        GameObject backgroundCanvasObject = GameObject.Find(BackgroundCanvasName);
        if (backgroundCanvasObject == null)
        {
            return;
        }

        Canvas backgroundCanvas = backgroundCanvasObject.GetComponent<Canvas>();
        Camera sceneCamera = GetComponent<Camera>();
        if (backgroundCanvas == null || sceneCamera == null)
        {
            return;
        }

        backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        backgroundCanvas.worldCamera = sceneCamera;
        backgroundCanvas.planeDistance = BackgroundCanvasPlaneDistance;
        backgroundCanvas.overrideSorting = true;
        backgroundCanvas.sortingOrder = -3;
    }

    void RepositionCamera(float x, float y)
    {
        Vector3 tempPosition = new Vector3(x / 2 + xOffset, y / 2 + yOffset, cameraOffset);
        transform.position = tempPosition;
        if (board.width >= board.height)
        {
            Camera.main.orthographicSize = (board.width / 2 + padding) / aspectRatio;
        }
        else
        {
            Camera.main.orthographicSize = board.height / 2 + padding;
        }
    }
}
