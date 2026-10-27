#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu Editor: DinoDash > Create Default Obstacles and Biomes
///
/// Tạo trong Assets/_Project/Data/:
///   Obstacles/  12 asset ObstacleData + ObstacleCatalog tổng (chứa tất cả)
///   Biomes/     3 BiomeData (Rừng / Hang động / Núi lửa) + 3 ObstacleCatalog riêng (Catalog_Forest...)
/// rồi tự gán danh sách biome vào PrototypeBootstrap trong scene đang mở.
///
/// AN TOÀN khi chạy lại: không bao giờ ghi đè asset đã có (giữ nguyên số liệu bạn đã chỉnh).
/// Catalog đã tồn tại chỉ được thêm các obstacle MỚI tạo trong lần chạy này, nên obstacle bạn cố ý
/// gỡ khỏi một catalog sẽ không bị thêm lại.
/// </summary>
public static class ObstacleAssetGenerator
{
    const string DataFolder = "Assets/_Project/Data";
    const string ObstacleFolder = DataFolder + "/Obstacles";
    const string BiomeFolder = DataFolder + "/Biomes";
    const string MasterCatalogPath = ObstacleFolder + "/ObstacleCatalog.asset";

    [MenuItem("DinoDash/Create Default Obstacles and Biomes")]
    public static void CreateAll()
    {
        // ---- 1) Obstacle assets + catalog tổng
        EnsureFolder(ObstacleFolder);

        var master = LoadOrCreateCatalog(MasterCatalogPath, out bool masterIsNew);

        var obstacles = new Dictionary<string, ObstacleData>();
        var createdNames = new HashSet<string>();
        int obstaclesKept = 0;

        foreach (ObstacleData def in DefaultObstacles.CreateAll())
        {
            string key = def.name;
            string path = $"{ObstacleFolder}/{key}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ObstacleData>(path);

            if (existing != null)
            {
                Object.DestroyImmediate(def); // bản tạm trong bộ nhớ, bỏ đi
                obstacles[key] = existing;
                obstaclesKept++;
            }
            else
            {
                AssetDatabase.CreateAsset(def, path);
                obstacles[key] = def;
                createdNames.Add(key);
            }
        }

        foreach (var kv in obstacles)
            if (masterIsNew || createdNames.Contains(kv.Key)) AddIfMissing(master, kv.Value);
        EditorUtility.SetDirty(master);

        // ---- 2) Biome assets
        EnsureFolder(BiomeFolder);

        var biomes = new List<BiomeData>();
        int biomesCreated = 0, biomesKept = 0;

        foreach (DefaultBiomes.Def def in DefaultBiomes.Definitions)
        {
            string catalogPath = $"{BiomeFolder}/Catalog_{def.assetName}.asset";
            var catalog = LoadOrCreateCatalog(catalogPath, out bool catalogIsNew);

            foreach (string obstacleName in def.obstacleNames)
            {
                if (!obstacles.TryGetValue(obstacleName, out ObstacleData o))
                {
                    Debug.LogWarning($"[DinoDash] Không tìm thấy obstacle '{obstacleName}' cho biome {def.assetName}.");
                    continue;
                }
                if (catalogIsNew || createdNames.Contains(obstacleName)) AddIfMissing(catalog, o);
            }
            EditorUtility.SetDirty(catalog);

            string biomePath = $"{BiomeFolder}/Biome_{def.assetName}.asset";
            var biome = AssetDatabase.LoadAssetAtPath<BiomeData>(biomePath);
            if (biome == null)
            {
                biome = ScriptableObject.CreateInstance<BiomeData>();
                DefaultBiomes.ApplyDef(biome, def);
                biome.catalog = catalog;
                AssetDatabase.CreateAsset(biome, biomePath);
                biomesCreated++;
            }
            else
            {
                if (biome.catalog == null)
                {
                    biome.catalog = catalog;
                    EditorUtility.SetDirty(biome);
                }
                biomesKept++;
            }
            biomes.Add(biome);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ---- 3) Gán vào scene
        bool assigned = AssignBiomes(biomes);

        if (biomes.Count > 0)
        {
            Selection.activeObject = biomes[0];
            EditorGUIUtility.PingObject(biomes[0]);
        }

        Debug.Log($"[DinoDash] Obstacle: tạo mới {createdNames.Count}, giữ nguyên {obstaclesKept}. " +
                  $"Biome: tạo mới {biomesCreated}, giữ nguyên {biomesKept}. " +
                  (assigned
                      ? "Đã gán danh sách biome vào PrototypeBootstrap (nhớ Ctrl+S để lưu scene)."
                      : "Không thấy PrototypeBootstrap trong scene - hãy kéo 3 asset Biome_* vào danh sách 'Biomes' thủ công."));
    }

    [MenuItem("DinoDash/Create Default Obstacles and Biomes", true)]
    static bool CanCreateAll() => !EditorApplication.isPlayingOrWillChangePlaymode;

    // ---------------------------------------------------------------- helpers

    static ObstacleCatalog LoadOrCreateCatalog(string path, out bool isNew)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ObstacleCatalog>(path);
        isNew = catalog == null;
        if (isNew)
        {
            catalog = ScriptableObject.CreateInstance<ObstacleCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }
        return catalog;
    }

    static void AddIfMissing(ObstacleCatalog catalog, ObstacleData data)
    {
        if (!catalog.obstacles.Contains(data)) catalog.obstacles.Add(data);
    }

    static bool AssignBiomes(List<BiomeData> biomes)
    {
        var bootstrap = UnityEngine.Object.FindFirstObjectByType<PrototypeBootstrap>();
        if (bootstrap == null) return false;

        var so = new SerializedObject(bootstrap);
        SerializedProperty prop = so.FindProperty("biomes");
        if (prop == null || !prop.isArray) return false;

        prop.arraySize = biomes.Count;
        for (int i = 0; i < biomes.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = biomes[i];

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
        return true;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
