using UnityEngine;

/// <summary>HUD tạm bằng OnGUI để không cần dựng Canvas. Thay bằng uGUI/UI Toolkit ở giai đoạn P1.</summary>
public class PrototypeHud : MonoBehaviour
{
    public RunManager run;
    public PlayerHealth health;
    public PlayerController controller;

    GUIStyle big, mid, small, title, center;
    bool stylesReady;

    void InitStyles()
    {
        stylesReady = true;

        big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
        big.normal.textColor = Color.white;

        mid = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperRight };
        mid.normal.textColor = new Color(0.75f, 0.9f, 1f);

        small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.LowerCenter };
        small.normal.textColor = new Color(1f, 1f, 1f, 0.55f);

        title = new GUIStyle(GUI.skin.label) { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        title.normal.textColor = new Color(1f, 0.35f, 0.3f);

        center = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        center.normal.textColor = Color.white;
    }

    static void FillRect(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        if (run == null || health == null) return;
        if (!stylesReady) InitStyles();

        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s;
        const float H = 720f;

        // Tim
        for (int i = 0; i < health.maxHearts; i++)
        {
            Color c = i < health.Current ? new Color(1f, 0.3f, 0.35f) : new Color(0.25f, 0.1f, 0.12f);
            FillRect(new Rect(24f + i * 40f, 24f, 32f, 32f), c);
        }

        // Thanh dash
        if (controller != null)
        {
            FillRect(new Rect(24f, 66f, 128f, 8f), new Color(1f, 1f, 1f, 0.15f));
            Color dashColor = controller.DashReady01 >= 1f ? new Color(0.4f, 1f, 0.6f) : new Color(0.9f, 0.8f, 0.3f);
            FillRect(new Rect(24f, 66f, 128f * controller.DashReady01, 8f), dashColor);
        }

        // Quãng đường, gem, bậc tốc độ
        GUI.Label(new Rect(W - 340f, 14f, 316f, 44f), $"{Mathf.FloorToInt(run.Distance)} m", big);
        GUI.Label(new Rect(W - 340f, 58f, 316f, 30f), $"Gem: {run.Gems}   |   Tốc độ cấp {run.Tier + 1}", mid);
        GUI.Label(new Rect(W - 340f, 86f, 316f, 30f), $"Kỷ lục: {Mathf.FloorToInt(run.BestDistance)} m", mid);

        string mode = controller != null && controller.moveMode == PlayerController.MoveMode.EightWay ? "8 hướng" : "4 hướng";
        GUI.Label(new Rect(0f, H - 36f, W, 30f),
            $"WASD/Mũi tên: di chuyển   |   Space/Shift: Dash   |   T: đổi chế độ ({mode})   |   R: chơi lại", small);

        if (run.State == RunManager.RunState.GameOver) DrawGameOver(W, H);
    }

    void DrawGameOver(float W, float H)
    {
        FillRect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.7f));

        float cy = H * 0.5f;
        GUI.Label(new Rect(0f, cy - 190f, W, 80f), "BẠN ĐÃ CHẾT!", title);
        GUI.Label(new Rect(W * 0.2f, cy - 100f, W * 0.6f, 40f), run.RecapCause, center);
        GUI.Label(new Rect(W * 0.2f, cy - 60f, W * 0.6f, 80f), run.RecapTip, center);

        string result = $"Quãng đường: {Mathf.FloorToInt(run.Distance)} m   |   Gem: {run.Gems}   |   Điểm: {run.Score}";
        GUI.Label(new Rect(0f, cy + 30f, W, 40f), result, center);
        if (run.NewBest)
            GUI.Label(new Rect(0f, cy + 70f, W, 40f), "KỶ LỤC MỚI!", center);

        GUI.Label(new Rect(0f, cy + 130f, W, 40f), "Nhấn Space / Enter để chơi lại   (hoặc R)", center);
    }
}
