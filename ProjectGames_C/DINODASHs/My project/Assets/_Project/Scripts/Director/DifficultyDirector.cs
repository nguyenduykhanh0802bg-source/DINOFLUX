using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều phối độ khó theo "ngân sách": mỗi giây tích điểm, mỗi obstacle tốn điểm (difficultyCost).
/// - Nhịp căng - thả: sau giai đoạn spawn dày là một khoảng thở với hàng gem.
/// - Obstacle chỉ xuất hiện sau minDistance để giới thiệu từng loại một.
/// - BiomeManager đổi catalog + hệ số ngân sách qua SetBiome, kèm một khoảng nghỉ khi vào biome mới.
/// - Chống lặp: obstacle vừa spawn gần đây bị giảm xác suất được chọn lại.
/// - "Chọn trước, dành điểm chờ": chọn obstacle theo trọng số TRƯỚC, rồi đợi đủ ngân sách. Nếu chỉ chọn trong số loại
///   đang đủ điểm thì loại đắt (laze, sóng xung kích...) gần như không bao giờ xuất hiện vì loại rẻ luôn tiêu hết điểm.
/// </summary>
public class DifficultyDirector : MonoBehaviour
{
    enum Phase { Tension, Release }

    [Header("Tham chiếu")]
    public RunManager run;
    public ObstacleSpawner spawner;
    public Transform player;

    [Header("Danh mục obstacle (BiomeManager tự gán theo biome)")]
    public List<ObstacleData> catalog = new List<ObstacleData>();

    [Header("Ngân sách độ khó")]
    public float baseBudgetPerSecond = 1.6f;
    public float growthPer100m = 0.2f;
    public float maxBudgetPerSecond = 6f;
    [Tooltip("Ngân sách tích lũy tối đa (chặn việc xả cả đống sau đoạn nghỉ).")]
    public float maxBank = 6f;
    public float minSpawnGap = 0.5f;
    [Tooltip("Hệ số của biome hiện tại (BiomeManager tự đặt).")]
    public float budgetMultiplier = 1f;

    [Header("Nhịp căng - thả")]
    public Vector2 tensionDuration = new Vector2(9f, 13f);
    public float releaseDuration = 2.5f;

    [Header("Đa dạng")]
    [Range(0f, 0.95f)] [Tooltip("Giảm xác suất chọn lại obstacle vừa spawn gần đây (0 = không giảm).")]
    public float repeatPenalty = 0.7f;
    [Tooltip("Nhớ bao nhiêu lần spawn gần nhất.")]
    public int recentMemory = 2;

    [Header("Giới hạn công bằng")]
    public int baseConcurrentLaneHazards = 1;
    public int maxConcurrentLaneHazards = 3;
    public float metersPerExtraLane = 400f;
    public float laneSpacing = 2.2f;
    [Range(0f, 1f)] public float targetPlayerChance = 0.6f;
    [Tooltip("Sóng xung kích / trục laze xoay không được xuất hiện gần người chơi hơn mức này.")]
    public float minSpawnDistanceFromPlayer = 3f;

    Phase phase = Phase.Tension;
    float phaseTimer = 12f;   // đoạn mở đầu dài hơn cho người chơi làm quen
    float gapTimer = 1.2f;
    float gemTimer = 4f;
    float budget;
    readonly List<ObstacleData> candidates = new List<ObstacleData>();
    readonly List<float> weights = new List<float>();
    readonly List<ObstacleData> recent = new List<ObstacleData>();

    const int MaxFailedTries = 3; // spawn thất bại (hết làn, không có chỗ đặt...) quá số lần này thì bỏ obstacle đã chọn
    ObstacleData pending;         // obstacle đã chọn, đang chờ đủ ngân sách
    int failedTries;

    // ---------------------------------------------------------------- API cho BiomeManager

    public void SetBiome(List<ObstacleData> list, float budgetMult, float calmSeconds)
    {
        catalog = list ?? new List<ObstacleData>();
        budgetMultiplier = budgetMult;
        recent.Clear();
        pending = null;
        failedTries = 0;
        if (calmSeconds > 0f) BeginCalm(calmSeconds);
    }

    /// <summary>Ép vào nhịp "thả": không spawn obstacle mới, rải hàng gem để người chơi thở.</summary>
    public void BeginCalm(float seconds)
    {
        phase = Phase.Release;
        phaseTimer = seconds;
        budget = Mathf.Min(budget, 1.5f);
        if (spawner != null) SpawnGemLine(4);
    }

    // ---------------------------------------------------------------- vòng lặp chính

    void Update()
    {
        if (run == null || spawner == null || !run.IsPlaying) return;

        float dt = Time.deltaTime;
        phaseTimer -= dt;
        gapTimer -= dt;
        gemTimer -= dt;

        if (phaseTimer <= 0f) SwitchPhase();
        if (phase == Phase.Release) return;

        float rate = Mathf.Min(maxBudgetPerSecond, baseBudgetPerSecond + growthPer100m * run.Distance / 100f);
        rate *= budgetMultiplier;
        rate *= RunManager.AssistFactor; // rubber-banding nhẹ nếu người chơi liên tục chết sớm
        budget = Mathf.Min(maxBank, budget + rate * dt);

        if (gemTimer <= 0f)
        {
            gemTimer = Random.Range(5f, 8f);
            SpawnGemLine(Random.Range(1, 4));
        }

        if (gapTimer > 0f) return;

        if (pending == null)
        {
            pending = PickCandidate();
            if (pending == null) return;
        }

        // Chờ đủ ngân sách (tối đa bằng maxBank để không bao giờ kẹt vì loại có cost > maxBank).
        if (budget < Mathf.Min(pending.difficultyCost, maxBank)) return;

        ObstacleData pick = pending;
        if (TrySpawn(pick))
        {
            budget -= pick.difficultyCost;
            gapTimer = minSpawnGap + pick.difficultyCost * 0.1f;
            RememberPick(pick);
            pending = null;
            failedTries = 0;
        }
        else
        {
            gapTimer = 0.15f; // thử lại sớm
            if (++failedTries >= MaxFailedTries)
            {
                RememberPick(pick); // bỏ lượt này và giảm xác suất chọn lại, tránh đứng chờ một obstacle không đặt được
                pending = null;
                failedTries = 0;
            }
        }
    }

    void SwitchPhase()
    {
        if (phase == Phase.Tension)
        {
            phase = Phase.Release;
            phaseTimer = releaseDuration;
            budget = Mathf.Min(budget, 2f);
            SpawnGemLine(5);
        }
        else
        {
            phase = Phase.Tension;
            phaseTimer = Random.Range(tensionDuration.x, tensionDuration.y);
        }
    }

    ObstacleData PickCandidate()
    {
        candidates.Clear();
        weights.Clear();
        float total = 0f;

        for (int i = 0; i < catalog.Count; i++)
        {
            var d = catalog[i];
            if (d == null) continue;
            if (d.minDistance > run.Distance) continue; // ngân sách xử lý ở Update (chờ đủ điểm), không loại ở đây

            float w = Mathf.Max(0.01f, d.spawnWeight);
            if (recent.Contains(d)) w = Mathf.Max(0.01f, w * (1f - repeatPenalty));

            candidates.Add(d);
            weights.Add(w);
            total += w;
        }
        if (candidates.Count == 0) return null;

        float r = Random.value * total;
        for (int i = 0; i < candidates.Count; i++)
        {
            r -= weights[i];
            if (r <= 0f) return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    void RememberPick(ObstacleData d)
    {
        if (recentMemory <= 0) return;
        recent.Add(d);
        while (recent.Count > recentMemory) recent.RemoveAt(0);
    }

    bool HasLaneRoom()
    {
        int maxLanes = Mathf.Min(maxConcurrentLaneHazards,
            baseConcurrentLaneHazards + (int)(run.Distance / Mathf.Max(1f, metersPerExtraLane)));
        return spawner.CountLaneBased() < maxLanes;
    }

    bool TrySpawn(ObstacleData data)
    {
        var dirs = data.allowedDirections;
        if (dirs == null || dirs.Length == 0) return false;

        SpawnDirection dir = dirs[Random.Range(0, dirs.Length)];
        Rect area = spawner.PlayArea;
        float lane = 0f;
        Vector2 anchor = area.center;

        switch (data.behavior)
        {
            case ObstacleBehavior.Scrolling:
                lane = Random.Range(area.yMin + 1f, area.yMax - 1f); // chỉ dùng khi dir = Left/Right
                break;

            case ObstacleBehavior.Sine:
                lane = Random.Range(area.yMin, area.yMax); // Obstacle tự kẹp vào khoảng an toàn
                break;

            case ObstacleBehavior.GravityField:
                // Chỉ một vùng hút tại một thời điểm: hai xoáy chồng nhau sẽ kéo người chơi vào thế không thoát được.
                if (spawner.CountBehavior(ObstacleBehavior.GravityField) >= 1) return false;
                lane = Random.Range(area.yMin + 1.5f, area.yMax - 1.5f);
                break;

            case ObstacleBehavior.Strike:
            case ObstacleBehavior.Beam:
            {
                if (!HasLaneRoom()) return false;

                bool vertical = dir == SpawnDirection.Top || dir == SpawnDirection.Bottom;
                float min = (vertical ? area.xMin : area.yMin) + 1f;
                float max = (vertical ? area.xMax : area.yMax) - 1f;

                if (player != null && Random.value < targetPlayerChance)
                {
                    float around = vertical ? player.position.x : player.position.y;
                    lane = Mathf.Clamp(around + Random.Range(-1.5f, 1.5f), min, max); // nhắm vào người chơi
                }
                else
                {
                    lane = Random.Range(min, max);
                }

                if (!spawner.IsLaneFree(vertical, lane, laneSpacing)) return false;
                break;
            }

            case ObstacleBehavior.Chase:
            {
                if (!HasLaneRoom()) return false;

                float min = area.yMin + 1f;
                float max = area.yMax - 1f;
                lane = (player != null && Random.value < targetPlayerChance)
                    ? Mathf.Clamp(player.position.y + Random.Range(-1.5f, 1.5f), min, max)
                    : Random.Range(min, max);

                if (!spawner.IsLaneFree(false, lane, laneSpacing)) return false;
                break;
            }

            case ObstacleBehavior.Shockwave:
                if (!HasLaneRoom()) return false;
                if (!TryPickAnchor(area, 1.5f, out anchor)) return false;
                break;

            case ObstacleBehavior.RotatingBeam:
                if (!HasLaneRoom()) return false;
                if (!TryPickAnchor(area, 2f, out anchor)) return false;
                break;
        }

        spawner.Spawn(data, dir, lane, anchor);
        return true;
    }

    /// <summary>Chọn điểm ngẫu nhiên trong màn hình, cách người chơi ít nhất minSpawnDistanceFromPlayer.</summary>
    bool TryPickAnchor(Rect area, float margin, out Vector2 anchor)
    {
        for (int i = 0; i < 8; i++)
        {
            var p = new Vector2(
                Random.Range(area.xMin + margin, area.xMax - margin),
                Random.Range(area.yMin + margin, area.yMax - margin));

            if (player == null || Vector2.Distance(p, player.position) >= minSpawnDistanceFromPlayer)
            {
                anchor = p;
                return true;
            }
        }
        anchor = area.center;
        return false;
    }

    void SpawnGemLine(int count)
    {
        Rect area = spawner.PlayArea;
        float y0 = Random.Range(area.yMin + 1.5f, area.yMax - 1.5f);
        for (int i = 0; i < count; i++)
        {
            var pos = new Vector2(area.xMax + 1f + i * 0.9f, y0 + Mathf.Sin(i * 0.7f) * 0.8f);
            spawner.SpawnGem(pos);
        }
    }
}
