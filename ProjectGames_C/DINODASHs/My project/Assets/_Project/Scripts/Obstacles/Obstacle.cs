using UnityEngine;

/// <summary>
/// Obstacle dùng chung cho mọi loại, hành vi lấy từ ObstacleData.
/// State machine: Dormant -> Telegraph -> Active -> Cooldown.
/// Mọi nguy hiểm có telegraph (Strike/Beam/Chase/Shockwave/RotatingBeam) luôn cảnh báo trước
/// để cái chết luôn "công bằng".
/// </summary>
public class Obstacle : MonoBehaviour
{
    public enum State { Dormant, Telegraph, Active, Cooldown }

    public State Current { get; private set; }
    public ObstacleData Data { get; private set; }

    /// <summary>Toạ độ làn (x nếu đường dọc, y nếu đường ngang). Chỉ có nghĩa khi UsesLane.</summary>
    public float Lane { get; private set; }
    public bool LaneIsVerticalLine { get; private set; }

    /// <summary>Có chiếm một "làn" thẳng trên màn hình (dùng cho kiểm tra khoảng cách giữa các làn).</summary>
    public bool UsesLane => Data != null &&
        (Data.behavior == ObstacleBehavior.Strike ||
         Data.behavior == ObstacleBehavior.Beam ||
         Data.behavior == ObstacleBehavior.Chase);

    /// <summary>Là nguy hiểm có telegraph - tính vào giới hạn "số nguy hiểm đồng thời".</summary>
    public bool IsLaneBased => Data != null &&
        (UsesLane ||
         Data.behavior == ObstacleBehavior.Shockwave ||
         Data.behavior == ObstacleBehavior.RotatingBeam);

    static readonly Color WarningColor = new Color(1f, 0.25f, 0.2f, 1f);

    ObstacleSpawner owner;
    SpriteRenderer body;
    Transform art;
    Sprite currentFrame;
    Sprite warningSprite;
    Vector3 warningFit = Vector3.one;
    bool animated;
    SpriteRenderer warning;
    BoxCollider2D box;
    CircleCollider2D circle;
    Collider2D activeCollider;
    Rect area;
    SpawnDirection dir;
    Vector2 moveDir;
    Vector2 anchor;
    float timer, age, baseY;
    float radius;                 // Shockwave
    float armAngle, armLength, spinSign; // RotatingBeam
    bool released;
    bool usesCollider;

    void Awake()
    {
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        // Hình nằm ở object con "Art" để co giãn vừa khít sprite thật mà không làm lệch scale/hitbox của root.
        art = new GameObject("Art").transform;
        art.SetParent(transform, false);
        body = art.gameObject.AddComponent<SpriteRenderer>();
        body.sortingOrder = 5;

        box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        circle = gameObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;

        // Vạch cảnh báo là object riêng (không là con) để không bị ảnh hưởng bởi scale/rotation của obstacle.
        var w = new GameObject("Warning");
        warning = w.AddComponent<SpriteRenderer>();
        warning.sprite = PlaceholderSprites.Get(ShapeType.Square);
        warning.sortingOrder = 1;
        w.SetActive(false);
    }

    public void Init(ObstacleSpawner spawner, ObstacleData data, SpawnDirection direction,
                     float lane, Vector2 anchorPoint, Rect playArea)
    {
        owner = spawner;
        Data = data;
        dir = direction;
        area = playArea;
        anchor = anchorPoint;
        released = false;
        age = 0f;
        timer = 0f;
        radius = 0f;
        Lane = 0f;
        LaneIsVerticalLine = false;
        Current = State.Dormant;

        currentFrame = null;
        animated = data.IsAnimated;
        ShowSprite(data.ResolveSprite());
        body.color = data.BodyColor;
        // Lật theo hướng spawn (chỉ khi dùng sprite thật với facing = MirrorByDirection).
        bool mirror = data.HasRealSprite && data.facing == SpriteFacing.MirrorByDirection;
        body.flipX = mirror && direction == SpawnDirection.Left;
        body.flipY = mirror && direction == SpawnDirection.Top;
        body.enabled = true;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(data.size.x, data.size.y, 1f);

        warning.transform.SetParent(owner.transform, false);
        warningSprite = data.ResolveWarningSprite();
        warning.sprite = warningSprite;
        warningFit = FitScale(warningSprite);
        warning.transform.rotation = Quaternion.identity;
        warning.gameObject.SetActive(false);

        // Shockwave và GravityField tự kiểm tra va chạm bằng khoảng cách, không dùng collider.
        usesCollider = data.behavior != ObstacleBehavior.Shockwave &&
                       data.behavior != ObstacleBehavior.GravityField;

        bool round = data.shape == ShapeType.Circle || data.shape == ShapeType.Gear;
        box.size = Vector2.one * data.hitboxScale;
        circle.radius = 0.5f * data.hitboxScale;
        activeCollider = round ? (Collider2D)circle : box;
        box.enabled = false;
        circle.enabled = false;

        switch (data.behavior)
        {
            case ObstacleBehavior.Scrolling:    InitScrolling(lane); break;
            case ObstacleBehavior.Sine:         InitSine(lane); break;
            case ObstacleBehavior.Strike:       InitLane(lane, false); break;
            case ObstacleBehavior.Beam:         InitLane(lane, true); break;
            case ObstacleBehavior.Chase:        InitChase(lane); break;
            case ObstacleBehavior.Shockwave:    InitShockwave(); break;
            case ObstacleBehavior.RotatingBeam: InitRotatingBeam(); break;
            case ObstacleBehavior.GravityField: InitGravityField(lane); break;
        }
    }

    /// <summary>Đổi hình của body và co về đúng 1x1 unit (root đã có scale = Size), kể cả khi khung hình khác kích thước.</summary>
    void ShowSprite(Sprite sprite)
    {
        if (sprite == currentFrame) return;
        currentFrame = sprite;
        body.sprite = sprite;

        art.localScale = FitScale(sprite);
    }

    /// <summary>Scale để một sprite có kích thước bất kỳ vừa đúng 1x1 unit.</summary>
    static Vector3 FitScale(Sprite sprite)
    {
        if (sprite != null && sprite.bounds.size.x > 0.0001f && sprite.bounds.size.y > 0.0001f)
            return new Vector3(1f / sprite.bounds.size.x, 1f / sprite.bounds.size.y, 1f);
        return Vector3.one;
    }

    /// <summary>Đặt kích thước (unit) của vạch cảnh báo, bù theo kích thước sprite cảnh báo.</summary>
    void SetWarningScale(Transform wt, float x, float y)
    {
        wt.localScale = new Vector3(x * warningFit.x, y * warningFit.y, 1f);
    }

    // ---------------------------------------------------------------- khởi tạo theo hành vi

    void InitScrolling(float lane)
    {
        float w = Data.size.x, h = Data.size.y;
        Lane = lane;
        float x = area.xMax + w;
        float y;
        float rot;
        switch (dir)
        {
            case SpawnDirection.Top:    y = area.yMax - h * 0.5f; rot = 180f; break;
            case SpawnDirection.Bottom: y = area.yMin + h * 0.5f; rot = 0f;   break;
            default:
                y = Mathf.Clamp(lane, area.yMin + h * 0.5f, area.yMax - h * 0.5f);
                rot = 90f; // gai hướng sang trái
                break;
        }
        if (Data.RotatesToDirection) transform.rotation = Quaternion.Euler(0f, 0f, rot);
        transform.position = new Vector3(x, y, 0f);
        EnterActive();
    }

    void InitSine(float lane)
    {
        float amp = Data.sineAmplitude;
        float h = Data.size.y;
        float lo = area.yMin + amp + h * 0.5f;
        float hi = area.yMax - amp - h * 0.5f;
        baseY = lo < hi ? Mathf.Clamp(lane, lo, hi) : area.center.y;
        Lane = baseY;
        transform.position = new Vector3(area.xMax + Data.size.x, baseY, 0f);
        EnterActive();
    }

    void InitLane(float lane, bool isBeam)
    {
        bool vertical = dir == SpawnDirection.Top || dir == SpawnDirection.Bottom;
        LaneIsVerticalLine = vertical;

        float w = Data.size.x, h = Data.size.y;
        float half = isBeam ? h * 0.5f : (vertical ? w * 0.5f : h * 0.5f);
        Lane = vertical
            ? Mathf.Clamp(lane, area.xMin + half, area.xMax - half)
            : Mathf.Clamp(lane, area.yMin + half, area.yMax - half);

        Vector2 c = area.center;
        Transform wt = warning.transform;

        if (isBeam)
        {
            const float thin = 0.12f;
            if (vertical)
            {
                wt.position = new Vector3(Lane, c.y, 0f);
                SetWarningScale(wt, thin, area.height + 2f);
                transform.position = new Vector3(Lane, c.y, 0f);
            }
            else
            {
                wt.position = new Vector3(c.x, Lane, 0f);
                SetWarningScale(wt, area.width + 2f, thin);
                transform.position = new Vector3(c.x, Lane, 0f);
            }
            body.enabled = false;
        }
        else
        {
            if (vertical)
            {
                wt.position = new Vector3(Lane, c.y, 0f);
                SetWarningScale(wt, w, area.height + 2f);
            }
            else
            {
                wt.position = new Vector3(c.x, Lane, 0f);
                SetWarningScale(wt, area.width + 2f, h);
            }

            switch (dir)
            {
                case SpawnDirection.Top:
                    moveDir = Vector2.down;
                    transform.position = new Vector3(Lane, area.yMax + h * 0.5f + 0.5f, 0f);
                    break;
                case SpawnDirection.Bottom:
                    moveDir = Vector2.up;
                    transform.position = new Vector3(Lane, area.yMin - h * 0.5f - 0.5f, 0f);
                    break;
                case SpawnDirection.Right:
                    moveDir = Vector2.left;
                    transform.position = new Vector3(area.xMax + w * 0.5f + 0.5f, Lane, 0f);
                    break;
                default: // Left
                    moveDir = Vector2.right;
                    transform.position = new Vector3(area.xMin - w * 0.5f - 0.5f, Lane, 0f);
                    break;
            }

            // Hình tam giác (nhũ đá, mũi tên...) luôn hướng mũi theo chiều lao.
            if (Data.RotatesToDirection)
            {
                float ang = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        EnterTelegraph();
    }

    void InitChase(float lane)
    {
        float w = Data.size.x, h = Data.size.y;
        LaneIsVerticalLine = false;
        Lane = Mathf.Clamp(lane, area.yMin + h * 0.5f, area.yMax - h * 0.5f);

        transform.position = new Vector3(area.xMax + w * 0.5f + 0.5f, Lane, 0f);

        // Vạch ngang báo trước độ cao nơi nó xuất hiện.
        Transform wt = warning.transform;
        wt.position = new Vector3(area.center.x, Lane, 0f);
        SetWarningScale(wt, area.width + 2f, h);

        EnterTelegraph();
    }

    void InitShockwave()
    {
        transform.position = new Vector3(anchor.x, anchor.y, 0f);
        transform.localScale = new Vector3(0.1f, 0.1f, 1f);
        body.enabled = false;

        // Điểm nguồn nhấp nháy báo nơi sóng sẽ bắt đầu lan ra.
        warning.sprite = PlaceholderSprites.Get(ShapeType.Circle);
        warning.transform.position = new Vector3(anchor.x, anchor.y, 0f);
        warning.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        EnterTelegraph();
    }

    void InitRotatingBeam()
    {
        armLength = Mathf.Sqrt(area.width * area.width + area.height * area.height);
        spinSign = Random.value < 0.5f ? -1f : 1f;
        armAngle = PickArmAngle();

        transform.position = new Vector3(anchor.x, anchor.y, 0f);
        body.enabled = false;

        // Vạch mảnh chỉ hướng ban đầu của tia.
        Vector2 d = new Vector2(Mathf.Cos(armAngle * Mathf.Deg2Rad), Mathf.Sin(armAngle * Mathf.Deg2Rad));
        Transform wt = warning.transform;
        wt.position = new Vector3(anchor.x + d.x * armLength * 0.5f, anchor.y + d.y * armLength * 0.5f, 0f);
        wt.rotation = Quaternion.Euler(0f, 0f, armAngle);
        SetWarningScale(wt, armLength, 0.12f);

        EnterTelegraph();
    }

    void InitGravityField(float lane)
    {
        float r = Data.fieldRadius;
        Lane = Mathf.Clamp(lane, area.yMin + 1f, area.yMax - 1f);
        Vector3 pos = new Vector3(area.xMax + r + 0.5f, Lane, 0f);
        transform.position = pos;

        // Vòng "aura" cho thấy vùng bị hút (dùng lại object cảnh báo).
        warning.sprite = PlaceholderSprites.Get(ShapeType.Ring);
        warning.transform.position = pos;
        warning.transform.localScale = new Vector3(r * 2f, r * 2f, 1f);
        Color aura = Data.color;
        aura.a = 0.35f;
        warning.color = aura;

        EnterActive(); // không có telegraph: nó trôi từ ngoài màn hình vào nên người chơi thấy trước
    }

    /// <summary>Chọn góc ban đầu của tia, tránh hướng thẳng vào người chơi.</summary>
    float PickArmAngle()
    {
        for (int i = 0; i < 8; i++)
        {
            float a = Random.Range(0f, 360f);
            if (owner.Player == null) return a;

            Vector2 to = (Vector2)owner.Player.position - anchor;
            if (to.sqrMagnitude < 1f) return a;

            float toAngle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(a, toAngle)) > 40f) return a;
        }
        return Random.Range(0f, 360f);
    }

    // ---------------------------------------------------------------- state machine

    void EnterTelegraph()
    {
        Current = State.Telegraph;
        timer = Mathf.Max(0.05f, Data.telegraphTime);
        warning.gameObject.SetActive(true);
        PulseWarning();
    }

    void EnterActive()
    {
        Current = State.Active;
        warning.gameObject.SetActive(Data.behavior == ObstacleBehavior.GravityField); // aura giữ nguyên
        body.enabled = true;
        activeCollider.enabled = usesCollider;

        switch (Data.behavior)
        {
            case ObstacleBehavior.Beam:
            {
                timer = Data.activeTime;
                bool vertical = dir == SpawnDirection.Top || dir == SpawnDirection.Bottom;
                transform.localScale = vertical
                    ? new Vector3(Data.size.y, area.height + 2f, 1f)
                    : new Vector3(area.width + 2f, Data.size.y, 1f);
                break;
            }
            case ObstacleBehavior.RotatingBeam:
                timer = Data.activeTime;
                UpdateArm();
                break;
            case ObstacleBehavior.Shockwave:
                radius = 0.4f;
                ApplyRing();
                break;
        }
    }

    void Update()
    {
        if (released || Data == null) return;

        float dt = Time.deltaTime;
        age += dt;
        if (animated) ShowSprite(Data.FrameAt(age));

        switch (Current)
        {
            case State.Telegraph:
                timer -= dt;
                PulseWarning();
                if (timer <= 0f) EnterActive();
                break;
            case State.Active:
                TickActive(dt);
                break;
            case State.Cooldown:
                timer -= dt;
                if (timer <= 0f) Despawn();
                break;
        }
    }

    void TickActive(float dt)
    {
        Vector3 p = transform.position;

        switch (Data.behavior)
        {
            case ObstacleBehavior.Scrolling:
                p.x -= owner.ScrollSpeed * Data.scrollMultiplier * dt;
                transform.position = p;
                if (p.x < area.xMin - Data.size.x - 1f) Despawn();
                break;

            case ObstacleBehavior.Sine:
                p.x -= owner.ScrollSpeed * Data.scrollMultiplier * dt;
                p.y = baseY + Mathf.Sin(age * Data.sineFrequency) * Data.sineAmplitude;
                transform.position = p;
                Spin(dt);
                if (p.x < area.xMin - Data.size.x - 1f) Despawn();
                break;

            case ObstacleBehavior.Strike:
                p += (Vector3)(moveDir * Data.strikeSpeed * dt);
                transform.position = p;
                Spin(dt);
                if (OutsideArea(p)) Despawn();
                break;

            case ObstacleBehavior.Beam:
                timer -= dt;
                if (timer <= 0f) EndTimedBeam();
                break;

            case ObstacleBehavior.Chase:
                TickChase(dt, p);
                break;

            case ObstacleBehavior.Shockwave:
                TickShockwave(dt);
                break;

            case ObstacleBehavior.RotatingBeam:
                armAngle += spinSign * Data.spinSpeed * dt;
                UpdateArm();
                timer -= dt;
                if (timer <= 0f) EndTimedBeam();
                break;

            case ObstacleBehavior.GravityField:
                TickGravityField(dt, p);
                break;
        }
    }

    void TickChase(float dt, Vector3 p)
    {
        p.x -= Data.strikeSpeed * dt;

        // Chỉ bám theo khi còn cách người chơi xa; sát gần thì "chốt hướng" để người chơi luôn có cơ hội né.
        Transform player = owner.Player;
        // Khoảng chốt hướng tăng theo tốc độ bay (~0.4s trước khi chạm) để luôn đủ thời gian né.
        float lockDistance = Mathf.Max(2.5f, Data.strikeSpeed * 0.4f);
        if (player != null && p.x > player.position.x + lockDistance)
            p.y = Mathf.MoveTowards(p.y, player.position.y, Data.chaseSpeed * dt);

        float half = Data.size.y * 0.5f;
        p.y = Mathf.Clamp(p.y, area.yMin + half, area.yMax - half);

        transform.position = p;
        if (p.x < area.xMin - Data.size.x - 1f) Despawn();
    }

    void TickShockwave(float dt)
    {
        radius += Data.strikeSpeed * dt;
        ApplyRing();

        Transform player = owner.Player;
        if (player != null)
        {
            Vector2 d = (Vector2)player.position - (Vector2)transform.position;
            float dist = d.magnitude;
            float mid = radius * 0.94f;                              // giữa dải vòng (khớp sprite Ring: 0.88R - 1.0R)
            float half = radius * 0.06f * Data.hitboxScale;
            if (Mathf.Abs(dist - mid) <= half + owner.PlayerHitRadius) DamagePlayer(d);
        }

        if (radius >= Data.maxRadius) Despawn();
    }

    void TickGravityField(float dt, Vector3 p)
    {
        p.x -= owner.ScrollSpeed * Data.scrollMultiplier * dt;
        transform.position = p;
        warning.transform.position = p;
        Spin(dt);

        Transform player = owner.Player;
        if (player != null)
        {
            Vector2 toCenter = (Vector2)p - (Vector2)player.position;
            float dist = toCenter.magnitude;

            if (dist < Data.fieldRadius && dist > 0.001f && owner.PlayerCtl != null)
            {
                float falloff = 1f - dist / Data.fieldRadius; // càng gần tâm càng mạnh
                float strength = Data.fieldStrength * (0.35f + 0.65f * falloff);
                owner.PlayerCtl.AddExternalVelocity(toCenter / dist * strength);
            }

            float coreRadius = Data.size.x * 0.5f * Data.hitboxScale;
            if (dist < coreRadius + owner.PlayerHitRadius) DamagePlayer(-toCenter);
        }

        if (p.x < area.xMin - Data.fieldRadius - 1f) Despawn();
    }

    void UpdateArm()
    {
        float rad = armAngle * Mathf.Deg2Rad;
        Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        transform.position = new Vector3(anchor.x + d.x * armLength * 0.5f, anchor.y + d.y * armLength * 0.5f, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, armAngle);
        transform.localScale = new Vector3(armLength, Data.size.y, 1f);
    }

    void ApplyRing()
    {
        float d = radius * 2f;
        transform.localScale = new Vector3(d, d, 1f);

        Color c = Data.BodyColor;
        c.a = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(radius / Mathf.Max(1f, Data.maxRadius))); // mờ dần khi lan xa
        body.color = c;
    }

    void EndTimedBeam()
    {
        Current = State.Cooldown;
        activeCollider.enabled = false;
        body.enabled = false;
        timer = 0.05f;
    }

    void DamagePlayer(Vector2 fromObstacleToPlayer)
    {
        if (owner.PlayerHp != null) owner.PlayerHp.TakeDamage(Data, fromObstacleToPlayer);
    }

    void Spin(float dt)
    {
        if (Data.spinSpeed != 0f) transform.Rotate(0f, 0f, -Data.spinSpeed * dt);
    }

    bool OutsideArea(Vector3 p)
    {
        const float margin = 3f;
        return p.x < area.xMin - margin || p.x > area.xMax + margin ||
               p.y < area.yMin - margin || p.y > area.yMax + margin;
    }

    void PulseWarning()
    {
        float t01 = 1f - Mathf.Clamp01(timer / Mathf.Max(0.05f, Data.telegraphTime));
        float pulse = 0.7f + 0.3f * Mathf.Sin(age * Mathf.Lerp(10f, 40f, t01)); // nhấp nháy nhanh dần
        Color c = WarningColor;
        c.a = Mathf.Lerp(0.15f, 0.6f, t01) * pulse;
        warning.color = c;
    }

    void Despawn()
    {
        if (released) return;
        released = true;
        if (activeCollider != null) activeCollider.enabled = false;
        warning.gameObject.SetActive(false);
        owner.Release(this);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Current != State.Active || !other.CompareTag("Player")) return;
        if (other.TryGetComponent(out PlayerHealth hp))
        {
            Vector2 fromObstacle = (Vector2)other.transform.position - (Vector2)transform.position;
            hp.TakeDamage(Data, fromObstacle);
        }
    }
}
