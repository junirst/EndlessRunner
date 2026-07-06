using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindMatches : MonoBehaviour
{
    private Board board;
    public List<GameObject> currentMatches = new List<GameObject>();

    void Start()
    {
        board = Object.FindFirstObjectByType<Board>();
    }

    public void FindAllMatches()
    {
        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                GameObject currentDot = board.allDots[i, j];
                if (currentDot != null)
                {
                    // 1. Scan Right (Fixes Match-4 and Match-5 horizontally)
                    if (i < board.width - 2)
                    {
                        GameObject dotRight1 = board.allDots[i + 1, j];
                        GameObject dotRight2 = board.allDots[i + 2, j];
                        if (dotRight1 != null && dotRight2 != null)
                        {
                            if (dotRight1.tag == currentDot.tag && dotRight2.tag == currentDot.tag)
                            {
                                currentDot.GetComponent<Dot>().isMatched = true;
                                dotRight1.GetComponent<Dot>().isMatched = true;
                                dotRight2.GetComponent<Dot>().isMatched = true;
                            }
                        }
                    }

                    // 2. Scan Up (Fixes Match-4 and Match-5 vertically)
                    if (j < board.height - 2)
                    {
                        GameObject dotUp1 = board.allDots[i, j + 1];
                        GameObject dotUp2 = board.allDots[i, j + 2];
                        if (dotUp1 != null && dotUp2 != null)
                        {
                            if (dotUp1.tag == currentDot.tag && dotUp2.tag == currentDot.tag)
                            {
                                currentDot.GetComponent<Dot>().isMatched = true;
                                dotUp1.GetComponent<Dot>().isMatched = true;
                                dotUp2.GetComponent<Dot>().isMatched = true;
                            }
                        }
                    }
                }
            }
        }
    }
}