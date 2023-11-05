using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] int damage;
    [SerializeField] float knockback;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && anim.GetBool("isAttacking"))
        {
            var player = other.GetComponent<ThirdPersonController>();
            if (!player.isInvulnerable)
            {
                player.TakeDamage(damage);
                player.ApplyKnockback(transform.forward, knockback, 0.5f);
            }
        }
    }

}
