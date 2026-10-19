using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 5 obstacle mặc định của biome Rừng. Dùng cho 2 việc:
/// 1) Editor generator ghi chúng ra asset (.asset) để chỉnh trong Inspector.
/// 2) Fallback khi PrototypeBootstrap chưa được gán ObstacleCatalog.
/// Sau khi đã có asset, hãy chỉnh trên asset, không chỉnh ở đây.
/// </summary>
public static class DefaultObstacles
{
    public static List<ObstacleData> CreateAll()
    {
        return new List<ObstacleData>
        {
            // 0m: gai trôi - dạy "đọc không gian"
            Make("Obstacle_01_Spike", d =>
            {
                d.displayName = "Gai nhọn";
                d.recapTip = "Gai trôi từ bên phải - đổi làn sớm hoặc dash qua.";
                d.shape = ShapeType.Triangle;
                d.color = new Color(0.85f, 0.88f, 0.95f);
                d.size = new Vector2(1.4f, 1.1f);
                d.behavior = ObstacleBehavior.Scrolling;
                d.allowedDirections = new[] { SpawnDirection.Bottom, SpawnDirection.Top };
                d.difficultyCost = 1; d.spawnWeight = 3f; d.minDistance = 0f;
            }),
            // 40m: đá rơi - dạy "phản ứng theo cảnh báo"
            Make("Obstacle_02_FallingRock", d =>
            {
                d.displayName = "Đá rơi";
                d.recapTip = "Vạch đỏ báo trước đường rơi - rời khỏi làn khi vạch nhấp nháy nhanh.";
                d.shape = ShapeType.Circle;
                d.color = new Color(0.62f, 0.56f, 0.5f);
                d.size = new Vector2(1.1f, 1.1f);
                d.behavior = ObstacleBehavior.Strike;
                d.allowedDirections = new[] { SpawnDirection.Top };
                d.telegraphTime = 0.9f; d.strikeSpeed = 16f;
                d.difficultyCost = 2; d.spawnWeight = 2f; d.minDistance = 40f;
            }),
            // 120m: cưa xoay - dạy "canh nhịp"
            Make("Obstacle_03_SpinningSaw", d =>
            {
                d.displayName = "Cưa xoay";
                d.recapTip = "Cưa dao động lên-xuống theo chu kỳ - canh nhịp rồi dash qua.";
                d.shape = ShapeType.Gear;
                d.color = new Color(0.7f, 0.82f, 0.95f);
                d.size = new Vector2(1.2f, 1.2f);
                d.behavior = ObstacleBehavior.Sine;
                d.allowedDirections = new[] { SpawnDirection.Right };
                d.sineAmplitude = 2.2f; d.sineFrequency = 1.6f; d.spinSpeed = 360f;
                d.difficultyCost = 3; d.spawnWeight = 2f; d.minDistance = 120f;
            }),
            // 250m: laze - telegraph dài, hậu quả lớn
            Make("Obstacle_04_Laser", d =>
            {
                d.displayName = "Tia laze";
                d.recapTip = "Đường mảnh nhấp nháy là nơi tia sắp bật - rời đi hoặc dash xuyên qua lúc tia bật.";
                d.shape = ShapeType.Square;
                d.color = new Color(1f, 0.4f, 0.2f);
                d.size = new Vector2(1f, 0.55f);
                d.behavior = ObstacleBehavior.Beam;
                d.allowedDirections = new[] { SpawnDirection.Right, SpawnDirection.Top };
                d.telegraphTime = 1f; d.activeTime = 0.6f;
                d.difficultyCost = 4; d.spawnWeight = 1.5f; d.minDistance = 250f;
            }),
            // 350m: đá lao ngang/dọc từ nhiều phía
            Make("Obstacle_05_ChargingRock", d =>
            {
                d.displayName = "Đá lao tới";
                d.recapTip = "Cảnh báo cho biết hướng lao tới - kiểm tra cả 4 phía màn hình.";
                d.shape = ShapeType.Diamond;
                d.color = new Color(1f, 0.6f, 0.25f);
                d.size = new Vector2(1f, 1f);
                d.behavior = ObstacleBehavior.Strike;
                d.allowedDirections = new[] { SpawnDirection.Left, SpawnDirection.Right, SpawnDirection.Bottom };
                d.telegraphTime = 0.8f; d.strikeSpeed = 18f;
                d.difficultyCost = 3; d.spawnWeight = 2f; d.minDistance = 350f;
            }),
        };
    }

    /// <summary>Tên (d.name) cũng là tên file asset khi Editor generator ghi ra đĩa.</summary>
    static ObstacleData Make(string assetName, System.Action<ObstacleData> setup)
    {
        var d = ScriptableObject.CreateInstance<ObstacleData>();
        d.name = assetName;
        setup(d);
        return d;
    }
}
