using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;         // Bullet speed
    private Vector2 moveDirection;    // Unique direction per bullet
    public float lifetime = 5f;       // Time before despawn

    private Rigidbody2D rb;

    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;   // Normalize so direction is consistent
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = moveDirection * speed;
        Destroy(gameObject, lifetime);    // Auto-destroy after lifetime
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<FrogController>().TakeDamage();
            Destroy(gameObject);    //despawn on hit
        }

    }
}