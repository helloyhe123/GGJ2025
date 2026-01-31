using UnityEngine;

public class CameraRoomSystem : MonoBehaviour
{
    [Header("房间设置")]
    public float roomWidth = 16f;  // 一个房间的宽度
    public float smoothSpeed = 10f; // 移动平滑度

    private Vector3 targetPos;

    void Start()
    {
        // 游戏开始时记下当前位置
        targetPos = transform.position;
    }

    void Update()
    {
        // 平滑移动摄像机到目标位置
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
    }

    // 提供给触发器调用的方法：移动到下一个房间
    public void MoveToNextRoom()
    {
        // 目标 X 轴 + 16，Y 和 Z 不变
        targetPos += new Vector3(roomWidth, 0, 0);
    }
}
