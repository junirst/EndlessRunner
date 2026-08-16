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
    public GameObject BreakableTilePrefab;

    // --- Dots Section ---
    public GameObject[] dots;
    public GameObject[,] allDots;

    public GameObject destroyEffect;

    // Score goals used by the UI ScoreManager. Exposed to the inspector.
    public int[] scoreGoals = new int[] { 1000, 2000, 3000 };

    // --- Board Layout Section ---
    [NonReorderable] // This attribute forces the classic "Size" field to appear
    public tileType[] boardLayout;
    public Dot currentDot;

    private bool[,] blankSpaces;
    private BackgroundTile[,] breakableTiles;
    private BackgroundTile[,] allTiles;
    private FindMatches findMatches;
    [SerializeField] private AudioClip gameOverSfx;
    private AudioManagerM3 audioManager;
    private int lastMatchCount;

        // Start is called before the first frame update
        void Start()
    {
        breakableTiles = new BackgroundTile[width, height];
        findMatches = FindObjectOfType<FindMatches>();
        audioManager = FindObjectOfType<AudioManagerM3>();

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

    public void GenerateBreakableTiles()
    {
        //look at all the tiles in the layout
        for (int i = 0; i < boardLayout.Length; i++)
        {
            //if a tile is a "jelly" tile  
            if (boardLayout[i].tileKind == TileKind.Breakable)
            {
                //create a "jelly" tile at that position
                Vector2 breakableTilePosition = new Vector2(boardLayout[i].x, boardLayout[i].y);
                GameObject tile = Instantiate(BreakableTilePrefab, breakableTilePosition, Quaternion.identity);
                breakableTiles[boardLayout[i].x, boardLayout[i].y] = tile.GetComponent<BackgroundTile>();
                tile.transform.parent = this.transform;
                tile.name = "( " + boardLayout[i].x + ", " + boardLayout[i].y + " )";
                BackgroundTile backgroundTileComponent = tile.GetComponent<BackgroundTile>();
                if (backgroundTileComponent != null)
                {
                    backgroundTileComponent.hitPoints = boardLayout[i].breakableValue;
                }
                allTiles[boardLayout[i].x, boardLayout[i].y] = backgroundTileComponent;
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
                        Debug.Log(MaxIterations);
                    }
                    MaxIterations = 0;

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
                if (findMatches.currentMatches.Count == 4 || findMatches.currentMatches.Count == 7 || findMatches.currentMatches.Count == 5 || findMatches.currentMatches.Count == 8)
                {
                    CheckToMakeBombs();
                }
                //does a tile need to break?
                if (breakableTiles[column, row] != null)
                {
                    //if it does, give it damage
                    breakableTiles[column, row].TakeDamage(1);
                    if (breakableTiles[column, row].hitPoints <= 0)
                    {
                        breakableTiles[column, row] = null;
                    }
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
        // Remember how many pieces are in the current match group so we can play the correct SFX once
        if (findMatches != null)
        {
            lastMatchCount = findMatches.currentMatches.Count;
        }

        Debug.Log($"Board: DestroyMatches called. lastMatchCount={lastMatchCount}");
        if (audioManager == null)
        {
            Debug.LogWarning("Board: AudioManagerM3 not found in scene. No SFX will play.");
        }
        else if (lastMatchCount > 0)
        {
            Debug.Log($"Board: Playing match SFX for {lastMatchCount} pieces.");
            audioManager.PlayMatchSfx(lastMatchCount);
        }
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
        findMatches.currentMatches.Clear();
        StartCoroutine(DecreaseRowCo());
    }

    private IEnumerator DecreaseRowCo2()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] == null)
                {
                    //if the curent spot isn't blank and is empty
                    if (!blankSpaces[i, j] && allDots[i, j] == null)
                    {
                        //loop from the space above to the top of the column
                        for (int k = j + 1; k < height; k++)
                        {
                            //if a dot is found, move it to the empty space
                            if (allDots[i, k] != null)
                            {
                                //move the dot to the empty space
                                allDots[i, k].GetComponent<Dot>().row = j;
                                //set that spot to be null
                                allDots[i, k] = null;
                                //break out of the loop
                                break;
                            }
                        }
                    }
                }
            }
        }
        yield return new WaitForSeconds(.4f);
        StartCoroutine(FillBoardCo());
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
                if (!blankSpaces[i, j] && allDots[i, j] == null && !blankSpaces[i,j])
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

        while (MatchesOnBoard())
        {
            yield return new WaitForSeconds(.5f);

            DestroyMatches();
        }
            findMatches.currentMatches.Clear();
            currentDot = null;
            yield return new WaitForSeconds(.5f);

            if (isDeadLocked())
            {
                ShuffleBoard();
                Debug.Log("Deadlock detected! Shuffling the board...");
            }   
            currentState = GameState.move;
    }

    private void SwitchPieces(int column, int row, Vector2 direction)
    {
        //take the firce piece and save it in a holder
        GameObject holder = allDots[column + (int)direction.x, row + (int)direction.y] as GameObject;
        //switching the first dot to be the second position
        allDots[column + (int)direction.x, row + (int)direction.y] = allDots[column, row];
        //set the first dot to be the second dot
        allDots[column, row] = holder;
    }

    private bool CheckForMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    //make sure that one and two to the right are in the board
                    if (i < width - 2)
                    {
                        //check if dot to the right and two to the right exist
                        if (allDots[i + 1, j] != null && allDots[i + 2, j] != null)
                        {
                            if (allDots[i + 1, j].tag == allDots[i, j].tag && allDots[i + 2, j].tag == allDots[i, j].tag)
                            {
                                return true;
                            }
                        }
                    }
                    //make sure that one and two above are in the board
                    if (j < height - 2)
                    {
                        if (allDots[i, j + 1] != null && allDots[i, j + 2] != null)
                        {
                            if (allDots[i, j + 1].tag == allDots[i, j].tag && allDots[i, j + 2].tag == allDots[i, j].tag)
                            {
                                return true;
                            }

                        }
                    }
                }
            }
        }
        return false;
    }

    public bool SwitchAndCheck(int column, int row, Vector2 direction)
    {
        SwitchPieces(column, row, direction);
        if (CheckForMatches())
        {
            SwitchPieces(column, row, direction);
            return true;
        }
            SwitchPieces(column, row, direction);
            return false;
    }

    private bool isDeadLocked()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    if (i < width - 1)
                    {
                        if (SwitchAndCheck(i, j, Vector2.right))
                        {
                            return false;
                        }
                    }
                    if (j < height - 1)
                    {
                        if (SwitchAndCheck(i, j, Vector2.up))
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

    private void ShuffleBoard()
    {
        //create a list of game objects
        List<GameObject> newBoard = new List<GameObject>();
        //add everything to the list
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allDots[i, j] != null)
                {
                    newBoard.Add(allDots[i, j]);
                }
            }
        }
        //for every spot on the board
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //if this spot shouldn't be blank
                if (!blankSpaces[i, j])
                {
                    //pick a random number
                    int pieceToUse = Random.Range(0, newBoard.Count);
                    //assign the column and row to the piece
                    int MaxIterations = 0;
                    while (MatchesAt(i, j, newBoard[pieceToUse]) && MaxIterations < 100)
                    {
                        pieceToUse = Random.Range(0, newBoard.Count);
                        MaxIterations++;
                        Debug.Log(MaxIterations);
                    }
                    //make a container for the piece
                    Dot piece = newBoard[pieceToUse].GetComponent<Dot>();
                    MaxIterations = 0;
                    piece.column = i;
                    piece.row = j;
                    //fill the dot array with this new piece
                    allDots[i, j] = newBoard[pieceToUse];
                    //remove it from the list
                    newBoard.RemoveAt(pieceToUse);
                }
            }
        }
        //check if deadlocked
        if (isDeadLocked())
        {
            // If the board is still deadlocked after shuffling once, try again.
            ShuffleBoard();
        }
    }

    // Call this when the match-3 game should trigger a game-over (assign clip per-scene in Inspector)
    public void TriggerGameOver()
    {
        Match3AudioManager.Instance?.PlayGameOverSfx(gameOverSfx);
    }
}