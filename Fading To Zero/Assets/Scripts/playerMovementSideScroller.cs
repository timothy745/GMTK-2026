using UnityEngine;

public class SideScrollerPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float moveInput;
    private float currentSpeed;
    private bool isFacingRight = false;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        currentSpeed = walkSpeed;
    }

    void Update()
    {
        if (MazeManager.IsAnyMazeActive || InventoryUI.IsOpen)
        {
            moveInput = 0f;
            currentSpeed = walkSpeed;
            animator.SetFloat("Speed", 0f);
            return;
        }

        
        moveInput = Input.GetAxis("Horizontal");

        
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        
        if (moveInput > 0 && !isFacingRight)
        {
            isFacingRight = true;
            spriteRenderer.flipX = true;
        }
        else if (moveInput < 0 && isFacingRight)
        {
            isFacingRight = false;
            spriteRenderer.flipX = false;
        }

        
        animator.SetFloat("Speed", Mathf.Abs(moveInput));

        
    }

    void FixedUpdate()
    {
        if (MazeManager.IsAnyMazeActive || InventoryUI.IsOpen)
            return;

        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);
    }
}