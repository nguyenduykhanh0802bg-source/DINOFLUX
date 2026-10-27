using UnityEngine;

/// <summary>
/// Dữ liệu của một loại obstacle. Thêm obstacle mới = tạo asset
/// (Create > DinoDash > Obstacle Data) rồi kéo vào ObstacleCatalog của một biome.
/// </summary>
[CreateAssetMenu(menuName = "DinoDash/Obstacle Data", fileName = "NewObstacle")]
public class ObstacleData : ScriptableObject
{
    [Header("Nhận diện")]
    public string displayName = "Obstacle";
    [TextArea] public string recapTip = "Quan sát kỹ rồi dash qua.";
    public ShapeType shape = ShapeType.Square;
    public Color color = Color.white;

    [Tooltip("Sprite pixel-art thật. Để trống = dùng hình placeholder theo 'Shape' + 'Color'. " +
             "Sprite được co giãn vừa khít 'Size' (không phụ thuộc Pixels Per Unit).")]
    public Sprite sprite;
    [Tooltip("Chỉ áp dụng khi có Sprite: bật = nhân sprite với 'Color' (dùng cho hiệu ứng nhuộm màu); tắt = giữ nguyên màu gốc của sprite.")]
    public bool tintSprite = false;

    [Tooltip("Animation: danh sách khung hình, phát lặp theo 'Fps'. Từ 2 khung trở lên mới chạy animation; " +
             "chỉ 1 khung thì dùng như Sprite tĩnh. Để trống = dùng 'Sprite' / placeholder.")]
    public Sprite[] frames;
    [Min(0.5f)] public float fps = 8f;

    [Tooltip("Sprite thật phản ứng thế nào với hướng spawn: Fixed = giữ nguyên; PointInDirection = vẽ hướng LÊN, tự xoay theo hướng lao/gai; " +
             "MirrorByDirection = vẽ quay TRÁI, lật khi spawn từ Left/Top. Placeholder Triangle luôn tự xoay như cũ.")]
    public SpriteFacing facing = SpriteFacing.Fixed;

    [Tooltip("Sprite cho vạch cảnh báo hình chữ nhật (Strike/Beam/Chase/RotatingBeam), bị kéo giãn vừa vạch. " +
             "Nên vẽ màu trắng/xám để nhận màu cảnh báo đỏ. Trống = vạch màu đặc mặc định.")]
    public Sprite warningSprite;
    [Tooltip("Kích thước thế giới (unit). Beam/RotatingBeam: y = độ dày tia. GravityField: kích thước lõi nguy hiểm.")]
    public Vector2 size = Vector2.one;
    [Range(0.4f, 1f)] [Tooltip("Hitbox so với sprite. Nhỏ hơn 1 = công bằng hơn.")]
    public float hitboxScale = 0.8f;

    [Header("Hành vi")]
    public ObstacleBehavior behavior = ObstacleBehavior.Scrolling;
    public SpawnDirection[] allowedDirections = { SpawnDirection.Right };
    [Tooltip("Thời gian cảnh báo trước khi nguy hiểm (Strike/Beam/Chase/Shockwave/RotatingBeam). Khuyến nghị 0.6 - 1.2s, tối thiểu 0.4s.")]
    public float telegraphTime = 0.8f;
    [Tooltip("Thời gian nguy hiểm tồn tại (Beam / RotatingBeam).")]
    public float activeTime = 0.7f;
    [Tooltip("Strike: tốc độ lao. Chase: tốc độ bay ngang. Shockwave: tốc độ lan của bán kính. Đơn vị/giây.")]
    public float strikeSpeed = 14f;
    [Tooltip("Hệ số nhân tốc độ cuộn (Scrolling/Sine/GravityField).")]
    public float scrollMultiplier = 1f;
    public float sineAmplitude = 2f;
    public float sineFrequency = 1.5f;
    [Tooltip("Độ/giây. RotatingBeam: tốc độ quay của tia. Loại khác: chỉ xoay hình cho đẹp.")]
    public float spinSpeed = 0f;

    [Header("Hành vi nâng cao")]
    [Tooltip("Chase: tốc độ bám theo độ cao người chơi (đơn vị/giây). Nên nhỏ hơn tốc độ di chuyển của dino.")]
    public float chaseSpeed = 2.4f;
    [Tooltip("Shockwave: bán kính tối đa của vòng sóng.")]
    public float maxRadius = 7f;
    [Tooltip("GravityField: bán kính vùng ảnh hưởng.")]
    public float fieldRadius = 3.2f;
    [Tooltip("GravityField: lực kéo tại tâm (đơn vị/giây). Dương = hút, âm = đẩy.")]
    public float fieldStrength = 5f;

    [Header("Độ khó")]
    public int damage = 1;
    [Tooltip("Điểm 'ngân sách' mà Difficulty Director phải trả để spawn.")]
    public int difficultyCost = 2;
    public float spawnWeight = 1f;
    [Tooltip("Chỉ xuất hiện sau khi đạt quãng đường này (mét). Dùng để giới thiệu từng obstacle một.")]
    public float minDistance = 0f;

    /// <summary>Sprite sẽ vẽ: ưu tiên sprite thật, trống thì quay về placeholder theo shape.</summary>
    public Sprite ResolveSprite()
    {
        if (sprite != null) return sprite;
        Sprite f = FirstFrame();
        return f != null ? f : PlaceholderSprites.Get(shape);
    }

    /// <summary>Hình dùng cho vạch cảnh báo hình chữ nhật.</summary>
    public Sprite ResolveWarningSprite() => warningSprite != null ? warningSprite : PlaceholderSprites.Get(ShapeType.Square);

    /// <summary>Có xoay obstacle theo hướng lao/gai không (sprite thật theo 'facing'; placeholder chỉ Triangle).</summary>
    public bool RotatesToDirection => HasRealSprite ? facing == SpriteFacing.PointInDirection : shape == ShapeType.Triangle;

    /// <summary>Có sprite thật (tĩnh hoặc animation) hay đang dùng placeholder.</summary>
    public bool HasRealSprite => sprite != null || FirstFrame() != null;

    /// <summary>Có từ 2 khung hình hợp lệ trở lên -> chạy animation.</summary>
    public bool IsAnimated => frames != null && frames.Length > 1 && FirstFrame() != null;

    /// <summary>Khung hình tại thời điểm t (giây) kể từ lúc spawn; khung trống được bỏ qua.</summary>
    public Sprite FrameAt(float t)
    {
        if (!IsAnimated) return ResolveSprite();
        int i = Mathf.FloorToInt(Mathf.Max(0f, t) * Mathf.Max(0.5f, fps)) % frames.Length;
        return frames[i] != null ? frames[i] : ResolveSprite();
    }

    Sprite FirstFrame()
    {
        if (frames == null) return null;
        foreach (Sprite f in frames) if (f != null) return f;
        return null;
    }

    /// <summary>Màu nhân vào SpriteRenderer: sprite thật giữ màu gốc (trừ khi bật tintSprite).</summary>
    public Color BodyColor => (HasRealSprite && !tintSprite) ? Color.white : color;

    /// <summary>Chặn các giá trị làm game hỏng hoặc bất công khi chỉnh trong Inspector.</summary>
    void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        damage = Mathf.Max(1, damage);
        difficultyCost = Mathf.Max(1, difficultyCost);
        spawnWeight = Mathf.Max(0.01f, spawnWeight);
        minDistance = Mathf.Max(0f, minDistance);
        fps = Mathf.Max(0.5f, fps);
        activeTime = Mathf.Max(0.1f, activeTime);
        chaseSpeed = Mathf.Max(0f, chaseSpeed);
        maxRadius = Mathf.Max(1f, maxRadius);
        fieldRadius = Mathf.Max(1f, fieldRadius);

        // Luật công bằng: mọi nguy hiểm có telegraph phải cảnh báo tối thiểu 0.4s.
        switch (behavior)
        {
            case ObstacleBehavior.Strike:
            case ObstacleBehavior.Beam:
            case ObstacleBehavior.Chase:
            case ObstacleBehavior.Shockwave:
            case ObstacleBehavior.RotatingBeam:
                telegraphTime = Mathf.Max(0.4f, telegraphTime);
                break;
        }

        if (allowedDirections == null || allowedDirections.Length == 0)
            allowedDirections = new[] { SpawnDirection.Right };
    }
}
