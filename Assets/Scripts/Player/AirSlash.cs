using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirSlash : MonoBehaviour
{
    public float speed = 30;
    public float slowDownRate = 0.01f;
    public float destroyDelay = 3;

    public int damage = 8;
    public float knockback = 6;
    [SerializeField] GameObject vfx;

    private Rigidbody rb;
    private bool stopped;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        if(GetComponentInParent<Rigidbody>() != null)
        {
            rb = GetComponentInParent<Rigidbody>();
            StartCoroutine(SlowDown());
        }
        else
        {
            Debug.Log("No rigidbody");
        }
        Destroy(vfx, destroyDelay);
        Destroy(gameObject, destroyDelay);
        
    }

    private void FixedUpdate()
    {
        if (!stopped)
        {
            if (!stopped)
            {
                RaycastHit hit;
                Vector3 raycastOrigin = transform.position;
                Vector3 raycastDirection = -Vector3.up;

                // Perform the raycast
                if (Physics.Raycast(raycastOrigin, raycastDirection, out hit))
                {
                    // Calculate the target position with the correct height
                    Vector3 targetPosition = hit.point + Vector3.up;

                    // Move the projectile towards the target position
                    float step = speed * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
                }
                else
                {
                    // If no hit, continue moving the projectile forward
                    float step = speed * Time.deltaTime;
                    transform.position += transform.forward * step;
                }
            }
        }
    }

    IEnumerator SlowDown()
    {
        float t = 1;
        while(t > 0)
        {
            rb.velocity = Vector3.Lerp(Vector3.zero, rb.velocity, t);
            t -= slowDownRate;
            yield return new WaitForSeconds(0.1f);
        }
        stopped = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<EnemyAttributes>() != null)
        {
            var enemy = other.gameObject.GetComponent<EnemyAttributes>();
            enemy.TakeDamage(damage);
            enemy.ApplyKnockback(transform.forward, knockback, 0.5f);
        }
    }
}
