using UnityEngine;

/// <summary>
/// Obstacle dùng chung cho mọi loại, hành vi lấy từ ObstacleData.
/// State machine: Dormant -> Telegraph -> Active -> Cooldown.
/// Mọi nguy hiểm kiểu Strike/Beam đều có telegraph để cái chết luôn "công bằng".
/// </summary>
public class Obstacle : MonoBehaviour
{
    public enum State { Dormant, Telegraph, Active, Cooldown }

    public State Current { get; private set; }
    public ObstacleData Data { get; private set; }

    /// <summary>Toạ độ làn (x nếu đường dọc, y nếu đường ngang). Dùng cho kiểm tra khoảng cách làn.</summary>
    public float Lane { get; private set; }
    public bool LaneIsVerticalLine { get; private set; }
    public bool IsLaneBased => Data != null &&
        (Data.behavior == ObstacleBehavior.Strike || Data.behavior == ObstacleBehavior.Beam);

    static readonly Color WarningColor = new Color(1f, 0.25f, 0.2f, 1f);

    ObstacleSpawner owner;
    SpriteRenderer body;
    SpriteRenderer warning;
    BoxCollider2D box;
    CircleCollider2D circle;
    Collider2D activeCollider;
    Rect area;
    SpawnDirection dir;
    Vector2 moveDir;
    float timer, age, baseY;
    bool released;

    void Awake()
    {
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        body = gameObject.AddComponent<SpriteRenderer>();
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

    public void Init(ObstacleSpawner spawner, ObstacleData data, SpawnDirection direction, float lane, Rect playArea)
    {
        owner = spawner;
        Data = data;
        dir = direction;
        area = playArea;
        released = false;
        age = 0f;
        timer = 0f;
        Current = State.Dormant;

        body.sprite = PlaceholderSprites.Get(data.shape);
        body.color = data.color;
        body.enabled = true;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(data.size.x, data.size.y, 1f);

        warning.transform.SetParent(owner.transform, false);
        warning.gameObject.SetActive(false);

        bool round = data.shape == ShapeType.Circle || data.shape == ShapeType.Gear;
        box.size = Vector2.one * data.hitboxScale;
        circle.radius = 0.5f * data.hitboxScale;
        activeCollider = round ? (Collider2D)circle : box;
        box.enabled = false;
        circle.enabled = false;

        switch (data.behavior)
        {
            case ObstacleBehavior.Scrolling: InitScrolling(lane); break;
            case ObstacleBehavior.Sine:      InitSine(lane); break;
            case ObstacleBehavior.Strike:    InitLane(lane, false); break;
            case ObstacleBehavior.Beam:      InitLane(lane, true); break;
        }
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
        if (Data.shape == ShapeType.Triangle) transform.rotation = Quaternion.Euler(0f, 0f, rot);
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
                wt.localScale = new Vector3(thin, area.height + 2f, 1f);
                transform.position = new Vector3(Lane, c.y, 0f);
            }
            else
            {
                wt.position = new Vector3(c.x, Lane, 0f);
                wt.localScale = new Vector3(area.width + 2f, thin, 1f);
                transform.position = new Vector3(c.x, Lane, 0f);
            }
            body.enabled = false;
        }
        else
        {
            if (vertical)
            {
                wt.position = new Vector3(Lane, c.y, 0f);
                wt.localScale = new Vector3(w, area.height + 2f, 1f);
            }
            else
            {
                wt.position = new Vector3(c.x, Lane, 0f);
                wt.localScale = new Vector3(area.width + 2f, h, 1f);
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
        }

        EnterTelegraph();
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
        warning.gameObject.SetActive(false);
        body.enabled = true;
        activeCollider.enabled = true;

        if (Data.behavior == ObstacleBehavior.Beam)
        {
            timer = Data.activeTime;
            bool vertical = dir == SpawnDirection.Top || dir == SpawnDirection.Bottom;
            transform.localScale = vertical
                ? new Vector3(Data.size.y, area.height + 2f, 1f)
                : new Vector3(area.width + 2f, Data.size.y, 1f);
        }
    }

    void Update()
    {
        if (released || Data == null) return;

        float dt = Time.deltaTime;
        age += dt;

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
                if (timer <= 0f)
                {
                    Current = State.Cooldown;
                    activeCollider.enabled = false;
                    body.enabled = false;
                    timer = 0.05f;
                }
                break;
        }
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
