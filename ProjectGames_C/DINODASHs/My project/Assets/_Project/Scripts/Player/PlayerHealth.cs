using UnityEngine;

/// <summary>Hệ thống tim + bất tử tạm thời sau khi dính đòn + bất tử khi dash.</summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxHearts = 3;
    public float hitInvulnerability = 1.2f;

    /// <summary>PlayerController bật cờ này trong lúc dash (i-frame).</summary>
    [HideInInspector] public bool dashInvulnerable;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public bool IsInvulnerable => dashInvulnerable || invulTimer > 0f || IsDead;

    public event System.Action<int, int> HeartsChanged;               // (hiện tại, tối đa)
    public event System.Action<ObstacleData, Vector2> Damaged;        // (nguồn, vector từ obstacle -> player)
    public event System.Action Died;

    float invulTimer;
    bool lastVisible = true;
    SpriteRenderer[] renderers;

    void Awake()
    {
        Current = maxHearts;
    }

    void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (invulTimer > 0f) invulTimer -= Time.deltaTime;

        // Nhấp nháy khi đang bất tử do dính đòn (không nhấp nháy khi dash).
        bool blink = invulTimer > 0f && !IsDead && (((int)(Time.time * 20f)) & 1) == 0;
        SetVisible(!blink);
    }

    void SetVisible(bool visible)
    {
        if (renderers == null || visible == lastVisible) return;
        lastVisible = visible;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = visible;
    }

    public void TakeDamage(ObstacleData source, Vector2 fromObstacleToPlayer)
    {
        if (IsInvulnerable) return;

        Current = Mathf.Max(0, Current - Mathf.Max(1, source.damage));
        invulTimer = hitInvulnerability;

        HeartsChanged?.Invoke(Current, maxHearts);
        Damaged?.Invoke(source, fromObstacleToPlayer);
        if (Current <= 0) Died?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHearts, Current + amount);
        HeartsChanged?.Invoke(Current, maxHearts);
    }
}
