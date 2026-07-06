using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum GameState
{
    wait,
    move
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
    public GameObject destroyEffect;
    private BackgroundTile[,] allTiles;
    private FindMatches findMatches;

    // Start is called before the first frame update
    void Start()
    {
        findMatches = FindObjectOfType<FindMatches>();
        allTiles = new BackgroundTile[width, height];
        allDots = new GameObject[width, height];
        SetUp();
    }

    private void SetUp()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
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
    private void DestroyMatchesAt(int column, int row)
    {
        if (allDots[column, row] != null)
        {
            Dot dotComponent = allDots[column, row].GetComponent<Dot>();
            if (dotComponent != null && dotComponent.isMatched)
            {
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
                if (allDots[i, j] == null)
                {
                    for (int k = j + 1; k < height; k++)
                    {
                        if (allDots[i, k] != null)
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
                if (allDots[i, j] == null)
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
            yield return new WaitForSeconds(.25f);
            currentState = GameState.move;
        }
    }
}