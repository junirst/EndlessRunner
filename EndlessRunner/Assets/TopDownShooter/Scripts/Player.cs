using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [Range(0.1f, 2f)]
    [SerializeField] private float fireRate = 0.5f;

    private Rigidbody2D rb;
    private float mx;
    private float my;

    private float fireTimer;

    private Vector2 mousePos;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!ShooterLevelManager.manager.CanAcceptInput())
        {
            mx = 0f;
            my = 0f;
            return;
        }

        mx = Input.GetAxisRaw("Horizontal");
        my = Input.GetAxisRaw("Vertical");
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        float angle = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x) * Mathf.Rad2Deg - 90f;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (Input.GetMouseButton(0) && fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireRate;
        } else {
            fireTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(mx, my).normalized * speed;
    }

    private void Shoot()
    {
        Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);
        ShooterAudioManager.Instance?.PlayPlayerShootSfx();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("EnemyBullet"))
        {
            ShooterLevelManager.manager.GameOver();
            Destroy(gameObject);
        }
    }
}
