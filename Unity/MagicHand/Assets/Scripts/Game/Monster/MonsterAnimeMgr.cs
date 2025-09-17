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
    [SerializeField] private bool enableDebugLog = true;
    
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
        // 可在此处添加状态变化的特殊处理逻辑
        // 例如：播放音效、触发特效等
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
            animator.SetBool("hit", true);
            
            if (enableDebugLog)
            {
                Debug.Log($"[MonsterAnimeMgr] 触发受击效果，hit参数设为true");
            }
        }
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
               $"hit: {isHit}";
    }
    
    #endregion
}