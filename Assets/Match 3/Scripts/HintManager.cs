using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    private const float HintDelaySeconds = 15f;
    private Board board;
    public float hintDelay;
    private float hintDelaySeconds;
    public GameObject hintParticle;
    public GameObject currentHint;
    // Start is called before the first frame update
    void Start()
    {
        board = FindObjectOfType<Board>();
        if (board == null)
        {
            Debug.LogWarning("HintManager could not find a Board in the scene. Hints will be disabled.");
        }
        hintDelay = HintDelaySeconds;
        hintDelaySeconds = hintDelay;
        if (hintParticle == null)
        {
            Debug.LogWarning("HintManager.hintParticle is not assigned in the Inspector. Hints will be disabled.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        hintDelaySeconds -= Time.deltaTime;
        if(hintDelaySeconds <= 0 && currentHint == null)
        {
            MarkHint();
            hintDelaySeconds += hintDelay;
        }
    }

    //find all possible matches on the board
    List<GameObject> FindAllMatches()
    {
        List<GameObject> possibleMoves = new List<GameObject>();
        if (board == null)
        {
            Debug.LogWarning("HintManager.FindAllMatches called but board reference is null.");
            return possibleMoves;
        }
        for (int i = 0; i < board.width; i++)
        {
            for (int j = 0; j < board.height; j++)
            {
                if (board.allDots[i, j] != null)
                {
                    if (i < board.width - 1)
                    {
                        if (board.SwitchAndCheck(i, j, Vector2.right))
                        {
                            possibleMoves.Add(board.allDots[i, j]);
                        }
                    }
                    if (j < board.height - 1)
                    {
                        if (board.SwitchAndCheck(i, j, Vector2.up))
                        {
                            possibleMoves.Add(board.allDots[i, j]);
                        }
                    }
                }
            }
        }
        // Debug: report how many possible moves were found (helps diagnose missing hints)
        if (possibleMoves.Count == 0)
        {
            Debug.Log("HintManager: no possible moves found on board.");
        }
        return possibleMoves;
    }
    //pick one of those matches at random
    GameObject PickOneRandomly()
    {
        List<GameObject> possibleMoves = new List<GameObject>();
        possibleMoves = FindAllMatches();
        if (possibleMoves.Count > 0)
        {
            int pieceToUse = Random.Range(0, possibleMoves.Count);
            return possibleMoves[pieceToUse];
        }
        return null;
    }
    //create the hint behind the chosen match
    private void MarkHint()
    {
        // Defensive: avoid Instantiate when no particle prefab is assigned
        if (hintParticle == null)
        {
            // reset delay so we don't spam warnings every frame
            hintDelaySeconds = hintDelay;
            return;
        }

        GameObject move = PickOneRandomly();
        if (move != null)
        {
            currentHint = Instantiate(hintParticle, move.transform.position, Quaternion.identity);
            currentHint.transform.parent = move.transform;
            Match3VfxController.ConfigureHint(currentHint);
        }
        else
        {
            // No move found to show a hint for
            Debug.Log("HintManager: PickOneRandomly returned null - no hint will be shown.");
            hintDelaySeconds = hintDelay;
        }
    }
    //destroy the hint.
    public void DestroyHint()
    {
        if (currentHint != null)
        {
            Destroy(currentHint);
            currentHint = null;
            hintDelaySeconds = hintDelay;
        }
    }
}
