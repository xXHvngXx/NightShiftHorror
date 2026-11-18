<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:0b0b12,50:3a0a12,100:8a1020&height=190&section=header&text=CA%20TR%E1%BB%B0C%20%C4%90%C3%8AM&fontSize=54&fontColor=f2e9e4&fontAlignY=38&desc=Night%20Shift%20Horror&descSize=20&descAlignY=60&animation=fadeIn" alt="Ca Trực Đêm - Night Shift Horror" width="100%"/>

### 🌙 &nbsp;Năm tiếng đồng hồ. Một quyển nội quy. Không ai được phép vào sau 3 giờ sáng.&nbsp; 🩸

[![C#](https://img.shields.io/badge/C%23-.NET%209.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Raylib](https://img.shields.io/badge/Raylib--cs-3D%20Engine-F54242?style=for-the-badge)](https://www.raylib.com/)
[![Genre](https://img.shields.io/badge/Genre-Psychological%20Horror-8a1020?style=for-the-badge)]()
[![View](https://img.shields.io/badge/View-First%20Person-1f6feb?style=for-the-badge)]()
[![Status](https://img.shields.io/badge/Status-In%20Development-orange?style=for-the-badge)]()

<br/>

> *"Cửa hàng tiện lợi cuối khu tuyển người trực đêm, trả tiền mặt ngay khi hết ca.*
> *Chỉ cần đọc kỹ tờ quy tắc và tuân thủ nó. Nghe thì đơn giản, phải không?"*

<br/>

[**Giới thiệu**](#-giới-thiệu) •
[**Tính năng**](#-tính-năng-nổi-bật) •
[**Điều khiển**](#-điều-khiển) •
[**Cài đặt**](#-cài-đặt--chạy-game) •
[**Cấu trúc**](#-cấu-trúc-dự-án) •
[**Lộ trình**](#-lộ-trình-phát-triển)

</div>

---

## 👁️ Giới thiệu

**Ca Trực Đêm** là tựa game kinh dị tâm lý góc nhìn thứ nhất. Bạn nhận một ca trực đêm tại cửa hàng tiện lợi, từ **00:00 đến 05:00**, với một tờ quy tắc đặt trên quầy thu ngân. Những vị khách ghé qua nhìn rất bình thường, cho đến khi có điều gì đó không khớp.

Toàn bộ thế giới 3D, nhân vật, âm thanh và giao diện đều được **dựng bằng code** với **C#** và **Raylib-cs**, không dùng model hay file âm thanh dựng sẵn. Kết quả là phong cách retro-horror gọn nhẹ, tối giản và rất ngột ngạt.

<table>
<tr>
<td width="33%" align="center">🕛<br/><b>5 tiếng</b><br/>một ca trực</td>
<td width="33%" align="center">📜<br/><b>3 quy tắc</b><br/>sống còn</td>
<td width="33%" align="center">🚪<br/><b>1 cánh cửa</b><br/>không được mở</td>
</tr>
</table>

---

## 💀 Tính năng nổi bật

### 🎭 Khuôn mặt thật trên hình khối 3D
- Dán ảnh khuôn mặt lên mô hình đầu trái xoan bằng lưới UV tùy biến, chạy qua `Rlgl`.
- **Tự khớp tỉ lệ ảnh**: bề ngang vùng dán được tính từ khung ảnh nên mặt không bị méo.
- Cắt viền UV và dịch vị trí riêng cho từng ảnh qua bảng `FaceFits`.
- Đầu NPC **xoay theo ánh nhìn của người chơi**, kèm chuyển động mắt.
- Có mặt dự phòng bằng khối hình học khi chưa có ảnh, và log chẩn đoán trong Console nếu nạp ảnh lỗi.

### 🏪 Thế giới dựng hoàn toàn bằng khối hình học
- Quán cà phê ấm cúng và cửa hàng tiện lợi lạnh lẽo, được thiết kế như hai thế giới đối lập.
- Kệ hàng đầy sản phẩm, tủ mát, quầy thu ngân, máy tính tiền, camera an ninh có đèn đỏ nhấp nháy.
- Đồng hồ treo tường **chạy theo thời gian trong game**.
- Mất điện, đèn chập chờn, vật thể tối dần thay vì đen kịt.

### 🔊 Âm thanh tổng hợp bằng code
- Tiếng bước chân, chuông điện thoại, tiếng gõ cửa, nhịp tim, tiếng ù nền... đều được **sinh ra từ sóng âm trong code**.
- Giọng nói mỗi nhân vật có cao độ riêng, với hiệu ứng chữ chạy theo từng nhịp.

### 💬 Hội thoại phân nhánh
- Hộp thoại gõ chữ từng ký tự, có thể bỏ qua để hiện đủ câu.
- Lựa chọn của bạn **ảnh hưởng đến nội dung và kết thúc** của câu chuyện.
- Nhiều kết thúc khác nhau, khuyến khích chơi lại.

### 🎬 Cảm giác điện ảnh
- Vignette tối hai mép, rung màn hình khi cú sốc xuất hiện.
- Camera tự quay và zoom vào người đối diện khi trò chuyện.
- Đầu camera nhún nhẹ khi di chuyển, viền sáng quanh vật có thể tương tác.

---

## 🎮 Điều khiển

| Phím | Hành động |
|:----:|:----------|
| <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> | Di chuyển |
| 🖱️ **Chuột** | Nhìn xung quanh |
| <kbd>F</kbd> | Tương tác / nói chuyện / hiện nhanh chữ |
| <kbd>Space</kbd> | Hiện nhanh chữ, tiếp tục hội thoại |
| <kbd>↑</kbd> <kbd>↓</kbd> | Chọn đáp án |
| <kbd>Enter</kbd> | Xác nhận / sang câu tiếp theo |
| <kbd>Esc</kbd> | Tạm dừng |
| <kbd>R</kbd> | Chơi lại (ở màn hình kết thúc) |

---

## 🕯️ Diễn biến một đêm

```mermaid
timeline
    title Một ca trực
    00:00 : Nhận ca : Đọc tờ quy tắc
    02:00 : Điện thoại bàn đổ chuông : Có người lạ bước vào
    03:00 : Giờ khóa cửa
    05:00 : Hết ca : Nhận tiền công
```

> ⚠️ **Gợi ý nhỏ:** hãy đọc kỹ nội quy, và đừng tin tất cả những gì bạn nghe thấy.

---

## 🛠️ Công nghệ

| Hạng mục | Chi tiết |
|:---------|:---------|
| **Ngôn ngữ** | C# (.NET 9.0) |
| **Đồ họa** | Raylib-cs (`Raylib`, `Rlgl`), dựng hình hoàn toàn bằng code |
| **Âm thanh** | Tổng hợp sóng PCM tại runtime, nạp qua `LoadWaveFromMemory` |
| **IDE** | Visual Studio 2022 |
| **Nền tảng** | Windows (font hội thoại dùng `Segoe UI` hệ thống) |

---

## 🚀 Cài đặt & chạy game

**Yêu cầu:** Windows, [.NET 9 SDK](https://dotnet.microsoft.com/download), Visual Studio 2022 hoặc `dotnet` CLI.

```bash
# 1. Tải mã nguồn
git clone <repository-url>
cd NightShiftHorror

# 2. Chạy game
dotnet run -c Release
```

### 🖼️ Thêm khuôn mặt nhân vật

Đặt ảnh PNG vào `Assets/Faces/` với **đúng tên** sau:

| Tên file | Nhân vật |
|:---------|:---------|
| `old_man.png` | Ông cụ |
| `girl.png` | Cô gái |
| `strange_man.png` | Người đàn ông lạ |
| `yellow_normal.png` | Áo mưa vàng (bình thường) |
| `yellow_horror.png` | Áo mưa vàng (kinh dị) |
| `manager.png` | Quản lý |
| `friend1.png` `friend2.png` `friend3.png` | Nhóm bạn ở quán cà phê |

Đảm bảo ảnh luôn được copy sang thư mục build bằng cách thêm vào `.csproj`:

```xml
<ItemGroup>
  <None Update="Assets\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

<details>
<summary><b>🔧 Khắc phục sự cố: mặt nhân vật không hiện</b></summary>

<br/>

Mở cửa sổ Console khi chạy game, mỗi ảnh sẽ in một dòng báo cáo:

| Thông báo | Ý nghĩa |
|:----------|:--------|
| `[Faces] OK old_man.png (512x512)` | Nạp thành công |
| `[Faces] THIẾU ảnh cho ...` | Không tìm thấy file, game dùng mặt dự phòng |
| `[Faces] LỖI ... là WebP đội lốt .png` | Hãy xuất lại thành PNG thật |

Nếu mặt bị lệch, chỉnh bảng `FaceFits` trong `Props.Faces.cs` (`U0/U1/V0/V1`, `VertShift`, `Vert`).

</details>

---

## 📂 Cấu trúc dự án

```text
NightShiftHorror/
│
├── 📁 Assets/
│   └── 📁 Faces/           # Texture khuôn mặt nhân vật (.png)
│
├── 📄 Program.cs           # Vòng lặp game, cảnh, hội thoại, sự kiện, âm thanh tổng hợp
├── 📄 Props.cs             # Mô hình 3D: người, quán cà phê, cửa hàng tiện lợi
├── 📄 Props.Faces.cs       # Nạp ảnh mặt, tự khớp tỉ lệ, dán mặt lên đầu 3D
└── 📄 NightShiftHorror.csproj
```

---

## 🗺️ Lộ trình phát triển

- [x] Quán cà phê và cảnh mở đầu
- [x] Cửa hàng tiện lợi với đầy đủ vật dụng
- [x] Khuôn mặt thật trên mô hình 3D, đầu xoay theo người chơi
- [x] Âm thanh tổng hợp, vignette, rung màn hình
- [x] Menu tạm dừng
- [x] Nhiều kết thúc
- [ ] Làm mặt tối theo ánh sáng khi mất điện
- [ ] Hiệu ứng jumpscare bằng hình ảnh
- [ ] Thêm sự kiện và vị khách mới
- [ ] Lưu tiến trình, tùy chỉnh âm lượng
- [ ] Bản phát hành đa nền tảng

---

## 🤝 Đóng góp

Mọi ý tưởng, báo lỗi và pull request đều được chào đón. Hãy mở một **Issue** để thảo luận trước khi gửi thay đổi lớn.

## 📜 Giấy phép

Chưa chọn giấy phép. Hãy thêm file `LICENSE` trước khi công khai mã nguồn.

<div align="center">

<br/>

*Chúc bạn sống sót đến 05:00.* 🌅

<img src="https://capsule-render.vercel.app/api?type=waving&color=0:8a1020,50:3a0a12,100:0b0b12&height=110&section=footer" width="100%" alt=""/>

</div>
