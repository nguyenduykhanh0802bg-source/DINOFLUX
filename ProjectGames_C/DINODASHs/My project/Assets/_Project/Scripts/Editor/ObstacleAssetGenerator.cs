#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu Editor: DinoDash > Create Default Obstacle Assets
/// - Tạo 5 asset ObstacleData + 1 ObstacleCatalog trong Assets/_Project/Data/Obstacles/
/// - KHÔNG ghi đè asset đã có (giữ nguyên số liệu bạn đã chỉnh), chỉ bổ sung cái còn thiếu
/// - Tự gán catalog vào PrototypeBootstrap trong scene đang mở
/// </summary>
public static class ObstacleAssetGenerator
{
    const string DataFolder = "Assets/_Project/Data";
    const string ObstacleFolder = DataFolder + "/Obstacles";
    const string CatalogPath = ObstacleFolder + "/ObstacleCatalog.asset";

    [MenuItem("DinoDash/Create Default Obstacle Assets")]
    public static void CreateDefaults()
    {
        EnsureFolder(ObstacleFolder);

        var catalog = AssetDatabase.LoadAssetAtPath<ObstacleCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ObstacleCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        int created = 0, kept = 0;
        foreach (ObstacleData def in DefaultObstacles.CreateAll())
        {
            string path = $"{ObstacleFolder}/{def.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ObstacleData>(path);

            if (existing != null)
            {
                Object.DestroyImmediate(def); // bản tạm trong bộ nhớ, bỏ đi
                AddIfMissing(catalog, existing);
                kept++;
            }
            else
            {
                AssetDatabase.CreateAsset(def, path);
                AddIfMissing(catalog, def);
                created++;
            }
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool assigned = AssignToBootstrap(catalog);

        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);

        Debug.Log($"[DinoDash] Obstacle assets: tạo mới {created}, giữ nguyên {kept}. " +
                  (assigned
                      ? "Đã gán ObstacleCatalog vào PrototypeBootstrap trong scene (nhớ Ctrl+S để lưu scene)."
                      : "Không thấy PrototypeBootstrap trong scene - hãy kéo ObstacleCatalog vào ô 'Obstacle Catalog' thủ công."));
    }

    [MenuItem("DinoDash/Create Default Obstacle Assets", true)]
    static bool CanCreateDefaults() => !EditorApplication.isPlayingOrWillChangePlaymode;

    // ---------------------------------------------------------------- helpers

    static void AddIfMissing(ObstacleCatalog catalog, ObstacleData data)
    {
        if (!catalog.obstacles.Contains(data)) catalog.obstacles.Add(data);
    }

    static bool AssignToBootstrap(ObstacleCatalog catalog)
    {
        var bootstrap = Object.FindFirstObjectByType<PrototypeBootstrap>();
        if (bootstrap == null) return false;

        var so = new SerializedObject(bootstrap);
        SerializedProperty prop = so.FindProperty("obstacleCatalog");
        if (prop == null) return false;

        prop.objectReferenceValue = catalog;
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
