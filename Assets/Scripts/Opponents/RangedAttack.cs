using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedAttack : MonoBehaviour
{
    [SerializeField] Animator anim;
    FireBall fireBallScript;
    Transform player;
    [SerializeField] GameObject firePoint;
    public GameObject projectile;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void ShootProjectile()
    {
        Vector3 playerPosition = player.position;
        InstantiateProjectile(playerPosition);
    }

    void InstantiateProjectile(Vector3 targetPosition)
    {
        var projectileObj = Instantiate(projectile, firePoint.transform.position, Quaternion.identity) as GameObject;

        fireBallScript = projectileObj.GetComponentInChildren<FireBall>();
        RotateToDestination(projectileObj, targetPosition, true);
        projectileObj.GetComponent<Rigidbody>().velocity = (targetPosition - firePoint.transform.position).normalized * fireBallScript.speed;
    }

    void RotateToDestination(GameObject obj, Vector3 destination, bool onlyY)
    {
        var direction = destination - obj.transform.position;
        var rotation = Quaternion.LookRotation(direction);

        if (onlyY)
        {
            // Keep only the Y-axis rotation
            rotation.x = 0;
            rotation.z = 0;
        }

        // Use Quaternion.LookRotation to directly set the rotation
        obj.transform.rotation = rotation;
    }
}
