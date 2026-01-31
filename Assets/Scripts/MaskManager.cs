using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MaskManager : MonoBehaviour
{
    public enum MaskType { Normal, Hawking, Newton, Einstein, Schrodinger }

    [Header("=== 核心组件 ===")]
    public PlayerController player;
    public Animator playerAnimator;
    public LineRenderer connectionLine;

    [Header("=== 动画控制器 ===")]
    public RuntimeAnimatorController normalAnimController;
    public AnimatorOverrideController hawkingAnimController;
    public AnimatorOverrideController einsteinAnimController;
    public AnimatorOverrideController newtonAnimController;

    [Header("=== 设置 ===")]
    public float switchCooldown = 0.5f;
    public float residualTime = 4.0f;

    [Header("=== 霍金 (H) ===")]
    public float hawkingSpeedMult = 1.8f;
    public float hawkingJumpMult = 0.8f;

    [Header("=== 爱因斯坦 (E) ===")]
    public float einsteinMaxDuration = 10f;
    public LayerMask blockLayer;

    [Header("=== 牛顿 (N) ===")]
    public GameObject conveyorPrefab;
    public int maxBeltLength = 5;
    public float drawRange = 2.5f;

    // --- 内部状态 ---
    private MaskType currentMask = MaskType.Normal;
    private float lastSwitchTime = -99f;

    // 状态标记
    private bool isHawkingActive = false;
    private bool isEinsteinActive = false;
    private bool isNewtonActive = false;

    // 协程
    private Coroutine hawkingDisableCoroutine;
    private Coroutine einsteinDisableCoroutine;
    private Coroutine newtonDisableCoroutine;

    // 能力变量
    private RelativityBlock currentBlock;
    private float einsteinTimer = 0f;
    private List<GameObject> activeBelt = new List<GameObject>();
    private bool isDrawingBelt = false;

    // ★★★ 新增：用于计算玩家真实速度的变量 ★★★
    private Vector2 lastPlayerPos;

    void Start()
    {
        // 初始化上一帧位置
        if (player != null) lastPlayerPos = player.transform.position;
    }

    void Update()
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;

        // 按键切换
        if (Input.GetKeyDown(KeyCode.H)) SwitchMask(MaskType.Hawking);
        else if (Input.GetKeyDown(KeyCode.E)) SwitchMask(MaskType.Einstein);
        else if (Input.GetKeyDown(KeyCode.N)) SwitchMask(MaskType.Newton);
        else if (Input.GetKeyDown(KeyCode.O)) SwitchMask(MaskType.Normal);

        // 逻辑循环
        if (currentMask == MaskType.Einstein)
        {
            HandleEinsteinInput();
            HandleEinsteinDeathTimer();
        }

        if (isEinsteinActive) UpdateConnectionLine();
        else if (connectionLine) connectionLine.enabled = false;

        if (currentMask == MaskType.Newton)
        {
            HandleNewtonInput();
        }
    }

    void FixedUpdate()
    {
        // 3. 爱因斯坦物理
        if (isEinsteinActive && currentBlock != null)
        {
            ApplyRelativityPhysics();
        }

        // ★★★ 每一帧结束时，记录当前位置，供下一帧计算使用 ★★★
        if (player != null) lastPlayerPos = player.GetComponent<Rigidbody2D>().position;
    }

    // ... (SwitchMask, Reset, Activate, Deactivate 代码与之前完全一致，此处省略以节省篇幅，请保留原样) ...
    // 为了方便你复制，我在下面把核心修改的 ApplyRelativityPhysics 完整写出来
    // 其他部分 (SwitchMask 到 HandleNewtonInput) 请保持上一版不变

    // ================== 这里只贴出修改过的函数 ==================

    void SwitchMask(MaskType newMask)
    {
        if (currentMask == newMask) return;
        if (newMask == MaskType.Normal)
        {
            ResetAllAbilities();
            UpdateAnimator(MaskType.Normal);
            currentMask = MaskType.Normal;
            lastSwitchTime = Time.time;
            return;
        }
        DeactivateMaskDelayed(currentMask);
        ActivateMaskImmediate(newMask);
        UpdateAnimator(newMask);
        currentMask = newMask;
        lastSwitchTime = Time.time;
    }

    void ResetAllAbilities()
    {
        StopAllCoroutines();
        isHawkingActive = false; isEinsteinActive = false; isNewtonActive = false;
        ClearBelt(); isDrawingBelt = false;
        DeselectBlock(); einsteinTimer = 0f;
        ApplyStats();
    }

    void ActivateMaskImmediate(MaskType type)
    {
        switch (type)
        {
            case MaskType.Hawking: if (hawkingDisableCoroutine != null) StopCoroutine(hawkingDisableCoroutine); isHawkingActive = true; ApplyStats(); break;
            case MaskType.Einstein: if (einsteinDisableCoroutine != null) StopCoroutine(einsteinDisableCoroutine); isEinsteinActive = true; einsteinTimer = 0f; break;
            case MaskType.Newton: if (newtonDisableCoroutine != null) StopCoroutine(newtonDisableCoroutine); isNewtonActive = true; break;
        }
    }

    void DeactivateMaskDelayed(MaskType type)
    {
        switch (type)
        {
            case MaskType.Hawking: if (hawkingDisableCoroutine != null) StopCoroutine(hawkingDisableCoroutine); hawkingDisableCoroutine = StartCoroutine(DisableHawkingRoutine()); break;
            case MaskType.Einstein: if (einsteinDisableCoroutine != null) StopCoroutine(einsteinDisableCoroutine); einsteinDisableCoroutine = StartCoroutine(DisableEinsteinRoutine()); break;
            case MaskType.Newton: if (newtonDisableCoroutine != null) StopCoroutine(newtonDisableCoroutine); newtonDisableCoroutine = StartCoroutine(DisableNewtonRoutine()); break;
        }
    }

    IEnumerator DisableHawkingRoutine() { yield return new WaitForSeconds(residualTime); isHawkingActive = false; ApplyStats(); }
    IEnumerator DisableEinsteinRoutine() { yield return new WaitForSeconds(residualTime); isEinsteinActive = false; DeselectBlock(); }
    IEnumerator DisableNewtonRoutine() { yield return new WaitForSeconds(residualTime); isNewtonActive = false; ClearBelt(); }

    void ApplyStats()
    {
        float speed = 1f; float jump = 1f;
        if (isHawkingActive) { speed *= hawkingSpeedMult; jump *= hawkingJumpMult; }
        player.speedMultiplier = speed; player.jumpMultiplier = jump;
    }

    // ★★★★★ 核心修复：基于真实位移的相对论 ★★★★★
    void ApplyRelativityPhysics()
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Rigidbody2D blockRb = currentBlock.GetComponent<Rigidbody2D>();

        // 1. 获取传送带速度
        Vector2 beltVel = currentBlock.conveyorVelocity;

        // 2. ★ 计算玩家的真实速度 (Real Velocity) ★
        // 也就是：这一帧实际移动了多少距离 / 时间
        // 如果撞墙，currentPos 和 lastPlayerPos 会几乎一样，算出来就是 0
        Vector2 currentPlayerPos = playerRb.position;
        Vector2 realPlayerVel = (currentPlayerPos - lastPlayerPos) / Time.fixedDeltaTime;

        // 3. 计算方块目标速度
        // X轴：用真实速度取反 + 传送带
        float targetBlockX = -realPlayerVel.x + beltVel.x;

        // Y轴：保持之前的逻辑 (地上只受传送带，空中受相对论)
        float targetBlockY = 0f;
        if (player.isGrounded) targetBlockY = beltVel.y;
        else targetBlockY = -realPlayerVel.y + beltVel.y; // 这里也用真实速度，手感更准

        blockRb.linearVelocity = new Vector2(targetBlockX, targetBlockY);

        // 4. 反作用力 (只在X轴)
        player.externalVelocity = new Vector2(-beltVel.x, 0f);
    }

    // ... (HandleEinsteinInput 等辅助函数与之前完全一致) ...
    void HandleEinsteinInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (currentBlock != null) DeselectBlock();
            else
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0.1f, blockLayer);
                if (hit.collider != null)
                {
                    RelativityBlock block = hit.collider.GetComponent<RelativityBlock>();
                    if (block != null) SelectBlock(block);
                }
            }
        }
    }
    void HandleEinsteinDeathTimer()
    {
        einsteinTimer += Time.deltaTime;
        if (einsteinTimer > einsteinMaxDuration) SwitchMask(MaskType.Normal);
    }
    void SelectBlock(RelativityBlock block) { currentBlock = block; currentBlock.OnSelect(); }
    void DeselectBlock() { if (currentBlock != null) { currentBlock.OnDeselect(); currentBlock = null; } }
    void UpdateConnectionLine()
    {
        if (currentBlock != null && connectionLine != null)
        {
            connectionLine.enabled = true;
            connectionLine.SetPosition(0, player.transform.position + Vector3.up * 0.5f);
            connectionLine.SetPosition(1, currentBlock.transform.position);
        }
    }
    void HandleNewtonInput()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); mousePos.z = 0;
        if (Input.GetMouseButtonDown(0) && Vector3.Distance(player.transform.position, mousePos) <= drawRange) StartDrawingBelt(mousePos);
        if (Input.GetMouseButton(0) && isDrawingBelt) ContinueDrawingBelt(mousePos);
        if (Input.GetMouseButtonUp(0)) isDrawingBelt = false;
    }
    void StartDrawingBelt(Vector3 startPos) { ClearBelt(); Vector3Int gridPos = Vector3Int.RoundToInt(startPos); CreateBeltSegment(gridPos); isDrawingBelt = true; }
    void ContinueDrawingBelt(Vector3 currentPos)
    {
        if (activeBelt.Count >= maxBeltLength) return;
        Vector3Int gridPos = Vector3Int.RoundToInt(currentPos);
        GameObject lastSegmentObj = activeBelt[activeBelt.Count - 1];
        Vector3Int lastGridPos = Vector3Int.RoundToInt(lastSegmentObj.transform.position);
        if (gridPos == lastGridPos) return;
        int dist = Mathf.Abs(gridPos.x - lastGridPos.x) + Mathf.Abs(gridPos.y - lastGridPos.y);
        if (dist == 1)
        {
            Vector2 direction = (Vector3)(gridPos - lastGridPos);
            ConveyorSegment lastScript = lastSegmentObj.GetComponent<ConveyorSegment>();
            lastScript.pushDirection = direction;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            lastSegmentObj.transform.rotation = Quaternion.Euler(0, 0, angle);
            CreateBeltSegment(gridPos);
            activeBelt[activeBelt.Count - 1].GetComponent<ConveyorSegment>().pushDirection = direction;
            activeBelt[activeBelt.Count - 1].transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    void CreateBeltSegment(Vector3Int pos)
    {
        if (conveyorPrefab == null) return;
        GameObject newSeg = Instantiate(conveyorPrefab, pos, Quaternion.identity);
        activeBelt.Add(newSeg);
    }
    void ClearBelt() { foreach (var seg in activeBelt) if (seg != null) Destroy(seg); activeBelt.Clear(); }
    void UpdateAnimator(MaskType type)
    {
        switch (type)
        {
            case MaskType.Normal: if (normalAnimController) playerAnimator.runtimeAnimatorController = normalAnimController; break;
            case MaskType.Hawking: if (hawkingAnimController) playerAnimator.runtimeAnimatorController = hawkingAnimController; break;
            case MaskType.Einstein: if (einsteinAnimController) playerAnimator.runtimeAnimatorController = einsteinAnimController; break;
            case MaskType.Newton: if (newtonAnimController) playerAnimator.runtimeAnimatorController = newtonAnimController; break;
        }
    }
}