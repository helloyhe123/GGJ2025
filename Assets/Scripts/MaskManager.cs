using UnityEngine;
using System.Collections;

public class MaskManager : MonoBehaviour
{
    // 1. 确保枚举里有 Normal (普通人)
    public enum MaskType { Normal, Hawking, Newton, Einstein, Schrodinger }

    [Header("引用")]
    public PlayerController player;
    public Animator playerAnimator;

    [Header("动画控制器资源")]
    public RuntimeAnimatorController normalAnimController; // 普通人的动画控制器
    public AnimatorOverrideController hawkingAnimController; // 霍金的 Override Controller
    // 其他科学家的 Controller...

    [Header("霍金能力参数")]
    public float hawkingSpeedMult = 1.8f;
    public float hawkingJumpMult = 0.8f;

    [Header("设置")]
    public float switchCooldown = 0.5f;
    public float residualTime = 4.0f;

    private MaskType currentMask = MaskType.Normal;
    private float lastSwitchTime = -99f;

    // 状态标记
    private bool isHawkingActive = false;
    private Coroutine hawkingDisableCoroutine;

    void Update()
    {
        // 检查冷却
        if (Time.time - lastSwitchTime < switchCooldown) return;

        // --- 监听按键 ---
        if (Input.GetKeyDown(KeyCode.H)) SwitchMask(MaskType.Hawking);

        // ★★★ 新增：按 O 键变回普通人 ★★★
        else if (Input.GetKeyDown(KeyCode.O)) SwitchMask(MaskType.Normal);

        // 其他按键...
        // else if (Input.GetKeyDown(KeyCode.N)) SwitchMask(MaskType.Newton);
    }

    void SwitchMask(MaskType newMask)
    {
        if (currentMask == newMask) return; // 如果已经是普通人，按O就不重复触发

        Debug.Log($"切换面具: {currentMask} -> {newMask}");

        // 1. 处理旧面具残留 (如果你是从霍金切回普通人，霍金加速会残留4秒)
        DeactivateMaskDelayed(currentMask);

        // 2. 激活新面具 (普通人没有特殊数值，所以什么都不用做，只要把旧的残留处理好就行)
        ActivateMaskImmediate(newMask);

        // 3. 切换动画/外观 (立刻变回普通人走路的样子)
        UpdateAnimator(newMask);

        currentMask = newMask;
        lastSwitchTime = Time.time;
    }

    void UpdateAnimator(MaskType type)
    {
        switch (type)
        {
            case MaskType.Normal:
                // ★★★ 变回普通人时，使用基础控制器 ★★★
                if (normalAnimController != null)
                    playerAnimator.runtimeAnimatorController = normalAnimController;
                break;

            case MaskType.Hawking:
                if (hawkingAnimController != null)
                    playerAnimator.runtimeAnimatorController = hawkingAnimController;
                break;
        }
    }

    void ActivateMaskImmediate(MaskType type)
    {
        switch (type)
        {
            case MaskType.Normal:
                // 普通人没有任何特殊能力要激活
                // 这里什么都不用写，逻辑会自动回到 ApplyStats 的默认值 1.0
                break;

            case MaskType.Hawking:
                // 如果正在残留倒计时，取消倒计时，视为“续费”
                if (hawkingDisableCoroutine != null) StopCoroutine(hawkingDisableCoroutine);
                isHawkingActive = true;
                ApplyStats();
                break;
        }
    }

    void DeactivateMaskDelayed(MaskType type)
    {
        switch (type)
        {
            case MaskType.Normal:
                // 普通人没有“残留能力”，切走就切走了
                break;

            case MaskType.Hawking:
                // 霍金切走时，开启4秒残留
                if (hawkingDisableCoroutine != null) StopCoroutine(hawkingDisableCoroutine);
                hawkingDisableCoroutine = StartCoroutine(DisableHawkingRoutine());
                break;
        }
    }

    IEnumerator DisableHawkingRoutine()
    {
        // 保持霍金能力开启 4秒
        yield return new WaitForSeconds(residualTime);

        // 4秒后关闭
        isHawkingActive = false;
        ApplyStats(); // 更新数值
        Debug.Log("霍金残留效果结束");
    }

    void ApplyStats()
    {
        // 每次计算前先重置为 1.0 (普通人状态)
        float speed = 1f;
        float jump = 1f;

        // 如果霍金还激活着 (无论是戴着面具，还是处于残留期)
        if (isHawkingActive)
        {
            speed *= hawkingSpeedMult;
            jump *= hawkingJumpMult;
        }

        // 应用到主角脚本
        player.speedMultiplier = speed;
        player.jumpMultiplier = jump;
    }
}