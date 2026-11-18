using System.Numerics;
using Raylib_cs;

public static partial class Props
{
    static readonly Dictionary<FaceId, Texture2D> FaceTextures = new();

    static string? ResolveFacePath(string fileName)
    {
        string[] candidates =
        [
            Path.Combine("assets", "faces", fileName),
            Path.Combine("faces", fileName),
            Path.Combine("assets", fileName),
            fileName
        ];

        foreach (string path in candidates)
        {
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public static void LoadFaceTextures()
    {
        UnloadFaceTextures();

        (FaceId Id, string FileName, bool IsHorror)[] specs =
        [
            (FaceId.OldMan,       "oldman.png",        false),
            (FaceId.Girl,         "girl.png",          false),
            (FaceId.StrangeMan,   "strangeman.png",    false),
            (FaceId.YellowNormal, "yellownormal.png",  false),
            (FaceId.YellowHorror, "yellow_horror.png", true),
            (FaceId.Manager,      "manager.png",       false),
            (FaceId.Friend1,      "friend1.png",       false),
            (FaceId.Friend2,      "friend2.png",       false),
            (FaceId.Friend3,      "friend3.png",       false),
        ];

        Color baseSkin3D = Col(222, 186, 160);

        foreach (var (id, fileName, isHorror) in specs)
        {
            string? path = ResolveFacePath(fileName);
            if (path == null && id == FaceId.YellowHorror)
                path = ResolveFacePath("yellowhorror.png");

            if (path == null) continue;

            Image raw = Raylib.LoadImage(path);
            if (raw.Width <= 0 || raw.Height <= 0) continue;

            if (!isHorror)
            {
                Image normalized = NormalizeFaceSkinImage(raw, baseSkin3D);
                Raylib.UnloadImage(raw);
                Texture2D tex = Raylib.LoadTextureFromImage(normalized);
                Raylib.UnloadImage(normalized);
                Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                FaceTextures[id] = tex;
            }
            else
            {
                Texture2D horrorTex = Raylib.LoadTextureFromImage(raw);
                Raylib.UnloadImage(raw);
                Raylib.SetTextureFilter(horrorTex, TextureFilter.Bilinear);
                FaceTextures[id] = horrorTex;
            }
        }
    }

    public static void UnloadFaceTextures()
    {
        foreach (var tex in FaceTextures.Values)
        {
            if (tex.Id != 0)
            {
                Raylib.UnloadTexture(tex);
            }
        }
        FaceTextures.Clear();
    }

    static Image NormalizeFaceSkinImage(Image rawImg, Color targetSkin)
    {
        Image img = Raylib.ImageCopy(rawImg);
        Raylib.ImageFormat(ref img, PixelFormat.UncompressedR8G8B8A8);

        int w = img.Width;
        int h = img.Height;

        long sumR = 0, sumG = 0, sumB = 0;
        int count = 0;

        for (int y = (int)(h * 0.44f); y < (int)(h * 0.62f); y++)
        {
            for (int x = (int)(w * 0.22f); x < (int)(w * 0.78f); x++)
            {
                if (x > w * 0.36f && x < w * 0.64f) continue;

                Color p = Raylib.GetImageColor(img, x, y);
                if (p.A > 210)
                {
                    sumR += p.R;
                    sumG += p.G;
                    sumB += p.B;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            float avgR = (float)sumR / count;
            float avgG = (float)sumG / count;
            float avgB = (float)sumB / count;

            const float blend = 0.72f;
            float ratioR = 1f + ((targetSkin.R / Math.Max(1f, avgR)) - 1f) * blend;
            float ratioG = 1f + ((targetSkin.G / Math.Max(1f, avgG)) - 1f) * blend;
            float ratioB = 1f + ((targetSkin.B / Math.Max(1f, avgB)) - 1f) * blend;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color p = Raylib.GetImageColor(img, x, y);
                    if (p.A == 0) continue;

                    byte nr = (byte)Math.Clamp(p.R * ratioR, 0, 255);
                    byte ng = (byte)Math.Clamp(p.G * ratioG, 0, 255);
                    byte nb = (byte)Math.Clamp(p.B * ratioB, 0, 255);

                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (y - h * 0.5f) / (h * 0.5f);
                    float dist = MathF.Sqrt(nx * nx + ny * ny);
                    float edgeFade = dist > 0.80f ? Math.Clamp(1f - (dist - 0.80f) / 0.20f, 0f, 1f) : 1f;
                    byte na = (byte)(p.A * edgeFade);

                    Raylib.ImageDrawPixel(ref img, x, y, new Color(nr, ng, nb, na));
                }
            }
        }

        return img;
    }

    static void DrawHead(bool isFemale, Color hair, Color skin, FaceId faceId, Vector2 eyeGaze)
    {
        Ellipsoid(0f, 0.16f, 0f, 0.135f, 0.165f, 0.145f, skin, 16, 20);

        Ellipsoid(-0.136f, 0.15f, -0.01f, 0.022f, 0.042f, 0.028f, skin, 8, 10);
        Ellipsoid(0.136f, 0.15f, -0.01f, 0.022f, 0.042f, 0.028f, skin, 8, 10);

        Ellipsoid(0f, 0.21f, -0.025f, 0.142f, 0.148f, 0.152f, hair, 14, 18);
        if (isFemale)
        {
            Box(0f, 0.06f, -0.09f, 0.25f, 0.26f, 0.11f, hair);
        }

        if (faceId != FaceId.None && FaceTextures.TryGetValue(faceId, out Texture2D faceTex) && faceTex.Id != 0)
        {
            DrawCurvedFacePatch(faceTex, eyeGaze);
        }
        else
        {
            DrawProceduralFallbackFace(hair, skin, eyeGaze);
        }
    }

    static void DrawCurvedFacePatch(Texture2D tex, Vector2 eyeGaze)
    {
        const int cols = 14;
        const int rows = 14;
        const float rx = 0.136f;
        const float ry = 0.164f;
        const float rz = 0.147f;
        const float centerY = 0.158f;

        const float maxYaw = 0.94f;
        const float maxPitch = 1.08f;

        float uvShiftX = -eyeGaze.X * 0.018f;
        float uvShiftY = eyeGaze.Y * 0.015f;

        Rlgl.SetTexture(tex.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(255, 255, 255, 255);

        for (int r = 0; r < rows; r++)
        {
            float v0 = (float)r / rows;
            float v1 = (float)(r + 1) / rows;
            float p0 = (0.5f - v0) * 2f * maxPitch;
            float p1 = (0.5f - v1) * 2f * maxPitch;

            for (int c = 0; c < cols; c++)
            {
                float u0 = (float)c / cols;
                float u1 = (float)(c + 1) / cols;
                float y0 = (u0 - 0.5f) * 2f * maxYaw;
                float y1 = (u1 - 0.5f) * 2f * maxYaw;

                Vector3 pt00 = FaceSurfacePoint(y0, p0, rx, ry, rz, centerY);
                Vector3 pt10 = FaceSurfacePoint(y1, p0, rx, ry, rz, centerY);
                Vector3 pt11 = FaceSurfacePoint(y1, p1, rx, ry, rz, centerY);
                Vector3 pt01 = FaceSurfacePoint(y0, p1, rx, ry, rz, centerY);

                float tu0 = Math.Clamp(u0 + uvShiftX, 0f, 1f);
                float tu1 = Math.Clamp(u1 + uvShiftX, 0f, 1f);
                float tv0 = Math.Clamp(v0 + uvShiftY, 0f, 1f);
                float tv1 = Math.Clamp(v1 + uvShiftY, 0f, 1f);

                Rlgl.TexCoord2f(tu0, tv0); Rlgl.Vertex3f(pt00.X, pt00.Y, pt00.Z);
                Rlgl.TexCoord2f(tu0, tv1); Rlgl.Vertex3f(pt01.X, pt01.Y, pt01.Z);
                Rlgl.TexCoord2f(tu1, tv1); Rlgl.Vertex3f(pt11.X, pt11.Y, pt11.Z);
                Rlgl.TexCoord2f(tu1, tv0); Rlgl.Vertex3f(pt10.X, pt10.Y, pt10.Z);
            }
        }

        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    static Vector3 FaceSurfacePoint(float yaw, float pitch, float rx, float ry, float rz, float centerY)
    {
        float cosP = MathF.Cos(pitch);
        float x = rx * cosP * MathF.Sin(yaw);
        float y = centerY + ry * MathF.Sin(pitch);
        float z = rz * cosP * MathF.Cos(yaw) + 0.002f;
        return new Vector3(x, y, z);
    }

    static void DrawProceduralFallbackFace(Color hair, Color skin, Vector2 eyeGaze)
    {
        // Mũi
        Ellipsoid(0f, 0.145f, 0.145f, 0.016f, 0.025f, 0.022f, LerpCol(skin, Col(175, 130, 105), 0.35f), 8, 10);

        // Mắt & lông mày
        float gx = eyeGaze.X * 0.008f;
        float gy = eyeGaze.Y * 0.006f;
        foreach (float sx in new[] { -0.048f, 0.048f })
        {
            Box(sx, 0.205f, 0.132f, 0.044f, 0.010f, 0.012f, hair);
            Ellipsoid(sx, 0.182f, 0.132f, 0.020f, 0.013f, 0.012f, Col(245, 245, 245), 8, 10);
            Ellipsoid(sx + gx, 0.182f + gy, 0.141f, 0.010f, 0.010f, 0.006f, Col(30, 26, 24), 8, 10);
        }

        // Miệng
        Box(0f, 0.095f, 0.134f, 0.052f, 0.010f, 0.012f, Col(165, 95, 90));
    }
}