using UnityEngine;

/// <summary>
/// Một biome (Rừng / Hang động / Núi lửa...): giao diện, hệ số độ khó và danh mục obstacle riêng.
/// Tạo bằng menu DinoDash > Create Default Obstacles and Biomes, hoặc Create > DinoDash > Biome Data.
/// </summary>
[CreateAssetMenu(menuName = "DinoDash/Biome Data", fileName = "NewBiome")]
public class BiomeData : ScriptableObject
{
    [Header("Nhận diện")]
    public string displayName = "Biome";
    [TextArea] public string tagline = "";
    [Tooltip("Biome bắt đầu khi quãng đường (mét) đạt giá trị này.")]
    public float startDistance = 0f;

    [Header("Giao diện")]
    public Color backgroundColor = new Color(0.04f, 0.07f, 0.12f);
    public Color particleColor = new Color(0.6f, 0.8f, 1f);
    [Tooltip("Tốc độ trôi dọc của hạt nền: dương = bay lên (tro bụi), âm = rơi xuống (giọt nước).")]
    public float particleDrift = 0f;

    [Header("Gameplay")]
    public ObstacleCatalog catalog;
    [Range(0.8f, 1.5f)] [Tooltip("Nhân với tốc độ cuộn của màn chơi.")]
    public float speedMultiplier = 1f;
    [Range(0.5f, 2f)] [Tooltip("Nhân với tốc độ tích ngân sách độ khó (cao hơn = nhiều obstacle hơn).")]
    public float budgetMultiplier = 1f;

    void OnValidate()
    {
        startDistance = Mathf.Max(0f, startDistance);
    }
}
