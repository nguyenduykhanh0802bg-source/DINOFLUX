using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Định nghĩa mặc định của 3 biome. Dùng cho 2 việc:
/// 1) Editor generator ghi ra asset (.asset) để chỉnh trong Inspector.
/// 2) Fallback (tạo trong bộ nhớ) khi PrototypeBootstrap chưa được gán danh sách Biomes.
/// Obstacle được tham chiếu bằng hằng số tên trong DefaultObstacles (sai tên = lỗi biên dịch, không phải lỗi âm thầm).
/// </summary>
public static class DefaultBiomes
{
    public struct Def
    {
        public string assetName;      // dùng cho tên file: Biome_{assetName}, Catalog_{assetName}
        public string displayName;
        public string tagline;
        public float startDistance;
        public Color background;
        public Color particle;
        public float drift;
        public float speedMultiplier;
        public float budgetMultiplier;
        public string[] obstacleNames;
    }

    public static readonly Def[] Definitions =
    {
        new Def
        {
            assetName = "Forest",
            displayName = "Rừng rậm",
            tagline = "Làm quen: né gai, đá rơi và cưa xoay.",
            startDistance = 0f,
            background = new Color(0.04f, 0.09f, 0.07f),
            particle = new Color(0.55f, 0.9f, 0.6f),
            drift = 0.15f,
            speedMultiplier = 1f,
            budgetMultiplier = 1f,
            obstacleNames = new[]
            {
                DefaultObstacles.Spike, DefaultObstacles.FallingRock, DefaultObstacles.SpinningSaw,
                DefaultObstacles.Laser, DefaultObstacles.ChargingRock
            }
        },
        new Def
        {
            assetName = "Cave",
            displayName = "Hang động",
            tagline = "Cẩn thận: dơi truy đuổi và sóng xung kích!",
            startDistance = 500f,
            background = new Color(0.05f, 0.05f, 0.11f),
            particle = new Color(0.55f, 0.65f, 1f),
            drift = -0.5f,
            speedMultiplier = 1.05f,
            budgetMultiplier = 1.05f,
            obstacleNames = new[]
            {
                DefaultObstacles.Spike, DefaultObstacles.SpinningSaw, DefaultObstacles.Laser,
                DefaultObstacles.Stalactite, DefaultObstacles.Bat, DefaultObstacles.Shockwave
            }
        },
        new Def
        {
            assetName = "Volcano",
            displayName = "Núi lửa",
            tagline = "Dung nham, xoáy nhiệt và tia laze xoay!",
            startDistance = 1200f,
            background = new Color(0.13f, 0.04f, 0.03f),
            particle = new Color(1f, 0.55f, 0.2f),
            drift = 0.9f,
            speedMultiplier = 1.1f,
            budgetMultiplier = 1.1f,
            obstacleNames = new[]
            {
                DefaultObstacles.Spike, DefaultObstacles.Laser, DefaultObstacles.ChargingRock,
                DefaultObstacles.Shockwave, DefaultObstacles.Geyser, DefaultObstacles.LavaBall,
                DefaultObstacles.HeatVortex, DefaultObstacles.RotatingLaser
            }
        }
    };

    /// <summary>Ghi thông số của Def vào một BiomeData (không đụng tới catalog).</summary>
    public static void ApplyDef(BiomeData b, Def def)
    {
        b.name = "Biome_" + def.assetName;
        b.displayName = def.displayName;
        b.tagline = def.tagline;
        b.startDistance = def.startDistance;
        b.backgroundColor = def.background;
        b.particleColor = def.particle;
        b.particleDrift = def.drift;
        b.speedMultiplier = def.speedMultiplier;
        b.budgetMultiplier = def.budgetMultiplier;
    }

    /// <summary>Tạo bản chạy tạm trong bộ nhớ (không ghi ra đĩa) - dùng làm fallback.</summary>
    public static List<BiomeData> BuildRuntime()
    {
        var byName = new Dictionary<string, ObstacleData>();
        foreach (ObstacleData o in DefaultObstacles.CreateAll()) byName[o.name] = o;

        var list = new List<BiomeData>();
        for (int i = 0; i < Definitions.Length; i++)
        {
            Def def = Definitions[i];

            var catalog = ScriptableObject.CreateInstance<ObstacleCatalog>();
            catalog.name = "Catalog_" + def.assetName;
            for (int n = 0; n < def.obstacleNames.Length; n++)
                if (byName.TryGetValue(def.obstacleNames[n], out ObstacleData data)) catalog.obstacles.Add(data);

            var biome = ScriptableObject.CreateInstance<BiomeData>();
            ApplyDef(biome, def);
            biome.catalog = catalog;
            list.Add(biome);
        }
        return list;
    }
}
