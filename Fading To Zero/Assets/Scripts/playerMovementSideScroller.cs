using UnityEngine;

public class SideScrollerPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float moveInput;
    private bool isFacingRight = true;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Input
        moveInput = Input.GetAxisRaw("Horizontal");

        // Flip
    if (moveInput > 0 && !isFacingRight)
    {
        isFacingRight = true;
        spriteRenderer.flipX = true;   // berubah dari false ke true
    }
    else if (moveInput < 0 && isFacingRight)
    {
        isFacingRight = false;
        spriteRenderer.flipX = false;  // berubah dari true ke false
    }
        // Update Animator parameter
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }
}