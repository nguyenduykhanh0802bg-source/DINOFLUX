using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sinh sprite placeholder bằng code để prototype chạy được mà không cần asset nào.
/// Thay bằng pixel art thật sau: chỉ cần gán sprite vào SpriteRenderer.
/// </summary>
public static class PlaceholderSprites
{
    const int Size = 64;
    static readonly Dictionary<ShapeType, Sprite> cache = new Dictionary<ShapeType, Sprite>();

    public static Sprite Get(ShapeType shape)
    {
        if (cache.TryGetValue(shape, out var cached) && cached != null) return cached;

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "Placeholder_" + shape
        };

        var pixels = new Color32[Size * Size];
        var white = new Color32(255, 255, 255, 255);
        var clear = new Color32(255, 255, 255, 0);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size * 2f - 1f;
                float v = (y + 0.5f) / Size * 2f - 1f;
                pixels[y * Size + x] = Inside(shape, u, v) ? white : clear;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        // Pixels-per-unit = Size => sprite luôn rộng đúng 1 unit, scale transform = kích thước thế giới.
        var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        sprite.name = "Placeholder_" + shape;
        cache[shape] = sprite;
        return sprite;
    }

    static bool Inside(ShapeType shape, float u, float v)
    {
        switch (shape)
        {
            case ShapeType.Circle:
                return u * u + v * v <= 1f;
            case ShapeType.Triangle: // đỉnh hướng lên
                return Mathf.Abs(u) <= (1f - v) * 0.5f;
            case ShapeType.Diamond:
                return Mathf.Abs(u) + Mathf.Abs(v) <= 1f;
            case ShapeType.Gear:
            {
                float r = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Atan2(v, u);
                float limit = Mathf.Cos(8f * a) > 0f ? 1f : 0.8f; // 8 răng cưa
                return r <= limit && r >= 0.25f;                  // lỗ ở giữa
            }
            default:
                return true;
        }
    }
}
