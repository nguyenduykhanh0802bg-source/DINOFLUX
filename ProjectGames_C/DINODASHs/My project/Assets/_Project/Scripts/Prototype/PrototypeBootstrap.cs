using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dựng toàn bộ prototype từ code: gắn script này vào MỘT GameObject trống trong scene trống rồi bấm Play.
/// Placeholder sprite, catalog obstacle, HUD... đều được sinh ra ở đây.
/// Khi chuyển sang bản thật: thay các phần này bằng prefab + ScriptableObject asset.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PrototypeBootstrap : MonoBehaviour
{
    public Color backgroundColor = new Color(0.04f, 0.07f, 0.12f);

    [Header("Dữ liệu obstacle")]
    [Tooltip("Kéo asset ObstacleCatalog vào đây. Để trống = dùng bản mặc định trong code (DefaultObstacles).")]
    public ObstacleCatalog obstacleCatalog;

    GameObject root;
    Camera cam;
    CameraShake shake;
    Rect area;
    bool restarting;
    List<ObstacleData> fallbackCatalog; // chỉ tạo 1 lần để restart không sinh thêm ScriptableObject rác

    void Awake()
    {
        Time.timeScale = 1f;
        SetupCamera();
        Build();
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        if (!cam.TryGetComponent(out shake)) shake = cam.gameObject.AddComponent<CameraShake>();

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        area = new Rect(-halfW, -halfH, halfW * 2f, halfH * 2f);
    }

    void Build()
    {
        Time.timeScale = 1f;
        root = new GameObject("DinoDash_Run");

        var spawnerGo = new GameObject("Spawner");
        spawnerGo.transform.SetParent(root.transform, false);
        var spawner = spawnerGo.AddComponent<ObstacleSpawner>();
        spawner.Setup(area);

        var controller = BuildPlayer(root.transform, out PlayerHealth health);

        var runGo = new GameObject("Run");
        runGo.transform.SetParent(root.transform, false);
        var run = runGo.AddComponent<RunManager>();
        run.player = health;
        run.spawner = spawner;
        run.cameraShake = shake;
        run.RestartRequested = OnRestartRequested;

        var directorGo = new GameObject("Director");
        directorGo.transform.SetParent(root.transform, false);
        var director = directorGo.AddComponent<DifficultyDirector>();
        director.run = run;
        director.spawner = spawner;
        director.player = controller.transform;
        director.catalog = ResolveCatalog();

        var backdropGo = new GameObject("Backdrop");
        backdropGo.transform.SetParent(root.transform, false);
        backdropGo.AddComponent<ScrollingBackdrop>().Init(area, run);

        var hudGo = new GameObject("Hud");
        hudGo.transform.SetParent(root.transform, false);
        var hud = hudGo.AddComponent<PrototypeHud>();
        hud.run = run;
        hud.health = health;
        hud.controller = controller;
    }

    void OnRestartRequested()
    {
        if (restarting) return;
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        restarting = true;
        Time.timeScale = 1f;
        Destroy(root);
        yield return null;
        Build();
        restarting = false;
    }

    // ---------------------------------------------------------------- dino placeholder

    PlayerController BuildPlayer(Transform parent, out PlayerHealth health)
    {
        var go = new GameObject("Dino");
        go.transform.SetParent(parent, false);
        go.tag = "Player";
        go.transform.position = new Vector3(area.xMin + area.width * 0.22f, 0f, 0f);

        health = go.AddComponent<PlayerHealth>();

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f; // hitbox nhỏ hơn sprite (nửa thân = 0.45) -> cảm giác công bằng

        var visual = new GameObject("Visual").transform;
        visual.SetParent(go.transform, false);
        AddSprite(visual, "Tail",  ShapeType.Square, new Color(0.25f, 0.65f, 0.3f), new Vector2(0.45f, 0.25f), new Vector2(-0.55f, -0.12f), 9);
        AddSprite(visual, "Body",  ShapeType.Square, new Color(0.35f, 0.85f, 0.4f), new Vector2(0.9f, 0.9f), Vector2.zero, 10);
        AddSprite(visual, "Belly", ShapeType.Square, new Color(0.7f, 0.95f, 0.6f), new Vector2(0.5f, 0.4f), new Vector2(-0.02f, -0.22f), 11);
        AddSprite(visual, "Eye",   ShapeType.Square, Color.white, new Vector2(0.24f, 0.24f), new Vector2(0.2f, 0.18f), 12);
        AddSprite(visual, "Pupil", ShapeType.Square, Color.black, new Vector2(0.11f, 0.11f), new Vector2(0.26f, 0.18f), 13);

        var controller = go.AddComponent<PlayerController>(); // tự thêm Rigidbody2D nhờ RequireComponent
        controller.visual = visual;
        controller.SetBounds(area);
        return controller;
    }

    static SpriteRenderer AddSprite(Transform parent, string name, ShapeType shape, Color color,
                                    Vector2 size, Vector2 localPos, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PlaceholderSprites.Get(shape);
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // ---------------------------------------------------------------- catalog obstacle

    List<ObstacleData> ResolveCatalog()
    {
        if (obstacleCatalog != null)
        {
            List<ObstacleData> list = obstacleCatalog.ToList();
            if (list.Count > 0) return list;
            Debug.LogWarning("[DinoDash] ObstacleCatalog đang trống - dùng bản mặc định trong code.", obstacleCatalog);
        }
        else
        {
            Debug.LogWarning("[DinoDash] Chưa gán Obstacle Catalog cho PrototypeBootstrap - dùng bản mặc định trong code. " +
                             "Chạy menu DinoDash > Create Default Obstacle Assets để tạo asset chỉnh được trong Inspector.");
        }

        if (fallbackCatalog == null) fallbackCatalog = DefaultObstacles.CreateAll();
        return fallbackCatalog;
    }
}
