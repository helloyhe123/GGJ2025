using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("基础属性")]
    public float baseMoveSpeed = 5f;
    public float baseJumpForce = 12f;

    [Header("地面检测")]
    public Transform groundCheckPoint;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;

    // --- 由 MaskManager 控制的倍率 ---
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float jumpMultiplier = 1f;

   
    [HideInInspector] public Vector2 externalVelocity;

    
    private Rigidbody2D rb;
    private Animator anim;
    public bool isGrounded;
    private float moveInput;
    private bool jumpRequest;
    private bool facingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>(); // 或者是 GetComponent<Animator>()
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequest = true;
        }

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        if (anim)
        {
            anim.SetFloat("Speed", Mathf.Abs(moveInput));
            anim.SetBool("IsGrounded", isGrounded);
            anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }
    }

    void FixedUpdate()
    {
        
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, checkRadius, groundLayer);

        
        float finalSpeed = baseMoveSpeed * speedMultiplier;
        float xVelocity = moveInput * finalSpeed;

        
        float yVelocity = rb.linearVelocity.y;

        
        Vector2 finalExternal = externalVelocity;

        
        if (jumpRequest)
        {
            float finalJump = baseJumpForce * jumpMultiplier;
            
            yVelocity = finalJump;
            jumpRequest = false;
        }

        
        rb.linearVelocity = new Vector2(xVelocity + finalExternal.x, yVelocity);

        
        externalVelocity = Vector2.zero;
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, checkRadius);
        }
    }
}