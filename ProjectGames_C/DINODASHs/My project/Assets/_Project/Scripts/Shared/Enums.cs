using UnityEngine;

/// <summary>Phía obstacle xuất hiện (Right = từ bên phải bay sang trái, Top = từ trên rơi xuống...).</summary>
public enum SpawnDirection { Top, Bottom, Left, Right }

/// <summary>Hành vi của obstacle. Thêm hành vi mới = thêm 1 case trong Obstacle.cs.</summary>
public enum ObstacleBehavior
{
    Scrolling, // trôi theo tốc độ cuộn (gai sàn/trần)
    Sine,      // trôi + dao động lên xuống (cưa xoay)
    Strike,    // cảnh báo trên một làn rồi lao qua (đá rơi, đá lao ngang)
    Beam       // cảnh báo một đường rồi bật tia (laze)
}

public enum ShapeType { Square, Circle, Triangle, Gear, Diamond }
