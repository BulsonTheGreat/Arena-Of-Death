using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyAttributes : MonoBehaviour
{
    public int maxHP;
    public int range;
    [HideInInspector] public int health;
    Animator anim;
    Rigidbody rb;
    [SerializeField] HealthBar healthBar;
    [SerializeField] GameObject healthPack;
    private BoxCollider hitbox;

    private NavMeshAgent navMeshAgent;
    private Vector3 knockbackDirection;
    private float knockbackForce;
    private float knockbackDuration;
    private float knockbackTimer;
    GameController gameController;

    private void Start()
    {
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        health = maxHP;
        healthBar.SetMaxHealth(maxHP);
        hitbox = GetComponent<BoxCollider>();
    }

    public void TakeDamage(int damage)
    {
        //play animation
        anim.SetTrigger("damage");
        health -= damage;
        healthBar.SetHealth(health);
        if (health <= 0)
        {
            hitbox.enabled = false;
            int r = UnityEngine.Random.Range(0, 100);
            if (r >= 70)
            {
                SpawnPack(transform.position);
            }
            EnemyDeath();
        }
    }

    private void Update()
    {
        if (knockbackTimer > 0)
        {
            // Apply knockback force in the desired direction
            Vector3 knockbackVelocity = knockbackDirection * knockbackForce;
            navMeshAgent.velocity = knockbackVelocity;

            // Reduce the knockback timer
            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0)
            {
                // Resume normal navigation
                navMeshAgent.isStopped = false;
            }
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        knockbackDirection = direction.normalized;
        knockbackForce = force;
        knockbackDuration = duration;
        knockbackTimer = knockbackDuration;

        // Pause the NavMeshAgent during knockback
        navMeshAgent.isStopped = true;
    }

    private void EnemyDeath()
    {
        gameController.IncrementEnemiesKilled();
        //play death animation
        anim.SetTrigger("death");
    }

    public void SpawnPack(Vector3 position)
    {
        GameObject pack = Instantiate(healthPack, position, Quaternion.identity);
        pack.SetActive(true);
    }

    public void DestroyEnemy()
    {
        Destroy(gameObject);
    }
}
