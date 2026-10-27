using UnityEngine;

/// <summary>Phía obstacle xuất hiện (Right = từ bên phải bay sang trái, Top = từ trên rơi xuống...).</summary>
public enum SpawnDirection { Top, Bottom, Left, Right }

/// <summary>
/// Hành vi của obstacle. Thêm hành vi mới = thêm 1 giá trị ở CUỐI enum (để không làm lệch asset đã lưu)
/// rồi xử lý trong Obstacle.cs và DifficultyDirector.TrySpawn.
/// </summary>
public enum ObstacleBehavior
{
    Scrolling,    // trôi theo tốc độ cuộn (gai sàn/trần)
    Sine,         // trôi + dao động lên xuống (cưa xoay, bóng dung nham)
    Strike,       // cảnh báo trên một làn rồi lao qua (đá rơi, nhũ đá, cột dung nham)
    Beam,         // cảnh báo một đường rồi bật tia (laze)
    Chase,        // bay vào, bám theo độ cao người chơi rồi lao thẳng (quái vật bay)
    Shockwave,    // vòng sóng lan ra từ một điểm
    RotatingBeam, // tia laze quay quanh một trục
    GravityField  // vùng hút người chơi về lõi nguy hiểm (thay đổi luật di chuyển)
}

/// <summary>Hình placeholder. Chỉ thêm ở cuối enum.</summary>
public enum ShapeType { Square, Circle, Triangle, Gear, Diamond, Ring, Bat }

/// <summary>Cách sprite thật phản ứng với hướng spawn. Chỉ thêm ở cuối enum.</summary>
public enum SpriteFacing
{
    /// <summary>Giữ nguyên, không xoay/lật.</summary>
    Fixed,
    /// <summary>Sprite vẽ mũi/đầu hướng LÊN; tự xoay theo hướng lao (Strike) hoặc hướng gai nhô ra (Scrolling từ Top/Bottom).</summary>
    PointInDirection,
    /// <summary>Sprite vẽ quay mặt sang TRÁI (hướng bay mặc định); lật ngang khi spawn từ Left, lật dọc khi spawn từ Top.</summary>
    MirrorByDirection
}
