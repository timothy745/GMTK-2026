using UnityEngine;

public class PlayerMovementIsometric : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
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