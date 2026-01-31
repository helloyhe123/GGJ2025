using UnityEngine;

public class ConveyorSegment : MonoBehaviour
{
    public float forceStrength = 5f;
    public Vector2 pushDirection = Vector2.right;

    private void OnTriggerStay2D(Collider2D other)
    {
        // 1. 处理相对论方块
        RelativityBlock block = other.GetComponent<RelativityBlock>();

        if (block != null)
        {
            //  情况A：被爱因斯坦控制 
            if (block.isSelectedByEinstein)
            {
                
                block.SetConveyorVelocity(pushDirection * forceStrength);
            }
            //  情况B：自由状态 
            else if (other.attachedRigidbody != null)
            {
                Rigidbody2D rb = other.attachedRigidbody;
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, pushDirection * forceStrength, Time.deltaTime * 10f);
            }
        }
        // 2. 处理玩家 (如果有 Rigidbody)
        else if (other.CompareTag("Player") && other.attachedRigidbody != null)
        {
            other.attachedRigidbody.linearVelocity = Vector2.Lerp(other.attachedRigidbody.linearVelocity, pushDirection * forceStrength, Time.deltaTime * 10f);
        }
    }
}