using System.Numerics;
using Raylib_cs;

public static partial class Props
{
    public static void LoadFaceTextures() { }
    public static void UnloadFaceTextures() { }

    static void AngledBox(float x, float y, float z, float w, float h, float l, float rotX, float rotY, float rotZ, Color c)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(x, y, z);
        if (rotY != 0f) Rlgl.Rotatef(rotY, 0, 1, 0);
        if (rotX != 0f) Rlgl.Rotatef(rotX, 1, 0, 0);
        if (rotZ != 0f) Rlgl.Rotatef(rotZ, 0, 0, 1);
        Raylib.DrawCube(Vector3.Zero, w, h, l, c);
        Rlgl.PopMatrix();
    }

    static void DrawHead(bool isFemale, Color hair, Color skin, FaceId faceId, Vector2 eyeGaze)
    {
        bool fem = isFemale || faceId is FaceId.Girl or FaceId.YellowNormal or FaceId.YellowHorror;

        Color baseSkin = faceId switch
        {
            FaceId.OldMan => Col(212, 182, 158),
            FaceId.StrangeMan => Col(198, 184, 172),
            FaceId.YellowHorror => Col(178, 182, 186),
            FaceId.Friend3 => Col(194, 152, 122),
            FaceId.Manager => Col(214, 178, 150),
            _ when fem => Col(232, 198, 176),
            _ => skin
        };

        Color shadeSkin = LerpCol(baseSkin, Col(75, 48, 38), 0.22f);
        Color deepShade = LerpCol(baseSkin, Col(45, 28, 24), 0.38f);
        Color hairCol = faceId switch
        {
            FaceId.OldMan => Col(224, 224, 228),
            FaceId.StrangeMan => Col(28, 26, 30),
            FaceId.Manager => Col(24, 24, 28),
            _ => hair
        };

        float jawWidth = fem ? 0.088f : 0.102f;
        float cheekWidth = fem ? 0.108f : 0.116f;

        float neckRadius = fem ? 0.043f : 0.051f;
        Raylib.DrawCylinder(new Vector3(0f, -0.05f, -0.008f), neckRadius, neckRadius * 1.05f, 0.14f, 16, shadeSkin);

        Ellipsoid(0f, 0.195f, -0.006f, 0.122f, 0.132f, 0.132f, baseSkin, 14, 18);
        Ellipsoid(0f, 0.112f, 0.018f, jawWidth, 0.092f, 0.108f, baseSkin, 12, 16);
        Ellipsoid(0f, 0.058f, 0.086f, fem ? 0.036f : 0.046f, 0.032f, 0.042f, baseSkin, 10, 12);

        foreach (int sd in new[] { -1, 1 })
        {
            Ellipsoid(sd * (cheekWidth * 0.72f), 0.148f, 0.082f, 0.044f, 0.036f, 0.046f, baseSkin, 10, 12);
            if (faceId is FaceId.OldMan or FaceId.StrangeMan or FaceId.YellowHorror)
                Ellipsoid(sd * 0.076f, 0.112f, 0.078f, 0.028f, 0.034f, 0.032f, shadeSkin, 8, 10);

            Ellipsoid(sd * 0.121f, 0.168f, -0.008f, 0.016f, 0.036f, 0.024f, shadeSkin, 8, 10);
        }

        float noseLen = faceId == FaceId.OldMan ? 0.026f : (fem ? 0.018f : 0.022f);
        float noseW = faceId == FaceId.OldMan ? 0.022f : (fem ? 0.015f : 0.018f);
        AngledBox(0f, 0.166f, 0.124f, noseW * 0.85f, 0.048f, 0.026f, -16f, 0f, 0f, baseSkin);
        Ellipsoid(0f, 0.142f, 0.128f + noseLen * 0.45f, noseW, 0.015f, noseLen, baseSkin, 10, 12);
        Ellipsoid(0f, 0.132f, 0.126f + noseLen * 0.30f, noseW * 0.85f, 0.008f, 0.014f, deepShade, 8, 10);

        bool isHorror = faceId == FaceId.YellowHorror;
        Color scleraCol = isHorror ? Col(18, 16, 20) : Col(244, 244, 240);
        Color irisCol = faceId switch
        {
            FaceId.YellowHorror => Col(255, 245, 210),
            FaceId.OldMan => Col(72, 62, 54),
            FaceId.Manager => Col(42, 34, 28),
            _ => Col(48, 36, 28)
        };

        foreach (int sd in new[] { -1, 1 })
        {
            float ex = sd * 0.045f;
            float ey = 0.184f;
            float ez = 0.111f;

            Ellipsoid(ex, ey, ez - 0.004f, 0.026f, 0.019f, 0.016f, deepShade, 10, 12);
            if (faceId is FaceId.StrangeMan or FaceId.OldMan or FaceId.YellowHorror)
                Ellipsoid(ex, ey - 0.016f, ez + 0.002f, 0.024f, 0.010f, 0.014f, Col(68, 52, 58, 170), 8, 10);

            float eyeScaleY = isHorror ? 1.28f : (faceId == FaceId.OldMan ? 0.82f : 0.95f);
            Ellipsoid(ex, ey, ez + 0.002f, 0.019f, 0.014f * eyeScaleY, 0.014f, scleraCol, 10, 12);

            float gx = Math.Clamp(eyeGaze.X, -1f, 1f) * 0.0065f;
            float gy = Math.Clamp(eyeGaze.Y, -1f, 1f) * 0.0045f;
            float irisR = isHorror ? 0.0048f : 0.0092f;
            Ellipsoid(ex + gx, ey + gy, ez + 0.0135f, irisR, irisR, 0.005f, irisCol, 8, 10);
            if (!isHorror)
                Ellipsoid(ex + gx, ey + gy, ez + 0.0165f, 0.0046f, 0.0046f, 0.003f, Col(12, 12, 15), 8, 8);

            if (!isHorror)
            {
                float lidDrop = faceId == FaceId.OldMan ? 0.005f : 0.0075f;
                Ellipsoid(ex, ey + lidDrop, ez + 0.004f, 0.021f, 0.008f, 0.015f, shadeSkin, 8, 10);
                if (fem)
                    AngledBox(ex + sd * 0.003f, ey + 0.006f, ez + 0.014f, 0.036f, 0.0035f, 0.005f, 0f, -sd * 14f, -sd * 6f, Col(22, 20, 24));
            }

            float browY = ey + (isHorror ? 0.028f : 0.022f);
            float browTilt = faceId switch
            {
                FaceId.StrangeMan => sd * 14f,
                FaceId.Manager => sd * 10f,
                FaceId.YellowHorror => -sd * 18f,
                FaceId.OldMan => -sd * 6f,
                _ when fem => -sd * 5f,
                _ => sd * 3f
            };
            float browThick = faceId == FaceId.Friend1 ? 0.0075f : (fem ? 0.0045f : 0.0062f);
            AngledBox(ex, browY, ez + 0.012f, 0.038f, browThick, 0.009f, 0f, -sd * 12f, browTilt, hairCol);
        }

        Color lipCol = fem ? Col(188, 96, 98) : LerpCol(baseSkin, Col(145, 75, 72), 0.45f);
        float mouthY = 0.098f;
        float mouthZ = 0.118f;

        if (isHorror)
        {
            Ellipsoid(0f, 0.092f, 0.112f, 0.058f, 0.036f, 0.026f, Col(18, 8, 12), 12, 14);
            for (int t = -3; t <= 3; t++)
            {
                float tx = t * 0.012f;
                float curveY = MathF.Abs(t) * 0.0022f;
                Box(tx, 0.112f + curveY, 0.126f - MathF.Abs(t) * 0.003f, 0.008f, 0.012f, 0.006f, Col(238, 234, 220));
                Box(tx, 0.072f - curveY, 0.124f - MathF.Abs(t) * 0.003f, 0.008f, 0.011f, 0.006f, Col(238, 234, 220));
            }
        }
        else if (faceId == FaceId.Friend1)
        {
            Ellipsoid(0f, mouthY, mouthZ, 0.034f, 0.012f, 0.014f, Col(45, 22, 24), 10, 12);
            Box(0f, mouthY + 0.002f, mouthZ + 0.009f, 0.044f, 0.007f, 0.006f, Col(242, 240, 235));
            Ellipsoid(0f, mouthY + 0.008f, mouthZ + 0.003f, 0.034f, 0.005f, 0.012f, lipCol, 8, 10);
            Ellipsoid(0f, mouthY - 0.009f, mouthZ + 0.002f, 0.030f, 0.006f, 0.012f, lipCol, 8, 10);
        }
        else if (faceId == FaceId.Manager)
        {
            AngledBox(0f, mouthY + 0.001f, mouthZ + 0.004f, 0.046f, 0.005f, 0.012f, 0f, 0f, 7f, lipCol);
            Box(0f, mouthY + 0.015f, mouthZ + 0.008f, 0.042f, 0.006f, 0.008f, hairCol);
        }
        else if (faceId == FaceId.OldMan)
        {
            Ellipsoid(0f, mouthY + 0.015f, mouthZ + 0.007f, 0.032f, 0.007f, 0.010f, hairCol, 8, 10);
            Ellipsoid(0f, mouthY, mouthZ + 0.003f, 0.026f, 0.005f, 0.010f, lipCol, 8, 10);
            Box(0f, 0.228f, 0.112f, 0.075f, 0.0025f, 0.006f, shadeSkin);
            Box(0f, 0.238f, 0.108f, 0.065f, 0.0025f, 0.006f, shadeSkin);
        }
        else
        {
            float smileW = fem ? 0.027f : 0.030f;
            Ellipsoid(0f, mouthY + 0.003f, mouthZ + 0.004f, smileW, 0.0055f, 0.011f, lipCol, 8, 10);
            Ellipsoid(0f, mouthY - 0.004f, mouthZ + 0.003f, smileW * 0.90f, 0.0065f, 0.011f, lipCol, 8, 10);
            Box(0f, mouthY, mouthZ + 0.011f, smileW * 1.55f, 0.0022f, 0.004f, deepShade);
        }

        if (faceId == FaceId.Friend2)
        {
            Color frameCol = Col(24, 24, 28);
            foreach (int sd in new[] { -1, 1 })
            {
                float gx = sd * 0.045f;
                float gy = 0.184f;
                float gz = 0.128f;
                Box(gx, gy + 0.016f, gz, 0.048f, 0.004f, 0.005f, frameCol);
                Box(gx, gy - 0.015f, gz, 0.046f, 0.004f, 0.005f, frameCol);
                Box(gx - 0.023f, gy, gz, 0.004f, 0.032f, 0.005f, frameCol);
                Box(gx + 0.023f, gy, gz, 0.004f, 0.032f, 0.005f, frameCol);
                Box(sd * 0.118f, gy + 0.002f, 0.058f, 0.004f, 0.004f, 0.135f, frameCol);
            }
            Box(0f, 0.188f, 0.129f, 0.020f, 0.004f, 0.005f, frameCol);
        }

        DrawSculptedHair(fem, hairCol, faceId);
    }

    static void DrawSculptedHair(bool fem, Color hairCol, FaceId faceId)
    {
        if (faceId == FaceId.OldMan)
        {
            Ellipsoid(0f, 0.215f, -0.045f, 0.124f, 0.115f, 0.105f, hairCol, 12, 14);
            foreach (int sd in new[] { -1, 1 })
                Box(sd * 0.118f, 0.195f, -0.015f, 0.018f, 0.075f, 0.085f, hairCol);
            return;
        }

        Ellipsoid(0f, 0.228f, -0.018f, 0.128f, 0.118f, 0.134f, hairCol, 14, 16);
        Box(0f, 0.155f, -0.112f, 0.21f, 0.15f, 0.055f, hairCol);

        if (fem)
        {
            AngledBox(-0.035f, 0.242f, 0.098f, 0.12f, 0.042f, 0.045f, 18f, 12f, -14f, hairCol);
            AngledBox(0.052f, 0.238f, 0.092f, 0.09f, 0.040f, 0.045f, 18f, -16f, 16f, hairCol);

            foreach (int sd in new[] { -1, 1 })
                Box(sd * 0.116f, 0.075f, -0.025f, 0.036f, 0.25f, 0.11f, hairCol);
            Box(0f, 0.065f, -0.108f, 0.23f, 0.26f, 0.055f, hairCol);
        }
        else if (faceId == FaceId.Manager)
        {
            AngledBox(0f, 0.268f, 0.035f, 0.19f, 0.042f, 0.15f, -12f, 0f, 0f, hairCol);
            foreach (int sd in new[] { -1, 1 })
                Box(sd * 0.118f, 0.195f, -0.010f, 0.015f, 0.075f, 0.075f, hairCol);
        }
        else if (faceId == FaceId.Friend1)
        {
            for (int i = -2; i <= 2; i++)
                AngledBox(i * 0.036f, 0.278f, 0.065f - MathF.Abs(i) * 0.01f, 0.034f, 0.052f, 0.045f, 24f, i * 6f, i * 5f, hairCol);
            foreach (int sd in new[] { -1, 1 })
                Box(sd * 0.118f, 0.195f, -0.008f, 0.015f, 0.072f, 0.072f, hairCol);
        }
        else
        {
            AngledBox(-0.028f, 0.246f, 0.096f, 0.115f, 0.044f, 0.048f, 20f, 10f, -10f, hairCol);
            AngledBox(0.042f, 0.244f, 0.094f, 0.105f, 0.042f, 0.048f, 20f, -12f, 12f, hairCol);
            foreach (int sd in new[] { -1, 1 })
                Box(sd * 0.118f, 0.192f, -0.008f, 0.016f, 0.078f, 0.078f, hairCol);
        }
    }
}