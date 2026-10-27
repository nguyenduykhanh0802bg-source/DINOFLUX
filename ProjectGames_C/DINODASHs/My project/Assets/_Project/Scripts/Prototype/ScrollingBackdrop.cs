using UnityEngine;

/// <summary>
/// Các hạt nền trôi theo tốc độ cuộn (parallax) để cảm nhận được tốc độ.
/// BiomeManager đổi màu (SetTint) và hướng trôi dọc (SetDrift) theo từng biome.
/// </summary>
public class ScrollingBackdrop : MonoBehaviour
{
    struct Star
    {
        public Transform t;
        public SpriteRenderer sr;
        public float factor;
        public float alpha;
    }

    Star[] stars;
    Rect area;
    RunManager run;
    float drift;

    public void Init(Rect playArea, RunManager runManager, int count = 40)
    {
        area = playArea;
        run = runManager;
        stars = new Star[count];

        var sprite = PlaceholderSprites.Get(ShapeType.Square);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Star");
            go.transform.SetParent(transform, false);
            float size = Random.Range(0.04f, 0.16f);
            go.transform.localScale = new Vector3(size, size, 1f);
            go.transform.position = new Vector3(
                Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax), 0f);

            float alpha = Random.Range(0.12f, 0.4f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.6f, 0.8f, 1f, alpha);
            sr.sortingOrder = -10;

            stars[i] = new Star { t = go.transform, sr = sr, alpha = alpha, factor = Mathf.Lerp(0.15f, 0.7f, size / 0.16f) };
        }
    }

    /// <summary>Đổi màu hạt nhưng giữ độ trong suốt riêng của từng hạt.</summary>
    public void SetTint(Color c)
    {
        if (stars == null) return;
        for (int i = 0; i < stars.Length; i++)
            if (stars[i].sr != null) stars[i].sr.color = new Color(c.r, c.g, c.b, stars[i].alpha);
    }

    /// <summary>Dương = hạt bay lên (tro bụi), âm = rơi xuống (giọt nước).</summary>
    public void SetDrift(float value)
    {
        drift = value;
    }

    void Update()
    {
        if (stars == null || run == null) return;
        float dt = Time.deltaTime;

        for (int i = 0; i < stars.Length; i++)
        {
            Vector3 p = stars[i].t.position;
            p.x -= run.ScrollSpeed * stars[i].factor * dt;
            p.y += drift * stars[i].factor * 2f * dt;

            if (p.x < area.xMin - 0.5f)
            {
                p.x = area.xMax + 0.5f;
                p.y = Random.Range(area.yMin, area.yMax);
            }
            if (p.y > area.yMax + 0.5f) p.y = area.yMin - 0.5f;
            else if (p.y < area.yMin - 0.5f) p.y = area.yMax + 0.5f;

            stars[i].t.position = p;
        }
    }
}
