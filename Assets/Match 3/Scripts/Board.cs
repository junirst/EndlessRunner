using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum GameState
{
    wait,
    move
}

public enum TileKind
{
    Normal,
    Breakable,
    Blank
}

[System.Serializable]
public class tileType
{
    public int x;
    public int y;
    public TileKind tileKind;
    public int breakableValue;
}

public class Board : MonoBehaviour
{
    public GameState currentState = GameState.move;
    public int width;
    public int height;
    public int offSet;
    public GameObject tilePrefab;
    public GameObject[] dots;
    public GameObject[,] allDots;
    public Dot currentDot;
    public GameObject destroyEffect;
    public tileType[] boardLayout;
    private bool[,] blankSpaces;
    private BackgroundTile[,] allTiles;
    private FindMatches findMatches;

    // Start is called before the first frame update
    void Start()
    {
        findMatches = FindObjectOfType<FindMatches>();

        // Basic validation to catch inspector misconfiguration early
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Board width/height must be > 0 in the inspector.");
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }
        if (tilePrefab == null)
        {
            Debug.LogError("Board.tilePrefab is not assigned in the inspector.");
        }
        if (dots == null || dots.Length == 0)
        {
            Debug.LogError("Board.dots array is empty - assign at least one dot prefab.");
        }

        blankSpaces = new bool[width, height];
        allDots = new GameObject[width, height];
        allTiles = new BackgroundTile[width, height];

        if (boardLayout == null || boardLayout.Length == 0)
        {
            Debug.LogWarning("Board.boardLayout is empty. No blank tiles will be generated.");
        }

        SetUp();
    }

    public void GenerateBlankSpaces()
    {
        for (int i = 0; i < boardLayout.Length; i++)
        {
            if (boardLayout[i].tileKind == TileKind.Blank)
            {
                blankSpaces[boardLayout[i].x, boardLayout[i].y] = true;
            }
        }
    }

    private void SetUp()
    {
        GenerateBlankSpaces();
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (!blankSpaces[i, j])
                {
                    // 1. Background tiles stay down at normal grid positions (No offset!)
                    Vector2 tilePosition = new Vector2(i, j);
                    GameObject backgroundTile = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
                    backgroundTile.transform.parent = this.transform;
                    backgroundTile.name = "( " + i + ", " + j + " )";
                    allTiles[i, j] = backgroundTile.GetComponent<BackgroundTile>();

                    // Pick a random fruit
                    int dotToUse = Random.Range(0, dots.Length);
                    int MaxIterations = 0;
                    while (MatchesAt(i, j, dots[dotToUse]) && MaxIterations < 100)
                    {
                        dotToUse = Random.Range(0, dots.Length);
                        MaxIterations++;
                    }

                    // 2. Dots spawn high up in the sky and fall down into the tiles
                    Vector2 dotSpawnPosition = new Vector2(i, j + offSet);
                    GameObject dot = Instantiate(dots[dotToUse], dotSpawnPosition, Quaternion.identity);

                    dot.transform.parent = this.transform;
                    dot.name = "( " + i + ", " + j + " )";

                    Dot dotComponent = dot.GetComponent<Dot>();
                    if (dotComponent != null)
                    {
                        dotComponent.column = i;
                        dotComponent.row = j;
                    }

                    allDots[i, j] = dot;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private bool MatchesAt(int column, int row, GameObject piece)
    {
        // 1. Independent Horizontal Check
        if (column > 1)
        {
            if (allDots[column - 1, row] != null && allDots[column - 2, row] != null)
            {
                if (allDots[column - 1, row].tag == piece.tag && allDots[column - 2, row].tag == piece.tag)
                {
                    return true;
                }
            }
        }

        // 2. Independent Vertical Check
        if (row > 1)
        {
            if (allDots[column, row - 1] != null && allDots[column, row - 2] != null)
            {
                if (allDots[column, row - 1].tag == piece.tag && allDots[column, row - 2].tag == piece.tag)
                {
                    return true;
                }
            }
        }
        else if(column <= 1 || row <= 1)
        {
            if (column > 1 && row > 1)
            {
                if (allDots[column - 1, row] != null && allDots[column, row - 2] != null)
                {
                    if (allDots[column - 1, row].tag == piece.tag && allDots[column, row - 2].tag == piece.tag)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void FindAllMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    allDots[i, j].GetComponent<Dot>().FindMatches();
                }
            }
        }
    }

    private bool ColumnOrRow()
    {
        int numberHorizontal = 0;
        int numberVertical = 0;
        Dot firstPiece = findMatches.currentMatches[0].GetComponent<Dot>();
        if (firstPiece != null)
        {
            foreach (GameObject currentPiece in findMatches.currentMatches)
            {
                Dot dot = currentPiece.GetComponent<Dot>();
                if (dot != null)
                {
                    if (dot.row == firstPiece.row)
                    {
                        numberHorizontal++;
                    }
                    if (dot.column == firstPiece.column)
                    {
                        numberVertical++;
                    }
                }
            }
        }
        return (numberHorizontal == findMatches.currentMatches.Count || numberVertical == findMatches.currentMatches.Count);
    }
    private void CheckToMakeBombs()
    {
        if (findMatches.currentMatches.Count == 4 || findMatches.currentMatches.Count == 7)
        {
            findMatches.CheckBombs();
        }
        if (findMatches.currentMatches.Count == 5 || findMatches.currentMatches.Count == 8)
        {
            if (ColumnOrRow())
            {
                //make color bomb
                //is the current dot matched?
                if (currentDot != null)
                {
                    if (currentDot.isMatched)
                    {
                        if (!currentDot.isColorBomb)
                        {
                            currentDot.isMatched = false;
                            currentDot.MakeColorBomb();
                        }
                    }
                    else
                    {
                        if (currentDot.otherDot != null)
                        {
                            Dot otherDot = currentDot.otherDot.GetComponent<Dot>();
                            if (otherDot.isMatched)
                            {
                                if (!otherDot.isColorBomb)
                                {
                                    otherDot.isMatched = false;
                                    otherDot.MakeColorBomb();
                                }
                            }
                        }
                    }
                }
                else
                {
                    //make adjacent bomb
                    //is the current dot matched?
                    if (currentDot != null)
                    {
                        if (currentDot.isMatched)
                        {
                            if (!currentDot.isAdjacentBomb)
                            {
                                currentDot.isMatched = false;
                                currentDot.MakeAdjacentBomb();
                            }
                        }
                        else
                        {
                            if (currentDot.otherDot != null)
                            {
                                Dot otherDot = currentDot.otherDot.GetComponent<Dot>();
                                if (otherDot.isMatched)
                                {
                                    if (!otherDot.isAdjacentBomb)
                                    {
                                        otherDot.isMatched = false;
                                        otherDot.MakeAdjacentBomb();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private void DestroyMatchesAt(int column, int row)
    {
        if (allDots[column, row] != null)
        {
            Dot dotComponent = allDots[column, row].GetComponent<Dot>();
            if (dotComponent != null && dotComponent.isMatched)
            {
                if(findMatches.currentMatches.Count == 4 || findMatches.currentMatches.Count == 7)
                {
                    CheckToMakeBombs();
                }
                if (findMatches != null && findMatches.currentMatches.Contains(allDots[column, row]))
                {
                    findMatches.currentMatches.Remove(allDots[column, row]);
                }

                if (destroyEffect != null)
                {
                    GameObject particle = Instantiate(destroyEffect, allDots[column, row].transform.position, Quaternion.identity);
                    Destroy(particle, .5f);
                }

                Destroy(allDots[column, row]);
                allDots[column, row] = null;
            }
        }
    }

    public void DestroyMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    DestroyMatchesAt(i, j);
                }
            }
        }
        StartCoroutine(DecreaseRowCo());
    }

    private IEnumerator DecreaseRowCo()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                    // Only attempt to fill slots that are not blank spaces
                    if (!blankSpaces[i, j] && allDots[i, j] == null)
                    {
                        for (int k = j + 1; k < height; k++)
                        {
                            // find the next non-blank piece above this spot
                            if (!blankSpaces[i, k] && allDots[i, k] != null)
                            {
                                allDots[i, j] = allDots[i, k];
                                allDots[i, k] = null;

                                Dot dotComponent = allDots[i, j].GetComponent<Dot>();
                                if (dotComponent != null)
                                {
                                    dotComponent.row = j;
                                }
                                break;
                            }
                        }
                    }
            }
        }
        yield return new WaitForSeconds(.25f);
        StartCoroutine(FillBoardCo());
    }

    private void RefillBoard()
    {
        for (int i = 0; i < width; i++)
        {
            int missingPiecesCount = 0;

            for (int j = 0; j < height; j++)
            {
                // Only refill positions that are not blank spaces
                if (!blankSpaces[i, j] && allDots[i, j] == null)
                {
                    Vector2 tempPosition = new Vector2(i, height + missingPiecesCount);
                    missingPiecesCount++;

                    int dotToUse = Random.Range(0, dots.Length);
                    GameObject piece = Instantiate(dots[dotToUse], tempPosition, Quaternion.identity);

                    piece.transform.parent = this.transform;
                    piece.name = "( " + i + ", " + j + " )";

                    Dot dotComponent = piece.GetComponent<Dot>();
                    if (dotComponent != null)
                    {
                        dotComponent.column = i;
                        dotComponent.row = j;
                    }

                    allDots[i, j] = piece;
                }
            }
        }
    }

    private bool MatchesOnBoard()
    {
        FindAllMatches();
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    if (allDots[i, j].GetComponent<Dot>().isMatched)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator FillBoardCo()
    {
        RefillBoard();

        yield return new WaitForSeconds(.5f);

        if (MatchesOnBoard())
        {
            yield return new WaitForSeconds(.25f);

            DestroyMatches();
        }
        else
        {
            findMatches.currentMatches.Clear();
            currentDot = null;
            yield return new WaitForSeconds(.25f);
            currentState = GameState.move;
        }
    }
}