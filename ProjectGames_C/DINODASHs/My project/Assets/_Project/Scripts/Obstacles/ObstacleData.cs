using UnityEngine;

/// <summary>
/// Dữ liệu của một loại obstacle. Thêm obstacle mới = tạo asset
/// (Create > DinoDash > Obstacle Data) rồi kéo vào catalog của DifficultyDirector.
/// </summary>
[CreateAssetMenu(menuName = "DinoDash/Obstacle Data", fileName = "NewObstacle")]
public class ObstacleData : ScriptableObject
{
    [Header("Nhận diện")]
    public string displayName = "Obstacle";
    [TextArea] public string recapTip = "Quan sát kỹ rồi dash qua.";
    public ShapeType shape = ShapeType.Square;
    public Color color = Color.white;
    [Tooltip("Kích thước thế giới (unit). Với Beam: y = độ dày tia.")]
    public Vector2 size = Vector2.one;
    [Range(0.4f, 1f)] [Tooltip("Hitbox so với sprite. Nhỏ hơn 1 = công bằng hơn.")]
    public float hitboxScale = 0.8f;

    [Header("Hành vi")]
    public ObstacleBehavior behavior = ObstacleBehavior.Scrolling;
    public SpawnDirection[] allowedDirections = { SpawnDirection.Right };
    [Tooltip("Thời gian cảnh báo trước khi nguy hiểm (Strike/Beam). Khuyến nghị 0.6 - 1.0s.")]
    public float telegraphTime = 0.8f;
    [Tooltip("Thời gian tia bật (Beam).")]
    public float activeTime = 0.7f;
    public float strikeSpeed = 14f;
    [Tooltip("Hệ số nhân tốc độ cuộn (Scrolling/Sine).")]
    public float scrollMultiplier = 1f;
    public float sineAmplitude = 2f;
    public float sineFrequency = 1.5f;
    [Tooltip("Độ/giây.")]
    public float spinSpeed = 0f;

    [Header("Độ khó")]
    public int damage = 1;
    [Tooltip("Điểm 'ngân sách' mà Difficulty Director phải trả để spawn.")]
    public int difficultyCost = 2;
    public float spawnWeight = 1f;
    [Tooltip("Chỉ xuất hiện sau khi đạt quãng đường này (mét). Dùng để giới thiệu từng obstacle một.")]
    public float minDistance = 0f;

    /// <summary>Chặn các giá trị làm game hỏng hoặc bất công khi chỉnh trong Inspector.</summary>
    void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        damage = Mathf.Max(1, damage);
        difficultyCost = Mathf.Max(1, difficultyCost);
        spawnWeight = Mathf.Max(0.01f, spawnWeight);
        minDistance = Mathf.Max(0f, minDistance);

        // Luật công bằng: nguy hiểm kiểu Strike/Beam luôn phải có cảnh báo tối thiểu 0.4s.
        if (behavior == ObstacleBehavior.Strike || behavior == ObstacleBehavior.Beam)
            telegraphTime = Mathf.Max(0.4f, telegraphTime);

        if (allowedDirections == null || allowedDirections.Length == 0)
            allowedDirections = new[] { SpawnDirection.Right };
    }
}
