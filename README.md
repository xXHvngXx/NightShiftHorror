<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:0d0a1a,50:2b1854,100:16427a&height=190&section=header&text=CA%20TR%E1%BB%B0C%20%C4%90%C3%8AM&fontSize=54&fontColor=eef2ff&fontAlignY=38&desc=Night%20Shift%20Horror%20%E2%80%A2%20v1.6&descSize=20&descAlignY=60&animation=fadeIn" alt="Ca Trực Đêm - Night Shift Horror" width="100%"/>

### 🌙 &nbsp;Năm tiếng đồng hồ. Một quyển nội quy. Không ai được phép vào sau 3 giờ sáng.&nbsp; 🔮

[![C#](https://img.shields.io/badge/C%23-.NET%209.0-512BD4?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Raylib](https://img.shields.io/badge/Raylib--cs-8.1.0%203D%20Engine-3B82F6?style=for-the-badge)](https://www.raylib.com/)
[![Release](https://img.shields.io/badge/Release-v1.6-7C3AED?style=for-the-badge)](https://github.com/xXHvngXx/NightShiftHorror/releases/tag/v1.6)
[![Genre](https://img.shields.io/badge/Genre-Psychological%20Horror-1E3A8A?style=for-the-badge)]()
[![Voice](https://img.shields.io/badge/Voice-Vietnamese%20TTS-0EA5E9?style=for-the-badge)]()

<br/>

> *"Cửa hàng tiện lợi cuối đường Số 9 tuyển người trực ca đêm, trả tiền mặt ngay khi hết ca.*
> *Chỉ cần đọc kỹ tờ quy tắc và tuân thủ nó. Nghe thì đơn giản, phải không?"*

<br/>

[**Giới thiệu**](#-giới-thiệu) •
[**Tính năng**](#-tính-năng-nổi-bật-v16) •
[**Điều khiển**](#-điều-khiển) •
[**Diễn biến**](#-diễn-biến-một-đêm) •
[**Công nghệ**](#-công-nghệ) •
[**Cài đặt**](#-cài-đặt--chạy-game)

</div>

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:16427a,50:2b1854,100:0d0a1a&height=70&section=header" width="100%"/>

## 👁️ Giới thiệu

**Ca Trực Đêm (Night Shift Horror)** là tựa game kinh dị tâm lý góc nhìn thứ nhất mang đậm bối cảnh đêm sương đường phố Việt Nam. Từ màn hình chính phong cách VHS với nhạc nền piano, chọn tập chơi, cài đặt âm lượng, cuộc trò chuyện ở quán cà phê, tương tác điện thoại/thông báo cho đến chuyến xe đêm dọc con đường Số 9 mù sương, bạn bắt đầu ca trực tại cửa hàng tiện lợi từ **00:00 đến 05:00** với một tờ nội quy đặt trên quầy thu ngân. Những vị khách ghé qua nhìn rất bình thường, cho đến khi có điều gì đó không khớp.

Thế giới 3D kết hợp giữa **mô hình 3D nhập (`grocery_shelf.glb`)** và các vật thể dựng trực tiếp bằng code với **C# (.NET 9.0)** và **Raylib-cs**, đi kèm hệ thống Jumpscare có phòng test riêng, thực thể ma hỗ trợ trỏ model `.glb` ngoài, khu vực nhà vệ sinh chi tiết có gương phản chiếu thời gian thực, sân sau, hiệu ứng ánh sáng, âm thanh môi trường, thông báo điện thoại và lồng tiếng hội thoại tiếng Việt tự động.

<table>
<tr>
<td width="25%" align="center">🏍️<br/><b>Chuyến xe đêm</b><br/>qua phố sương</td>
<td width="25%" align="center">🕛<br/><b>5 tiếng</b><br/>một ca trực</td>
<td width="25%" align="center">📜<br/><b>3 quy tắc</b><br/>sống còn</td>
<td width="25%" align="center">🚪<br/><b>1 cánh cửa</b><br/>cấm mở sau 3h</td>
</tr>
</table>

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:0d0a1a,50:2b1854,100:16427a&height=70&section=header" width="100%"/>

## 💀 Tính năng nổi bật (v1.6)

### 👻 Hệ thống Jumpscare, Dev Sandbox & Thực thể ma (`Jumpscares/`)
- **Đa góc phục kích & đồng bộ nhịp âm thanh (`JumpscareSystem.cs`)**: Hỗ trợ các kịch bản hù trực diện (*Quầy thu ngân, Nhà vệ sinh, Cửa kính chính, Hành lang giữa tiệm*), khớp nhịp biểu diễn 2.0 giây với hiệu ứng rung camera, đèn chớp, nhiễu VHS và nhả rung mượt mà sau pha hù.
- **Phòng căn chỉnh Jumpscare trực tiếp (`F9` / Dev Mode)**: Cho phép chuyển nhanh giữa các góc cảnh, phát lại cú hù, test tiếng thét (`Assets/Audio/jumpscare.mp3`), bật/tắt đèn hoặc VHS và căn khoảng cách dừng trước camera theo thời gian thực.
- **Thực thể ma lai Procedural & Model ngoài (`GhostModel.cs`)**: Dựng sẵn tạo hình thực thể 3D chuyển động giật cục trong code, đồng thời hỗ trợ tự động nạp model 3D bên ngoài (`Assets/Models/ghost.glb` / `.obj`) khi cần thay thế.

### 🚿 Khu nhà vệ sinh chi tiết, Gương phản chiếu & Sân sau
- **Không gian nhà vệ sinh & 3 buồng riêng biệt (`Props.WC.cs`)**: Bao gồm cửa chính nhà vệ sinh đóng/mở được, công tắc đèn riêng cho WC với mặt tường bên trong đồng bộ khi tắt đèn mà không làm tối mặt ngoài tiệm, dãy bồn lavabo có vòi nước chảy/rửa tay góc nhìn thứ nhất, 3 buồng vệ sinh với cửa đóng/mở độc lập, bồn cầu và xử lý va chạm ổn định.
- **Gương phản chiếu thời gian thực (`RenderTexture2D`)**: Phản chiếu không gian bên trong phòng vệ sinh, dãy buồng và hình ảnh nhân vật người chơi, căn chỉnh gọn mặt tiền để không lem sang khu biển máy nước của tiệm.
- **Kiểm tra che khuất raycast (`IsRayBlockedByWall`)**: Chặn tương tác `[F]` xuyên qua tường đối với các vật thể phía sau tường, thùng cầu chì ngoài tường, chồng hộp hàng và thùng rác.
- **Khám phá sân sau**: Dựng xe máy của Tú dưới mái che sân sau, kiểm tra cổng sắt ra hẻm và các vật cản xung quanh.

### 🏠 Màn hình chính VHS, Nhạc nền & Cài đặt âm lượng
- **Màn hình chính động (`Program.Home.cs`)**: Camera 3D chuyển động quanh cửa hàng kết hợp hiệu ứng băng từ VHS, chuyển đổi ngôn ngữ (*English / Tiếng Việt*), chọn tập (*Episodes*), xem *Credits* và mở *Options* để điều chỉnh **Âm lượng tổng (Master)**, **Nhạc nền (Music)** và **Hiệu ứng (SFX)** bằng chuột hoặc bàn phím.
- **Nhạc nền & Thông báo điện thoại**: Phát nhạc nền (`Assets/Audio/Myuu-Misconception-Piano-Version.mp3`) tại màn hình chính và tích hợp luồng thông báo điện thoại (`Assets/Audio/fears-to-fathom-notification-sound.mp3`) dẫn dắt mạch truyện.

### 🎙️ Lồng tiếng hội thoại tiếng Việt thông minh
- Tự động phát giọng lồng tiếng tiếng Việt cho các câu thoại đối đáp trực tiếp của nhân vật, điều chỉnh **cao độ (Pitch) riêng biệt** cho từng tuyến nhân vật (*Tú, Dũng, Đen, Hưng, Quản lý, Ông cụ, Anh Hùng, Chị Mai, Cô gái áo mưa vàng*).
- Tự động nhận diện và **tắt giọng với các câu tự sự**, độc thoại nội tâm hoặc nội dung đọc trên giấy tờ, trả về âm thanh gõ nhịp retro để giữ không khí căng thẳng.

### 🏍️ Trải nghiệm lái xe đêm & Kính chiếu hậu thời gian thực
- Phân cảnh điều khiển xe máy chạy dọc tuyến phố Việt Nam ban đêm với biển hiệu tạp hóa, cột điện, sương mù dày đặc, đèn pha chiếu xa, tiếng động cơ biến thiên theo tốc độ và phím **`Shift`** nẹt ga/tăng tốc kết hợp phản hồi FOV.
- **Kính chiếu hậu hai bên render thời gian thực** qua `RenderTexture2D`, cho phép quan sát phía sau trong lúc di chuyển.
- **Trình phát nhạc Playlist tích hợp trên màn hình**: tự động nạp các file `.mp3` từ thư mục `Assets/Playlist`, hiển thị đĩa than xoay tròn và hỗ trợ chuyển bài ngay trong lúc chạy xe.

### 🏪 Thế giới 3D kết hợp Model & Ánh sáng động
- Nạp và render mô hình 3D (`Assets/Models/grocery_shelf.glb` qua `Program.Models.cs`) cùng hệ thống kệ hàng, tủ mát, quầy thu ngân, máy tính tiền, camera an ninh nhấp nháy đèn đỏ và đồng hồ treo tường **chạy theo thời gian thực trong game**.
- Quán cà phê ấm áp đối lập với cửa hàng tiện lợi lạnh lẽo cuối đường Số 9, kết hợp hiệu ứng mất điện, đèn huỳnh quang chập chờn và sương mù chiều sâu.

### 💬 Hội thoại phân nhánh & Đa kết thúc
- Hộp thoại gõ chữ từng ký tự, hỗ trợ tua nhanh và ngắt giọng mượt mà khi chuyển câu.
- Quyết định gửi tin nhắn định vị, tương tác với khách hàng và hành động tại mốc **03:00 sáng** sẽ dẫn tới nhiều kết cục khác nhau.

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:16427a,50:2b1854,100:0d0a1a&height=70&section=header" width="100%"/>

## 🎮 Điều khiển

### 🚶 Trong quán cà phê, Cửa hàng tiện lợi & Nhà vệ sinh

| Phím | Hành động |
|:----:|:----------|
| <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> | Di chuyển |
| <kbd>Shift</kbd> | Chạy nhanh |
| 🖱️ **Chuột** | Quan sát xung quanh |
| <kbd>F</kbd> | Tương tác (cửa, buồng WC, công tắc đèn WC, vòi rửa tay, NPC, sổ giao ca, quy tắc...) / Nói chuyện / Hiện nhanh chữ |
| <kbd>Space</kbd> | Hiện nhanh chữ |
| <kbd>↑</kbd> <kbd>↓</kbd> | Chọn phương án trả lời |
| <kbd>Enter</kbd> | Xác nhận / Sang câu tiếp theo |
| <kbd>Esc</kbd> | Tạm dừng |
| <kbd>R</kbd> | Chơi lại (tại màn hình kết thúc) |

### 👻 Trong phòng căn chỉnh Jumpscare (Dev Mode)

| Phím | Hành động |
|:----:|:----------|
| <kbd>1</kbd> <kbd>2</kbd> <kbd>3</kbd> <kbd>4</kbd> | Chuyển nhanh giữa 4 góc cảnh Jumpscare |
| <kbd>Space</kbd> / <kbd>R</kbd> | Phát lại cú hù (2.0s) |
| <kbd>T</kbd> | Test riêng âm thanh `jumpscare.mp3` |
| <kbd>↑</kbd> / <kbd>↓</kbd> | Chỉnh khoảng cách ma dừng trước camera |
| <kbd>L</kbd> | Bật / Tắt đèn chiếu sáng |
| <kbd>V</kbd> | Bật / Tắt hiệu ứng nhiễu VHS |
| <kbd>Esc</kbd> | Quay về màn hình chính |

### 🏍️ Trong cảnh chạy xe máy

| Phím | Hành động |
|:----:|:----------|
| <kbd>W</kbd> / <kbd>S</kbd> | Tăng ga / Phanh giảm tốc |
| <kbd>A</kbd> / <kbd>D</kbd> | Đánh lái trái / phải |
| <kbd>Shift</kbd> | Nẹt ga / Tăng tốc & điều chỉnh FOV |
| <kbd>L</kbd> | Bật / Tắt đèn pha |
| <kbd>Space</kbd> | Bấm còi xe |
| <kbd>1</kbd> | **Trước** (Chuyển bài nhạc trước đó) |
| <kbd>2</kbd> | **Bật / Tắt** (Tạm dừng hoặc phát tiếp nhạc) |
| <kbd>3</kbd> | **Kế** (Chuyển sang bài nhạc tiếp theo) |

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:0d0a1a,50:2b1854,100:16427a&height=70&section=header" width="100%"/>

## 🕯️ Diễn biến một đêm

| Thời gian | Sự kiện chính | Ghi chú |
| :---: | :--- | :--- |
| **23:00** | Quán cà phê & Lên đường | Nhận việc qua điện thoại, chạy xe tới cuối đường Số 9 |
| **00:00** | Bắt đầu ca trực | Nhận bàn giao và đọc kỹ tờ quy tắc trên quầy |
| **02:00** | Điện thoại bàn đổ chuông | Những vị khách bất thường bắt đầu ghé tiệm |
| **03:00** | Thời điểm khóa cửa | Tuyệt đối không mở cửa cho bất kỳ ai bên ngoài |
| **05:00** | Kết thúc ca trực | Nhận tiền công và hoàn thành ca làm việc |

> ⚠️ **Gợi ý nhỏ:** Đừng bỏ qua các dòng chữ viết tay bên lề, và đừng tin mọi giọng nói vang lên ngoài cửa kính lúc 3 giờ sáng.

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:16427a,50:2b1854,100:0d0a1a&height=70&section=header" width="100%"/>

## 🛠️ Công nghệ

| Hạng mục | Chi tiết |
|:---------|:---------|
| **Ngôn ngữ** | C# (.NET 9.0) |
| **Đồ họa** | Raylib-cs (`8.1.0`), Model 3D (`.glb` / `.obj`), render 3D thủ tục & `RenderTexture2D` |
| **Âm thanh** | Tổng hợp sóng PCM tại runtime + Phát nhạc stream `.mp3`, âm thanh thông báo & Jumpscare |
| **Lồng tiếng** | Pipeline tự động sinh audio tiếng Việt (`gTTS` + mã hóa MD5) |
| **IDE & Nền tảng** | Visual Studio 2022 • Windows |

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:0d0a1a,50:2b1854,100:16427a&height=70&section=header" width="100%"/>

## 🚀 Cài đặt & chạy game

**Yêu cầu:** Windows, [.NET 9.0 SDK](https://dotnet.microsoft.com/download), Visual Studio 2022 hoặc `dotnet` CLI.

```bash
# 1. Tải mã nguồn
git clone [https://github.com/xXHvngXx/NightShiftHorror.git](https://github.com/xXHvngXx/NightShiftHorror.git)
cd NightShiftHorror/NightShiftHorror

# 2. (Tùy chọn) Thêm nhạc cá nhân để nghe khi lái xe hoặc model ma 3D
# Chép file .mp3 vào thư mục Assets/Playlist/ và model .glb vào Assets/Models/ghost.glb

# 3. Chạy game
dotnet run -c Release
