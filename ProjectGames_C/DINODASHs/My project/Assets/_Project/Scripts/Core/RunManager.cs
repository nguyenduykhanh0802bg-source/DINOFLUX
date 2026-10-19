using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý một lượt chơi: quãng đường, tốc độ cuộn theo bậc, gem, game over,
/// death recap (nguyên nhân + lời khuyên) và restart nhanh.
/// </summary>
public class RunManager : MonoBehaviour
{
    public enum RunState { Playing, Dying, GameOver }

    const string BestKey = "DinoDash.BestDistance";
    const string EarlyKey = "DinoDash.EarlyDeaths";

    [Header("Tham chiếu")]
    public PlayerHealth player;
    public ObstacleSpawner spawner;
    public CameraShake cameraShake;

    [Header("Tốc độ cuộn (tăng theo bậc)")]
    public float baseSpeed = 6f;
    public float speedStepPerTier = 0.8f;
    public float metersPerTier = 200f;
    public float maxSpeed = 13f;
    public float speedSmoothing = 2f;

    [Header("Thưởng")]
    public int gemsPerHeal = 10;
    public int scorePerGem = 10;

    public RunState State { get; private set; } = RunState.Playing;
    public bool IsPlaying => State == RunState.Playing;
    public float Distance { get; private set; }
    public int Gems { get; private set; }
    public int Tier => Mathf.FloorToInt(Distance / Mathf.Max(1f, metersPerTier));
    public int Score => Mathf.FloorToInt(Distance) + Gems * scorePerGem;
    public float ScrollSpeed { get; private set; }

    public float BestDistance { get; private set; }
    public bool NewBest { get; private set; }
    public string RecapCause { get; private set; } = "";
    public string RecapTip { get; private set; } = "";

    /// <summary>Hệ số hỗ trợ (rubber-banding): 0.8 nếu 3 lượt gần nhất đều chết trước 150m.</summary>
    public static float AssistFactor { get; private set; } = 1f;

    /// <summary>Bootstrap gán hàm dựng lại màn chơi.</summary>
    public System.Action RestartRequested;

    InputAction restartAction, confirmAction;
    ObstacleData lastHit;
    Vector2 lastHitFrom;

    void Awake()
    {
        ScrollSpeed = baseSpeed;
        BestDistance = PlayerPrefs.GetFloat(BestKey, 0f);
        AssistFactor = PlayerPrefs.GetInt(EarlyKey, 0) >= 3 ? 0.8f : 1f;

        restartAction = new InputAction("Restart", InputActionType.Button);
        restartAction.AddBinding("<Keyboard>/r");
        restartAction.AddBinding("<Gamepad>/select");

        confirmAction = new InputAction("Confirm", InputActionType.Button);
        confirmAction.AddBinding("<Keyboard>/space");
        confirmAction.AddBinding("<Keyboard>/enter");
        confirmAction.AddBinding("<Gamepad>/buttonSouth");
        confirmAction.AddBinding("<Gamepad>/start");
    }

    void OnEnable()
    {
        restartAction.Enable();
        confirmAction.Enable();
    }

    void OnDisable()
    {
        restartAction.Disable();
        confirmAction.Disable();
    }

    void Start()
    {
        if (player != null)
        {
            player.Damaged += HandleDamaged;
            player.Died += HandleDied;
        }
        if (spawner != null) spawner.GemCollected += AddGem;
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.Damaged -= HandleDamaged;
            player.Died -= HandleDied;
        }
        if (spawner != null) spawner.GemCollected -= AddGem;
        restartAction?.Dispose();
        confirmAction?.Dispose();
    }

    void Update()
    {
        if (State == RunState.Playing)
        {
            float target = Mathf.Min(maxSpeed, baseSpeed + speedStepPerTier * Tier);
            ScrollSpeed = Mathf.MoveTowards(ScrollSpeed, target, speedSmoothing * Time.deltaTime);
            Distance += ScrollSpeed * Time.deltaTime;
        }

        if (spawner != null) spawner.ScrollSpeed = ScrollSpeed;

        // R: restart tức thì ở mọi lúc. Space/Enter: chơi lại ở màn hình game over.
        if (restartAction.WasPressedThisFrame() ||
            (State == RunState.GameOver && confirmAction.WasPressedThisFrame()))
        {
            Restart();
        }
    }

    void Restart()
    {
        Time.timeScale = 1f;
        RestartRequested?.Invoke();
    }

    void AddGem()
    {
        if (State != RunState.Playing) return;
        Gems++;
        if (gemsPerHeal > 0 && Gems % gemsPerHeal == 0 && player != null) player.Heal(1);
    }

    // ---------------------------------------------------------------- sự kiện

    void HandleDamaged(ObstacleData source, Vector2 fromObstacleToPlayer)
    {
        lastHit = source;
        lastHitFrom = fromObstacleToPlayer;

        if (cameraShake != null) cameraShake.Shake(0.2f, 0.18f);
        if (player != null && !player.IsDead) StartCoroutine(HitStop(0.06f));
    }

    void HandleDied()
    {
        if (State != RunState.Playing) return;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator HitStop(float seconds)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        if (State == RunState.Playing) Time.timeScale = 1f;
    }

    IEnumerator DeathRoutine()
    {
        State = RunState.Dying;
        Time.timeScale = 0.25f; // slow-motion khoảnh khắc chết

        if (player != null && player.TryGetComponent(out PlayerController pc)) pc.enabled = false;
        if (cameraShake != null) cameraShake.Shake(0.4f, 0.35f);

        BuildRecap();
        SaveResult();

        yield return new WaitForSecondsRealtime(0.9f);

        Time.timeScale = 0f; // đóng băng, hiện recap
        State = RunState.GameOver;
    }

    void BuildRecap()
    {
        if (lastHit == null)
        {
            RecapCause = "Nguyên nhân: không xác định";
            RecapTip = "";
            return;
        }

        string where;
        if (Mathf.Abs(lastHitFrom.x) > Mathf.Abs(lastHitFrom.y))
            where = lastHitFrom.x > 0f ? "từ bên trái" : "từ bên phải";
        else
            where = lastHitFrom.y > 0f ? "từ phía dưới" : "từ phía trên";

        RecapCause = $"Nguyên nhân: {lastHit.displayName} {where}";
        RecapTip = lastHit.recapTip;
        if ((lastHit.behavior == ObstacleBehavior.Strike || lastHit.behavior == ObstacleBehavior.Beam) &&
            lastHit.telegraphTime > 0f)
        {
            RecapTip += $"  (Có cảnh báo {lastHit.telegraphTime:0.0}s trước.)";
        }
    }

    void SaveResult()
    {
        NewBest = Distance > BestDistance;
        if (NewBest)
        {
            BestDistance = Distance;
            PlayerPrefs.SetFloat(BestKey, BestDistance);
        }

        int streak = PlayerPrefs.GetInt(EarlyKey, 0);
        streak = Distance < 150f ? streak + 1 : 0;
        PlayerPrefs.SetInt(EarlyKey, streak);
        PlayerPrefs.Save();
    }
}
