using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Tạo/tái sử dụng obstacle và gem bằng object pooling (UnityEngine.Pool).
/// Spawner KHÔNG quyết định spawn gì/khi nào - đó là việc của DifficultyDirector.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    public Rect PlayArea { get; private set; }
    public float ScrollSpeed { get; set; } = 6f;
    public event System.Action GemCollected;

    readonly List<Obstacle> active = new List<Obstacle>();
    ObjectPool<Obstacle> obstaclePool;
    ObjectPool<Collectible> gemPool;

    public void Setup(Rect playArea)
    {
        PlayArea = playArea;

        obstaclePool = new ObjectPool<Obstacle>(
            createFunc: () =>
            {
                var go = new GameObject("Obstacle");
                go.transform.SetParent(transform, false);
                return go.AddComponent<Obstacle>();
            },
            actionOnGet: o => o.gameObject.SetActive(true),
            actionOnRelease: o => o.gameObject.SetActive(false),
            actionOnDestroy: o => { if (o != null) Destroy(o.gameObject); },
            collectionCheck: false, defaultCapacity: 16, maxSize: 64);

        gemPool = new ObjectPool<Collectible>(
            createFunc: () =>
            {
                var go = new GameObject("Gem");
                go.transform.SetParent(transform, false);
                return go.AddComponent<Collectible>();
            },
            actionOnGet: g => g.gameObject.SetActive(true),
            actionOnRelease: g => g.gameObject.SetActive(false),
            actionOnDestroy: g => { if (g != null) Destroy(g.gameObject); },
            collectionCheck: false, defaultCapacity: 16, maxSize: 64);
    }

    public Obstacle Spawn(ObstacleData data, SpawnDirection dir, float lane)
    {
        var o = obstaclePool.Get();
        active.Add(o);
        o.Init(this, data, dir, lane, PlayArea);
        return o;
    }

    public void Release(Obstacle o)
    {
        active.Remove(o);
        obstaclePool.Release(o);
    }

    public void SpawnGem(Vector2 position)
    {
        var g = gemPool.Get();
        g.Init(this, position, PlayArea);
    }

    public void ReleaseGem(Collectible g) => gemPool.Release(g);

    public void NotifyGemCollected() => GemCollected?.Invoke();

    // ---------------------------------------------------------------- truy vấn cho Director

    /// <summary>Số obstacle kiểu "làn" (Strike/Beam) đang hoạt động.</summary>
    public int CountLaneBased()
    {
        int n = 0;
        for (int i = 0; i < active.Count; i++)
            if (active[i].IsLaneBased) n++;
        return n;
    }

    /// <summary>Làn mới phải cách các làn cùng trục đang hoạt động ít nhất 'spacing' (tránh bịt kín màn hình).</summary>
    public bool IsLaneFree(bool verticalLine, float lane, float spacing)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var o = active[i];
            if (!o.IsLaneBased) continue;
            if (o.LaneIsVerticalLine == verticalLine && Mathf.Abs(o.Lane - lane) < spacing) return false;
        }
        return true;
    }
}
