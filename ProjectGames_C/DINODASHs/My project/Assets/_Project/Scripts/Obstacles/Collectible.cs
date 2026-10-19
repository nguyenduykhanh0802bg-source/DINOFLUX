using UnityEngine;

/// <summary>Gem: trôi theo tốc độ cuộn, nhặt để tăng điểm; đủ N gem thì hồi 1 tim.</summary>
public class Collectible : MonoBehaviour
{
    ObstacleSpawner owner;
    SpriteRenderer sr;
    Rect area;
    float age, baseY;
    bool released;

    void Awake()
    {
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprites.Get(ShapeType.Diamond);
        sr.color = new Color(0.55f, 0.85f, 1f);
        sr.sortingOrder = 6;

        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.9f; // hào phóng hơn sprite để nhặt dễ chịu

        transform.localScale = new Vector3(0.5f, 0.5f, 1f);
    }

    public void Init(ObstacleSpawner spawner, Vector2 position, Rect playArea)
    {
        owner = spawner;
        area = playArea;
        baseY = position.y;
        age = Random.value * 6f;
        released = false;
        transform.position = position;
    }

    void Update()
    {
        if (released) return;

        float dt = Time.deltaTime;
        age += dt;

        Vector3 p = transform.position;
        p.x -= owner.ScrollSpeed * dt;
        p.y = baseY + Mathf.Sin(age * 3f) * 0.12f;
        transform.position = p;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 2f) * 15f);

        if (p.x < area.xMin - 1f) Despawn();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (released || !other.CompareTag("Player")) return;
        owner.NotifyGemCollected();
        Despawn();
    }

    void Despawn()
    {
        if (released) return;
        released = true;
        owner.ReleaseGem(this);
    }
}
