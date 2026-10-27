using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 12 obstacle mặc định cho 3 biome (Rừng / Hang động / Núi lửa). Dùng cho 2 việc:
/// 1) Editor generator ghi chúng ra asset (.asset) để chỉnh trong Inspector.
/// 2) Fallback khi PrototypeBootstrap chưa được gán dữ liệu biome.
/// Sau khi đã có asset, hãy chỉnh trên asset, không chỉnh ở đây.
/// Tên (hằng số bên dưới) cũng là tên file .asset và là khóa mà DefaultBiomes tham chiếu.
/// </summary>
public static class DefaultObstacles
{
    public const string Spike = "Obstacle_01_Spike";
    public const string FallingRock = "Obstacle_02_FallingRock";
    public const string SpinningSaw = "Obstacle_03_SpinningSaw";
    public const string Laser = "Obstacle_04_Laser";
    public const string ChargingRock = "Obstacle_05_ChargingRock";
    public const string Stalactite = "Obstacle_06_Stalactite";
    public const string Bat = "Obstacle_07_Bat";
    public const string Shockwave = "Obstacle_08_Shockwave";
    public const string Geyser = "Obstacle_09_Geyser";
    public const string LavaBall = "Obstacle_10_LavaBall";
    public const string HeatVortex = "Obstacle_11_HeatVortex";
    public const string RotatingLaser = "Obstacle_12_RotatingLaser";

    public static List<ObstacleData> CreateAll()
    {
        return new List<ObstacleData>
        {
            // ===== Rừng rậm (0 - 500m): làm quen từng loại một =====

            // 0m: gai trôi - dạy "đọc không gian".
            // Right = chặn một làn giữa màn hình (mối đe dọa thật). Top/Bottom = gai ở mép, chỉ nguy hiểm khi đứng sát mép.
            // (P1 chỉ có Top/Bottom nên người chơi đứng giữa màn hình gần như không bao giờ dính gai.)
            Make(Spike, d =>
            {
                d.displayName = "Gai nhọn";
                d.recapTip = "Gai trôi từ bên phải - đổi làn sớm hoặc dash qua.";
                d.shape = ShapeType.Triangle;
                d.color = new Color(0.85f, 0.88f, 0.95f);
                d.size = new Vector2(1.4f, 1.1f);
                d.behavior = ObstacleBehavior.Scrolling;
                d.allowedDirections = new[] { SpawnDirection.Right, SpawnDirection.Bottom, SpawnDirection.Top };
                d.difficultyCost = 1; d.spawnWeight = 3f; d.minDistance = 0f;
            }),
            // 40m: đá rơi - dạy "phản ứng theo cảnh báo"
            Make(FallingRock, d =>
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
            Make(SpinningSaw, d =>
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
            Make(Laser, d =>
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
            Make(ChargingRock, d =>
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

            // ===== Hang động (500 - 1200m): truy đuổi + vùng nguy hiểm lan rộng =====

            // 500m: nhũ đá - rơi từ trần, mũi nhọn tự xoay theo chiều rơi
            Make(Stalactite, d =>
            {
                d.displayName = "Nhũ đá";
                d.recapTip = "Nhũ đá rơi từ trần hang - vạch đỏ chỉ đúng làn sẽ rơi, rời làn đó trước khi vạch nhấp nháy nhanh.";
                d.shape = ShapeType.Triangle;
                d.color = new Color(0.55f, 0.62f, 0.75f);
                d.size = new Vector2(0.9f, 1.6f);
                d.behavior = ObstacleBehavior.Strike;
                d.allowedDirections = new[] { SpawnDirection.Top };
                d.telegraphTime = 0.7f; d.strikeSpeed = 20f;
                d.difficultyCost = 2; d.spawnWeight = 2.5f; d.minDistance = 500f;
            }),
            // 560m: dơi - bám theo độ cao của bạn, nhưng chốt hướng khi đến gần
            Make(Bat, d =>
            {
                d.displayName = "Dơi";
                d.recapTip = "Dơi bám theo độ cao của bạn nhưng chốt hướng khi đến gần - đổi độ cao sát phút cuối hoặc dash xuyên qua.";
                d.shape = ShapeType.Bat;
                d.color = new Color(0.72f, 0.45f, 0.95f);
                d.size = new Vector2(1.1f, 0.8f);
                d.hitboxScale = 0.7f;
                d.behavior = ObstacleBehavior.Chase;
                d.allowedDirections = new[] { SpawnDirection.Right };
                d.telegraphTime = 0.9f;
                d.strikeSpeed = 8f;   // tốc độ bay ngang
                d.chaseSpeed = 2.6f;  // tốc độ bám độ cao (nhỏ hơn tốc độ dino 7.5)
                d.difficultyCost = 3; d.spawnWeight = 1.5f; d.minDistance = 560f;
            }),
            // 650m: sóng xung kích - vòng tròn lan ra từ chấm cảnh báo
            Make(Shockwave, d =>
            {
                d.displayName = "Sóng xung kích";
                d.recapTip = "Sóng lan ra từ chấm nhấp nháy - chạy ra xa hoặc dash xuyên qua đúng lúc vành sóng chạm bạn.";
                d.shape = ShapeType.Ring;
                d.color = new Color(0.4f, 0.85f, 1f);
                d.size = new Vector2(1f, 1f);
                d.hitboxScale = 1f;
                d.behavior = ObstacleBehavior.Shockwave;
                d.allowedDirections = new[] { SpawnDirection.Right };
                d.telegraphTime = 1f;
                d.strikeSpeed = 6.5f; // tốc độ lan của bán kính
                d.maxRadius = 6f;
                d.difficultyCost = 4; d.spawnWeight = 1.2f; d.minDistance = 650f;
            }),

            // ===== Núi lửa (1200m+): nhanh, nhiều thứ thay đổi luật di chuyển =====

            // 1200m: cột dung nham - phun từ đáy lên
            Make(Geyser, d =>
            {
                d.displayName = "Cột dung nham";
                d.recapTip = "Cột dung nham phun lên từ đáy - vạch đỏ dọc cho biết làn nào, rời đi trước khi vạch nhấp nháy nhanh.";
                d.shape = ShapeType.Square;
                d.color = new Color(1f, 0.5f, 0.1f);
                d.size = new Vector2(1f, 2.6f);
                d.behavior = ObstacleBehavior.Strike;
                d.allowedDirections = new[] { SpawnDirection.Bottom };
                d.telegraphTime = 0.9f; d.strikeSpeed = 19f;
                d.difficultyCost = 3; d.spawnWeight = 2f; d.minDistance = 1200f;
            }),
            // 1300m: bóng dung nham - nảy nhanh hơn cưa
            Make(LavaBall, d =>
            {
                d.displayName = "Bóng dung nham";
                d.recapTip = "Bóng dung nham nảy lên-xuống nhanh hơn cưa - canh nhịp rồi lướt qua giữa hai nhịp nảy.";
                d.shape = ShapeType.Circle;
                d.color = new Color(1f, 0.38f, 0.12f);
                d.size = new Vector2(1f, 1f);
                d.behavior = ObstacleBehavior.Sine;
                d.allowedDirections = new[] { SpawnDirection.Right };
                d.sineAmplitude = 2.6f; d.sineFrequency = 2.1f; d.scrollMultiplier = 1.2f;
                d.difficultyCost = 3; d.spawnWeight = 2f; d.minDistance = 1300f;
            }),
            // 1450m: xoáy nhiệt - thay đổi luật di chuyển (kéo bạn về lõi)
            Make(HeatVortex, d =>
            {
                d.displayName = "Xoáy nhiệt";
                d.recapTip = "Xoáy nhiệt hút bạn về lõi đỏ ở giữa - giữ khoảng cách, dash để thoát (dash không bị hút).";
                d.shape = ShapeType.Diamond;
                d.color = new Color(1f, 0.25f, 0.2f);
                d.size = new Vector2(0.9f, 0.9f);
                d.behavior = ObstacleBehavior.GravityField;
                d.allowedDirections = new[] { SpawnDirection.Right };
                d.scrollMultiplier = 0.8f; d.spinSpeed = 200f;
                d.fieldRadius = 3.2f; d.fieldStrength = 5f; // yếu hơn tốc độ dino (7.5): luôn thoát được
                d.difficultyCost = 4; d.spawnWeight = 1.2f; d.minDistance = 1450f;
            }),
            // 1650m: laze xoay - tia quét quanh một trục
            Make(RotatingLaser, d =>
            {
                d.displayName = "Laze xoay";
                d.recapTip = "Tia quét quanh một trục - vạch mảnh chỉ hướng bắt đầu; luôn rời xa trục hoặc dash xuyên qua khi tia quét tới.";
                d.shape = ShapeType.Square;
                d.color = new Color(1f, 0.25f, 0.35f);
                d.size = new Vector2(1f, 0.45f);
                d.behavior = ObstacleBehavior.RotatingBeam;
                d.allowedDirections = new[] { SpawnDirection.Right };
                d.telegraphTime = 1.2f; d.activeTime = 3.2f; d.spinSpeed = 62f;
                d.difficultyCost = 5; d.spawnWeight = 1f; d.minDistance = 1650f;
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
