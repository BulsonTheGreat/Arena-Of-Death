using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    public float speed = 15;
    public float slowDownRate = 0.01f;
    public float destroyDelay = 3;

    public int damage = 7;
    public float knockback = 5;
    [SerializeField] GameObject vfx;

    private Rigidbody rb;
    private bool stopped;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        if (GetComponent<Rigidbody>() != null)
        {
            rb = GetComponent<Rigidbody>();
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
            rb.MovePosition(transform.position + transform.forward * speed * Time.fixedDeltaTime);
        }
    }

    IEnumerator SlowDown()
    {
        float t = 1;
        while (t > 0)
        {
            rb.velocity = Vector3.Lerp(Vector3.zero, rb.velocity, t);
            t -= slowDownRate;
            yield return new WaitForSeconds(0.1f);
        }
        stopped = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ThirdPersonController>() != null)
        {
            var player = other.GetComponent<ThirdPersonController>();
            if(!player.isInvulnerable)
            {
                player.TakeDamage(damage);
                player.ApplyKnockback(transform.forward, knockback, 0.5f);
                Destroy(gameObject);
            }
        }
    }
}

