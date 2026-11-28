using System.Numerics;
using Raylib_cs;

partial class Program
{
    const string HomeVersion = "v1.75";

    static int homeSelection = 0;
    static int homeSubMenu = 0;
    static float homeOrbitAngle = 0f;
    static float homePanAngle = 0f;
    static float homeSubMenuAnim = 0f;
    static float titleUnderlineProgress = 0f;
    static readonly float[] menuHoverAnim = new float[5];
    static readonly float[] menuPressAnim = new float[5];

    static Font homeTitleFont;
    static Font homeHeadingFont;
    static Font homeBodyFont;
    static Font homeMenuFont;
    static bool homeFontsLoaded = false;

    static Music homeBgm;
    static bool homeBgmLoaded = false;
    static float masterVolume = 0.85f;
    static float homeMusicVolume = 0.70f;
    static float homeSfxVolume = 0.85f;
    static int homeOptionRow = 0;
    static int draggingSlider = -1;

    static RenderTexture2D homeSceneTarget;
    static bool homeTargetLoaded = false;
    static float homeIntro = 0f;
    static int homeLang = 0;
    static bool homeLangOpen = false;
    static float homeLangOpenAnim = 0f;
    static float homeGlitchTimer = 0f;
    static float homeGlitchCooldown = 2.5f;
    static int homeGlitchTarget = 0;
    static Vector2 homeLastMouse = Vector2.Zero;
    static readonly Random homeRng = new(7);

    static bool creditsClosing = false;
    static float creditsBlend = 0f;
    static float creditsScrollY = 0f;
    static float creditsElapsed = 0f;
    static float creditsSkipHover = 0f;

    record HomeCreditLine(string RoleEn, string RoleVi, string ValueEn, string ValueVi, int Kind);

    static readonly HomeCreditLine[] homeCreditsList =
    [
        new("", "", "DREAD TO DWELL", "ÁM ẢNH ĐỜI THƯỜNG", 0),
        new("", "", "CHAPTER 1: NIGHT SHIFT", "TẬP 1: CA TRỰC ĐÊM", 1),
        new("", "", "", "", 3),

        new("CREATED & DIRECTED BY", "SÁNG TẠO & ĐẠO DIỄN", "Minh Hưng & Tiến Dũng", "Minh Hưng & Tiến Dũng", 2),
        new("STORY & SCRIPTWRITING", "KỊCH BẢN & CỐT TRUYỆN", "Minh Hưng & Tiến Dũng", "Minh Hưng & Tiến Dũng", 2),
        new("GAMEPLAY PROGRAMMING", "LẬP TRÌNH GAMEPLAY", "Minh Hưng & Tiến Dũng", "Minh Hưng & Tiến Dũng", 2),
        new("UI / UX & VHS POST-PROCESSING", "GIAO DIỆN & HIỆU ỨNG VHS", "Chromatic Aberration, Scanlines & Strobe Shaders", "Quang sai màu, sọc nhiễu băng từ & chớp đèn", 2),
        new("", "", "", "", 3),

        new("", "", "3D ENVIRONMENTS & PROPS", "MÔI TRƯỜNG & VẬT THỂ 3D", 1),
        new("24H CONVENIENCE STORE", "CỬA HÀNG TIỆN LỢI 24H", "Counter, Checkout Register, Storage & Alley Gate", "Quầy thu ngân, máy tính tiền, kho sau & cổng hẻm", 2),
        new("OPENING & CUTSCENE SCENES", "PHÂN CẢNH MỞ ĐẦU & KẾT THÚC", "Evening Café, Street 9 & Midnight Motorbike Ride", "Quán cà phê chiều, đường Số 9 & cảnh chạy xe đêm", 2),
        new("WASHROOM & MIRROR SYSTEM", "NHÀ VỆ SINH & HỆ THỐNG GƯƠNG", "Real-Time Reflection & Flickering Fluorescent Lights", "Gương phản chiếu thời gian thực & đèn huỳnh quang", 2),
        new("3D SHELF ASSET", "TÀI NGUYÊN KỆ HÀNG 3D", "Grocery Shelf by Palmart Productions", "Grocery Shelf by Palmart Productions", 2),
        new("", "", "", "", 3),

        new("", "", "CHARACTERS & ENTITIES", "NHÂN VẬT & THỰC THỂ", 1),
        new("ENTITY 3D MODEL (.GLB)", "MÔ HÌNH THỰC THỂ 3D (.GLB)", "Custom Rigged Entity & Dynamic Face-Lock Framing", "Mô hình thực thể tùy chỉnh & khóa góc nhìn khuôn mặt", 2),
        new("NPC CAST & BEHAVIORS", "DÀN NHÂN VẬT & HÀNH VI", "Store Manager, Old Man, Mai, Hùng & Night Customers", "Quản lý, Ông cụ, Chị Mai, Anh Hùng & Khách trực đêm", 2),
        new("PROCEDURAL ANIMATION", "CHUYỂN ĐỘNG THỦ TỤC", "Walk Cycles, Head Tracking & Eye Gaze System", "Dáng đi, xoay đầu theo mục tiêu & ánh nhìn", 2),
        new("", "", "", "", 3),

        new("", "", "HORROR & NARRATIVE SYSTEMS", "HỆ THỐNG KINH DỊ & DẪN TRUYỆN", 1),
        new("MULTI-ANGLE JUMPSCARE ENGINE", "HỆ THỐNG JUMPSCARE ĐA GÓC", "Counter Lunge, Washroom Mirror, Front Door & Aisle", "Quầy thu ngân, Gương nhà vệ sinh, Cửa kính & Hành lang", 2),
        new("INTERACTIVE SMARTPHONE", "ĐIỆN THOẠI TƯƠNG TÁC", "Real-Time Messaging, Branching Replies & Notifications", "Nhắn tin thời gian thực, chọn phản hồi & thông báo", 2),
        new("5 BRANCHING ENDINGS", "5 NHÁNH KẾT THÚC", "Multiple Fates & Secret VHS Tape Replay (Shift+F6..F8)", "Đa kết cục & băng từ ký ức bí mật (Shift+F6..F8)", 2),
        new("", "", "", "", 3),

        new("", "", "ENGINE & AUDIO", "ENGINE & ÂM THANH", 1),
        new("GAME ENGINE & RUNTIME", "NỀN TẢNG PHÁT TRIỂN", "C# (.NET 9.0) • Raylib-cs", "C# (.NET 9.0) • Raylib-cs", 2),
        new("MAIN THEME PIANO", "NHẠC NỀN PIANO", "Myuu - Misconception (Piano Version)", "Myuu - Misconception (Piano Version)", 2),
        new("SOUND DESIGN & FOLEY", "THIẾT KẾ ÂM THANH", "Night Ambience, Engine Audio, Static & Voice Synth", "Không khí đêm, tiếng động cơ, nhiễu đài & giọng thoại", 2),
        new("", "", "", "", 3),

        new("", "", "SPECIAL THANKS", "CẢM ƠN ĐẶC BIỆT", 1),
        new("OPEN-SOURCE COMMUNITY", "CỘNG ĐỒNG MÃ NGUỒN MỞ", "Raylib & .NET Contributors", "Các nhà phát triển Raylib & .NET", 2),
        new("PLAYERS", "NGƯỜI CHƠI", "Thank you for surviving the night shift.", "Cảm ơn bạn đã sống sót qua ca trực đêm.", 2),
        new("", "", "", "", 3),
        new("", "", "TO BE CONTINUED IN CHAPTER 2...", "HẸN GẶP LẠI Ở TẬP 2...", 1)
    ];

    static byte AlphaByte(float value)
        => (byte)Math.Clamp((int)MathF.Round(value), 0, 255);

    static Color HC(int r, int g, int b, float a = 255f)
        => new Color((byte)r, (byte)g, (byte)b, AlphaByte(a));

    static string HT(string en, string vi) => homeLang == 1 ? vi : en;

    static int HomeHash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (int)(h & 0x7fffffff);
        }
    }

    static void ApplyAudioVolumes()
    {
        Raylib.SetMasterVolume(Math.Clamp(masterVolume, 0f, 1f));
        if (homeBgmLoaded)
        {
            Raylib.SetMusicVolume(homeBgm, Math.Clamp(homeMusicVolume, 0f, 1f));
        }
        Raylib.SetSoundVolume(voiceStaticSound, Math.Clamp(homeSfxVolume, 0f, 1f));
        Raylib.SetSoundVolume(footstepSound, Math.Clamp(homeSfxVolume, 0f, 1f));
    }

    static void HomeBlip(float pitch = 1.38f)
    {
        Raylib.SetSoundVolume(voiceStaticSound, Math.Clamp(homeSfxVolume, 0f, 1f));
        Raylib.SetSoundPitch(voiceStaticSound, pitch);
        Raylib.PlaySound(voiceStaticSound);
    }

    static byte[] BuildFallbackPianoWav()
    {
        const int sampleRate = 22050;
        const int durationSec = 16;
        int totalSamples = sampleRate * durationSec;
        short[] pcm = new short[totalSamples];

        float[][] chords =
        [
            [220.00f, 261.63f, 329.63f, 440.00f],
            [174.61f, 220.00f, 261.63f, 349.23f],
            [146.83f, 174.61f, 220.00f, 293.66f],
            [164.81f, 207.65f, 246.94f, 329.63f]
        ];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            int bar = (int)(t / 4f) % chords.Length;
            float barT = t % 4f;
            float[] ch = chords[bar];

            float sample = 0f;
            for (int n = 0; n < ch.Length; n++)
            {
                float noteStart = n * 0.45f;
                if (barT >= noteStart)
                {
                    float nt = barT - noteStart;
                    float env = MathF.Exp(-nt * 1.15f) * MathF.Min(1f, nt * 28f);
                    float freq = ch[n];
                    float tone = MathF.Sin(2f * MathF.PI * freq * nt) * 0.65f
                               + MathF.Sin(2f * MathF.PI * freq * 2f * nt) * 0.22f
                               + MathF.Sin(2f * MathF.PI * (freq * 0.5f) * nt) * 0.28f;
                    sample += tone * env * 0.22f;
                }
            }

            float loopFade = MathF.Min(1f, MathF.Min(t * 2f, (durationSec - t) * 2f));
            pcm[i] = (short)Math.Clamp(sample * loopFade * 24000f, short.MinValue, short.MaxValue);
        }

        using MemoryStream ms = new();
        using BinaryWriter bw = new(ms);
        int dataSize = pcm.Length * 2;
        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);
        bw.Write("fmt "u8);
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write("data"u8);
        bw.Write(dataSize);
        for (int i = 0; i < pcm.Length; i++) bw.Write(pcm[i]);
        return ms.ToArray();
    }

    static void EnsureHomeBgm()
    {
        if (!homeBgmLoaded)
        {
            string baseDir = AppContext.BaseDirectory;
            string[] candidates =
            [
                Path.Combine(baseDir, "Assets", "Audio", "Myuu-Misconception-Piano-Version.mp3"),
                "Assets/Audio/Myuu-Misconception-Piano-Version.mp3",
                "assets/audio/Myuu-Misconception-Piano-Version.mp3"
            ];

            string foundPath = candidates.FirstOrDefault(File.Exists) ?? "";
            if (foundPath != "")
            {
                homeBgm = Raylib.LoadMusicStream(foundPath);
            }
            else
            {
                byte[] wavData = BuildFallbackPianoWav();
                homeBgm = Raylib.LoadMusicStreamFromMemory(".wav", wavData);
            }

            homeBgm.Looping = true;
            homeBgmLoaded = true;
            ApplyAudioVolumes();
        }

        if (homeBgmLoaded)
        {
            if (!Raylib.IsMusicStreamPlaying(homeBgm))
            {
                Raylib.PlayMusicStream(homeBgm);
            }
            Raylib.UpdateMusicStream(homeBgm);
        }
    }

    static float HomeGlitchFor(int target)
        => (homeGlitchTimer > 0f && homeGlitchTarget == target) ? 1f : 0f;

    static int[] BuildVietnameseCodepoints()
    {
        const string extra =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            "áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđ" +
            "ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴĐ" +
            " .,!?:;\"'()[]{}<>+-=/*&%$#@~`^_|\\•–—…·";
        return extra.Select(c => (int)c).Distinct().ToArray();
    }

    static void EnsureHomeFonts()
    {
        if (homeFontsLoaded) return;
        homeFontsLoaded = true;

        int[] codepoints = BuildVietnameseCodepoints();

        string[] titlePaths =
        [
            @"C:\Windows\Fonts\segoeuib.ttf",
            @"C:\Windows\Fonts\arialbd.ttf",
            @"C:\Windows\Fonts\tahomabd.ttf",
            @"C:\Windows\Fonts\bahnschrift.ttf"
        ];
        string titlePath = titlePaths.FirstOrDefault(File.Exists) ?? "";
        homeTitleFont = titlePath != ""
            ? Raylib.LoadFontEx(titlePath, 96, codepoints, codepoints.Length)
            : viFont;
        Raylib.SetTextureFilter(homeTitleFont.Texture, TextureFilter.Bilinear);

        string[] headingPaths =
        [
            @"C:\Windows\Fonts\segoeuib.ttf",
            @"C:\Windows\Fonts\bahnschrift.ttf",
            @"C:\Windows\Fonts\arialbd.ttf"
        ];
        string headingPath = headingPaths.FirstOrDefault(File.Exists) ?? "";
        homeHeadingFont = headingPath != ""
            ? Raylib.LoadFontEx(headingPath, 40, codepoints, codepoints.Length)
            : viFont;
        Raylib.SetTextureFilter(homeHeadingFont.Texture, TextureFilter.Bilinear);

        string[] bodyPaths =
        [
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\arial.ttf"
        ];
        string bodyPath = bodyPaths.FirstOrDefault(File.Exists) ?? "";
        homeBodyFont = bodyPath != ""
            ? Raylib.LoadFontEx(bodyPath, 26, codepoints, codepoints.Length)
            : viFont;
        Raylib.SetTextureFilter(homeBodyFont.Texture, TextureFilter.Bilinear);

        string[] menuPaths =
        [
            @"C:\Windows\Fonts\seguisb.ttf",
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\arial.ttf"
        ];
        string menuPath = menuPaths.FirstOrDefault(File.Exists) ?? "";
        homeMenuFont = menuPath != ""
            ? Raylib.LoadFontEx(menuPath, 40, codepoints, codepoints.Length)
            : viFont;
        Raylib.SetTextureFilter(homeMenuFont.Texture, TextureFilter.Bilinear);
    }

    static void EnsureHomeTarget()
    {
        if (homeTargetLoaded) return;
        homeSceneTarget = Raylib.LoadRenderTexture(ScreenWidth, ScreenHeight);
        Raylib.SetTextureFilter(homeSceneTarget.Texture, TextureFilter.Bilinear);
        homeTargetLoaded = true;
    }

    static float SmoothStep01(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    static float EaseOutCubic(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        float inv = 1f - x;
        return 1f - inv * inv * inv;
    }

    static Color LerpCol(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Color(
            AlphaByte(a.R + (b.R - a.R) * t),
            AlphaByte(a.G + (b.G - a.G) * t),
            AlphaByte(a.B + (b.B - a.B) * t),
            AlphaByte(a.A + (b.A - a.A) * t)
        );
    }

    static void DrawVhsTextCore(Font font, string text, float x, float y, float size, float spacing, Color col, float ab)
    {
        if (ab > 0.01f)
        {
            Raylib.DrawTextEx(font, text, new Vector2(x - ab, y), size, spacing, HC(60, 190, 255, col.A * 0.30f));
            Raylib.DrawTextEx(font, text, new Vector2(x + ab, y), size, spacing, HC(255, 40, 90, col.A * 0.30f));
        }
        Raylib.DrawTextEx(font, text, new Vector2(x, y), size, spacing, col);
    }

    static void DrawVhsText(Font font, string text, float x, float y, float size, float spacing, Color col,
                            float ab = 1.5f, float glitch = 0f, int seed = 0)
    {
        if (glitch <= 0.001f)
        {
            DrawVhsTextCore(font, text, x, y, size, spacing, col, ab);
            return;
        }

        Vector2 m = Raylib.MeasureTextEx(font, text, size, spacing);
        const int bands = 4;
        float bandH = (m.Y + 6f) / bands;
        for (int b = 0; b < bands; b++)
        {
            int hsh = HomeHash(seed, b + 11);
            float off = (hsh % 3 == 0) ? 0f : ((hsh % 2 == 0) ? 1f : -1f) * ((hsh % 100) / 100f) * glitch * 16f;

            Raylib.BeginScissorMode((int)(x - 48f), (int)(y - 3f + b * bandH), (int)(m.X + 96f), (int)bandH + 1);
            DrawVhsTextCore(font, text, x + off, y, size, spacing, col, ab + glitch * 2.5f);
            Raylib.EndScissorMode();
        }
    }

    static void DrawHomeMenuItem(string label, float x, float y, float size, float ease, float press,
                                 float alpha, float glitch, int seed)
    {
        float shift = ease * 5f - press * 2f;

        if (ease > 0.02f)
        {
            float side = size * 0.64f * (0.45f + 0.55f * ease);
            float cy = y + size * 0.58f;
            float mx = x - size * 1.32f + shift * 0.5f;
            float a = ease * alpha;
            Raylib.DrawRectangleRec(new Rectangle(mx - 1.6f, cy - side * 0.5f, side, side), HC(60, 190, 255, 70 * a));
            Raylib.DrawRectangleRec(new Rectangle(mx + 1.6f, cy - side * 0.5f, side, side), HC(255, 40, 90, 70 * a));
            Raylib.DrawRectangleRec(new Rectangle(mx, cy - side * 0.5f, side, side), HC(255, 255, 255, 255 * a));
        }

        Color c = LerpCol(HC(222, 228, 240, 212 * alpha), HC(255, 255, 255, 255 * alpha), ease);
        DrawVhsText(homeMenuFont, label, x + shift, y, size, 0.6f, c, 1.5f, glitch, seed);
    }

    static void DrawHomeVolumeSlider(string label, float value, float x, float y, float barW, bool selected, float alpha)
    {
        Color labelCol = selected ? HC(255, 238, 64, 255 * alpha) : HC(232, 238, 248, 225 * alpha);
        DrawVhsText(homeHeadingFont, label, x, y, 16f, 1.2f, labelCol, 1.1f);

        string pct = $"{(int)MathF.Round(value * 100f)}%";
        float pctW = Raylib.MeasureTextEx(homeHeadingFont, pct, 16f, 1.0f).X;
        DrawVhsText(homeHeadingFont, pct, x + barW - pctW, y, 16f, 1.0f, HC(255, 255, 255, 245 * alpha), 1.1f);

        float trackY = y + 25f;
        const float trackH = 8f;
        Rectangle track = new(x, trackY, barW, trackH);
        Raylib.DrawRectangleRec(track, HC(14, 18, 36, 210 * alpha));
        Raylib.DrawRectangleLinesEx(track, 1.5f, selected ? HC(255, 238, 64, 235 * alpha) : HC(170, 188, 220, 140 * alpha));

        float fillW = barW * Math.Clamp(value, 0f, 1f);
        if (fillW > 0.5f)
        {
            Raylib.DrawRectangleRec(new Rectangle(x, trackY, fillW, trackH),
                selected ? HC(255, 52, 128, 245 * alpha) : HC(210, 224, 250, 220 * alpha));
        }

        float knobX = x + fillW;
        float knobY = trackY + trackH * 0.5f;
        Raylib.DrawRectangleRec(new Rectangle(knobX - 5f, knobY - 8f, 10f, 16f), HC(255, 255, 255, 255 * alpha));
    }

    static Rectangle HomeLangRow(Rectangle dd, int i)
        => new(dd.X, dd.Y + dd.Height + 4f + i * (dd.Height - 2f), dd.Width, dd.Height);

    static void DrawHomeLanguageDropdown(Rectangle r, string[] names, Vector2 mouse, float alpha)
    {
        if (alpha <= 0.01f) return;
        float openT = SmoothStep01(homeLangOpenAnim);

        Raylib.DrawRectangleRec(r, HC(8, 12, 30, 120 * alpha));
        Raylib.DrawRectangleLinesEx(r, 2f, HC(196, 214, 255, 235 * alpha));
        DrawVhsText(homeMenuFont, names[homeLang], r.X + 16f, r.Y + (r.Height - 24f) * 0.5f - 1f, 24f, 0.4f,
            HC(255, 255, 255, 255 * alpha), 1.2f);

        float cx = r.X + r.Width - 30f;
        float cy = r.Y + r.Height * 0.5f;
        float dir = 1f - 2f * openT;
        Color lc = HC(220, 232, 255, 240 * alpha);
        Raylib.DrawLineEx(new Vector2(cx - 9f, cy - 4f * dir), new Vector2(cx, cy + 5f * dir), 2f, lc);
        Raylib.DrawLineEx(new Vector2(cx, cy + 5f * dir), new Vector2(cx + 9f, cy - 4f * dir), 2f, lc);

        if (openT > 0.01f)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Rectangle row = HomeLangRow(r, i);
                bool hov = homeLangOpen && Raylib.CheckCollisionPointRec(mouse, row);
                float a = openT * alpha;

                Raylib.DrawRectangleRec(row, HC(6, 10, 26, 235 * a));
                Raylib.DrawRectangleLinesEx(row, 1.5f, HC(196, 214, 255, (hov ? 235f : 120f) * a));

                Color tc = (i == homeLang)
                    ? HC(255, 238, 64, 255 * a)
                    : HC(255, 255, 255, (hov ? 255f : 215f) * a);
                DrawVhsText(homeMenuFont, names[i], row.X + 16f, row.Y + (row.Height - 24f) * 0.5f - 1f, 24f, 0.4f, tc, 1.0f);
            }
        }
    }

    static void DrawHomeStrip(Texture2D tex, int y, int h, float dx, Color tint)
    {
        if (h <= 0) return;
        Rectangle src = new(0, tex.Height - (y + h), tex.Width, -h);
        Raylib.DrawTexturePro(tex, src, new Rectangle(dx, y, ScreenWidth, h), Vector2.Zero, 0f, tint);
        if (dx > 0.5f)
            Raylib.DrawTexturePro(tex, src, new Rectangle(dx - ScreenWidth, y, ScreenWidth, h), Vector2.Zero, 0f, tint);
        else if (dx < -0.5f)
            Raylib.DrawTexturePro(tex, src, new Rectangle(dx + ScreenWidth, y, ScreenWidth, h), Vector2.Zero, 0f, tint);
    }

    static void DrawHomeSceneComposite(float time, float ab, float blurAmount)
    {
        Texture2D tex = homeSceneTarget.Texture;
        int W = ScreenWidth, H = ScreenHeight;
        Rectangle src = new(0, 0, tex.Width, -tex.Height);

        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawTexturePro(tex, src, new Rectangle(-ab, 0, W, H), Vector2.Zero, 0f, HC(255, 0, 0));
        Raylib.DrawTexturePro(tex, src, new Rectangle(0, 0, W, H), Vector2.Zero, 0f, HC(0, 255, 0));
        Raylib.DrawTexturePro(tex, src, new Rectangle(ab, 0, W, H), Vector2.Zero, 0f, HC(0, 0, 255));
        Raylib.EndBlendMode();

        Color haze = HC(255, 255, 255, 38);
        const float sp = 2.6f;
        Raylib.DrawTexturePro(tex, src, new Rectangle(-sp, 0, W, H), Vector2.Zero, 0f, haze);
        Raylib.DrawTexturePro(tex, src, new Rectangle(sp, 0, W, H), Vector2.Zero, 0f, haze);
        Raylib.DrawTexturePro(tex, src, new Rectangle(0, -sp * 0.7f, W, H), Vector2.Zero, 0f, haze);
        Raylib.DrawTexturePro(tex, src, new Rectangle(0, sp * 0.7f, W, H), Vector2.Zero, 0f, haze);

        if (blurAmount > 0.005f)
        {
            float r1 = 7.5f * blurAmount;
            float r2 = 16.5f * blurAmount;
            Color bCol1 = HC(255, 255, 255, 58f * blurAmount);
            Color bCol2 = HC(255, 255, 255, 38f * blurAmount);

            Vector2[] ring1 =
            [
                new(-r1, 0f), new(r1, 0f), new(0f, -r1), new(0f, r1),
                new(-r1 * 0.71f, -r1 * 0.71f), new(r1 * 0.71f, -r1 * 0.71f),
                new(-r1 * 0.71f, r1 * 0.71f), new(r1 * 0.71f, r1 * 0.71f)
            ];
            for (int i = 0; i < ring1.Length; i++)
            {
                Raylib.DrawTexturePro(tex, src, new Rectangle(ring1[i].X, ring1[i].Y, W, H), Vector2.Zero, 0f, bCol1);
            }

            Vector2[] ring2 =
            [
                new(-r2, 0f), new(r2, 0f), new(0f, -r2), new(0f, r2),
                new(-r2 * 0.71f, -r2 * 0.71f), new(r2 * 0.71f, -r2 * 0.71f),
                new(-r2 * 0.71f, r2 * 0.71f), new(r2 * 0.71f, r2 * 0.71f)
            ];
            for (int i = 0; i < ring2.Length; i++)
            {
                Raylib.DrawTexturePro(tex, src, new Rectangle(ring2[i].X, ring2[i].Y, W, H), Vector2.Zero, 0f, bCol2);
            }
        }

        float trackY = (time * 38f) % (H + 160f) - 80f;
        DrawHomeStrip(tex, (int)Math.Clamp(trackY, 0f, H - 1f), 24, MathF.Sin(time * 7.3f) * 5f, HC(255, 255, 255, 235 * (1f - blurAmount * 0.6f)));

        DrawHomeStrip(tex, H - 14, 14, MathF.Sin(time * 2.3f) * 9f + homeRng.Next(-3, 4), HC(205, 205, 215, 255 * (1f - blurAmount * 0.6f)));

        if (homeGlitchTimer > 0f && blurAmount < 0.25f)
        {
            int gs = (int)(time * 24f);
            int ty = HomeHash(gs, 1) % Math.Max(1, H - 60);
            int th = 8 + HomeHash(gs, 2) % 36;
            float dx = (HomeHash(gs, 3) % 41) - 20;
            DrawHomeStrip(tex, ty, th, dx, HC(255, 255, 255, 255));
        }
    }

    static void DrawHomeVhsOverlay(float time)
    {
        int W = ScreenWidth, H = ScreenHeight;

        for (int y = 0; y < H; y += 3)
            Raylib.DrawRectangle(0, y, W, 1, HC(0, 0, 0, 24));

        int bandY = (int)((time * 52f) % (H + 240f) - 120f);
        Raylib.DrawRectangleGradientV(0, bandY, W, 120, HC(190, 205, 255, 0), HC(190, 205, 255, 10));
        Raylib.DrawRectangleGradientV(0, bandY + 120, W, 120, HC(190, 205, 255, 10), HC(190, 205, 255, 0));

        for (int i = 0; i < 420; i++)
        {
            int nx = homeRng.Next(W);
            int ny = homeRng.Next(H);
            bool light = homeRng.Next(3) != 0;
            Raylib.DrawRectangle(nx, ny, homeRng.Next(1, 4), 1,
                light ? HC(255, 255, 255, homeRng.Next(10, 38)) : HC(0, 0, 0, homeRng.Next(18, 48)));
        }
    }

    static void OpenHomeCredits()
    {
        homeSubMenu = 2;
        homeSubMenuAnim = 0f;
        creditsClosing = false;
        creditsElapsed = 0f;
        creditsScrollY = ScreenHeight + 48f;
        creditsSkipHover = 0f;
        homeLangOpen = false;
    }

    static void CloseHomeCredits()
    {
        if (homeSubMenu != 2 || creditsClosing) return;
        creditsClosing = true;
        HomeBlip(1.1f);
    }

    static void DrawHomeCreditsOverlay(float dt, Vector2 mousePos, float blurEase)
    {
        int W = ScreenWidth, H = ScreenHeight;
        float contentAlpha = blurEase;

        Raylib.DrawRectangle(0, 0, W, H, HC(4, 6, 14, 178f * contentAlpha));

        int cursorY = (int)creditsScrollY;
        for (int i = 0; i < homeCreditsList.Length; i++)
        {
            var entry = homeCreditsList[i];
            if (entry.Kind == 3)
            {
                cursorY += 44;
                continue;
            }

            if (cursorY > -90 && cursorY < H + 90)
            {
                float edgeFade = 1f;
                if (cursorY < 130) edgeFade = Math.Clamp((cursorY - 24f) / 106f, 0f, 1f);
                else if (cursorY > H - 130) edgeFade = Math.Clamp((H - 24f - cursorY) / 106f, 0f, 1f);

                float alpha = contentAlpha * edgeFade;
                if (alpha > 0.01f)
                {
                    if (entry.Kind == 0)
                    {
                        string title = HT(entry.ValueEn, entry.ValueVi);
                        float w = Raylib.MeasureTextEx(homeTitleFont, title, 46f, 1.4f).X;
                        DrawVhsText(homeTitleFont, title, (W - w) * 0.5f, cursorY, 46f, 1.4f,
                            HC(255, 52, 128, 255f * alpha), 2.2f);
                    }
                    else if (entry.Kind == 1)
                    {
                        string sec = HT(entry.ValueEn, entry.ValueVi);
                        float w = Raylib.MeasureTextEx(homeHeadingFont, sec, 20f, 2.2f).X;
                        DrawVhsText(homeHeadingFont, sec, (W - w) * 0.5f, cursorY, 20f, 2.2f,
                            HC(255, 238, 64, 245f * alpha), 1.5f);
                    }
                    else
                    {
                        string role = HT(entry.RoleEn, entry.RoleVi);
                        float roleW = Raylib.MeasureTextEx(homeHeadingFont, role, 14f, 1.5f).X;
                        DrawVhsText(homeHeadingFont, role, (W - roleW) * 0.5f, cursorY, 14f, 1.5f,
                            HC(175, 190, 220, 225f * alpha), 1.0f);

                        string val = HT(entry.ValueEn, entry.ValueVi);
                        float valW = Raylib.MeasureTextEx(homeBodyFont, val, 21f, 0.7f).X;
                        DrawVhsText(homeBodyFont, val, (W - valW) * 0.5f, cursorY + 20f, 21f, 0.7f,
                            HC(246, 248, 252, 250f * alpha), 1.2f);
                    }
                }
            }

            cursorY += entry.Kind switch
            {
                0 => 58,
                1 => 46,
                _ => 64
            };
        }

        if (!creditsClosing && cursorY < -40)
        {
            CloseHomeCredits();
        }

        float skipFadeIn = SmoothStep01((creditsElapsed - 0.55f) / 0.75f);
        float skipAlpha = skipFadeIn * contentAlpha;

        if (skipAlpha > 0.01f)
        {
            const float skipW = 148f;
            const float skipH = 42f;
            float skipX = W - skipW - W * 0.052f;
            float skipY = H - skipH - 38f;
            Rectangle skipRect = new(skipX, skipY, skipW, skipH);

            bool hovered = !creditsClosing && Raylib.CheckCollisionPointRec(mousePos, skipRect);
            creditsSkipHover = Math.Clamp(creditsSkipHover + (hovered ? dt * 7f : -dt * 7f), 0f, 1f);
            float hEase = SmoothStep01(creditsSkipHover);

            Color boxBg = LerpCol(HC(8, 12, 30, 175f * skipAlpha), HC(255, 52, 128, 220f * skipAlpha), hEase);
            Color boxBorder = LerpCol(HC(196, 214, 255, 200f * skipAlpha), HC(255, 238, 64, 255f * skipAlpha), hEase);

            Raylib.DrawRectangleRec(skipRect, boxBg);
            Raylib.DrawRectangleLinesEx(skipRect, 1.8f, boxBorder);

            string skipText = "SKIP >>";
            float txtW = Raylib.MeasureTextEx(homeHeadingFont, skipText, 18f, 1.6f).X;
            DrawVhsText(homeHeadingFont, skipText, skipX + (skipW - txtW) * 0.5f, skipY + (skipH - 18f) * 0.5f, 18f, 1.6f,
                HC(255, 255, 255, 255f * skipAlpha), 1.3f);

            if (!creditsClosing && hovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                CloseHomeCredits();
            }
        }
    }

    static bool autoBootJumpscareDone = false;

    static bool DrawHomeScreen(float dt)
    {
        const bool BootStraightToJumpscare = false;

        if (BootStraightToJumpscare && !autoBootJumpscareDone)
        {
            autoBootJumpscareDone = true;
        }

        EnsureHomeFonts();
        EnsureHomeTarget();
        EnsureHomeBgm();

        float clampedDt = Math.Clamp(dt, 0.001f, 0.05f);
        float time = (float)Raylib.GetTime();
        int W = ScreenWidth, H = ScreenHeight;

        homeIntro = MathF.Min(1f, homeIntro + clampedDt * 0.9f);
        float uiIntro = SmoothStep01((homeIntro - 0.15f) / 0.85f);

        homeOrbitAngle += clampedDt * 0.20f;
        Vector2 mousePos = Raylib.GetMousePosition();
        bool mouseMoved = Vector2.DistanceSquared(mousePos, homeLastMouse) > 0.25f;
        homeLastMouse = mousePos;
        float normMouseX = (mousePos.X / Math.Max(1f, W) - 0.5f) * 2f;
        float normMouseY = (mousePos.Y / Math.Max(1f, H) - 0.5f) * 2f;
        homePanAngle += (normMouseX - homePanAngle) * (1f - MathF.Exp(-4.5f * clampedDt));

        camera.Position = new Vector3(
            -1.2f + MathF.Sin(homeOrbitAngle) * 3.6f + homePanAngle * 0.45f,
            2.35f + MathF.Sin(homeOrbitAngle * 0.65f) * 0.16f - normMouseY * 0.12f,
            14.8f + MathF.Cos(homeOrbitAngle) * 0.8f
        );
        camera.Target = new Vector3(-1.8f + homePanAngle * 0.35f, 1.85f, 3.5f);
        camera.Up = Vector3.UnitY;
        camera.FovY = 48f;

        homeSubMenuAnim += (1f - homeSubMenuAnim) * (1f - MathF.Exp(-10f * clampedDt));
        homeLangOpenAnim += ((homeLangOpen ? 1f : 0f) - homeLangOpenAnim) * (1f - MathF.Exp(-14f * clampedDt));

        if (homeSubMenu == 2)
        {
            if (!creditsClosing)
            {
                creditsBlend = MathF.Min(1f, creditsBlend + clampedDt * 2.4f);
                creditsElapsed += clampedDt;
                float scrollSpeed = Raylib.IsMouseButtonDown(MouseButton.Left) ? 125f : 52f;
                creditsScrollY -= scrollSpeed * clampedDt;
            }
            else
            {
                creditsBlend = MathF.Max(0f, creditsBlend - clampedDt * 2.8f);
                if (creditsBlend <= 0.001f)
                {
                    creditsBlend = 0f;
                    creditsClosing = false;
                    homeSubMenu = 0;
                    homeSelection = 2;
                    homeSubMenuAnim = 0f;
                }
            }
        }
        else
        {
            creditsBlend = MathF.Max(0f, creditsBlend - clampedDt * 2.8f);
        }

        float blurEase = SmoothStep01(creditsBlend);

        if (homeGlitchTimer > 0f)
        {
            homeGlitchTimer -= clampedDt;
        }
        else
        {
            homeGlitchCooldown -= clampedDt;
            if (homeGlitchCooldown <= 0f)
            {
                homeGlitchTimer = 0.10f + (float)homeRng.NextDouble() * 0.15f;
                homeGlitchTarget = homeRng.Next(0, 6);
                homeGlitchCooldown = 1.8f + (float)homeRng.NextDouble() * 3.8f;
            }
        }
        int gseed = (int)(time * 28f);

        string mainTitle = HT("DREAD TO DWELL", "ÁM ẢNH ĐỜI THƯỜNG");
        const float titleSpacing = 1.0f;
        float w100 = Raylib.MeasureTextEx(homeTitleFont, mainTitle, 100f, titleSpacing).X;
        float titleSize = Math.Clamp(100f * (W * 0.43f) / Math.Max(1f, w100), 40f, 78f);

        float titleX = W * 0.085f;
        float titleY = H * 0.425f;
        float titleW = Raylib.MeasureTextEx(homeTitleFont, mainTitle, titleSize, titleSpacing).X;
        float titleRight = titleX + titleW;

        float menuTextX = W * 0.735f;
        float menuSize = H * 0.042f;
        float menuStep = H * 0.0725f;
        float menuY0 = H * 0.385f;

        float epStep = H * 0.098f;
        float epY0 = menuY0 - 6f;
        float ChapterY(int i) => i == 3 ? epY0 + 3 * epStep - 8f : epY0 + i * epStep;

        float ddW = Math.Max(250f, W * 0.24f);
        float ddH = Math.Max(40f, H * 0.064f);
        Rectangle ddRect = new(W - ddW - W * 0.052f, H * 0.062f, ddW, ddH);
        string[] langNames = ["English", "Tiếng Việt"];

        float hoverRate = 1f - MathF.Exp(-13.5f * clampedDt);
        float pressRate = 1f - MathF.Exp(-18.0f * clampedDt);

        bool leftClick = Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool leftDown = Raylib.IsMouseButtonDown(MouseButton.Left);
        bool rightClick = Raylib.IsMouseButtonPressed(MouseButton.Right);
        bool escPressed = Raylib.IsKeyPressed(KeyboardKey.Escape);
        bool clickConsumed = false;
        bool overDd = Raylib.CheckCollisionPointRec(mousePos, ddRect);

        if (!leftDown) draggingSlider = -1;

        if (homeSubMenu != 2)
        {
            if (leftClick)
            {
                if (overDd)
                {
                    homeLangOpen = !homeLangOpen;
                    clickConsumed = true;
                    HomeBlip(1.2f);
                }
                else if (homeLangOpen)
                {
                    for (int i = 0; i < langNames.Length; i++)
                    {
                        if (Raylib.CheckCollisionPointRec(mousePos, HomeLangRow(ddRect, i)))
                        {
                            homeLang = i;
                            HomeBlip(1.5f);
                        }
                    }
                    homeLangOpen = false;
                    clickConsumed = true;
                }
            }
            if (homeLangOpen && escPressed)
            {
                homeLangOpen = false;
                escPressed = false;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.L))
            {
                homeLang = 1 - homeLang;
                HomeBlip(1.5f);
            }
        }
        if (clickConsumed) leftClick = false;

        bool hoverEnabled = !homeLangOpen && !overDd && homeSubMenu != 2;

        string[] mainItems =
        [
            HT("Play", "Chơi"),
            HT("Options", "Cài đặt"),
            HT("Credits", "Tác giả"),
            HT("Exit", "Thoát")
        ];
        (string Title, string Sub, bool Unlocked)[] chapters =
        [
            (HT("Chapter 1", "Tập 1"), HT("Night Shift", "Ca Trực Đêm"), true),
            (HT("Chapter 2", "Tập 2"), HT("The Last Bus", "Chuyến Xe Cuối"), false),
            (HT("Chapter 3", "Tập 3"), HT("Empty Boarding House", "Khu Trọ Vắng"), false),
            (HT("Back", "Quay lại"), "", true)
        ];

        if (homeSubMenu == 0)
        {
            for (int i = 0; i < mainItems.Length; i++)
            {
                float y = menuY0 + i * menuStep;
                Rectangle hitBox = new(menuTextX - menuSize * 1.6f, y - 8f, 360f, menuSize + 16f);
                bool hovered = hoverEnabled && Raylib.CheckCollisionPointRec(mousePos, hitBox);

                if (hovered)
                {
                    if (homeSelection != i && (mouseMoved || leftClick))
                    {
                        homeSelection = i;
                        HomeBlip();
                    }

                    if (leftClick)
                    {
                        menuPressAnim[i] = 1f;
                        if (i == 0)
                        {
                            homeSubMenu = 1;
                            homeSelection = 0;
                            homeSubMenuAnim = 0f;
                        }
                        else if (i == 1)
                        {
                            homeSubMenu = 3;
                            homeOptionRow = 0;
                            homeSubMenuAnim = 0f;
                        }
                        else if (i == 2)
                        {
                            OpenHomeCredits();
                        }
                        else return true;
                        break;
                    }
                }
            }

            if (homeSubMenu == 0)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
                {
                    homeSelection = (homeSelection + 1) % mainItems.Length;
                    HomeBlip();
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
                {
                    homeSelection = (homeSelection - 1 + mainItems.Length) % mainItems.Length;
                    HomeBlip();
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    if (homeSelection == 0)
                    {
                        homeSubMenu = 1;
                        homeSelection = 0;
                        homeSubMenuAnim = 0f;
                    }
                    else if (homeSelection == 1)
                    {
                        homeSubMenu = 3;
                        homeOptionRow = 0;
                        homeSubMenuAnim = 0f;
                    }
                    else if (homeSelection == 2)
                    {
                        OpenHomeCredits();
                    }
                    else return true;
                }
            }
        }
        else if (homeSubMenu == 1)
        {
            for (int i = 0; i < chapters.Length; i++)
            {
                float y = ChapterY(i);
                Rectangle hitBox = new(menuTextX - menuSize * 1.6f, y - 8f, 420f, i == 3 ? menuSize + 16f : menuSize + 40f);
                bool hovered = hoverEnabled && Raylib.CheckCollisionPointRec(mousePos, hitBox);
                if (!hovered) continue;

                if (!chapters[i].Unlocked)
                {
                    if (leftClick) HomeBlip(0.7f);
                    continue;
                }

                if (homeSelection != i && (mouseMoved || leftClick))
                {
                    homeSelection = i;
                    HomeBlip();
                }

                if (leftClick)
                {
                    menuPressAnim[i] = 1f;
                    if (i == 0)
                    {
                        StartGameFromHome();
                    }
                    else if (i == 3)
                    {
                        homeSubMenu = 0;
                        homeSelection = 0;
                        homeSubMenuAnim = 0f;
                    }
                    break;
                }
            }

            if (homeSubMenu == 1)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S) ||
                    Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
                {
                    homeSelection = (homeSelection == 0) ? 3 : 0;
                    HomeBlip();
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    if (homeSelection == 0) StartGameFromHome();
                    else
                    {
                        homeSubMenu = 0;
                        homeSelection = 0;
                        homeSubMenuAnim = 0f;
                    }
                }
                else if (escPressed || rightClick)
                {
                    homeSubMenu = 0;
                    homeSelection = 0;
                    homeSubMenuAnim = 0f;
                }
            }
        }
        else if (homeSubMenu == 2)
        {
            if (!creditsClosing && (escPressed || rightClick || Raylib.IsKeyPressed(KeyboardKey.Space)))
            {
                CloseHomeCredits();
            }
        }
        else if (homeSubMenu == 3)
        {
            float optX = menuTextX;
            float optY0 = menuY0 - 20f;
            float barW = Math.Max(220f, W * 0.21f);
            float rowStep = 64f;

            for (int r = 0; r < 3; r++)
            {
                float ry = optY0 + 52f + r * rowStep;
                Rectangle sliderHit = new(optX - 10f, ry - 6f, barW + 20f, 48f);
                if (hoverEnabled && Raylib.CheckCollisionPointRec(mousePos, sliderHit))
                {
                    if (mouseMoved) homeOptionRow = r;
                    if (leftClick)
                    {
                        homeOptionRow = r;
                        draggingSlider = r;
                    }
                }

                if (draggingSlider == r && leftDown)
                {
                    float norm = Math.Clamp((mousePos.X - optX) / barW, 0f, 1f);
                    if (r == 0) masterVolume = norm;
                    else if (r == 1) homeMusicVolume = norm;
                    else homeSfxVolume = norm;
                    ApplyAudioVolumes();
                }
            }

            float backY = optY0 + 52f + 3 * rowStep + 10f;
            Rectangle backHit = new(optX - menuSize * 1.6f, backY - 8f, 320f, menuSize + 16f);
            if (hoverEnabled && Raylib.CheckCollisionPointRec(mousePos, backHit))
            {
                if (homeOptionRow != 3 && mouseMoved)
                {
                    homeOptionRow = 3;
                    HomeBlip();
                }
                if (leftClick)
                {
                    HomeBlip();
                    homeSubMenu = 0;
                    homeSelection = 1;
                    homeSubMenuAnim = 0f;
                }
            }

            if (homeSubMenu == 3)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
                {
                    homeOptionRow = (homeOptionRow + 1) % 4;
                    HomeBlip();
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
                {
                    homeOptionRow = (homeOptionRow + 3) % 4;
                    HomeBlip();
                }
                else if (homeOptionRow < 3)
                {
                    float delta = 0f;
                    if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A)) delta = -0.05f;
                    else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D)) delta = 0.05f;

                    if (MathF.Abs(delta) > 0.001f)
                    {
                        if (homeOptionRow == 0) masterVolume = Math.Clamp(masterVolume + delta, 0f, 1f);
                        else if (homeOptionRow == 1) homeMusicVolume = Math.Clamp(homeMusicVolume + delta, 0f, 1f);
                        else homeSfxVolume = Math.Clamp(homeSfxVolume + delta, 0f, 1f);
                        ApplyAudioVolumes();
                        HomeBlip(1.25f);
                    }
                }
                else if (homeOptionRow == 3 && (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space)))
                {
                    HomeBlip();
                    homeSubMenu = 0;
                    homeSelection = 1;
                    homeSubMenuAnim = 0f;
                }

                if (escPressed || rightClick)
                {
                    HomeBlip();
                    homeSubMenu = 0;
                    homeSelection = 1;
                    homeSubMenuAnim = 0f;
                }
            }
        }

        for (int i = 0; i < menuHoverAnim.Length; i++)
        {
            bool active = (homeSubMenu == 0 && i < mainItems.Length && homeSelection == i) ||
                          (homeSubMenu == 1 && (i == 0 || i == 3) && homeSelection == i) ||
                          (homeSubMenu == 3 && i == 3 && homeOptionRow == 3);
            menuHoverAnim[i] += ((active ? 1f : 0f) - menuHoverAnim[i]) * hoverRate;
            menuPressAnim[i] += (0f - menuPressAnim[i]) * pressRate;
        }

        Raylib.BeginTextureMode(homeSceneTarget);
        Raylib.ClearBackground(HC(8, 12, 30));
        Raylib.BeginMode3D(camera);
        Props.DrawStore(lit: true, doorLocked: false, time, "23:48", phoneRinging: false, daylight: 0f);
        DrawStoreShelves(lit: true);
        Props.DrawStoreGlass(doorLocked: false, daylight: 0f);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        float aberration = 1.8f + (homeGlitchTimer > 0f ? 3.2f : 0f) + MathF.Sin(time * 1.7f) * 0.35f;
        DrawHomeSceneComposite(time, aberration, blurEase);

        Raylib.DrawRectangle(0, 0, W, H, HC(8, 20, 66, 62));
        Raylib.DrawRectangle(0, 0, W, H, HC(5, 7, 13, 70));
        int leftGradW = (int)(W * 0.50f);
        Raylib.DrawRectangleGradientH(0, 0, leftGradW, H, HC(4, 6, 16, 150), HC(4, 6, 16, 0));
        int rightGradW = (int)(W * 0.46f);
        Raylib.DrawRectangleGradientH(W - rightGradW, 0, rightGradW, H, HC(4, 6, 16, 0), HC(4, 6, 16, 205));
        DrawVignette(70);

        float homeUiAlpha = uiIntro * (1f - blurEase);
        if (homeUiAlpha > 0.01f)
        {
            float flick = 0.94f + 0.06f * MathF.Sin(time * 31f);
            Color white = HC(255, 255, 255, 245 * homeUiAlpha);
            Color pink = HC(255, 52, 128, 255 * homeUiAlpha * flick);
            Color yellow = HC(255, 238, 64, 255 * homeUiAlpha);

            DrawVhsText(homeHeadingFont, HT("AN EPISODIC HORROR", "KINH DỊ NHIỀU TẬP"),
                titleX + 2f, titleY - 15f, 17f, 1.2f, white, 1.4f);

            for (int k = 0; k < 4; k++)
            {
                float gx = (k == 0 ? -3.5f : (k == 1 ? 3.5f : 0f));
                float gy = (k == 2 ? -3.5f : (k == 3 ? 3.5f : 0f));
                Raylib.DrawTextEx(homeTitleFont, mainTitle, new Vector2(titleX + gx, titleY + gy), titleSize, titleSpacing,
                    HC(255, 52, 128, 30 * homeUiAlpha));
            }
            DrawVhsText(homeTitleFont, mainTitle, titleX, titleY, titleSize, titleSpacing, pink, 2.6f, HomeGlitchFor(0), gseed);

            string subTitle = HT("NIGHT SHIFT", "CA TRỰC ĐÊM");
            float subSize = titleSize * 0.40f;
            const float subSpacing = 1.0f;
            float subW = Raylib.MeasureTextEx(homeHeadingFont, subTitle, subSize, subSpacing).X;
            DrawVhsText(homeHeadingFont, subTitle, titleRight - subW, titleY + titleSize + 1f, subSize, subSpacing,
                yellow, 2.0f, HomeGlitchFor(1), gseed + 5);

            DrawVhsText(homeBodyFont, HomeVersion, titleX + 2f, H * 0.775f, 17f, 0.8f, HC(235, 238, 245, 215 * homeUiAlpha), 1.2f);

            DrawVhsText(homeBodyFont,
                HT("A night shift at the store at the end of Street 9.", "Một ca trực đêm tại cửa hàng cuối đường Số 9."),
                titleX + 2f, H - 82f, 16f, 0.6f, HC(205, 212, 224, 175 * homeUiAlpha), 1.0f);
            DrawVhsText(homeBodyFont,
                HT("Do not open the door after 3:00 AM.", "Đừng mở cửa sau 03:00 sáng."),
                titleX + 2f, H - 58f, 17f, 0.6f, HC(245, 246, 250, 225 * homeUiAlpha), 1.0f);

            float panelSlide = (1f - EaseOutCubic(homeSubMenuAnim)) * 22f;
            float panelAlpha = SmoothStep01(homeSubMenuAnim) * homeUiAlpha;

            if (homeSubMenu == 0)
            {
                for (int i = 0; i < mainItems.Length; i++)
                {
                    DrawHomeMenuItem(mainItems[i], menuTextX + panelSlide, menuY0 + i * menuStep, menuSize,
                        SmoothStep01(menuHoverAnim[i]), menuPressAnim[i], panelAlpha, HomeGlitchFor(2 + i), gseed + i * 7);
                }
            }
            else if (homeSubMenu == 1)
            {
                DrawVhsText(homeHeadingFont, HT("SELECT EPISODE", "CHỌN TẬP"), menuTextX + panelSlide, menuY0 - 52f, 16f, 2.4f,
                    HC(255, 238, 64, 240 * panelAlpha), 1.4f);

                for (int i = 0; i < chapters.Length; i++)
                {
                    float y = ChapterY(i);
                    var ch = chapters[i];

                    if (!ch.Unlocked)
                    {
                        DrawVhsText(homeMenuFont, ch.Title, menuTextX + panelSlide, y, menuSize * 0.92f, 0.6f,
                            HC(165, 172, 186, 140 * panelAlpha), 1.0f);
                        DrawVhsText(homeBodyFont, $"{ch.Sub}  •  {HT("Locked", "Chưa mở")}", menuTextX + panelSlide, y + menuSize + 2f, 16f, 0.6f,
                            HC(145, 152, 166, 120 * panelAlpha), 0.8f);
                    }
                    else
                    {
                        float ease = SmoothStep01(menuHoverAnim[i]);
                        DrawHomeMenuItem(ch.Title, menuTextX + panelSlide, y, menuSize, ease, menuPressAnim[i], panelAlpha, 0f, gseed);

                        if (!string.IsNullOrEmpty(ch.Sub))
                        {
                            float shift = ease * 5f;
                            Color subCol = LerpCol(HC(225, 195, 70, 215 * panelAlpha), HC(255, 238, 64, 255 * panelAlpha), ease);
                            DrawVhsText(homeBodyFont, ch.Sub, menuTextX + panelSlide + shift, y + menuSize + 2f, 18f, 0.6f, subCol, 1.2f);
                        }
                    }
                }
            }
            else if (homeSubMenu == 3)
            {
                float ox = menuTextX + panelSlide;
                float oy = menuY0 - 20f;
                float barW = Math.Max(220f, W * 0.21f);
                float rowStep = 64f;

                DrawVhsText(homeMenuFont, HT("OPTIONS", "CÀI ĐẶT"), ox, oy, 30f, 2.0f, HC(255, 52, 128, 255 * panelAlpha), 1.8f);

                DrawHomeVolumeSlider(HT("MASTER VOLUME", "ÂM LƯỢNG TỔNG"), masterVolume, ox, oy + 52f, barW, homeOptionRow == 0, panelAlpha);
                DrawHomeVolumeSlider(HT("MUSIC VOLUME", "NHẠC NỀN"), homeMusicVolume, ox, oy + 52f + rowStep, barW, homeOptionRow == 1, panelAlpha);
                DrawHomeVolumeSlider(HT("SFX VOLUME", "HIỆU ỨNG"), homeSfxVolume, ox, oy + 52f + rowStep * 2f, barW, homeOptionRow == 2, panelAlpha);

                float backY = oy + 52f + 3f * rowStep + 10f;
                DrawHomeMenuItem(HT("Back", "Quay lại"), ox, backY, menuSize * 0.92f,
                    SmoothStep01(menuHoverAnim[3]), menuPressAnim[3], panelAlpha, 0f, gseed);
            }

            DrawHomeLanguageDropdown(ddRect, langNames, mousePos, homeUiAlpha);
        }

        if (blurEase > 0.005f)
        {
            DrawHomeCreditsOverlay(clampedDt, mousePos, blurEase);
        }

        DrawHomeVhsOverlay(time);

        if (homeIntro < 1f)
        {
            Raylib.DrawRectangle(0, 0, W, H, HC(0, 0, 0, 255f * (1f - EaseOutCubic(homeIntro))));
        }

        Raylib.EndDrawing();
        return false;
    }

    static void StartGameFromHome()
    {
        if (homeBgmLoaded && Raylib.IsMusicStreamPlaying(homeBgm))
        {
            Raylib.StopMusicStream(homeBgm);
        }
        gameScene = -1;
        homeSubMenu = 0;
        homeLangOpen = false;
        draggingSlider = -1;
        phoneNotifStep = 0;
        phoneStepTimer = 0.7f;
        phoneChoiceStage = 0;
        phoneSelectedOption = 0;
        phoneEndHoldTimer = 0f;
        phoneSentReply = "";
        fadeAlpha = 1f;
        homePanAngle = 0f;
        Raylib.DisableCursor();
        Raylib.GetMouseDelta();
    }

    static void ReturnToHome()
    {
        ResetGame();
        gameScene = -2;
        homeSubMenu = 0;
        homeSelection = 0;
        homeSubMenuAnim = 0f;
        titleUnderlineProgress = 0f;
        homePanAngle = 0f;
        homeIntro = 0f;
        homeLangOpen = false;
        draggingSlider = -1;
        creditsClosing = false;
        creditsBlend = 0f;
        if (homeBgmLoaded)
        {
            Raylib.PlayMusicStream(homeBgm);
            ApplyAudioVolumes();
        }
        Raylib.EnableCursor();
    }
}