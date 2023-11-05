using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPack : MonoBehaviour
{
    [SerializeField] GameObject hpPack;

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<ThirdPersonController>() != null && other.GetComponent<ThirdPersonController>().hp < other.GetComponent<ThirdPersonController>().maxHp)
        {
            var player = other.GetComponent<ThirdPersonController>();
            player.RestoreHealth(30);
            Destroy(gameObject);
        }
    }
}
