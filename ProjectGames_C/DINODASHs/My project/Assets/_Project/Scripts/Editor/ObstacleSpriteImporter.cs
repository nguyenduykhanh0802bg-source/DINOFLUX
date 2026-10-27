#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu Editor: DinoDash > Import Obstacle Sprites
///
/// 1) Quét mọi ảnh trong Assets/_Project/Art/Obstacles/ và đặt cấu hình pixel-art chuẩn:
///    Sprite (Single), Filter = Point, Compression = None, không mipmap, pivot giữa.
/// 2) Tự gán ảnh vào ô "Sprite" của các asset ObstacleData theo TÊN FILE:
///    ảnh "Spike.png" hoặc "Obstacle_01_Spike.png" đều khớp asset "Obstacle_01_Spike".
///    Chỉ gán khi ô Sprite đang trống (không ghi đè sprite bạn đã chọn tay).
/// 3) Animation: ảnh đánh số "Bat_0.png, Bat_1.png, Bat_2.png" (hoặc Obstacle_06_Bat_0...) được gán vào
///    mảng "Frames" theo thứ tự số, khi mảng đang trống.
/// Chạy lại bao nhiêu lần cũng an toàn.
/// </summary>
public static class ObstacleSpriteImporter
{
    const string ArtFolder = "Assets/_Project/Art/Obstacles";
    const string DataFolder = "Assets/_Project/Data/Obstacles";
    const int PixelsPerUnit = 32; // chỉ ảnh hưởng độ nét khi xem; kích thước in-game do ObstacleData.size quyết định

    [MenuItem("DinoDash/Import Obstacle Sprites")]
    public static void ImportAll()
    {
        if (!AssetDatabase.IsValidFolder(ArtFolder))
        {
            EnsureFolder(ArtFolder);
            Debug.Log($"[DinoDash] Đã tạo thư mục {ArtFolder}. Chép ảnh .png vào đó (tên theo obstacle, vd Spike.png) rồi chạy lại menu.");
            return;
        }

        // ---- 1) Cấu hình import
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
        int reconfigured = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;

            bool changed = false;
            if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
            if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
            if (!Mathf.Approximately(ti.spritePixelsPerUnit, PixelsPerUnit)) { ti.spritePixelsPerUnit = PixelsPerUnit; changed = true; }

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            if (settings.spriteAlignment != (int)SpriteAlignment.Center)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                ti.SetTextureSettings(settings);
                changed = true;
            }

            if (changed) { ti.SaveAndReimport(); reconfigured++; }
        }

        // ---- 2) Gán sprite theo tên
        var sprites = new Dictionary<string, Sprite>();
        var frameSets = new Dictionary<string, SortedDictionary<int, Sprite>>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            Match m = FrameName.Match(fileName);
            if (m.Success)
            {
                string key = Normalize(m.Groups[1].Value);
                if (!frameSets.TryGetValue(key, out var set)) frameSets[key] = set = new SortedDictionary<int, Sprite>();
                set[int.Parse(m.Groups[2].Value)] = sp;
            }
            else sprites[Normalize(fileName)] = sp;
        }

        int assigned = 0, alreadySet = 0, animatedCount = 0;
        var missing = new List<string>();
        string[] dataGuids = AssetDatabase.FindAssets("t:ObstacleData", new[] { DataFolder });
        foreach (string guid in dataGuids)
        {
            var data = AssetDatabase.LoadAssetAtPath<ObstacleData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null) continue;

            // Animation (chỉ khi mảng frames đang trống)
            if (data.frames == null || data.frames.Length == 0)
            {
                var set = FindFrames(frameSets, data.name);
                if (set != null && set.Count >= 2)
                {
                    var list = new List<Sprite>(set.Values);
                    data.frames = list.ToArray();
                    EditorUtility.SetDirty(data);
                    animatedCount++;
                    continue;
                }
            }

            if (data.sprite != null || (data.frames != null && data.frames.Length > 0)) { alreadySet++; continue; }

            Sprite match = FindSprite(sprites, data.name);
            if (match == null) { missing.Add(data.name); continue; }

            data.sprite = match;
            EditorUtility.SetDirty(data);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DinoDash] Sprite obstacle: cấu hình lại {reconfigured}/{guids.Length} ảnh, gán mới {assigned}, animation {animatedCount}, đã có sẵn {alreadySet}, " +
                  $"chưa có ảnh {missing.Count}" + (missing.Count > 0 ? " (" + string.Join(", ", missing) + ") - các obstacle này dùng hình placeholder." : "."));
    }

    [MenuItem("DinoDash/Import Obstacle Sprites", true)]
    static bool CanImport() => !EditorApplication.isPlayingOrWillChangePlaymode;

    // ---------------------------------------------------------------- helpers

    /// <summary>Khớp tên đầy đủ trước, rồi khớp phần đuôi sau số thứ tự ("Obstacle_01_Spike" -> "spike").</summary>
    static Sprite FindSprite(Dictionary<string, Sprite> sprites, string assetName)
    {
        string full = Normalize(assetName);
        if (sprites.TryGetValue(full, out Sprite s)) return s;

        string[] parts = assetName.Split('_');
        if (parts.Length >= 3 && sprites.TryGetValue(Normalize(string.Join("_", parts, 2, parts.Length - 2)), out s)) return s;
        return null;
    }

    static SortedDictionary<int, Sprite> FindFrames(Dictionary<string, SortedDictionary<int, Sprite>> sets, string assetName)
    {
        if (sets.TryGetValue(Normalize(assetName), out var s)) return s;
        string[] parts = assetName.Split('_');
        if (parts.Length >= 3 && sets.TryGetValue(Normalize(string.Join("_", parts, 2, parts.Length - 2)), out s)) return s;
        return null;
    }

    static readonly Regex FrameName = new Regex(@"^(.+?)_(\d+)$", RegexOptions.Compiled);

    static string Normalize(string name) => name.Replace(" ", "").ToLowerInvariant();

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
