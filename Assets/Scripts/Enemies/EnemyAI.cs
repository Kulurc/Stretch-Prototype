using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyType
    {
        RamRusher,
        Shielder,
        Flyer,
        Shooter
    }

    [Header("Enemy Configuration")]
    // Assign the type of this specific enemy in the Inspector.
    public EnemyType thisEnemyType;
    
    [Header("Enemy Health")]
    public int health = 1;

    [Header("Moving Enemy Settings")]
    public float moveSpeed = 5f;

    [Header("Flyer Settings")]
    public GameObject player;
    private float distance;
    public float staggerInterval = 3f; 
    public float moveDuration = 1f;    
    private bool isMoving = false;     
    private float lastStateChangeTime; 

    [Header("Shooter Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;    
    public float fireRate = 1f;       
    private float nextFireTime = 0f;


    void Start()
    {
    }

    void Update()
    {
        switch (thisEnemyType)
        {
            case EnemyType.RamRusher:
                RamRusherBehavior();
                break;
            case EnemyType.Shielder:
                ShielderBehavior();
                break;
            case EnemyType.Flyer:
                FlyerBehavior();
                break;
            case EnemyType.Shooter:
                ShooterBehavior();
                break;
        }
    }

    private void RamRusherBehavior()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        DespawnIfOffScreen();
    }

    private void ShielderBehavior()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        DespawnIfOffScreen();

        // // Placeholder for future logic
        // if (playerTransform != null)
        // {
        //     // 1) Check if player is above (stomp)
        //     if (IsPlayerAbove())
        //     {
        //         Die();
        //     }
        //     // 2) Check if player is in front (blocking)
        //     else if (IsPlayerInFront())
        //     {
        //         // Block attack: enemy takes no damage
        //     }
        //     // 3) Else: player behind, enemy can take damage
        //     else
        //     {
        //         // Enemy vulnerable
        //     }
        // }
    }

    private void FlyerBehavior()
    {
        if (isMoving)
        {
            transform.position = Vector2.MoveTowards(this.transform.position, player.transform.position, moveSpeed * Time.deltaTime);

            if (Time.time > lastStateChangeTime + moveDuration)
            {
                isMoving = false; 
                lastStateChangeTime = Time.time; 
            }
        }
        else
        {
            if (Time.time > lastStateChangeTime + staggerInterval)
            {
                isMoving = true; 
                lastStateChangeTime = Time.time; 
            }
        }
    }

    private void ShooterBehavior()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            Vector2[] directions = {
            Vector2.left,
            Vector2.up,
            Vector2.right,
            Vector2.down,                        
            new Vector2(1, 1).normalized,      
            new Vector2(-1, 1).normalized,     
            new Vector2(1, -1).normalized,     
            new Vector2(-1, -1).normalized     
        };

            foreach (Vector2 dir in directions)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                Bullet bulletScript = bullet.GetComponent<Bullet>();
                bulletScript.SetDirection(dir);                     // Assign direction
            }
        }
    }

    private void DespawnIfOffScreen(float buffer = 1f)
    {
        float cameraLeftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        if (transform.position.x < cameraLeftEdge - buffer)
        {
            Destroy(gameObject);
        }
    }

    // public void TakeDamage(int damage)
    // {
    //     health -= damage;
    //     if (health <= 0)
    //     {
    //         Destroy(gameObject);
    //     }
    // }
    // --- Placeholders for relative position checks ---
    // private bool IsPlayerAbove()
    // {
    //     // Compare Y positions of player and enemy
    //     return 0;
    //     //return playerTransform.position.y > transform.position.y + 0.5f;
    // }

    // private bool IsPlayerInFront()
    // {
    //     // Compare X positions and facing direction
    //     // Placeholder: assume enemy moves left, so front is left
    //     return 0;
    //     //return playerTransform.position.x < transform.position.x;
    // }

    // private void Die()
    // {
    //     // Minimal death logic for merge
    //     Destroy(gameObject);
    // }
}