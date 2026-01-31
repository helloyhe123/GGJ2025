using UnityEngine;

public class RelativityBlock : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color originalColor;
    private Rigidbody2D rb;
    private float originalGravity;

   
    [HideInInspector] public Vector2 conveyorVelocity;

    
    [HideInInspector] public bool isSelectedByEinstein = false;

    // 这一帧是有传送带在推我？
    private bool isPushedThisFrame = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (sr) originalColor = sr.color;
        if (rb) originalGravity = rb.gravityScale;
    }

    
    public void SetConveyorVelocity(Vector2 velocity)
    {
        conveyorVelocity = velocity;
        isPushedThisFrame = true; // 标记：这一帧有人推我！
    }

    void FixedUpdate()
    {
        
        if (!isPushedThisFrame)
        {
            conveyorVelocity = Vector2.zero;
        }

        
        isPushedThisFrame = false;
    }

    public void OnSelect()
    {
        isSelectedByEinstein = true;
        if (sr) sr.color = Color.cyan;
        if (rb) { rb.gravityScale = 0; rb.linearVelocity = Vector2.zero; }
    }

    public void OnDeselect()
    {
        isSelectedByEinstein = false;
        

        if (sr) sr.color = originalColor;
        if (rb) { rb.gravityScale = originalGravity; rb.linearVelocity = Vector2.zero; }
    }
}