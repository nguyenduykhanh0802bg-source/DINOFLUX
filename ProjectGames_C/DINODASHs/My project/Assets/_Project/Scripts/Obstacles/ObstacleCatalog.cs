using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Danh mục các ObstacleData mà DifficultyDirector được phép spawn.
/// Tạo bằng menu: DinoDash > Create Default Obstacles and Biomes
/// (hoặc Create > DinoDash > Obstacle Catalog rồi tự kéo asset vào danh sách).
/// Sau này mỗi biome có thể có một catalog riêng.
/// </summary>
[CreateAssetMenu(menuName = "DinoDash/Obstacle Catalog", fileName = "ObstacleCatalog")]
public class ObstacleCatalog : ScriptableObject
{
    public List<ObstacleData> obstacles = new List<ObstacleData>();

    /// <summary>Bản sao danh sách đã loại bỏ ô trống (null), an toàn để đưa cho Director.</summary>
    public List<ObstacleData> ToList()
    {
        var list = new List<ObstacleData>(obstacles.Count);
        for (int i = 0; i < obstacles.Count; i++)
            if (obstacles[i] != null) list.Add(obstacles[i]);
        return list;
    }
}
