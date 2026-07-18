# MiniComp - Bộ sưu tập Minigame Unity

**DATN - Tốt nghiệp**  
Bộ sưu tập các minigame được phát triển bằng **Unity** để trình bày đồ án tốt nghiệp.

---

## 🎮 Các trò chơi

| Trò chơi              | Thể loại              | Mô tả ngắn |
|-----------------------|-----------------------|----------|
| **CubeDash**          | Endless Runner        | Game chạy vô tận chính, có hệ thống lưu điểm |
| **MiniGolf**          | Mini Golf             | Đánh golf mini với nhiều level |
| **Snake**             | Classic Snake         | Rắn ăn mồi kinh điển |
| **TopDownShooter**    | Top-down Shooter      | Bắn súng góc nhìn từ trên xuống |
| **Match3**            | Match3                | Nối 3+ theo đường thẳng và chéo |

Tất cả được kết nối qua **Title Screen** chung.

---

## 🛠️ Thông tin kỹ thuật

- **Engine**: Unity 2022.3.62f2 (URP 2D)
- **Ngôn ngữ**: C#
- **Build target**: Standalone Windows 64-bit
- **Render Pipeline**: Universal Render Pipeline (URP)

### Cấu trúc dự án

- `Assets/TitleScreen.unity` — Màn hình chính
- `Assets/CubeDash/` — Endless Runner
- `Assets/MiniGolf/` — Mini Golf
- `Assets/Snake/` — Snake
- `Assets/TopDownShooter/` — Top-down Shooter
- `Assets/Match 3/` — Match 3

---

## 🎯 Tính năng nổi bật

- Hệ thống chuyển cảnh mượt mà
- Quản lý âm thanh riêng cho từng game
- Hệ thống lưu tiến độ (JSON)
- Thiết kế theo kiểu Singleton Manager
- Hỗ trợ pause, game over, restart

---

## 🚀 Hướng dẫn chạy

1. Clone repo:
   ```bash
   git clone https://github.com/junirst/MiniComp.git
