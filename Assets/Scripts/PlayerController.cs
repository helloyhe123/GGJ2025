using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("参数设置")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("地面检测")]
    public Transform groundCheckPoint; // 我们等会要创建这个点
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;      // 告诉代码什么是地面

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool jumpRequest;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. 获取输入 (使用 GetAxisRaw 获得 0, 1, -1 的硬直输入，手感更脆)
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 检测跳跃输入
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequest = true;
        }

        // 3. 简单的面朝向翻转 (给美术做准备)
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    // 所有的物理操作必须在 FixedUpdate 里做
    void FixedUpdate()
    {
        // A. 地面检测
        // 在脚底下画一个小圆圈，看看有没有碰到 Ground 层
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, checkRadius, groundLayer);

        // B. 左右移动
        // 这里保留原本的 Y 轴速度，只改变 X 轴
        // 注意：Unity 6 建议使用 linearVelocity，旧版是用 velocity
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // C. 执行跳跃
        if (jumpRequest)
        {
            // 给一个向上的瞬时力
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // 直接修改速度比AddForce更精准控制高度
            jumpRequest = false; // 重置请求
        }
    }

    // 在编辑器里画出辅助线，方便你看地面检测范围
    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, checkRadius);
        }
    }
}