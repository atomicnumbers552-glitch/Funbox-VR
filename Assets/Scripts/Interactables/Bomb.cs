using System.Collections;
using UnityEngine;

public class Bomb : Interactable
{
    public float fuseDuration = 3.0f;
    private bool isLit = false;

    [Header("Visual Effects")]
    public GameObject explosionPrefab;

    private MeshRenderer[] childRenderers;
    private Color originalColor;

    void Start()
    {
        childRenderers = GetComponentsInChildren<MeshRenderer>();
        if (childRenderers.Length > 0)
        {
            originalColor = childRenderers[0].material.color;
        }
    }

    public void LightFuse()
    {
        if (isLit) return;
        isLit = true;
        StartCoroutine(FuseCountdown());
    }

    IEnumerator FuseCountdown()
    {
        float elapsed = 0;
        bool toggleColor = false;

        // Flash red and black as  fuse ticks down
        while (elapsed < fuseDuration)
        {
            Color flashColor = toggleColor ? Color.red : originalColor;
            SetBombColor(flashColor);
            
            toggleColor = !toggleColor;
            
            float flashSpeed = Mathf.Lerp(0.4f, 0.05f, elapsed / fuseDuration);
            yield return new WaitForSeconds(flashSpeed);
            
            elapsed += flashSpeed;
        }

        Explode();
    }

    void SetBombColor(Color newColor)
    {
        foreach (MeshRenderer renderer in childRenderers)
        {
            renderer.material.color = newColor;
        }
    }

    void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, 10f);
        System.Collections.Generic.HashSet<Rigidbody> pushedRigidbodies = new System.Collections.Generic.HashSet<Rigidbody>();

        foreach (Collider nearby in colliders)
        {
            // Standard object push
            Rigidbody rb = nearby.GetComponentInParent<Rigidbody>();
            if (rb != null && !pushedRigidbodies.Contains(rb) && rb.gameObject != this.gameObject)
            {
                rb.AddExplosionForce(1500f, transform.position, 10f);
                pushedRigidbodies.Add(rb);
            }

            if (nearby.CompareTag("Player"))
            {
                PlayerMotor motor = nearby.GetComponentInParent<PlayerMotor>();
                if (motor != null)
                {
                    // Find direction from bomb center to player capsule
                    Vector3 blastDir = nearby.transform.position - transform.position;
                    
                    // Trigger knockback function
                    motor.ApplyExplosionForce(blastDir, 25f);

                }

                PlayerHealth health = nearby.GetComponentInParent<PlayerHealth>();
                if(health != null)
                {
                    health.TakeDamage(30);
                }
            }
        }

        Destroy(gameObject);
    }

}
