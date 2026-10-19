# Dino 4D Dash — Prototype P0 (Unity 6, C#)

Prototype để trả lời đúng một câu hỏi: **"Điều khiển 4 hướng + dash có vui không?"**
Toàn bộ scene được dựng từ code, không cần asset nào.

## Cài đặt (5 phút)

1. Tạo project **Unity 6 (6000.x) → 2D (URP)** (hoặc 2D Built-in).
2. Kiểm tra package **Input System** đã cài (Window > Package Manager).
   `Edit > Project Settings > Player > Active Input Handling` = **Input System Package (New)** hoặc **Both**.
3. Chép toàn bộ file `.cs` vào `Assets/_Project/Scripts/`.
4. Mở scene trống (hoặc SampleScene), tạo **một GameObject trống**, gắn component **PrototypeBootstrap**.
5. Đặt Game view tỉ lệ **16:9**, bấm **Play**.

> Nếu sprite hiện màu đen trong URP: gán material `Sprite-Unlit-Default` cho SpriteRenderer, hoặc tắt Global Light 2D phụ thuộc.

## Điều khiển

| Phím | Chức năng |
|---|---|
| WASD / Mũi tên / Stick / D-pad | Di chuyển 4 hướng |
| Space / Shift / A / RB | Dash (i-frame, cooldown ~0.55s) |
| T / Y | Đổi 4 hướng ⇄ 8 hướng |
| R / Select | Restart tức thì |
| Space / Enter (khi chết) | Chơi lại |

## Có gì trong prototype

| Thành phần | File | Ghi chú |
|---|---|---|
| Dino 4/8 hướng + dash + input buffer | `PlayerController` | Hitbox nhỏ hơn sprite ~33% |
| Tim, i-frame, nhấp nháy | `PlayerHealth` | 3 tim; 10 gem = hồi 1 tim |
| Obstacle data-driven | `ObstacleData` + `Obstacle` | State machine Dormant→Telegraph→Active→Cooldown |
| 5 loại obstacle | `PrototypeBootstrap.BuildCatalog` | Gai, đá rơi, cưa xoay, laze, đá lao tới (mở dần theo mét) |
| Pooling | `ObstacleSpawner` | `UnityEngine.Pool` |
| Difficulty Director | `DifficultyDirector` | Ngân sách, nhịp căng–thả, giới hạn làn đồng thời, rubber-banding |
| Vòng chơi + death recap | `RunManager` + `PrototypeHud` | Tốc độ tăng theo bậc 200m, nguyên nhân chết + lời khuyên |
| Game feel | `CameraShake`, hit-stop, squash & stretch | |

## Tinh chỉnh nhanh (Inspector khi Play, hoặc sửa mặc định trong code)

| Muốn | Chỉnh |
|---|---|
| Điều khiển nhạy/ì hơn | `PlayerController.moveSpeed`, `acceleration` |
| Dash "đã" hơn | `dashSpeed`, `dashDuration`, `dashCooldown`, `dashInvulnerableTail` |
| Game dễ/khó hơn | `DifficultyDirector.baseBudgetPerSecond`, `growthPer100m`, `RunManager.speedStepPerTier` |
| Cảnh báo dài/ngắn | `ObstacleData.telegraphTime` (khuyến nghị 0.6–1.0s) |
| Nhịp thở dài/ngắn | `releaseDuration`, `tensionDuration` |

## Checklist playtest "Có vui không?"

- [ ] Sau 3 lần chết, bạn có muốn bấm R chơi tiếp ngay không?
- [ ] Chết có cảm giác "do mình sai" thay vì "game gian"?
- [ ] Dash có tạo ra khoảnh khắc "suýt chết mà thoát"?
- [ ] 4 hướng đủ thú vị chưa, hay 8 hướng (phím T) tốt hơn?
- [ ] Một lượt trung bình dài bao lâu? (mục tiêu 45–90s)

Xóa dữ liệu thử nghiệm: `Edit > Clear All PlayerPrefs`.

## Giới hạn đã biết (cố ý để cho P1)

- Placeholder sprite sinh bằng code, HUD bằng `OnGUI` → thay bằng pixel art + uGUI.
- Chưa có âm thanh, particle, biome, skin, Steam.
- Solvability chỉ là luật đơn giản (khoảng cách làn + giới hạn đồng thời); chưa có bộ kiểm tra đường thoát thật.
- Play area suy ra từ aspect camera lúc khởi động; đổi kích thước cửa sổ khi đang chơi chưa được xử lý.
- Chưa chạy thử trong Unity: nếu có lỗi biên dịch, gửi lại log để sửa.

## Bước tiếp theo (P1)

1. Test cảm giác điều khiển, chỉnh số liệu.
2. Thay placeholder bằng pixel art dino (tự vẽ), biome Rừng.
3. Chuyển catalog trong code thành asset `ObstacleData` + prefab.
4. Death recap dạng đồ họa, ghost replay, âm thanh (FMOD).

---

## P1 #1 — Obstacle dạng ScriptableObject (data-driven)

Thêm/sửa obstacle bằng **asset trong Inspector**, không cần sửa code.

### Cài đặt
1. Chép các file theo đúng đường dẫn trong bản vá (ghi đè `PrototypeBootstrap.cs` và `ObstacleData.cs`):
   ```
   Assets/_Project/Scripts/Obstacles/ObstacleCatalog.cs      (mới)
   Assets/_Project/Scripts/Obstacles/DefaultObstacles.cs     (mới)
   Assets/_Project/Scripts/Obstacles/ObstacleData.cs         (cập nhật)
   Assets/_Project/Scripts/Prototype/PrototypeBootstrap.cs   (cập nhật)
   Assets/_Project/Scripts/Editor/ObstacleAssetGenerator.cs  (mới - phải nằm trong thư mục tên "Editor")
   ```
2. Chờ Unity biên dịch xong, chọn menu **DinoDash > Create Default Obstacle Assets**.
3. Kết quả: 5 asset `ObstacleData` + `ObstacleCatalog` trong `Assets/_Project/Data/Obstacles/`, catalog tự gán vào `PrototypeBootstrap` trong scene. Nhấn **Ctrl+S** lưu scene.

### Dùng hằng ngày
- **Chỉnh độ khó**: chọn asset (ví dụ `Obstacle_03_SpinningSaw`) → sửa trong Inspector. Sửa khi đang Play cũng được; *giá trị trên asset được giữ lại sau khi thoát Play*.
- **Thêm obstacle mới**: nhân đôi một asset (Ctrl+D) hoặc Create > DinoDash > Obstacle Data → chỉnh → kéo vào danh sách của `ObstacleCatalog`.
- **Tắt tạm một obstacle**: bỏ khỏi catalog (hoặc để `minDistance` rất lớn).
- Chạy lại menu tạo asset là **an toàn**: không ghi đè asset đã có, chỉ bổ sung cái còn thiếu.
- Nếu chưa gán catalog, game dùng bản mặc định trong `DefaultObstacles.cs` và ghi cảnh báo ở Console.

### Luật tự kiểm tra (OnValidate)
Strike/Beam luôn có cảnh báo ≥ 0.4s; damage, cost ≥ 1; kích thước > 0; `allowedDirections` không được để trống.
