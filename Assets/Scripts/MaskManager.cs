using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MaskManager : MonoBehaviour
{
    public enum MaskType { Normal, Hawking, Newton, Einstein, Schrodinger }

    [Header("=== 核心组件引用 ===")]
    public PlayerController player;
    public Animator playerAnimator; // 拖入 Visuals 物体上的 Animator
    public LineRenderer connectionLine; // 拖入 LinkLine 物体

    [Header("=== 动画控制器 (Assets) ===")]
    // 拖入你刚才创建的 "Player_Animator_Controller"
    public RuntimeAnimatorController normalAnimController;

    // 拖入你刚才创建的 "Hawking_Override"
    public AnimatorOverrideController hawkingAnimController;

    // 如果还没做别的，暂时也可以先拖 Hawking_Override 进去凑数，防止报错
    public AnimatorOverrideController einsteinAnimController;
    public AnimatorOverrideController newtonAnimController;

    [Header("=== 全局设置 ===")]
    public float switchCooldown = 0.5f;
    public float residualTime = 4.0f;

    [Header("=== 霍金 (H) 参数 ===")]
    public float hawkingSpeedMult = 1.8f;
    public float hawkingJumpMult = 0.8f;

    [Header("=== 爱因斯坦 (E) 参数 ===")]
    public float einsteinMaxDuration = 10f;
    public LayerMask blockLayer;

    [Header("=== 牛顿 (N) 参数 ===")]
    public GameObject conveyorPrefab;
    public int maxBeltLength = 5;
    public float drawRange = 2.5f;

    // --- 内部状态变量 ---
    private MaskType currentMask = MaskType.Normal;
    private float lastSwitchTime = -99f;

    // 状态标记
    private bool isHawkingActive = false;
    private bool isEinsteinActive = false;
    private bool isNewtonActive = false;

    // 协程引用
    private Coroutine hawkingDisableCoroutine;
    private Coroutine einsteinDisableCoroutine;
    private Coroutine newtonDisableCoroutine;

    // 各能力专用变量
    private RelativityBlock currentBlock;
    private float einsteinTimer = 0f;
    private List<GameObject> activeBelt = new List<GameObject>();
    private bool isDrawingBelt = false;

    // 用于计算真实速度
    private Vector2 lastPlayerPos;

    void Start()
    {
        if (player != null) lastPlayerPos = player.transform.position;
    }

    void Update()
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;

        // --- 按键切换 ---
        if (Input.GetKeyDown(KeyCode.H)) SwitchMask(MaskType.Hawking);
        else if (Input.GetKeyDown(KeyCode.E)) SwitchMask(MaskType.Einstein);
        else if (Input.GetKeyDown(KeyCode.N)) SwitchMask(MaskType.Newton);
        else if (Input.GetKeyDown(KeyCode.O)) SwitchMask(MaskType.Normal); // 重置键

        // --- 逻辑循环 ---

        // 1. 爱因斯坦输入 (只有戴着面具时)
        if (currentMask == MaskType.Einstein)
        {
            HandleEinsteinInput();
            HandleEinsteinDeathTimer();
        }

        // 爱因斯坦连线 (只要能力激活)
        if (isEinsteinActive) UpdateConnectionLine();
        else if (connectionLine) connectionLine.enabled = false;

        // 2. 牛顿绘制 (只有戴着面具时)
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

        // ★ 记录当前位置，供下一帧计算真实速度
        if (player != null) lastPlayerPos = player.GetComponent<Rigidbody2D>().position;
    }

    // ==========================================
    //           核心：切换与重置
    // ==========================================
    void SwitchMask(MaskType newMask)
    {
        if (currentMask == newMask) return;

        // ★ 如果目标是 Normal，强制重置一切
        if (newMask == MaskType.Normal)
        {
            Debug.Log(">>> 强制重置所有能力 (按O) <<<");
            ResetAllAbilities();
            UpdateAnimator(MaskType.Normal);
            currentMask = MaskType.Normal;
            lastSwitchTime = Time.time;
            return;
        }

        // 正常的切换逻辑 (带残留)
        DeactivateMaskDelayed(currentMask);
        ActivateMaskImmediate(newMask);
        UpdateAnimator(newMask); // 这里会切换你的皮肤

        currentMask = newMask;
        lastSwitchTime = Time.time;
    }

    // ★ 暴力清除函数
    void ResetAllAbilities()
    {
        StopAllCoroutines();

        isHawkingActive = false;
        isEinsteinActive = false;
        isNewtonActive = false;

        ClearBelt();
        isDrawingBelt = false;

        DeselectBlock();
        einsteinTimer = 0f;

        ApplyStats();
    }

    // ==========================================
    //           激活与残留
    // ==========================================
    void ActivateMaskImmediate(MaskType type)
    {
        switch (type)
        {
            case MaskType.Hawking:
                if (hawkingDisableCoroutine != null) StopCoroutine(hawkingDisableCoroutine);
                isHawkingActive = true;
                ApplyStats();
                break;
            case MaskType.Einstein:
                if (einsteinDisableCoroutine != null) StopCoroutine(einsteinDisableCoroutine);
                isEinsteinActive = true;
                einsteinTimer = 0f;
                break;
            case MaskType.Newton:
                if (newtonDisableCoroutine != null) StopCoroutine(newtonDisableCoroutine);
                isNewtonActive = true;
                break;
        }
    }

    void DeactivateMaskDelayed(MaskType type)
    {
        switch (type)
        {
            case MaskType.Hawking:
                if (hawkingDisableCoroutine != null) StopCoroutine(hawkingDisableCoroutine);
                hawkingDisableCoroutine = StartCoroutine(DisableHawkingRoutine());
                break;
            case MaskType.Einstein:
                if (einsteinDisableCoroutine != null) StopCoroutine(einsteinDisableCoroutine);
                einsteinDisableCoroutine = StartCoroutine(DisableEinsteinRoutine());
                break;
            case MaskType.Newton:
                if (newtonDisableCoroutine != null) StopCoroutine(newtonDisableCoroutine);
                newtonDisableCoroutine = StartCoroutine(DisableNewtonRoutine());
                break;
        }
    }

    // 协程
    IEnumerator DisableHawkingRoutine() { yield return new WaitForSeconds(residualTime); isHawkingActive = false; ApplyStats(); }
    IEnumerator DisableEinsteinRoutine() { yield return new WaitForSeconds(residualTime); isEinsteinActive = false; DeselectBlock(); }
    IEnumerator DisableNewtonRoutine()
    {
        yield return new WaitForSeconds(residualTime);
        isNewtonActive = false;
        ClearBelt();
        Debug.Log("牛顿残留结束");
    }

    // ==========================================
    //           物理与逻辑
    // ==========================================
    void ApplyStats()
    {
        float speed = 1f;
        float jump = 1f;
        if (isHawkingActive) { speed *= hawkingSpeedMult; jump *= hawkingJumpMult; }
        player.speedMultiplier = speed;
        player.jumpMultiplier = jump;
    }

    // ★★★ 核心物理：真实位移相对论 ★★★
    void ApplyRelativityPhysics()
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Rigidbody2D blockRb = currentBlock.GetComponent<Rigidbody2D>();

        Vector2 beltVel = currentBlock.conveyorVelocity;

        // 计算真实速度 (防止撞墙滑步)
        Vector2 currentPlayerPos = playerRb.position;
        Vector2 realPlayerVel = (currentPlayerPos - lastPlayerPos) / Time.fixedDeltaTime;

        // 方块速度：X轴真实相对 + 传送带；Y轴只受传送带(地上)或相对(空中)
        float targetBlockX = -realPlayerVel.x + beltVel.x;
        float targetBlockY = 0f;

        if (player.isGrounded) targetBlockY = beltVel.y; // 地上：方块不乱飘
        else targetBlockY = -realPlayerVel.y + beltVel.y; // 空中：完全相对

        blockRb.linearVelocity = new Vector2(targetBlockX, targetBlockY);

        // 玩家反作用力：只取 X 轴
        player.externalVelocity = new Vector2(-beltVel.x, 0f);
    }

    // --- 爱因斯坦辅助 ---
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

    // --- 牛顿绘制辅助 ---
    void HandleNewtonInput()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); mousePos.z = 0;
        if (Input.GetMouseButtonDown(0) && Vector3.Distance(player.transform.position, mousePos) <= drawRange) StartDrawingBelt(mousePos);
        if (Input.GetMouseButton(0) && isDrawingBelt) ContinueDrawingBelt(mousePos);
        if (Input.GetMouseButtonUp(0)) isDrawingBelt = false;
    }

    void StartDrawingBelt(Vector3 startPos)
    {
        ClearBelt();
        Vector3Int gridPos = Vector3Int.RoundToInt(startPos);
        CreateBeltSegment(gridPos);
        isDrawingBelt = true;
    }

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

    void ClearBelt()
    {
        foreach (var seg in activeBelt) if (seg != null) Destroy(seg);
        activeBelt.Clear();
    }

    // ★★★ 动画切换核心 ★★★
    void UpdateAnimator(MaskType type)
    {
        switch (type)
        {
            case MaskType.Normal:
                if (normalAnimController) playerAnimator.runtimeAnimatorController = normalAnimController;
                break;
            case MaskType.Hawking:
                if (hawkingAnimController) playerAnimator.runtimeAnimatorController = hawkingAnimController;
                break;
            case MaskType.Einstein:
                if (einsteinAnimController) playerAnimator.runtimeAnimatorController = einsteinAnimController;
                break;
            case MaskType.Newton:
                if (newtonAnimController) playerAnimator.runtimeAnimatorController = newtonAnimController;
                break;
        }
    }
}