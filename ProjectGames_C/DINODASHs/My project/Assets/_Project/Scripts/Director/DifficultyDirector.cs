using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều phối độ khó theo "ngân sách": mỗi giây tích điểm, mỗi obstacle tốn điểm (difficultyCost).
/// Nhịp căng - thả: sau giai đoạn spawn dày là một khoảng thở với hàng gem.
/// Obstacle chỉ xuất hiện sau minDistance để giới thiệu từng loại một.
/// </summary>
public class DifficultyDirector : MonoBehaviour
{
    enum Phase { Tension, Release }

    [Header("Tham chiếu")]
    public RunManager run;
    public ObstacleSpawner spawner;
    public Transform player;

    [Header("Danh mục obstacle")]
    public List<ObstacleData> catalog = new List<ObstacleData>();

    [Header("Ngân sách độ khó")]
    public float baseBudgetPerSecond = 1.6f;
    public float growthPer100m = 0.2f;
    public float maxBudgetPerSecond = 6f;
    [Tooltip("Ngân sách tích lũy tối đa (chặn việc xả cả đống sau đoạn nghỉ).")]
    public float maxBank = 6f;
    public float minSpawnGap = 0.5f;

    [Header("Nhịp căng - thả")]
    public Vector2 tensionDuration = new Vector2(9f, 13f);
    public float releaseDuration = 2.5f;

    [Header("Giới hạn công bằng")]
    public int baseConcurrentLaneHazards = 1;
    public int maxConcurrentLaneHazards = 3;
    public float metersPerExtraLane = 400f;
    public float laneSpacing = 2.2f;
    [Range(0f, 1f)] public float targetPlayerChance = 0.6f;

    Phase phase = Phase.Tension;
    float phaseTimer = 12f;   // đoạn mở đầu dài hơn cho người chơi làm quen
    float gapTimer = 1.2f;
    float gemTimer = 4f;
    float budget;
    readonly List<ObstacleData> candidates = new List<ObstacleData>();

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
        rate *= RunManager.AssistFactor; // rubber-banding nhẹ nếu người chơi liên tục chết sớm
        budget = Mathf.Min(maxBank, budget + rate * dt);

        if (gemTimer <= 0f)
        {
            gemTimer = Random.Range(5f, 8f);
            SpawnGemLine(Random.Range(1, 4));
        }

        if (gapTimer > 0f) return;

        var pick = PickCandidate();
        if (pick == null) return;

        if (TrySpawn(pick))
        {
            budget -= pick.difficultyCost;
            gapTimer = minSpawnGap + pick.difficultyCost * 0.1f;
        }
        else
        {
            gapTimer = 0.15f; // thử lại sớm với lựa chọn khác
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
        float total = 0f;
        for (int i = 0; i < catalog.Count; i++)
        {
            var d = catalog[i];
            if (d == null) continue;
            if (d.minDistance > run.Distance || d.difficultyCost > budget) continue;
            candidates.Add(d);
            total += Mathf.Max(0.01f, d.spawnWeight);
        }
        if (candidates.Count == 0) return null;

        float r = Random.value * total;
        for (int i = 0; i < candidates.Count; i++)
        {
            r -= Mathf.Max(0.01f, candidates[i].spawnWeight);
            if (r <= 0f) return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    bool TrySpawn(ObstacleData data)
    {
        var dirs = data.allowedDirections;
        if (dirs == null || dirs.Length == 0) return false;

        SpawnDirection dir = dirs[Random.Range(0, dirs.Length)];
        Rect area = spawner.PlayArea;
        float lane = 0f;

        switch (data.behavior)
        {
            case ObstacleBehavior.Scrolling:
                lane = Random.Range(area.yMin + 1f, area.yMax - 1f); // chỉ dùng khi dir = Left/Right
                break;

            case ObstacleBehavior.Sine:
                lane = Random.Range(area.yMin, area.yMax); // Obstacle tự kẹp vào khoảng an toàn
                break;

            case ObstacleBehavior.Strike:
            case ObstacleBehavior.Beam:
            {
                int maxLanes = Mathf.Min(maxConcurrentLaneHazards,
                    baseConcurrentLaneHazards + (int)(run.Distance / Mathf.Max(1f, metersPerExtraLane)));
                if (spawner.CountLaneBased() >= maxLanes) return false;

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
        }

        spawner.Spawn(data, dir, lane);
        return true;
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
