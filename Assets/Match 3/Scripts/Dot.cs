using System.Collections;
using UnityEngine;

public class Dot : MonoBehaviour
{
    [Header("Board Variables")]
    public int column;
    public int row;
    public int previousColumn;
    public int previousRow;
    public int targetX;
    public int targetY;
    public bool isMatched = false;
    public GameObject otherDot;

    private EndGameManager endGameManager;
    private HintManager hintManager;
    private FindMatches findMatches;
    private Board board;
    
    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;
    private const float MovementSpeed = 8f;

    [Header("Swipe Stuff")]
    public float swipeAngle = 0;
    public float swipeResist = 1f;

    [Header("Powerup Stuff")]
    public bool isColorBomb;
    public bool isColumnBomb;
    public bool isRowBomb;
    public bool isAdjacentBomb;
    public GameObject adjacentMarker;
    public GameObject rowArrow;
    public GameObject columnArrow;
    public GameObject colorBomb;

    AudioManagerM3 audioManager;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip match3Sfx;
    [SerializeField] private AudioClip match4Sfx;
    [SerializeField] private AudioClip match5Sfx;
    [SerializeField] private AudioClip gameOverSfx;

    private void Awake()
    {
        // Prefer FindObjectOfType to avoid relying on a specific tag existing in the scene
        audioManager = FindObjectOfType<AudioManagerM3>();
        if (audioManager == null)
        {
            Debug.LogWarning("Dot.Awake: AudioManagerM3 not found in scene. SFX will not play for this Dot.");
        }
    }
    void Start()
    {
        isColumnBomb = false;
        isRowBomb = false;
        isAdjacentBomb = false;
        isColorBomb = false;
        // Updated to the modern Unity method to find the Board script
        endGameManager = FindObjectOfType<EndGameManager>();
        hintManager = FindObjectOfType<HintManager>();
        board = Object.FindFirstObjectByType<Board>();
        findMatches = FindObjectOfType<FindMatches>();
        //targetX = (int)transform.position.x;
        //targetY = (int)transform.position.y;
        //row = targetY;
        //column = targetX;
        //previousColumn = column;
        //previousRow = row;
    }

    //This is for testing and debug only
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isAdjacentBomb = true;
            GameObject marker = Instantiate(adjacentMarker, transform.position, Quaternion.identity);
            marker.transform.parent = this.transform;
        }
    }

    // Play appropriate SFX for this dot when it's destroyed as part of a match
    public void PlayDestroySfx(int matchCount)
    {
        if (audioManager == null)
            return;

        AudioClip clipToPlay = match3Sfx;
        if (matchCount >= 5)
        {
            clipToPlay = match5Sfx;
        }
        else if (matchCount == 4)
        {
            clipToPlay = match4Sfx;
        }

        if (clipToPlay != null)
        {
            audioManager.PlaySFX(clipToPlay);
        }
    }

    void Update()
    {
        /*
        if (isMatched)
        {
            SpriteRenderer mySprite = GetComponent<SpriteRenderer>();
            if (mySprite != null)
            {
                mySprite.color = new Color(1f, 1f, 1f, .2f);
            }
        }
        */
        Vector3 targetPosition = new Vector3(column, row, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);
    }

    public IEnumerator CheckMoveCo()
    {
        if (isColorBomb)
        {
            // This piece is a color bomb, and the other piece is the color to destroy
            findMatches.MatchPiecesOfColor(otherDot.tag);
            isMatched = true;

            // Add this color bomb to the list so it gets destroyed too
            if (!findMatches.currentMatches.Contains(this.gameObject))
            {
                findMatches.currentMatches.Add(this.gameObject);
            }
        }
        else if (otherDot.GetComponent<Dot>().isColorBomb)
        {
            // The other piece is a color bomb, and this piece is the color to destroy
            findMatches.MatchPiecesOfColor(this.gameObject.tag);
            otherDot.GetComponent<Dot>().isMatched = true;

            // Add the other color bomb to the list so it gets destroyed too
            if (!findMatches.currentMatches.Contains(otherDot))
            {
                findMatches.currentMatches.Add(otherDot);
            }
        }

        yield return new WaitForSeconds(.5f);

        if (otherDot != null)
        {
            //Only run the regular match scanner if NO color bomb was used
            if (!isColorBomb && !otherDot.GetComponent<Dot>().isColorBomb)
            {
                findMatches.FindAllMatches();
            }

            if (!isMatched && !otherDot.GetComponent<Dot>().isMatched)
            {
                otherDot.GetComponent<Dot>().row = row;
                otherDot.GetComponent<Dot>().column = column;
                row = previousRow;
                column = previousColumn;

                board.allDots[column, row] = this.gameObject;
                board.allDots[otherDot.GetComponent<Dot>().column, otherDot.GetComponent<Dot>().row] = otherDot;

                yield return new WaitForSeconds(.5f);
                board.currentDot = null;
                board.currentState = GameState.move;
                if (endGameManager == null)
                {
                    endGameManager = FindObjectOfType<EndGameManager>();
                }

                if (endGameManager != null)
                {
                    if (endGameManager.requirements != null && endGameManager.requirements.gameType == GameType.Moves)
                    {
                        endGameManager.DecreaseCounterValue();
                    }
                }

                board.RegisterPlayerMove();
            }
            else
            {
                if (endGameManager == null)
                {
                    endGameManager = FindObjectOfType<EndGameManager>();
                }

                if (endGameManager != null)
                {
                    if (endGameManager.requirements != null && endGameManager.requirements.gameType == GameType.Moves)
                    {
                        endGameManager.DecreaseCounterValue();
                    }
                }
                board.DestroyMatches();
                board.RegisterPlayerMove();
            }
        }
    }

    private void OnMouseDown()
    {
        //detroy hint
        if (hintManager != null)
        {
            hintManager.DestroyHint();
        }
        if (board.currentState == GameState.move)
        {
            firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Time.timeScale == 0f) return;
    }

    private void OnMouseUp()
    {
        if (board.currentState == GameState.move)
        {
            finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Time.timeScale == 0f) return;
        CalculateAngle();
    }

    void CalculateAngle()
    {
        if(Mathf.Abs(finalTouchPosition.y -firstTouchPosition.y) > swipeResist || Mathf.Abs(finalTouchPosition.x - firstTouchPosition.x) > swipeResist)
        {
            swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
            MovePieces();
            board.currentState = GameState.wait;
            board.currentDot = this;
        }
        else
        {
            board.currentState = GameState.move;
        }
    }

    void MovePiecesActual(Vector2 direction)
    {
        otherDot = board.allDots[column + (int)direction.x, row + (int)direction.y];
        previousColumn = column;
        previousRow = row;
        if (otherDot != null)
        {
            otherDot.GetComponent<Dot>().column -= (int)direction.x;
            otherDot.GetComponent<Dot>().row -= (int)direction.y;
            column += (int)direction.x;
            row += (int)direction.y;
            board.allDots[column, row] = this.gameObject;
            board.allDots[otherDot.GetComponent<Dot>().column, otherDot.GetComponent<Dot>().row] = otherDot;
            StartCoroutine(CheckMoveCo());
        }
        else
        {
            board.currentState = GameState.move;
        }
    }

    void MovePieces()
    {

        // Swipe Right
        if (swipeAngle > -45 && swipeAngle <= 45 && column < board.width - 1)
        {
            otherDot = board.allDots[column + 1, row];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().column -= 1;
            column += 1;
        }
        // Swipe Up
        else if (swipeAngle > 45 && swipeAngle <= 135 && row < board.height - 1)
        {
            otherDot = board.allDots[column, row + 1];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().row -= 1;
            row += 1;
        }
        // Swipe Left
        else if ((swipeAngle > 135 || swipeAngle <= -135) && column > 0)
        {
            otherDot = board.allDots[column - 1, row];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().column += 1;
            column -= 1;
        }
        // Swipe Down
        else if (swipeAngle < -45 && swipeAngle >= -135 && row > 0)
        {
            otherDot = board.allDots[column, row - 1];
            previousColumn = column;
            previousRow = row;
            otherDot.GetComponent<Dot>().row += 1;
            row -= 1;
        }

        if (otherDot != null)
        {
            board.allDots[column, row] = this.gameObject;
            board.allDots[otherDot.GetComponent<Dot>().column, otherDot.GetComponent<Dot>().row] = otherDot;
        }
    StartCoroutine(CheckMoveCo());
    }

    public void FindMatches()
    {
        // 1. Strict Horizontal Check (Must be a true line of 3)
        if (column > 0 && column < board.width - 1)
        {
            GameObject leftDot = board.allDots[column - 1, row];
            GameObject rightDot = board.allDots[column + 1, row];
            if (leftDot != null && rightDot != null)
            {
                if (leftDot.tag == this.gameObject.tag && rightDot.tag == this.gameObject.tag)
                {
                    leftDot.GetComponent<Dot>().isMatched = true;
                    rightDot.GetComponent<Dot>().isMatched = true;
                    this.isMatched = true;
                }
            }
        }

        // 2. Strict Vertical Check (Must be a true line of 3)
        if (row > 0 && row < board.height - 1)
        {
            GameObject upDot = board.allDots[column, row + 1];
            GameObject downDot = board.allDots[column, row - 1];
            if (upDot != null && downDot != null)
            {
                if (upDot.tag == this.gameObject.tag && downDot.tag == this.gameObject.tag)
                {
                    upDot.GetComponent<Dot>().isMatched = true;
                    downDot.GetComponent<Dot>().isMatched = true;
                    this.isMatched = true;
                }
            }
        }
    }

    public void MakeRowBomb()
    {
        isRowBomb = true;
        GameObject arrow = Instantiate(rowArrow, transform.position, Quaternion.identity);
        arrow.transform.parent = this.transform;
    }

    public void MakeColumnBomb()
    {
        isColumnBomb = true;
        GameObject arrow = Instantiate(columnArrow, transform.position, Quaternion.identity);
        arrow.transform.parent = this.transform;
    }

    public void MakeAdjacentBomb()
    {
        isAdjacentBomb = true;
        GameObject marker = Instantiate(adjacentMarker, transform.position, Quaternion.identity);
        marker.transform.parent = this.transform;
    }

    public void MakeColorBomb()
    {
        isColorBomb = true;
        GameObject bomb = Instantiate(colorBomb, transform.position, Quaternion.identity);
        bomb.transform.parent = this.transform;
    }
}