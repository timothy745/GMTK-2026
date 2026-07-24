using UnityEngine;

public class PlayerMovementIsometric : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;

    void Update()
    {

        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");


        moveInput = moveInput.normalized;


        if (moveInput.x > 0)
            spriteRenderer.flipX = false;
        else if (moveInput.x < 0)
            spriteRenderer.flipX = true;

        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}