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

    private FindMatches findMatches;
    private Board board;
    
    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;
    private Vector2 tempPosition;

    [Header("Swipe Stuff")]
    public float swipeAngle = 0;
    public float swipeResist = 1f;

    [Header("Powerup Stuff")]
    public bool isColorBomb;
    public bool isColumnBomb;
    public bool isRowBomb;
    public GameObject rowArrow;
    public GameObject columnArrow;
    public GameObject colorBomb;

    void Start()
    {
        isColumnBomb = false;
        isRowBomb = false;
        // Updated to the modern Unity method to find the Board script
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
            isColorBomb = true;
            GameObject color = Instantiate(colorBomb, transform.position, Quaternion.identity);
            color.transform.parent = this.transform;
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
        targetX = column;
        targetY = row;

        // Smoothly slide horizontally to targetX
        if (Mathf.Abs(targetX - transform.position.x) > .05f)
        {
            tempPosition = new Vector2(targetX, transform.position.y);
            transform.position = Vector2.Lerp(transform.position, tempPosition, .100f);
        }
        else
        {
            transform.position = new Vector2(targetX, transform.position.y);
        }

        // Smoothly slide vertically to targetY
        if (Mathf.Abs(targetY - transform.position.y) > .05f)
        {
            tempPosition = new Vector2(transform.position.x, targetY);
            transform.position = Vector2.Lerp(transform.position, tempPosition, .100f);
        }
        else
        {
            transform.position = new Vector2(transform.position.x, targetY);
        }
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
            }
            else
            {
                board.DestroyMatches();
            }
        }
    }

    private void OnMouseDown()
    {
        firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp()
    {
        finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
}