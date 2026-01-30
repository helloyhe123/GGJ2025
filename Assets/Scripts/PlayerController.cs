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

    // --- 隐藏变量，由 MaskManager 控制 ---
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float jumpMultiplier = 1f;

    // --- 组件引用 ---
    private Rigidbody2D rb;
    private Animator anim; // 引用动画控制器
    private bool isGrounded;
    private float moveInput;
    private bool jumpRequest;
    private bool facingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 获取子物体上的 Animator (假设 Visuals 是子物体)
        // 如果 Animator 在父物体上，就用 GetComponent<Animator>()
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 1. 输入
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 跳跃请求
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequest = true;
        }

        // 3. 翻转角色朝向
        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        // 4. --- 更新动画参数 (核心) ---
        // 告诉动画机：水平速度的绝对值 (0就是站立，>0就是跑)
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        // 告诉动画机：是否在地上 (决定是跳跃动画还是落地动画)
        anim.SetBool("IsGrounded", isGrounded);
        // 可选：告诉动画机垂直速度 (用于区分起跳和下落)
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    void FixedUpdate()
    {
        // 1. 地面检测
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, checkRadius, groundLayer);

        // 2. 移动 (应用倍率)
        float finalSpeed = baseMoveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(moveInput * finalSpeed, rb.linearVelocity.y);

        // 3. 跳跃 (应用倍率)
        if (jumpRequest)
        {
            float finalJump = baseJumpForce * jumpMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, finalJump);
            jumpRequest = false;
        }
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