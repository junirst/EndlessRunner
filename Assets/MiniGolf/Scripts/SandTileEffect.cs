using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapCollider2D))]
public class SandTileEffect : MonoBehaviour
{
    [Header("Sand Settings")]
    [SerializeField, Range(0f, 1f)] private float speedMultiplier = 0.15f;

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

        if (speedMultiplier < 0f)
        {
            speedMultiplier = 0f;
        }

        if (speedMultiplier > 1f)
        {
            speedMultiplier = 1f;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TrySlowBall(other);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TrySlowBall(collision.collider);
    }

    private void TrySlowBall(Collider2D other)
    {
        if (!other.CompareTag("Ball"))
        {
            return;
        }

        Rigidbody2D ballRigidbody = other.attachedRigidbody;
        if (ballRigidbody == null)
        {
            return;
        }

        ballRigidbody.velocity *= speedMultiplier;
    }
}