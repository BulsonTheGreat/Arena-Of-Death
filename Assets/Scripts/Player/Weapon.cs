using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;


public class Weapon : MonoBehaviour
{
    [HideInInspector] public int damage = 1;
    [HideInInspector] public float knockback = 1;
    public bool isEnabled;
    BoxCollider triggerBox;
    // Start is called before the first frame update
    void Start()
    {
        triggerBox = GetComponent<BoxCollider>();
        triggerBox.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            other.gameObject.GetComponent<EnemyAttributes>().TakeDamage(damage);
            other.gameObject.GetComponent<EnemyAttributes>().ApplyKnockback(transform.forward, knockback, 0.5f);
        }
    }

    public void EnableCollidder(bool enable)
    {
        triggerBox.enabled = enable;
        isEnabled = enable;
    }
}
