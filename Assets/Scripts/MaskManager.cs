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

    // 协程引用
    private Coroutine hawkingDisableCoroutine;
    private Coroutine einsteinDisableCoroutine;
    private Coroutine newtonDisableCoroutine;

    // 能力变量
    private RelativityBlock currentBlock;
    private float einsteinTimer = 0f;
    private List<GameObject> activeBelt = new List<GameObject>();
    private bool isDrawingBelt = false;

    void Update()
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;

        // --- 按键切换 ---
        if (Input.GetKeyDown(KeyCode.H)) SwitchMask(MaskType.Hawking);
        else if (Input.GetKeyDown(KeyCode.E)) SwitchMask(MaskType.Einstein);
        else if (Input.GetKeyDown(KeyCode.N)) SwitchMask(MaskType.Newton);
        else if (Input.GetKeyDown(KeyCode.O)) SwitchMask(MaskType.Normal); // 触发重置

        

        // 1. 爱因斯坦输入 (只有戴着面具时)
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
        
        if (isEinsteinActive && currentBlock != null)
        {
            ApplyRelativityPhysics();
        }
    }

    void SwitchMask(MaskType newMask)
    {
        if (currentMask == newMask) return;

        //  切换到normal的时候会把其他能力都切换掉
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
        UpdateAnimator(newMask);

        currentMask = newMask;
        lastSwitchTime = Time.time;
    }

    // 
    void ResetAllAbilities()
    {
        // 1. 
        StopAllCoroutines();

        // 2. 
        isHawkingActive = false;
        isEinsteinActive = false;
        isNewtonActive = false;

        // 3. 
        ClearBelt();
        isDrawingBelt = false;

        // 4. 
        DeselectBlock();
        einsteinTimer = 0f;

        // 5. 重置数值
        ApplyStats();
    }

  
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
        Debug.Log("牛顿残留结束，传送带消失");
    }

    
    void ApplyStats()
    {
        float speed = 1f;
        float jump = 1f;
        if (isHawkingActive) { speed *= hawkingSpeedMult; jump *= hawkingJumpMult; }
        player.speedMultiplier = speed;
        player.jumpMultiplier = jump;
    }

    void ApplyRelativityPhysics()
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Rigidbody2D blockRb = currentBlock.GetComponent<Rigidbody2D>();

        Vector2 playerVel = playerRb.linearVelocity;
        Vector2 beltVel = currentBlock.conveyorVelocity;

        // 方块速度：X轴相对论 + 传送带；Y轴只受传送带或强制0
        float targetBlockX = -playerVel.x + beltVel.x;
        float targetBlockY = 0f;

        // 检查玩家是否落地 (解决自动上飘Bug)
        if (player.isGrounded) targetBlockY = beltVel.y; // 地上：只受传送带
        else targetBlockY = -playerVel.y + beltVel.y;    // 空中：相对论 + 传送带

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