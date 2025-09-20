using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 怪物动画管理器
/// 负责管理怪物的动画状态和参数控制
/// </summary>
public class MonsterAnimeMgr : MonoBehaviour
{
    [Header("动画组件")]
    [SerializeField] private Animator animator;
    
    [Header("调试配置")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 动画参数名称常量
    private const string PARAM_ALIVE = "alive";
    private const string PARAM_IN_RANGE = "inRange";
    private const string PARAM_DIZZY = "dizzy";
    private const string PARAM_HIT = "hit";
    private const string PARAM_ATTACK = "attack";
    
    // 动画状态名称常量
    private const string STATE_WALK = "walk";
    private const string STATE_ATTACK = "attack";
    private const string STATE_STRUCK = "struck";
    private const string STATE_IDLE = "idle";
    private const string STATE_DIE = "die";
    
    // 动画状态枚举
    public enum AnimationState
    {
        Walk,
        Attack,
        Struck,
        Idle,
        Die,
        Unknown
    }
    
    // 当前动画状态
    private AnimationState currentState = AnimationState.Unknown;
    
    // 动画参数状态
    private bool isAlive = true;
    private bool isInRange = false;
    private bool isDizzy = false;
    private bool isHit = false;
    
    // 怪物运行时数据引用
    private MonsterRuntimeData runtimeData;
    
    // 移动相关配置
    [Header("移动配置")]
    [SerializeField] private float speedMultiplier = 1.0f; // 速度倍数
    
    // 移动状态
    private bool isMoving = false;
    private MonsterConfig monsterConfig;
    
    // 攻击相关
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private PlayerHealthManager[] playerHealthManagers;
    private Coroutine attackCoroutine;
    
    // 受击重置协程
    private Coroutine hitResetCoroutine;
    
    void Awake()
    {
        // 获取Animator组件
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // 获取怪物运行时数据
        runtimeData = GetComponent<MonsterRuntimeData>();
        
        // 获取怪物配置
        if (runtimeData != null)
        {
            monsterConfig = runtimeData.GetConfig();
        }
        
        if (animator == null)
        {
            Debug.LogError($"[MonsterAnimeMgr] 未找到Animator组件: {gameObject.name}");
            return;
        }
        
        // 初始化动画参数
        InitializeAnimationParameters();
    }
    
    void Start()
    {
        // 订阅怪物死亡事件
        if (runtimeData != null)
        {
            MonsterEventManager.OnMonsterDeathDetected += OnMonsterDeath;
        }
        
        // 查找角色生命值管理器
        FindPlayerHealthManagers();
    }
    
    void Update()
    {
        // 检查游戏是否暂停
        if (GameStateManager.Instance.IsPaused)
        {
            return; // 暂停时不更新动画和移动逻辑
        }
        
        // 监控动画状态变化
        MonitorAnimationState();
        
        // 处理移动逻辑
        HandleMovement();
    }
    
    /// <summary>
    /// 初始化动画参数
    /// </summary>
    private void InitializeAnimationParameters()
    {
        if (animator == null) return;
        
        // 设置默认参数值
        SetAlive(true);
        SetInRange(false);
        SetDizzy(false);
        SetHit(false);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 动画参数初始化完成: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 重置动画状态到初始状态（用于对象池重用）
    /// </summary>
    public void ResetAnimationState()
    {
        if (animator == null) return;
        
        // 停止所有攻击相关的协程
        StopAttacking();
        
        // 停止受击重置协程
        if (hitResetCoroutine != null)
        {
            StopCoroutine(hitResetCoroutine);
            hitResetCoroutine = null;
        }
        
        // 重置所有动画参数到初始状态
        SetAlive(true);
        SetInRange(false);
        SetDizzy(false);
        SetHit(false);
        
        // 重置内部状态
        isMoving = false;
        isAttacking = false;
        currentState = AnimationState.Unknown;
        
        // 重置运行时数据状态
        if (runtimeData != null)
        {
            runtimeData.isAttacking = false;
            runtimeData.isMoving = false;
        }
        
        // 重置动画状态机到Entry状态
        if (animator.isActiveAndEnabled)
        {
            // 重新启用Animator来触发Entry状态
            animator.enabled = false;
            animator.enabled = true;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 动画状态已重置: {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 监控动画状态变化
    /// </summary>
    private void MonitorAnimationState()
    {
        if (animator == null) return;
        
        AnimationState newState = GetCurrentAnimationState();
        if (newState != currentState)
        {
            AnimationState previousState = currentState;
            currentState = newState;
            
            // 当进入struck状态时，重置hit参数
            if (newState == AnimationState.Struck && animator != null)
            {
                animator.SetBool("hit", false);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[MonsterAnimeMgr] 进入struck状态，hit参数重置为false ({gameObject.name})");
                }
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterAnimeMgr] 动画状态变化: {previousState} -> {currentState} ({gameObject.name})");
            }
            
            // 触发状态变化事件（可扩展）
            OnAnimationStateChanged(previousState, currentState);
        }
    }
    
    /// <summary>
    /// 获取当前动画状态
    /// </summary>
    /// <returns>当前动画状态</returns>
    private AnimationState GetCurrentAnimationState()
    {
        if (animator == null) return AnimationState.Unknown;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // 根据状态名称判断当前状态
        if (stateInfo.IsName(STATE_WALK))
            return AnimationState.Walk;
        else if (stateInfo.IsName(STATE_ATTACK))
            return AnimationState.Attack;
        else if (stateInfo.IsName(STATE_STRUCK))
            return AnimationState.Struck;
        else if (stateInfo.IsName(STATE_IDLE))
            return AnimationState.Idle;
        else if (stateInfo.IsName(STATE_DIE))
            return AnimationState.Die;
        
        return AnimationState.Unknown;
    }
    
    /// <summary>
    /// 动画状态变化回调
    /// </summary>
    /// <param name="previousState">之前的状态</param>
    /// <param name="newState">新状态</param>
    private void OnAnimationStateChanged(AnimationState previousState, AnimationState newState)
    {
        // 处理攻击状态变化
        if (newState == AnimationState.Attack && previousState != AnimationState.Attack)
        {
            // 进入攻击状态
            StartAttacking();
        }
        else if (previousState == AnimationState.Attack && newState != AnimationState.Attack)
        {
            // 离开攻击状态
            StopAttacking();
        }
    }
    
    #region 动画参数控制接口
    
    /// <summary>
    /// 设置alive参数
    /// </summary>
    /// <param name="alive">是否存活</param>
    public void SetAlive(bool alive)
    {
        if (animator == null) return;
        
        isAlive = alive;
        animator.SetBool(PARAM_ALIVE, alive);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 设置alive参数: {alive} ({gameObject.name})");
        }
    }
    
    /// <summary>
    /// 设置inRange参数
    /// </summary>
    /// <param name="inRange">是否在攻击范围内</param>
    public void SetInRange(bool inRange)
    {
        if (animator == null) return;
        
        isInRange = inRange;
        animator.SetBool(PARAM_IN_RANGE, inRange);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 设置inRange参数: {inRange} ({gameObject.name})");
        }
    }
    
    /// <summary>
    /// 设置dizzy参数
    /// </summary>
    /// <param name="dizzy">是否眩晕</param>
    public void SetDizzy(bool dizzy)
    {
        if (animator == null) return;
        
        isDizzy = dizzy;
        animator.SetBool(PARAM_DIZZY, dizzy);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 设置dizzy参数: {dizzy} ({gameObject.name})");
        }
    }
    
    /// <summary>
    /// 设置hit参数
    /// </summary>
    /// <param name="hit">是否受击</param>
    public void SetHit(bool hit)
    {
        if (animator == null) return;
        
        isHit = hit;
        animator.SetBool(PARAM_HIT, hit);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 设置hit参数: {hit} ({gameObject.name})");
        }
    }
    
    /// <summary>
    /// 设置attack参数
    /// </summary>
    /// <param name="attack">是否攻击</param>
    public void SetAttack(bool attack)
    {
        if (animator == null) return;
        
        animator.SetBool(PARAM_ATTACK, attack);
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 设置attack参数: {attack} ({gameObject.name})");
        }
    }
    
    /// <summary>
    /// 触发受击效果
    /// </summary>
    public void TriggerHit()
    {
        if (animator != null)
        {
            // 停止之前的重置协程（如果存在）
            if (hitResetCoroutine != null)
            {
                StopCoroutine(hitResetCoroutine);
            }
            
            animator.SetBool("hit", true);
            
            // 启动自动重置协程，0.1秒后重置hit参数
            hitResetCoroutine = StartCoroutine(ResetHitAfterDelay(0.1f));
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterAnimeMgr] 触发受击效果，hit参数设为true，将在0.1秒后自动重置");
            }
        }
    }
    
    /// <summary>
    /// 延迟重置hit参数的协程
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <returns></returns>
    private IEnumerator ResetHitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (animator != null)
        {
            animator.SetBool("hit", false);
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterAnimeMgr] 自动重置hit参数为false ({gameObject.name})");
            }
        }
        
        hitResetCoroutine = null;
    }
    
    #endregion
    
    #region 状态查询接口
    
    /// <summary>
    /// 获取当前动画状态
    /// </summary>
    /// <returns>当前动画状态</returns>
    public AnimationState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 获取当前动画状态名称
    /// </summary>
    /// <returns>状态名称</returns>
    public string GetCurrentStateName()
    {
        return currentState.ToString();
    }
    
    /// <summary>
    /// 检查是否处于指定状态
    /// </summary>
    /// <param name="state">要检查的状态</param>
    /// <returns>是否处于该状态</returns>
    public bool IsInState(AnimationState state)
    {
        return currentState == state;
    }
    
    /// <summary>
    /// 获取alive参数值
    /// </summary>
    /// <returns>alive参数值</returns>
    public bool GetAlive()
    {
        return isAlive;
    }
    
    /// <summary>
    /// 获取inRange参数值
    /// </summary>
    /// <returns>inRange参数值</returns>
    public bool GetInRange()
    {
        return isInRange;
    }
    
    /// <summary>
    /// 获取dizzy参数值
    /// </summary>
    /// <returns>dizzy参数值</returns>
    public bool GetDizzy()
    {
        return isDizzy;
    }
    
    /// <summary>
    /// 获取hit参数值
    /// </summary>
    /// <returns>hit参数值</returns>
    public bool GetHit()
    {
        return isHit;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 处理怪物死亡事件
    /// </summary>
    /// <param name="deadMonsterData">死亡怪物的运行时数据</param>
    private void OnMonsterDeath(MonsterRuntimeData deadMonsterData)
    {
        // 检查是否是当前怪物
        if (runtimeData != null && deadMonsterData == runtimeData)
        {
            SetAlive(false);
            isMoving = false; // 死亡时停止移动
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterAnimeMgr] 怪物死亡，设置alive为false并停止移动: {gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// 处理移动逻辑
    /// </summary>
    private void HandleMovement()
    {
        if (!isAlive || monsterConfig == null) return;
        
        // 检查是否处于walk状态
        bool shouldMove = (currentState == AnimationState.Walk);
        
        if (shouldMove)
        {
            // 计算移动速度
            float moveSpeed = monsterConfig.moveSpeed * speedMultiplier;
            
            // 向前移动（z坐标减小）
            Vector3 movement = Vector3.forward * (-moveSpeed) * Time.deltaTime;
            transform.Translate(movement, Space.World);
            
            // 同步更新RuntimeData中的位置信息
            if (runtimeData != null)
            {
                runtimeData.UpdatePosition(transform.position);
            }
            
            isMoving = true;
            
            // 检查是否到达攻击距离
            if (transform.position.z <= monsterConfig.attackRange)
            {
                // 停止移动并设置攻击参数
                 isMoving = false;
                 SetInRange(true);
                 SetAttack(true);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[MonsterAnimeMgr] 怪物到达攻击距离，停止移动并开始攻击: {gameObject.name}");
                }
            }
        }
        else
        {
            isMoving = false;
        }
    }
    
    #endregion
    
    #region Unity生命周期
    
    void OnDestroy()
    {
        // 取消事件订阅
        MonsterEventManager.OnMonsterDeathDetected -= OnMonsterDeath;
    }
    
    void OnDisable()
    {
        // 重置动画参数
        if (animator != null)
        {
            InitializeAnimationParameters();
        }
    }
    
    #endregion
    
    #region 调试接口
    
    /// <summary>
    /// 获取动画调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        if (animator == null) return "Animator组件未找到";
        
        return $"当前状态: {currentState}\n" +
               $"alive: {isAlive}\n" +
               $"inRange: {isInRange}\n" +
               $"dizzy: {isDizzy}\n" +
               $"hit: {isHit}\n" +
               $"isAttacking: {isAttacking}";
    }
    
    #endregion
    
    #region 攻击逻辑
    
    /// <summary>
    /// 查找场景中的角色生命值管理器
    /// </summary>
    private void FindPlayerHealthManagers()
    {
        // 查找MainUI对象
        GameObject mainUIObj = GameObject.Find("MainUI");
        if (mainUIObj == null)
        {
            Debug.LogWarning("[MonsterAnimeMgr] 未找到MainUI对象，无法获取角色生命值管理器");
            return;
        }
        
        PlayerUIController playerUIController = mainUIObj.GetComponent<PlayerUIController>();
        if (playerUIController != null)
        {
            // 获取两个角色的生命值管理器
            playerHealthManagers = new PlayerHealthManager[2];
            playerHealthManagers[0] = playerUIController.GetPlayerHealthManager(0);
            playerHealthManagers[1] = playerUIController.GetPlayerHealthManager(1);
            
            if (enableDebugLog)
            {
                int validCount = 0;
                for (int i = 0; i < playerHealthManagers.Length; i++)
                {
                    if (playerHealthManagers[i] != null) validCount++;
                }
                Debug.Log($"[MonsterAnimeMgr] 找到 {validCount} 个有效的角色生命值管理器");
            }
        }
        else
        {
            Debug.LogWarning("[MonsterAnimeMgr] MainUI上未找到PlayerUIController组件");
        }
    }
    
    /// <summary>
    /// 开始攻击
    /// </summary>
    private void StartAttacking()
    {
        if (isAttacking || monsterConfig == null || !runtimeData.isAlive)
            return;
            
        isAttacking = true;
        runtimeData.isAttacking = true;
        
        // 启动攻击协程
        attackCoroutine = StartCoroutine(AttackCoroutine());
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 怪物 {runtimeData.uniqueNumber} 开始攻击，攻击间隔: {monsterConfig.attackInterval}s，伤害: {monsterConfig.attackDamage}");
        }
    }
    
    /// <summary>
    /// 停止攻击
    /// </summary>
    private void StopAttacking()
    {
        if (!isAttacking)
            return;
            
        isAttacking = false;
        if (runtimeData != null)
        {
            runtimeData.isAttacking = false;
        }
        
        // 停止攻击协程
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 怪物 {runtimeData?.uniqueNumber} 停止攻击");
        }
    }
    
    /// <summary>
    /// 攻击协程
    /// </summary>
    private System.Collections.IEnumerator AttackCoroutine()
    {
        while (isAttacking && runtimeData != null && runtimeData.isAlive && currentState == AnimationState.Attack)
        {
            // 执行一次攻击
            PerformAttack();
            
            // 等待攻击间隔
            yield return new WaitForSeconds(monsterConfig.attackInterval);
        }
    }
    
    /// <summary>
    /// 执行攻击
    /// </summary>
    private void PerformAttack()
    {
        if (monsterConfig == null || PlayerManager.Instance == null)
            return;
            
        // 使用PlayerManager的攻击系统，基于aiType执行不同攻击逻辑
        string aiType = monsterConfig.aiType.ToString();
        int damage = monsterConfig.attackDamage;
        Vector3 attackPosition = transform.position;
        string monsterName = monsterConfig.name;
        
        // 调用玩家管理器的攻击方法
        PlayerManager.Instance.MonsterAttackPlayers(attackPosition, damage, aiType, monsterName);
        
        // 记录攻击时间
        lastAttackTime = Time.time;
        
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAnimeMgr] 怪物 {runtimeData.uniqueNumber}({monsterName}) 执行{aiType}类型攻击，伤害: {damage}");
        }
    }
    
    #endregion
}