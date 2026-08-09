using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FindMatches : MonoBehaviour
{
    private Board board;
    public List<GameObject> currentMatches = new List<GameObject>();

    void Start()
    {
        board = Object.FindFirstObjectByType<Board>();
    }

    private List<GameObject> IsAdjacentBomb(Dot dot1, Dot dot2, Dot dot3)
    {
        // Use a set to avoid duplicate pieces when bombs overlap
        HashSet<GameObject> resultSet = new HashSet<GameObject>();
        if (dot1 != null && dot1.isAdjacentBomb)
        {
            foreach (var g in GetAdjacentPieces(dot1.column, dot1.row)) resultSet.Add(g);
        }
        if (dot2 != null && dot2.isAdjacentBomb)
        {
            foreach (var g in GetAdjacentPieces(dot2.column, dot2.row)) resultSet.Add(g);
        }
        if (dot3 != null && dot3.isAdjacentBomb)
        {
            foreach (var g in GetAdjacentPieces(dot3.column, dot3.row)) resultSet.Add(g);
        }

        return resultSet.ToList();
    }

    public void FindAllMatches()
    {
        // Clear out any old matches from the list first
        currentMatches.Clear();

        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                GameObject currentDot = board.allDots[i, j];
                if (currentDot != null)
                {
                    // 1. Scan Right (Horizontal Matches)
                    if (i < board.width - 2)
                    {
                        GameObject leftDot = board.allDots[i + 1, j];
                        GameObject rightDot = board.allDots[i + 2, j];
                        if (leftDot != null && rightDot != null)
                        {
                            if (leftDot.tag == currentDot.tag && rightDot.tag == currentDot.tag)
                            {
                                currentDot.GetComponent<Dot>().isMatched = true;
                                leftDot.GetComponent<Dot>().isMatched = true;
                                rightDot.GetComponent<Dot>().isMatched = true;

                                // --- FIX: Add the 3 basic matching dots to the list ---
                                if (!currentMatches.Contains(currentDot)) currentMatches.Add(currentDot);
                                if (!currentMatches.Contains(leftDot)) currentMatches.Add(leftDot);
                                if (!currentMatches.Contains(rightDot)) currentMatches.Add(rightDot);

                                // ROW BOMB CHECK
                                if (currentDot.GetComponent<Dot>().isRowBomb
                                    || leftDot.GetComponent<Dot>().isRowBomb
                                    || rightDot.GetComponent<Dot>().isRowBomb)
                                {
                                    foreach (GameObject piece in GetRowPieces(j))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }

                                // COLUMN BOMB CHECK (Inside Horizontal Match)
                                if (currentDot.GetComponent<Dot>().isColumnBomb)
                                {
                                    foreach (GameObject piece in GetColumnPieces(i))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                                if (leftDot.GetComponent<Dot>().isColumnBomb)
                                {
                                    foreach (GameObject piece in GetColumnPieces(i + 1))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                                if (rightDot.GetComponent<Dot>().isColumnBomb)
                                {
                                    foreach (GameObject piece in GetColumnPieces(i + 2))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                                // ADJACENT BOMB CHECK (Inside Horizontal Match)
                                List<GameObject> adjacentBombPiecesH = IsAdjacentBomb(currentDot.GetComponent<Dot>(), leftDot.GetComponent<Dot>(), rightDot.GetComponent<Dot>());
                                if (adjacentBombPiecesH != null && adjacentBombPiecesH.Count > 0)
                                {
                                    foreach (GameObject piece in adjacentBombPiecesH)
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 2. Scan Up (Vertical Matches)
                    if (j < board.height - 2)
                    {
                        GameObject upDot = board.allDots[i, j + 1];
                        GameObject downdot = board.allDots[i, j + 2];
                        if (upDot != null && downdot != null)
                        {
                            if (upDot.tag == currentDot.tag && downdot.tag == currentDot.tag)
                            {
                                currentDot.GetComponent<Dot>().isMatched = true;
                                upDot.GetComponent<Dot>().isMatched = true;
                                downdot.GetComponent<Dot>().isMatched = true;

                                // --- FIX: Add the 3 basic matching dots to the list ---
                                if (!currentMatches.Contains(currentDot)) currentMatches.Add(currentDot);
                                if (!currentMatches.Contains(upDot)) currentMatches.Add(upDot);
                                if (!currentMatches.Contains(downdot)) currentMatches.Add(downdot);

                                // COLUMN BOMB CHECK
                                if (currentDot.GetComponent<Dot>().isColumnBomb
                                    || upDot.GetComponent<Dot>().isColumnBomb
                                    || downdot.GetComponent<Dot>().isColumnBomb)
                                {
                                    foreach (GameObject piece in GetColumnPieces(i))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }

                                // ROW BOMB CHECK (Inside Vertical Match)
                                if (currentDot.GetComponent<Dot>().isRowBomb)
                                {
                                    foreach (GameObject piece in GetRowPieces(j))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                                if (upDot.GetComponent<Dot>().isRowBomb)
                                {
                                    foreach (GameObject piece in GetRowPieces(j + 1))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                                if (downdot.GetComponent<Dot>().isRowBomb)
                                {
                                    foreach (GameObject piece in GetRowPieces(j + 2))
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                                // ADJACENT BOMB CHECK (Inside Vertical Match)
                                List<GameObject> adjacentBombPiecesV = IsAdjacentBomb(currentDot.GetComponent<Dot>(), upDot.GetComponent<Dot>(), downdot.GetComponent<Dot>());
                                if (adjacentBombPiecesV != null && adjacentBombPiecesV.Count > 0)
                                {
                                    foreach (GameObject piece in adjacentBombPiecesV)
                                    {
                                        if (piece != null)
                                        {
                                            piece.GetComponent<Dot>().isMatched = true;
                                            if (!currentMatches.Contains(piece)) currentMatches.Add(piece);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public void MatchPiecesOfColor(string color)
    {
        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                //if the piece exists
                if (board.allDots[i, j] != null)
                {
                    //check the tag on the piece
                    if (board.allDots[i, j].tag == color)
                    {
                        //set the piece to matched
                        board.allDots[i, j].GetComponent<Dot>().isMatched = true;
                        if (!currentMatches.Contains(board.allDots[i, j])) currentMatches.Add(board.allDots[i, j]);
                    }
                }
            }
        }
    }

    List<GameObject> GetAdjacentPieces(int column, int row)
    {
        List<GameObject> dots = new List<GameObject>();
        for (int i = column - 1; i <= column + 1; i++)
        {
            for (int j = row - 1; j <= row + 1; j++)
            {
                // Check if the indices are within the bounds of the board
                if (i >= 0 && i < board.width && j >= 0 && j < board.height)
                {
                    if (board.allDots[i, j] != null)
                    {
                        dots.Add(board.allDots[i, j]);
                        board.allDots[i, j].GetComponent<Dot>().isMatched = true;
                    }
                }
            }
        }
        return dots;
    }

    List<GameObject> GetColumnPieces(int column)
    {
        List<GameObject> dots = new List<GameObject>();
        for (int i = 0; i < board.height; i++)
        {
            if (board.allDots[column, i] != null)
            {
                dots.Add(board.allDots[column, i]);
                board.allDots[column, i].GetComponent<Dot>().isMatched = true;
            }
        }
        return dots;
    }

    List<GameObject> GetRowPieces(int row)
    {
        List<GameObject> dots = new List<GameObject>();
        for (int i = 0; i < board.width; i++)
        {
            if (board.allDots[i, row] != null)
            {
                dots.Add(board.allDots[i, row]);
                board.allDots[i, row].GetComponent<Dot>().isMatched = true;
            }
        }
        return dots;
    }

    public void CheckBombs()
    {
        //if the player move smth
        if (board.currentDot != null)
        {
            //is the piece moved matched?
            if (board.currentDot.isMatched)
            {
                
                //make it unmatched
                board.currentDot.isMatched = false;
                /*
                //decide what kind of bomb to make
                int typeOfBomb = Random.Range(0, 100);
                if (typeOfBomb < 50)
                {
                    //make a row bomb
                    board.currentDot.MakeRowBomb();
                }
                else if (typeOfBomb >= 50)
                {
                    //make a column bomb
                    board.currentDot.MakeColumnBomb();
                }
                */

                if((board.currentDot.swipeAngle > -45 && board.currentDot.swipeAngle <= 45) || (board.currentDot.swipeAngle < -135 || board.currentDot.swipeAngle >= 135))
                {
                    //make a row bomb
                    board.currentDot.MakeRowBomb();
                }
                else
                {
                    //make a column bomb
                    board.currentDot.MakeColumnBomb();
                }
            }
            else if (board.currentDot.otherDot != null)
            {
                Dot otherDot = board.currentDot.otherDot.GetComponent<Dot>();
                //other dots matched?
                if (otherDot.isMatched)
                {
                    //make it unmatched
                    otherDot.isMatched = false;
                    /*
                    //choose which bomb to make
                    int typeOfBomb = Random.Range(0, 100);
                    if (typeOfBomb < 50)
                    {
                        //make a row bomb
                        otherDot.MakeRowBomb();
                    }
                    else if (typeOfBomb >= 50)
                    {
                        //make a column bomb
                        otherDot.MakeColumnBomb();
                    }
                    */
                    
                    if((board.currentDot.swipeAngle > -45 && board.currentDot.swipeAngle <= 45) || (board.currentDot.swipeAngle < -135 || board.currentDot.swipeAngle >= 135))
                    {
                        //make a row bomb
                        otherDot.MakeRowBomb();
                        Debug.Log($"Created Row Bomb at ({otherDot.column},{otherDot.row}) (otherDot)");
                    }
                    else
                    {
                        //make a column bomb
                        otherDot.MakeColumnBomb();
                        Debug.Log($"Created Column Bomb at ({otherDot.column},{otherDot.row}) (otherDot)");
                    }
                }
            }
       
        }
    }
}