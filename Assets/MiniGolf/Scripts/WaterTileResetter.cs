using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapCollider2D))]
public class WaterTileResetter : MonoBehaviour
{
    private TilemapCollider2D tilemapCollider;

    private void Awake()
    {
        tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            tilemapCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            tilemapCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryResetBall(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryResetBall(collision.collider);
    }

    private void TryResetBall(Collider2D other)
    {
        if (!other.CompareTag("Ball"))
        {
            return;
        }

        if (other.TryGetComponent<Ball>(out Ball ball))
        {
            ball.ResetToStrokeStart();
        }
    }
}