using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    public float speed = 5f;
    public float gravity = -9.8f;
    public float jumpHeight = 1.5f;

    // NEW variables to handle bomb blasts smoothly
    private Vector3 knockbackForce = Vector3.zero;
    public float knockbackDecay = 5f; // How fast the push slows down

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        
        // 1. Calculate normal walking movement
        Vector3 finalMove = transform.TransformDirection(moveDirection) * speed;

        // 2. NEW: Add horizontal knockback to our movement
        finalMove += knockbackForce;

        // 3. Apply the final walking + knockback movement to the controller
        controller.Move(finalMove * Time.deltaTime);

        // 4. Handle gravity calculation
        playerVelocity.y += gravity * Time.deltaTime;
        if(isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        controller.Move(playerVelocity * Time.deltaTime);

        // 5. NEW: Smoothly fade out the knockback over time so the player regains control
        knockbackForce = Vector3.Lerp(knockbackForce, Vector3.zero, knockbackDecay * Time.deltaTime);
    }

    public void Jump()
    {
        if(isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }

    // NEW: Public function that the bomb script can trigger
    public void ApplyExplosionForce(Vector3 blastDirection, float force)
    {
        // Ensure the direction vector is clean
        blastDirection.Normalize();

        // Push the player horizontally away from the explosion
        knockbackForce = new Vector3(blastDirection.x, 0f, blastDirection.z) * force;

        // Pop the player up into the air slightly for a better visual effect
        playerVelocity.y = force * 0.4f; 
    }
}
