using System.Numerics;
using Raylib_cs;

public static partial class Props
{
    static readonly Dictionary<FaceId, Texture2D> FaceTextures = new();

    public static readonly List<string> FaceLoadReport = new();

    sealed record FaceFit(float U0 = 0f, float U1 = 1f, float V0 = 0f, float V1 = 1f,
                          float Horiz = 1.15f, float Vert = 1.15f, float VertShift = 0.02f);

    static readonly FaceFit DefaultFit = new();

    static readonly Dictionary<FaceId, FaceFit> FaceFits = new();

    static List<string> FaceFolderCandidates()
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<string> result = new();

        void Try(string p)
        {
            string full = Path.GetFullPath(p);
            if (Directory.Exists(full) && seen.Add(full)) result.Add(full);
        }

        Try(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Faces"));
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && d != null; i++, d = d.Parent)
            Try(Path.Combine(d.FullName, "Assets", "Faces"));

        return result;
    }

    static bool IsWebp(string path)
    {
        try
        {
            using FileStream fs = File.OpenRead(path);
            Span<byte> h = stackalloc byte[12];
            if (fs.Read(h) < 12) return false;
            return h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F'
                && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P';
        }
        catch { return false; }
    }

    static void Report(string msg)
    {
        FaceLoadReport.Add(msg);
        Console.WriteLine("[Faces] " + msg);
    }

    public static void LoadFaceTextures()
    {
        UnloadFaceTextures();
        FaceLoadReport.Clear();

        Dictionary<string, FaceId> nameToId = new(StringComparer.OrdinalIgnoreCase)
        {
            ["old_man"] = FaceId.OldMan,
            ["girl"] = FaceId.Girl,
            ["strange_man"] = FaceId.StrangeMan,
            ["yellow_normal"] = FaceId.YellowNormal,
            ["yellow_horror"] = FaceId.YellowHorror,
            ["manager"] = FaceId.Manager,
            ["friend1"] = FaceId.Friend1,
            ["friend_1"] = FaceId.Friend1,
            ["friend2"] = FaceId.Friend2,
            ["friend_2"] = FaceId.Friend2,
            ["friend3"] = FaceId.Friend3,
            ["friend_3"] = FaceId.Friend3
        };

        List<string> folders = FaceFolderCandidates();
        if (folders.Count == 0)
        {
            Report("KHÔNG tìm thấy thư mục Assets/Faces ở bất kỳ đâu.");
            return;
        }

        Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
        foreach (string dir in folders)
        {
            Report("Quét: " + dir);
            foreach (string path in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is not (".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga")) continue;

                string stem = Path.GetFileNameWithoutExtension(path);
                if (!files.TryGetValue(stem, out string? old) ||
                    File.GetLastWriteTimeUtc(path) > File.GetLastWriteTimeUtc(old))
                    files[stem] = path;
            }
        }

        foreach (var (stem, path) in files)
        {
            if (!nameToId.TryGetValue(stem, out FaceId id))
            {
                Report($"Bỏ qua (tên không khớp FaceId): {Path.GetFileName(path)}");
                continue;
            }

            if (IsWebp(path))
            {
                Report($"LỖI {Path.GetFileName(path)}: thực chất là WebP đội lốt .png -> hãy xuất lại thành PNG thật.");
                continue;
            }

            Texture2D tex = Raylib.LoadTexture(path);
            if (tex.Id == 0)
            {
                Report($"LỖI không nạp được: {path}");
                continue;
            }

            Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
            FaceTextures[id] = tex;
            Report($"OK {Path.GetFileName(path)} ({tex.Width}x{tex.Height})  <- {Path.GetDirectoryName(path)}");
        }

        foreach (FaceId id in Enum.GetValues<FaceId>())
        {
            if (id == FaceId.None) continue;
            if (!FaceTextures.ContainsKey(id)) Report($"THIẾU ảnh cho {id} (đang dùng mặt dự phòng)");
        }
    }

    public static void UnloadFaceTextures()
    {
        foreach (var tex in FaceTextures.Values)
            if (tex.Id > 0) Raylib.UnloadTexture(tex);
        FaceTextures.Clear();
    }

    const float HeadCy = 0.15f, HeadCz = 0.005f;
    const float HeadRx = 0.128f, HeadRy = 0.178f, HeadRz = 0.142f;

    static void DrawFacePatch(Texture2D tex, FaceFit fit)
    {
        // Đẩy mesh mặt phồng hơn da đầu một chút để phủ kín hoàn toàn mặt trước
        float rx = HeadRx * 1.05f, ry = HeadRy * 1.04f, rz = HeadRz * 1.06f;

        float u0 = fit.U0, u1 = fit.U1, v0 = fit.V0, v1 = fit.V1;
        float vert = fit.Vert;
        float horiz = fit.Horiz;

        const int cols = 16, rows = 16;

        Vector3 PointAt(float uu, float vv)
        {
            float theta = (uu - 0.5f) * 2f * horiz;
            float phi = (0.5f - vv) * 2f * vert + fit.VertShift;
            float cosPhi = MathF.Cos(phi);
            return new Vector3(
                rx * cosPhi * MathF.Sin(theta),
                HeadCy + ry * MathF.Sin(phi),
                HeadCz + rz * cosPhi * MathF.Cos(theta));
        }

        Raylib.BeginBlendMode(BlendMode.Alpha);
        Rlgl.SetTexture(tex.Id);
        Rlgl.Begin(DrawMode.Triangles);
        Rlgl.Color4ub(255, 255, 255, 255);

        for (int r = 0; r < rows; r++)
        {
            float a0 = (float)r / rows, a1 = (float)(r + 1) / rows;
            for (int c = 0; c < cols; c++)
            {
                float b0 = (float)c / cols, b1 = (float)(c + 1) / cols;

                Vector3 tl = PointAt(b0, a0), bl = PointAt(b0, a1);
                Vector3 br = PointAt(b1, a1), tr = PointAt(b1, a0);

                float tu0 = u0 + (u1 - u0) * b0, tu1 = u0 + (u1 - u0) * b1;
                float tv0 = v0 + (v1 - v0) * a0, tv1 = v0 + (v1 - v0) * a1;

                Rlgl.TexCoord2f(tu0, tv0); Rlgl.Vertex3f(tl.X, tl.Y, tl.Z);
                Rlgl.TexCoord2f(tu0, tv1); Rlgl.Vertex3f(bl.X, bl.Y, bl.Z);
                Rlgl.TexCoord2f(tu1, tv1); Rlgl.Vertex3f(br.X, br.Y, br.Z);

                Rlgl.TexCoord2f(tu0, tv0); Rlgl.Vertex3f(tl.X, tl.Y, tl.Z);
                Rlgl.TexCoord2f(tu1, tv1); Rlgl.Vertex3f(br.X, br.Y, br.Z);
                Rlgl.TexCoord2f(tu1, tv0); Rlgl.Vertex3f(tr.X, tr.Y, tr.Z);
            }
        }

        Rlgl.End();
        Rlgl.SetTexture(0);
        Raylib.EndBlendMode();
    }

    internal static void DrawHead(bool isFemale, Color hair, Color skin, FaceId faceId, Vector2 eyeGaze)
    {
        Texture2D tex = default;
        bool hasTex = faceId != FaceId.None && FaceTextures.TryGetValue(faceId, out tex) && tex.Id > 0;

        Ellipsoid(0, HeadCy, HeadCz, HeadRx, HeadRy, HeadRz, skin, 16, 18);

        if (hasTex)
        {
            Ellipsoid(0, 0.232f, -0.03f, 0.134f, 0.118f, 0.14f, hair, 12, 14);
        }
        else
        {
            Ellipsoid(0, 0.03f, 0.018f, 0.095f, 0.075f, 0.112f, skin, 12, 14);
            Ellipsoid(0, 0.22f, -0.012f, 0.134f, 0.125f, 0.148f, hair, 12, 14);
        }

        if (isFemale)
        {
            Box(0, -0.02f, -0.145f, 0.31f, 0.56f, 0.07f, hair);
            Box(-0.142f, 0.02f, 0.02f, 0.048f, 0.44f, 0.14f, hair);
            Box(0.142f, 0.02f, 0.02f, 0.048f, 0.44f, 0.14f, hair);
        }
        else
        {
            Box(0, 0.17f, -0.132f, 0.24f, 0.20f, 0.06f, hair);
        }

        if (hasTex)
        {
            FaceFit fit = FaceFits.TryGetValue(faceId, out FaceFit? f) ? f : DefaultFit;
            DrawFacePatch(tex, fit);
            return;
        }

        float eyeY = 0.175f, scleraZ = 0.131f, pupilZ = 0.141f;
        float pupilOffsetX = eyeGaze.X * 0.013f;
        float pupilOffsetY = eyeGaze.Y * 0.010f;

        foreach (float side in new[] { -0.048f, 0.048f })
        {
            Ellipsoid(side, eyeY, scleraZ, 0.025f, 0.015f, 0.012f, Col(245, 245, 245), 8, 10);
            Ellipsoid(side + pupilOffsetX, eyeY + pupilOffsetY, pupilZ, 0.012f, 0.012f, 0.008f, Col(22, 20, 24), 8, 10);
        }
        Ellipsoid(0, 0.125f, 0.145f, 0.014f, 0.024f, 0.018f, Col(208, 170, 144), 8, 8);
        Box(0, 0.065f, 0.136f, 0.056f, 0.012f, 0.012f, Col(150, 90, 90));
    }
}