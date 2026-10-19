using UnityEngine;

/// <summary>Các hạt nền trôi theo tốc độ cuộn (parallax) để cảm nhận được tốc độ.</summary>
public class ScrollingBackdrop : MonoBehaviour
{
    struct Star
    {
        public Transform t;
        public float factor;
    }

    Star[] stars;
    Rect area;
    RunManager run;

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

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.6f, 0.8f, 1f, Random.Range(0.12f, 0.4f));
            sr.sortingOrder = -10;

            stars[i] = new Star { t = go.transform, factor = Mathf.Lerp(0.15f, 0.7f, size / 0.16f) };
        }
    }

    void Update()
    {
        if (stars == null || run == null) return;
        float dt = Time.deltaTime;

        for (int i = 0; i < stars.Length; i++)
        {
            Vector3 p = stars[i].t.position;
            p.x -= run.ScrollSpeed * stars[i].factor * dt;
            if (p.x < area.xMin - 0.5f)
            {
                p.x = area.xMax + 0.5f;
                p.y = Random.Range(area.yMin, area.yMax);
            }
            stars[i].t.position = p;
        }
    }
}
