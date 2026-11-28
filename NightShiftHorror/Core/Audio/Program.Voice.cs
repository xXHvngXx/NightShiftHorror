using System.Security.Cryptography;
using System.Text;
using Raylib_cs;

partial class Program
{
    static Sound currentVoiceClip;
    static bool hasActiveVoiceClip = false;
    static readonly Dictionary<string, Sound> voiceCache = [];

    // Các đối tượng/vật thể không phải người nói trực tiếp
    static readonly HashSet<string> nonSpokenSpeakers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Áp phích ngã tư",
        "Tờ quy tắc ca đêm",
        "Chữ viết tay bên lề",
        "Sổ giao ca",
        "Điện thoại bàn",
        "Tin nhắn (Dũng)",
        "Hệ thống",
        "Ghi chú"
    };

    static bool IsNarrationLine(string speaker, string text)
    {
        if (nonSpokenSpeakers.Contains(speaker.Trim()))
            return true;

        string t = text.Trim();

        // Câu mô tả hành động/cảnh vật đặt trong ngoặc hoặc dấu sao
        if (t.StartsWith('(') || t.StartsWith('*'))
            return true;

        // Các câu tự sự ngôi thứ 3, mô tả bối cảnh, hướng dẫn phím hoặc suy nghĩ thầm của Tú
        string[] narrationPrefixes =
        [
            "23:45",
            "Trên cột đèn",
            "Trước khi rời quán cà phê",
            "Trời tối sương bắt đầu xuống rồi",
            "Vào nhận bàn giao ca thôi",
            "Lời thằng Hưng dặn lúc nãy",
            "Mình vừa gạt chốt khóa cửa",
            "Bóng áo mưa vàng áp sát mặt kính",
            "Cô ta đứng khựng lại",
            "Đầu dây bên kia giọng Dũng",
            "Điện thoại Tú rung lên"
        ];

        foreach (string prefix in narrationPrefixes)
        {
            if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static string GetVoiceFileKey(string speaker, string text)
    {
        string raw = $"{speaker}|{text.Trim()}";
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    static void PlayDialogueVoice(string speaker, string text)
    {
        StopDialogueVoice();

        if (string.IsNullOrWhiteSpace(text)) return;

        // Bỏ qua lồng tiếng nếu là câu tự sự / suy nghĩ / đọc văn bản
        if (IsNarrationLine(speaker, text)) return;

        string key = GetVoiceFileKey(speaker, text);
        if (!voiceCache.TryGetValue(key, out Sound clip))
        {
            string mp3Path = Path.Combine("Assets", "Voices", $"{key}.mp3");
            string wavPath = Path.Combine("Assets", "Voices", $"{key}.wav");
            string? foundPath = File.Exists(mp3Path) ? mp3Path : (File.Exists(wavPath) ? wavPath : null);

            if (foundPath == null) return;

            clip = Raylib.LoadSound(foundPath);
            Raylib.SetSoundVolume(clip, 0.95f);
            voiceCache[key] = clip;
        }

        float pitch = speaker switch
        {
            "Ông cụ" => 0.80f,
            "Anh Hùng" => 0.86f,
            "Quản lý" => 0.88f,
            "Dũng" => 0.94f,
            "Đen" => 0.97f,
            "Hưng" => 1.04f,
            "Chị Mai" => 1.18f,
            "Cô gái (Áo mưa vàng)" => 1.12f,
            _ => 1.0f
        };

        currentVoiceClip = clip;
        hasActiveVoiceClip = true;
        Raylib.SetSoundPitch(currentVoiceClip, pitch);
        Raylib.PlaySound(currentVoiceClip);
    }

    static void StopDialogueVoice()
    {
        if (hasActiveVoiceClip)
        {
            Raylib.StopSound(currentVoiceClip);
            hasActiveVoiceClip = false;
        }
    }

    static void UnloadAllVoices()
    {
        StopDialogueVoice();
        foreach (var snd in voiceCache.Values)
        {
            Raylib.UnloadSound(snd);
        }
        voiceCache.Clear();
    }
}