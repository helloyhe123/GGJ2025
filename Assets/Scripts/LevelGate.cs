using UnityEngine;

public class LevelGate : MonoBehaviour
{
    [Header("引用")]
    public GameObject blocker; // 拖入子物体 Blocker (那堵墙)

    // 内部状态，防止重复触发
    private bool hasTriggered = false;

    // 当 Trigger 子物体被撞到时
    // 注意：如果是挂在父物体上，子物体的 Trigger 碰撞会传给父物体，前提是父物体有 Rigidbody
    // 但为了简单，建议直接把这个脚本挂在那个 Trigger 子物体上！或者如下操作：

    // === 建议把脚本挂在 LevelGate 父物体，然后把下面代码稍微改一下 ===
    // 或者简单粗暴点：
    // 请把这个脚本挂在 LevelGate 下面的 "Trigger" 子物体上！

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return; // 如果已经触发过，就不再执行

        if (other.CompareTag("Player"))
        {
            Debug.Log("进入下一关！");

            // 1. 让摄像机移动
            CameraRoomSystem cam = Camera.main.GetComponent<CameraRoomSystem>();
            if (cam != null)
            {
                cam.MoveToNextRoom();
            }

            // 2. 封路！(激活空气墙)
            if (blocker != null)
            {
                blocker.SetActive(true);
            }

            // 3. 锁定触发器
            hasTriggered = true;

            // 可选：关掉触发器自己，免得再碰到
            GetComponent<Collider2D>().enabled = false;
        }
    }
}
