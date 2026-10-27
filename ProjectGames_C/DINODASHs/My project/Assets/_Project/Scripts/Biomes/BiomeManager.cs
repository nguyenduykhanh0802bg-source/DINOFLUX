using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Chọn biome theo quãng đường, chuyển màu nền/hạt một cách mượt, đưa catalog + hệ số độ khó cho Director,
/// đưa hệ số tốc độ cho RunManager và cung cấp dữ liệu banner tên biome cho HUD.
/// Khi vào biome mới có một khoảng nghỉ ngắn (nhịp "thả") để người chơi kịp thở.
/// Được dựng và khởi tạo bởi PrototypeBootstrap thông qua Setup().
/// </summary>
public class BiomeManager : MonoBehaviour
{
    [Header("Chuyển biome")]
    public float transitionSeconds = 2.5f;
    [Tooltip("Thời gian nghỉ (không spawn obstacle mới) khi vào biome mới.")]
    public float calmSeconds = 3f;

    public BiomeData Current { get; private set; }
    public int CurrentIndex => index;
    public int Count => biomes.Count;
    public bool HasBiomes => biomes.Count > 0;
    public string CurrentName => Current != null ? Current.displayName : "";

    // HUD đọc các giá trị này để vẽ banner
    public string BannerText { get; private set; } = "";
    public string BannerSub { get; private set; } = "";
    public float BannerAlpha { get; private set; }

    const float BannerDuration = 3.2f;

    readonly List<BiomeData> biomes = new List<BiomeData>();
    RunManager run;
    DifficultyDirector director;
    Camera cam;
    ScrollingBackdrop backdrop;

    int index = -1;
    float blend = 1f;
    float bannerElapsed = 999f;

    Color bgFrom, bgTo, tintFrom, tintTo;
    float driftFrom, driftTo;
    Color curBg, curTint;
    float curDrift;

    /// <summary>
    /// Khởi tạo. Biome không hợp lệ (null, thiếu catalog hoặc catalog rỗng) bị bỏ qua kèm cảnh báo.
    /// Nếu không còn biome nào, manager ở trạng thái "tắt" (HasBiomes = false) và không đụng tới Director.
    /// </summary>
    public void Setup(IList<BiomeData> list, RunManager runManager, DifficultyDirector dir, Camera camera, ScrollingBackdrop bd)
    {
        run = runManager;
        director = dir;
        cam = camera;
        backdrop = bd;

        biomes.Clear();
        index = -1;
        Current = null;

        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                BiomeData b = list[i];
                if (b == null) continue;
                if (b.catalog == null || b.catalog.ToList().Count == 0)
                {
                    Debug.LogWarning($"[DinoDash] Biome '{b.name}' bị bỏ qua vì chưa có catalog hoặc catalog rỗng.", b);
                    continue;
                }
                biomes.Add(b);
            }
        }

        biomes.Sort((a, b) => a.startDistance.CompareTo(b.startDistance));

        if (biomes.Count > 0) Enter(IndexFor(run != null ? run.Distance : 0f), true);
    }

    void Update()
    {
        if (biomes.Count == 0 || run == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) DebugJumpToNext();
#endif

        if (run.IsPlaying)
        {
            int target = IndexFor(run.Distance);
            if (target != index) Enter(target, false);
        }

        if (blend < 1f)
        {
            blend = Mathf.MoveTowards(blend, 1f, Time.deltaTime / Mathf.Max(0.01f, transitionSeconds));
            ApplyVisuals();
        }

        UpdateBanner();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Phím F2 (chỉ Editor / Development build): nhảy tới đầu biome kế tiếp để test nhanh. Lượt chơi đó không tính kỷ lục.</summary>
    void DebugJumpToNext()
    {
        int next = Mathf.Min(biomes.Count - 1, index + 1);
        run.DebugJumpTo(biomes[next].startDistance + 1f);
    }
#endif

    int IndexFor(float distance)
    {
        for (int i = biomes.Count - 1; i >= 0; i--)
            if (distance >= biomes[i].startDistance) return i;
        return 0;
    }

    void Enter(int i, bool instant)
    {
        index = i;
        BiomeData b = biomes[i];
        Current = b;

        bgFrom = instant ? b.backgroundColor : curBg;
        tintFrom = instant ? b.particleColor : curTint;
        driftFrom = instant ? b.particleDrift : curDrift;
        bgTo = b.backgroundColor;
        tintTo = b.particleColor;
        driftTo = b.particleDrift;

        blend = instant ? 1f : 0f;
        ApplyVisuals();

        if (director != null) director.SetBiome(b.catalog.ToList(), b.budgetMultiplier, instant ? 0f : calmSeconds);
        if (run != null) run.BiomeSpeedMultiplier = b.speedMultiplier;

        BannerText = b.displayName;
        BannerSub = b.tagline;
        bannerElapsed = 0f;
    }

    void ApplyVisuals()
    {
        float s = Mathf.SmoothStep(0f, 1f, blend);
        curBg = Color.Lerp(bgFrom, bgTo, s);
        curTint = Color.Lerp(tintFrom, tintTo, s);
        curDrift = Mathf.Lerp(driftFrom, driftTo, s);

        if (cam != null) cam.backgroundColor = curBg;
        if (backdrop != null)
        {
            backdrop.SetTint(curTint);
            backdrop.SetDrift(curDrift);
        }
    }

    void UpdateBanner()
    {
        bannerElapsed += Time.deltaTime;
        if (bannerElapsed >= BannerDuration)
        {
            BannerAlpha = 0f;
            return;
        }

        float fadeIn = Mathf.Clamp01(bannerElapsed / 0.4f);
        float fadeOut = Mathf.Clamp01((BannerDuration - bannerElapsed) / 0.8f);
        BannerAlpha = Mathf.Min(fadeIn, fadeOut);
    }
}
