using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển dino: 4 hướng (mặc định) hoặc 8 hướng, dash có cooldown + i-frame,
/// input buffer và ưu tiên trục để điều khiển "mượt".
/// Input được định nghĩa bằng code nên không cần Input Actions asset.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public enum MoveMode { FourWay, EightWay }

    [Header("Di chuyển")]
    public MoveMode moveMode = MoveMode.FourWay;
    public float moveSpeed = 7.5f;
    public float acceleration = 70f;
    [Range(0.05f, 0.5f)] public float inputDeadzone = 0.25f;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 0.55f;
    [Tooltip("Bất tử thêm một chút sau khi dash kết thúc (cảm giác 'né sát' dễ chịu hơn).")]
    public float dashInvulnerableTail = 0.05f;
    [Tooltip("Bấm dash sớm tối đa bao lâu trước khi hết cooldown vẫn được tính.")]
    public float inputBufferTime = 0.1f;

    [Header("Tham chiếu")]
    public Transform visual;
    public float bodyRadius = 0.45f;

    public bool IsDashing { get; private set; }
    public float DashReady01 => cooldownTimer <= 0f ? 1f : 1f - Mathf.Clamp01(cooldownTimer / dashCooldown);

    Rigidbody2D rb;
    PlayerHealth health;
    InputAction moveAction, dashAction, toggleModeAction;

    Rect bounds = new Rect(-8f, -4.5f, 16f, 9f);
    Vector2 velocity;
    Vector2 currentInput;
    Vector2 lastDir = Vector2.right;
    Vector2 dashDir = Vector2.right;
    Vector2 squash = Vector2.one;
    Vector2 externalAccum, externalApplied; // lực từ bên ngoài (trường hấp dẫn...)
    float dashTimer, cooldownTimer, bufferTimer, tailTimer;
    float facing = 1f;
    bool preferHorizontal = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // để OnTriggerStay2D luôn được gọi
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        health = GetComponent<PlayerHealth>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.AddBinding("<Gamepad>/buttonSouth");
        dashAction.AddBinding("<Gamepad>/rightShoulder");

        toggleModeAction = new InputAction("ToggleMode", InputActionType.Button);
        toggleModeAction.AddBinding("<Keyboard>/t");
        toggleModeAction.AddBinding("<Gamepad>/buttonNorth");
    }

    void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
        toggleModeAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();
        toggleModeAction.Disable();
    }

    void OnDestroy()
    {
        moveAction?.Dispose();
        dashAction?.Dispose();
        toggleModeAction?.Dispose();
    }

    public void SetBounds(Rect playArea)
    {
        bounds = playArea;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        currentInput = ProcessInput(moveAction.ReadValue<Vector2>());
        if (currentInput.sqrMagnitude > 0.01f) lastDir = currentInput.normalized;

        if (toggleModeAction.WasPressedThisFrame())
            moveMode = moveMode == MoveMode.FourWay ? MoveMode.EightWay : MoveMode.FourWay;

        if (dashAction.WasPressedThisFrame()) bufferTimer = inputBufferTime;
        bufferTimer -= dt;
        cooldownTimer -= dt;
        tailTimer -= dt;

        if (!IsDashing && bufferTimer > 0f && cooldownTimer <= 0f) StartDash();

        if (IsDashing)
        {
            dashTimer -= dt;
            velocity = dashDir * dashSpeed;
            if (dashTimer <= 0f) EndDash();
        }
        else
        {
            velocity = Vector2.MoveTowards(velocity, currentInput * moveSpeed, acceleration * dt);
        }

        if (health != null) health.dashInvulnerable = IsDashing || tailTimer > 0f;
        UpdateVisual(dt);
    }

    /// <summary>Lực đẩy/hút từ bên ngoài (đơn vị/giây). Cộng dồn trong khung hình, áp dụng ở FixedUpdate. Dash không bị ảnh hưởng.</summary>
    public void AddExternalVelocity(Vector2 v)
    {
        externalAccum += v;
    }

    void LateUpdate()
    {
        externalApplied = IsDashing ? Vector2.zero : externalAccum;
        externalAccum = Vector2.zero;
    }

    void FixedUpdate()
    {
        Vector2 next = rb.position + (velocity + externalApplied) * Time.fixedDeltaTime;
        next.x = Mathf.Clamp(next.x, bounds.xMin + bodyRadius, bounds.xMax - bodyRadius);
        next.y = Mathf.Clamp(next.y, bounds.yMin + bodyRadius, bounds.yMax - bodyRadius);
        rb.MovePosition(next);
    }

    Vector2 ProcessInput(Vector2 raw)
    {
        if (raw.sqrMagnitude < inputDeadzone * inputDeadzone) return Vector2.zero;

        if (moveMode == MoveMode.FourWay)
        {
            float ax = Mathf.Abs(raw.x), ay = Mathf.Abs(raw.y);
            // Khi bấm chéo trên bàn phím (ax == ay) giữ trục đang dùng để không bị "giật".
            if (Mathf.Abs(ax - ay) > 0.05f) preferHorizontal = ax > ay;
            return preferHorizontal ? new Vector2(Mathf.Sign(raw.x) * (ax > 0.01f ? 1f : 0f), 0f)
                                    : new Vector2(0f, Mathf.Sign(raw.y) * (ay > 0.01f ? 1f : 0f));
        }

        // 8 hướng: snap về bội số 45 độ
        float step = Mathf.PI / 4f;
        float ang = Mathf.Round(Mathf.Atan2(raw.y, raw.x) / step) * step;
        return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
    }

    void StartDash()
    {
        bufferTimer = 0f;
        IsDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;
        Vector2 d = currentInput.sqrMagnitude > 0.01f ? currentInput : lastDir;
        dashDir = d.normalized;
    }

    void EndDash()
    {
        IsDashing = false;
        velocity = dashDir * moveSpeed; // giữ lại đà để chuyển động mượt
        tailTimer = dashInvulnerableTail;
    }

    void UpdateVisual(float dt)
    {
        if (visual == null) return;

        if (lastDir.x > 0.1f) facing = 1f;
        else if (lastDir.x < -0.1f) facing = -1f;

        Vector2 target = Vector2.one;
        if (IsDashing)
            target = Mathf.Abs(dashDir.x) > Mathf.Abs(dashDir.y) ? new Vector2(1.35f, 0.7f) : new Vector2(0.7f, 1.35f);

        squash = Vector2.Lerp(squash, target, 1f - Mathf.Exp(-25f * dt));
        visual.localScale = new Vector3(squash.x * facing, squash.y, 1f);
    }
}
