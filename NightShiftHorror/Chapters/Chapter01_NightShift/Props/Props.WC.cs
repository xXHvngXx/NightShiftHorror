using System.Numerics;
using Raylib_cs;

partial class Props
{
    static RenderTexture2D wcMirrorTarget;
    static bool wcMirrorInitialized = false;

    static void EnsureWcMirrorTarget()
    {
        if (wcMirrorInitialized) return;
        wcMirrorTarget = Raylib.LoadRenderTexture(1024, 512);
        Raylib.SetTextureFilter(wcMirrorTarget.Texture, TextureFilter.Bilinear);
        wcMirrorInitialized = true;
    }

    public static void UnloadWcMirrorTarget()
    {
        if (!wcMirrorInitialized) return;
        Raylib.UnloadRenderTexture(wcMirrorTarget);
        wcMirrorInitialized = false;
    }

    static void DrawRotatedBox(Vector3 hingePos, Vector3 localOffset, float yawDeg, float w, float h, float l, Color color)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(hingePos.X, hingePos.Y, hingePos.Z);
        Rlgl.Rotatef(yawDeg, 0f, 1f, 0f);
        Raylib.DrawCube(localOffset, w, h, l, color);
        Rlgl.PopMatrix();
    }

    public static void UpdateWashroomMirror(Camera3D mainCam, Vector3 playerPos, float camYaw, bool lightsOn, float time,
                                            bool wcDoorOpen, bool[] stallDoorsOpen, int activeSinkIndex, float washTimer,
                                            bool isPeeing, int activePeeStall, float peeTimer)
    {
        if (playerPos.X > -6.0f || playerPos.Z > 0.5f) return;

        EnsureWcMirrorTarget();

        bool roomLit = lightsOn && Program.wcLightOn;

        const float mirrorCenterX = -13.15f;
        const float mirrorCenterY = 1.62f;
        const float mirrorHalfW = 1.94f;
        const float mirrorHalfH = 0.65f;
        const float mirrorZ = -6.885f;

        const float mirrorMinX = mirrorCenterX - mirrorHalfW;
        const float mirrorMaxX = mirrorCenterX + mirrorHalfW;
        const float mirrorMinY = mirrorCenterY - mirrorHalfH;
        const float mirrorMaxY = mirrorCenterY + mirrorHalfH;

        Vector3 eyePos = mainCam.Position;
        float clampedEyeZ = Math.Max(eyePos.Z, mirrorZ + 0.14f);
        Vector3 virtualCamPos = new(eyePos.X, eyePos.Y, 2f * mirrorZ - clampedEyeZ);

        Camera3D mirrorCam = new()
        {
            Position = virtualCamPos,
            Target = virtualCamPos + Vector3.UnitZ,
            Up = Vector3.UnitY,
            FovY = 60.0f,
            Projection = CameraProjection.Perspective
        };

        Raylib.BeginTextureMode(wcMirrorTarget);
        Raylib.ClearBackground(roomLit ? new Color(188, 162, 118, 255) : new Color(4, 4, 6, 255));
        Raylib.BeginMode3D(mirrorCam);

        float distToMirror = Math.Max(0.14f, clampedEyeZ - mirrorZ);
        const float nearPlane = 0.05f;
        float scale = nearPlane / distToMirror;

        float left = -(mirrorMaxX - virtualCamPos.X) * scale;
        float right = -(mirrorMinX - virtualCamPos.X) * scale;
        float bottom = (mirrorMinY - virtualCamPos.Y) * scale;
        float top = (mirrorMaxY - virtualCamPos.Y) * scale;

        Rlgl.MatrixMode(MatrixMode.Projection);
        Rlgl.LoadIdentity();
        Rlgl.Frustum(left, right, bottom, top, nearPlane, 25.0f);
        Rlgl.MatrixMode(MatrixMode.ModelView);

        DrawWashroomInternal(lightsOn, time, wcDoorOpen, stallDoorsOpen, activeSinkIndex, washTimer,
                             isPeeing, activePeeStall, peeTimer, drawMirrorSurface: false, isMirrorPass: true);

        float bodyYawDeg = camYaw * 180f / MathF.PI;
        DrawFearsCharacter(
            new Vector3(playerPos.X, 0f, playerPos.Z),
            bodyYawDeg,
            roomLit ? new Color(42, 48, 58, 255) : new Color(6, 7, 9, 255),
            roomLit ? new Color(28, 28, 32, 255) : new Color(4, 4, 5, 255),
            FearsBodySpec.AverageMan,
            0f, 0f
        );

        Raylib.EndMode3D();
        Raylib.EndTextureMode();
    }

    public static void DrawWashroom(bool lightsOn, float time, bool wcDoorOpen, bool[] stallDoorsOpen,
                                    int activeSinkIndex, float washTimer, bool isPeeing, int activePeeStall, float peeTimer)
    {
        DrawWashroomInternal(lightsOn, time, wcDoorOpen, stallDoorsOpen, activeSinkIndex, washTimer,
                             isPeeing, activePeeStall, peeTimer, drawMirrorSurface: true, isMirrorPass: false);
    }

    static void DrawWashroomInternal(bool lightsOn, float time, bool wcDoorOpen, bool[] stallDoorsOpen,
                                     int activeSinkIndex, float washTimer, bool isPeeing, int activePeeStall, float peeTimer,
                                     bool drawMirrorSurface, bool isMirrorPass)
    {
        bool roomLit = lightsOn && Program.wcLightOn;

        const float minX = -15.50f;
        const float wallStopX = -10.58f;
        const float minZ = -6.95f;
        const float maxZ = -1.45f;
        const float roomH = 3.15f;
        float midX = (minX + wallStopX) * 0.5f;
        float midZ = (minZ + maxZ) * 0.5f;
        float sizeX = wallStopX - minX;
        float sizeZ = maxZ - minZ;

        Color tileFloor = roomLit ? new Color(224, 227, 232, 255) : new Color(5, 6, 8, 255);
        Color groutDark = roomLit ? new Color(135, 140, 148, 255) : new Color(2, 2, 3, 255);
        Color purpleWall = roomLit ? new Color(122, 102, 152, 255) : new Color(4, 3, 7, 255);
        Color mirrorWallBeige = roomLit ? new Color(188, 162, 118, 255) : new Color(6, 5, 4, 255);
        Color groutWhite = roomLit ? new Color(205, 198, 218, 255) : new Color(8, 7, 11, 255);
        Color ceilingCol = roomLit ? new Color(235, 238, 242, 255) : new Color(5, 6, 8, 255);

        Raylib.DrawCube(new Vector3(midX, -0.02f, midZ), sizeX, 0.04f, sizeZ, tileFloor);
        for (float gx = minX; gx <= wallStopX; gx += 0.45f)
            Raylib.DrawLine3D(new Vector3(gx, 0.002f, minZ), new Vector3(gx, 0.002f, maxZ), groutDark);
        for (float gz = minZ; gz <= maxZ; gz += 0.45f)
            Raylib.DrawLine3D(new Vector3(minX, 0.002f, gz), new Vector3(wallStopX, 0.002f, gz), groutDark);

        Raylib.DrawCube(new Vector3(midX, roomH + 0.04f, midZ), sizeX, 0.08f, sizeZ, ceilingCol);
        float[] ledX = [-14.0f, -11.6f];
        float[] ledZ = [-5.4f, -3.1f];
        foreach (float lx in ledX)
        {
            foreach (float lz in ledZ)
            {
                Raylib.DrawCylinder(new Vector3(lx, roomH - 0.01f, lz), 0.16f, 0.16f, 0.02f, 16,
                    roomLit ? new Color(190, 195, 200, 255) : new Color(10, 11, 13, 255));
                Raylib.DrawCylinder(new Vector3(lx, roomH - 0.015f, lz), 0.12f, 0.12f, 0.02f, 16,
                    roomLit ? new Color(255, 252, 240, 255) : new Color(6, 7, 9, 255));
            }
        }

        Raylib.DrawCube(new Vector3(minX - 0.05f, roomH * 0.5f, midZ), 0.10f, roomH, sizeZ + 0.1f, purpleWall);
        if (!isMirrorPass)
        {
            Raylib.DrawCube(new Vector3(midX, roomH * 0.5f, minZ - 0.05f), sizeX + 0.06f, roomH, 0.10f, purpleWall);
        }
        Raylib.DrawCube(new Vector3(midX, roomH * 0.5f, maxZ + 0.05f), sizeX + 0.06f, roomH, 0.10f, purpleWall);

        Raylib.DrawCube(new Vector3(-10.608f, roomH * 0.5f, -6.755f), 0.012f, roomH, 0.39f, mirrorWallBeige);
        Raylib.DrawCube(new Vector3(-10.608f, roomH * 0.5f, -3.445f), 0.012f, roomH, 3.99f, mirrorWallBeige);
        Raylib.DrawCube(new Vector3(-10.608f, 2.715f, -6.00f), 0.012f, 0.87f, 1.12f, mirrorWallBeige);

        for (float ty = 0.30f; ty < roomH; ty += 0.30f)
        {
            if (!isMirrorPass)
            {
                Raylib.DrawLine3D(new Vector3(minX, ty, minZ + 0.005f), new Vector3(wallStopX, ty, minZ + 0.005f), groutWhite);
            }
            Raylib.DrawLine3D(new Vector3(minX + 0.005f, ty, minZ), new Vector3(minX + 0.005f, ty, maxZ), groutWhite);
            Raylib.DrawLine3D(new Vector3(minX, ty, maxZ - 0.005f), new Vector3(wallStopX, ty, maxZ - 0.005f), groutWhite);
        }
        for (float tx = minX + 0.35f; tx < wallStopX; tx += 0.35f)
        {
            if (!isMirrorPass)
            {
                Raylib.DrawLine3D(new Vector3(tx, 0f, minZ + 0.005f), new Vector3(tx, roomH, minZ + 0.005f), groutWhite);
            }
            Raylib.DrawLine3D(new Vector3(tx, 0f, maxZ - 0.005f), new Vector3(tx, roomH, maxZ - 0.005f), groutWhite);
        }

        Color frameMetal = roomLit ? new Color(185, 190, 198, 255) : new Color(10, 11, 13, 255);
        Raylib.DrawCube(new Vector3(-10.50f, 1.125f, -6.54f), 0.24f, 2.25f, 0.05f, frameMetal);
        Raylib.DrawCube(new Vector3(-10.50f, 1.125f, -5.46f), 0.24f, 2.25f, 0.05f, frameMetal);
        Raylib.DrawCube(new Vector3(-10.50f, 2.26f, -6.00f), 0.24f, 0.05f, 1.14f, frameMetal);

        Raylib.DrawCube(new Vector3(-10.385f, 2.52f, -6.00f), 0.03f, 0.24f, 0.56f, new Color(32, 42, 68, 255));
        Raylib.DrawCubeWires(new Vector3(-10.385f, 2.52f, -6.00f), 0.035f, 0.25f, 0.57f, Color.White);

        Color doorCol = roomLit ? new Color(168, 124, 78, 255) : new Color(12, 8, 5, 255);
        float doorAngle = wcDoorOpen ? 84f : 0f;
        DrawRotatedBox(
            new Vector3(-10.52f, 0f, -5.49f),
            new Vector3(0f, 1.11f, -0.51f),
            doorAngle,
            0.055f, 2.20f, 1.02f,
            doorCol
        );
        DrawRotatedBox(
            new Vector3(-10.52f, 0f, -5.49f),
            new Vector3(-0.045f, 1.05f, -0.90f),
            doorAngle,
            0.035f, 0.03f, 0.11f,
            frameMetal
        );
        DrawRotatedBox(
            new Vector3(-10.52f, 0f, -5.49f),
            new Vector3(0.045f, 1.05f, -0.90f),
            doorAngle,
            0.035f, 0.03f, 0.11f,
            frameMetal
        );

        Color counterWhite = roomLit ? new Color(238, 240, 244, 255) : new Color(10, 11, 13, 255);
        Color cabinetGrey = roomLit ? new Color(210, 214, 220, 255) : new Color(8, 9, 11, 255);
        Raylib.DrawCube(new Vector3(-13.15f, 0.39f, -6.62f), 3.90f, 0.78f, 0.58f, cabinetGrey);
        Raylib.DrawCube(new Vector3(-13.15f, 0.80f, -6.60f), 3.96f, 0.06f, 0.64f, counterWhite);

        Raylib.DrawCubeWires(new Vector3(-13.15f, 1.62f, -6.88f), 3.92f, 1.34f, 0.04f, frameMetal);
        if (drawMirrorSurface)
        {
            if (wcMirrorInitialized)
            {
                DrawMirrorQuad(-13.15f, 1.62f, -6.885f, 3.88f, 1.30f, wcMirrorTarget.Texture,
                    roomLit ? Color.White : new Color(18, 20, 25, 255));
            }
            else
            {
                Color mirrorGlass = roomLit ? new Color(188, 162, 118, 235) : new Color(5, 5, 7, 235);
                Raylib.DrawCube(new Vector3(-13.15f, 1.62f, -6.89f), 3.88f, 1.30f, 0.02f, mirrorGlass);
            }
        }

        float[] sinkXs = [-11.85f, -13.15f, -14.45f];
        for (int i = 0; i < sinkXs.Length; i++)
        {
            float sx = sinkXs[i];
            Raylib.DrawCylinder(new Vector3(sx, 0.825f, -6.58f), 0.25f, 0.21f, 0.03f, 18, counterWhite);
            Raylib.DrawCylinder(new Vector3(sx, 0.832f, -6.58f), 0.20f, 0.17f, 0.025f, 18,
                roomLit ? new Color(195, 202, 210, 255) : new Color(8, 9, 11, 255));

            Raylib.DrawCylinder(new Vector3(sx, 0.84f, -6.78f), 0.022f, 0.026f, 0.16f, 10, frameMetal);
            Raylib.DrawLine3D(new Vector3(sx, 0.99f, -6.78f), new Vector3(sx, 1.03f, -6.66f), frameMetal);
            Raylib.DrawLine3D(new Vector3(sx, 1.03f, -6.66f), new Vector3(sx, 0.97f, -6.60f), frameMetal);

            if (i < 2)
            {
                float soapX = sx - 0.65f;
                Raylib.DrawCube(new Vector3(soapX, 1.24f, -6.86f), 0.13f, 0.22f, 0.10f, counterWhite);
                Raylib.DrawCube(new Vector3(soapX, 1.24f, -6.805f), 0.04f, 0.20f, 0.02f, frameMetal);
            }

            if (activeSinkIndex == i && washTimer > 0f)
            {
                float jitter = MathF.Sin(time * 32f) * 0.004f;
                Raylib.DrawCylinder(new Vector3(sx + jitter, 0.84f, -6.60f), 0.010f, 0.014f, 0.13f, 8, new Color(175, 225, 255, 210));
                for (int d = 0; d < 5; d++)
                {
                    float a = time * 14f + d * 1.25f;
                    float r = 0.04f + (d % 3) * 0.025f;
                    Raylib.DrawSphere(
                        new Vector3(sx + MathF.Cos(a) * r, 0.85f + MathF.Abs(MathF.Sin(a)) * 0.03f, -6.58f + MathF.Sin(a) * r),
                        0.008f,
                        new Color(210, 240, 255, 220)
                    );
                }
            }
        }

        Color woodPanel = roomLit ? new Color(174, 126, 76, 255) : new Color(10, 7, 4, 255);
        float[] stallStartX = [-12.50f, -13.95f, -15.40f];
        const float stallW = 1.45f;
        const float stallBackZ = -1.52f;
        const float stallFrontZ = -3.05f;
        float stallDepth = stallFrontZ - stallBackZ;
        float stallMidZ = (stallBackZ + stallFrontZ) * 0.5f;

        Raylib.DrawCube(new Vector3(-13.225f, 2.06f, stallFrontZ), 4.35f, 0.05f, 0.05f, frameMetal);

        for (int s = 0; s < 3; s++)
        {
            float x0 = stallStartX[s];
            float xMid = x0 + stallW * 0.5f;

            Raylib.DrawCube(new Vector3(x0 + stallW, 1.10f, stallMidZ), 0.04f, 1.90f, MathF.Abs(stallDepth), woodPanel);
            Raylib.DrawCube(new Vector3(x0 + stallW, 1.03f, stallFrontZ), 0.06f, 2.06f, 0.06f, frameMetal);
            Raylib.DrawCube(new Vector3(x0 + stallW - 0.18f, 1.10f, stallFrontZ), 0.32f, 1.90f, 0.04f, woodPanel);

            float sAng = stallDoorsOpen[s] ? 82f : 0f;
            DrawRotatedBox(
                new Vector3(x0 + stallW - 0.34f, 0f, stallFrontZ),
                new Vector3(-0.50f, 1.10f, 0f),
                sAng,
                1.00f, 1.88f, 0.04f,
                woodPanel
            );

            float toiletX = xMid - 0.05f;
            float toiletZ = -1.88f;
            Color porcelain = roomLit ? new Color(242, 244, 248, 255) : new Color(11, 12, 14, 255);
            Raylib.DrawCylinder(new Vector3(toiletX, 0.01f, toiletZ), 0.16f, 0.21f, 0.38f, 16, porcelain);
            Raylib.DrawCylinder(new Vector3(toiletX, 0.36f, toiletZ - 0.04f), 0.22f, 0.19f, 0.08f, 18, porcelain);
            Raylib.DrawCylinder(new Vector3(toiletX, 0.395f, toiletZ - 0.04f), 0.16f, 0.14f, 0.02f, 16, new Color(6, 7, 9, 255));
            Raylib.DrawCylinder(new Vector3(toiletX, 0.398f, toiletZ - 0.04f), 0.11f, 0.11f, 0.015f, 14,
                roomLit ? new Color(145, 185, 205, 210) : new Color(8, 12, 15, 210));
            Raylib.DrawCube(new Vector3(toiletX, 0.62f, -1.65f), 0.38f, 0.44f, 0.18f, porcelain);
            Raylib.DrawCube(new Vector3(toiletX, 0.60f, -1.75f), 0.32f, 0.38f, 0.03f, cabinetGrey);

            Raylib.DrawCylinder(new Vector3(x0 + stallW - 0.07f, 0.68f, -2.20f), 0.065f, 0.065f, 0.11f, 12, counterWhite);
            Raylib.DrawCylinder(new Vector3(x0 + stallW - 0.22f, 0.01f, -1.72f), 0.11f, 0.12f, 0.28f, 12, frameMetal);

            if (isPeeing && s == activePeeStall && peeTimer > 0.35f && peeTimer < 3.4f)
            {
                Vector3 startStream = new(toiletX, 0.92f, toiletZ - 0.34f);
                Vector3 endStream = new(toiletX + MathF.Sin(time * 9f) * 0.015f, 0.405f, toiletZ - 0.04f);
                Raylib.DrawLine3D(startStream, endStream, new Color(245, 230, 130, 185));
                Raylib.DrawSphere(endStream, 0.025f + MathF.Sin(time * 22f) * 0.008f, new Color(255, 250, 210, 190));
            }
        }

        Color switchPlate = roomLit ? new Color(234, 237, 242, 255) : new Color(55, 58, 64, 255);
        Color rockerCol = roomLit ? new Color(246, 248, 250, 255) : new Color(75, 78, 86, 255);
        Raylib.DrawCube(new Vector3(-10.626f, 1.32f, -5.24f), 0.026f, 0.15f, 0.12f, switchPlate);
        Raylib.DrawCubeWires(new Vector3(-10.626f, 1.32f, -5.24f), 0.028f, 0.152f, 0.122f, new Color(24, 26, 30, 255));
        Rlgl.PushMatrix();
        Rlgl.Translatef(-10.642f, 1.32f, -5.24f);
        Rlgl.Rotatef(Program.wcLightOn ? -14f : 14f, 0f, 0f, 1f);
        Raylib.DrawCube(Vector3.Zero, 0.020f, 0.07f, 0.05f, rockerCol);
        Rlgl.PopMatrix();
        Color ledIndicator = Program.wcLightOn ? new Color(55, 235, 110, 255) : new Color(255, 140, 35, 255);
        Raylib.DrawCube(new Vector3(-10.641f, 1.37f, -5.24f), 0.010f, 0.014f, 0.018f, ledIndicator);
    }

    static void DrawMirrorQuad(float cx, float cy, float z, float w, float h, Texture2D tex, Color tint)
    {
        float hw = w * 0.5f;
        float hh = h * 0.5f;
        Rlgl.SetTexture(tex.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(tint.R, tint.G, tint.B, tint.A);
        Rlgl.Normal3f(0f, 0f, 1f);

        Rlgl.TexCoord2f(1f, 0f); Rlgl.Vertex3f(cx - hw, cy - hh, z);
        Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(cx + hw, cy - hh, z);
        Rlgl.TexCoord2f(0f, 1f); Rlgl.Vertex3f(cx + hw, cy + hh, z);
        Rlgl.TexCoord2f(1f, 1f); Rlgl.Vertex3f(cx - hw, cy + hh, z);

        Rlgl.End();
        Rlgl.SetTexture(0);
    }

    public static void DrawFirstPersonWashHands(Camera3D cam, float washTimer)
    {
        if (washTimer <= 0f) return;

        Vector3 forward = Vector3.Normalize(cam.Target - cam.Position);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);

        float rub = MathF.Sin(washTimer * 11f) * 0.032f;
        float bob = MathF.Cos(washTimer * 8f) * 0.012f;
        Color skin = new(218, 178, 146, 255);
        Color sleeve = new(42, 48, 58, 255);

        Vector3 leftHand = cam.Position + forward * (0.46f + rub) - right * 0.075f - up * (0.20f + bob);
        Vector3 rightHand = cam.Position + forward * (0.46f - rub) + right * 0.075f - up * (0.20f - bob);

        Raylib.DrawLine3D(leftHand - forward * 0.22f - right * 0.08f - up * 0.12f, leftHand, sleeve);
        Raylib.DrawLine3D(rightHand - forward * 0.22f + right * 0.08f - up * 0.12f, rightHand, sleeve);
        Raylib.DrawSphere(leftHand, 0.048f, skin);
        Raylib.DrawSphere(rightHand, 0.048f, skin);
    }
}

partial class Program
{
    static bool wcDoorOpen = false;
    public static bool wcLightOn = true;
    static readonly bool[] wcStallDoorsOpen = [true, false, false];
    static int activeSinkIndex = -1;
    static float washHandsTimer = 0f;

    static bool isPeeing = false;
    static int activePeeStall = 0;
    static float peeTimer = 0f;
    static float savedPitchBeforePee = 0f;

    static readonly BoundingBox wcLightSwitchBox = new(new Vector3(-10.84f, 1.10f, -5.42f), new Vector3(-10.52f, 1.54f, -5.06f));
    static readonly BoundingBox[] wcSinkBoxes =
    [
        new BoundingBox(new Vector3(-12.25f, 0.75f, -6.90f), new Vector3(-11.45f, 1.15f, -6.30f)),
        new BoundingBox(new Vector3(-13.55f, 0.75f, -6.90f), new Vector3(-12.75f, 1.15f, -6.30f)),
        new BoundingBox(new Vector3(-14.85f, 0.75f, -6.90f), new Vector3(-14.05f, 1.15f, -6.30f))
    ];
    static readonly BoundingBox[] wcStallDoorBoxes =
    [
        new BoundingBox(new Vector3(-12.46f, 0.15f, -3.14f), new Vector3(-11.42f, 2.05f, -2.96f)),
        new BoundingBox(new Vector3(-13.91f, 0.15f, -3.14f), new Vector3(-12.87f, 2.05f, -2.96f)),
        new BoundingBox(new Vector3(-15.36f, 0.15f, -3.14f), new Vector3(-14.32f, 2.05f, -2.96f))
    ];
    static readonly BoundingBox[] wcToiletBoxes =
    [
        new BoundingBox(new Vector3(-12.15f, 0.05f, -2.18f), new Vector3(-11.55f, 0.85f, -1.62f)),
        new BoundingBox(new Vector3(-13.60f, 0.05f, -2.18f), new Vector3(-13.00f, 0.85f, -1.62f)),
        new BoundingBox(new Vector3(-15.05f, 0.05f, -2.18f), new Vector3(-14.45f, 0.85f, -1.62f))
    ];

    static BoundingBox GetCurrentWcDoorBox()
    {
        if (wcDoorOpen)
            return new BoundingBox(new Vector3(-11.54f, 0f, -5.62f), new Vector3(-10.50f, 2.25f, -5.40f));
        return new BoundingBox(new Vector3(-10.65f, 0f, -6.55f), new Vector3(-10.28f, 2.25f, -5.45f));
    }

    static bool IsWcBusy => washHandsTimer > 0f || isPeeing;

    static bool IsRayBlockedByWall(Ray eyeRay, float targetDist)
    {
        BoundingBox[] wallOccluders =
        [
            new BoundingBox(new Vector3(-15.55f, 0f, -7.00f), new Vector3(-10.45f, 3.2f, -6.90f)),
            new BoundingBox(new Vector3(-15.55f, 0f, -1.50f), new Vector3(-10.45f, 3.2f, -1.40f)),
            new BoundingBox(new Vector3(-15.55f, 0f, -7.00f), new Vector3(-15.45f, 3.2f, -1.40f)),
            new BoundingBox(new Vector3(-10.60f, 0f, -7.00f), new Vector3(-10.40f, 3.2f, -6.55f)),
            new BoundingBox(new Vector3(-10.60f, 0f, -5.45f), new Vector3(-10.40f, 3.2f, -1.40f)),
            new BoundingBox(new Vector3(-11.08f, 0f, -3.08f), new Vector3(-11.02f, 2.05f, -1.50f)),
            new BoundingBox(new Vector3(-12.53f, 0f, -3.08f), new Vector3(-12.47f, 2.05f, -1.50f)),
            new BoundingBox(new Vector3(-13.98f, 0f, -3.08f), new Vector3(-13.92f, 2.05f, -1.50f))
        ];

        foreach (var wb in wallOccluders)
        {
            RayCollision hit = Raylib.GetRayCollisionBox(eyeRay, wb);
            if (hit.Hit && hit.Distance < targetDist - 0.05f)
            {
                return true;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            if (!wcStallDoorsOpen[i])
            {
                RayCollision doorHit = Raylib.GetRayCollisionBox(eyeRay, wcStallDoorBoxes[i]);
                if (doorHit.Hit && doorHit.Distance < targetDist - 0.05f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    static void ApplyWcCollisionAndBounds()
    {
        if (playerPos.X < -9.8f && playerPos.Z >= -6.65f && playerPos.Z <= -1.65f)
        {
            playerPos.X = Math.Clamp(playerPos.X, -15.05f, 9.8f);
            playerPos.Z = Math.Clamp(playerPos.Z, -6.35f, -1.75f);

            if (Math.Abs(playerPos.X - (-10.48f)) < 0.42f && (playerPos.Z < -6.45f || playerPos.Z > -5.55f))
            {
                playerPos.X = playerPos.X > -10.48f ? -10.04f : -10.90f;
            }
            if (!wcDoorOpen && Math.Abs(playerPos.X - (-10.48f)) < 0.45f)
            {
                playerPos.X = playerPos.X > -10.48f ? -10.02f : -10.94f;
            }
            if (playerPos.Z < -5.85f && playerPos.X < -10.95f)
            {
                playerPos.Z = -5.85f;
            }

            if (playerPos.X < -10.95f)
            {
                int sIdx = Math.Clamp((int)((-11.05f - playerPos.X) / 1.45f), 0, 2);
                float stallLeftX = -12.50f - sIdx * 1.45f;
                float stallRightX = stallLeftX + 1.45f;

                if (Math.Abs(playerPos.X - stallRightX) < 0.34f && playerPos.Z > -3.05f)
                {
                    playerPos.X = playerPos.X > stallRightX ? stallRightX + 0.35f : stallRightX - 0.35f;
                }

                if (!wcStallDoorsOpen[sIdx] && Math.Abs(playerPos.Z - (-3.05f)) < 0.38f)
                {
                    if (playerPos.Z <= -2.95f)
                        playerPos.Z = -3.45f;
                    else
                        playerPos.Z = -2.65f;
                }
            }
        }
    }

    static bool TryDetectWcInteraction(Ray eyeRay)
    {
        RayCollision swHit = Raylib.GetRayCollisionBox(eyeRay, wcLightSwitchBox);
        if (swHit.Hit && swHit.Distance <= 2.5f && !IsRayBlockedByWall(eyeRay, swHit.Distance))
        {
            activeInteractId = "wc_light_switch";
            skullPromptKey = "F";
            skullPromptText = wcLightOn ? "TẮT ĐÈN NHÀ VỆ SINH" : "BẬT ĐÈN NHÀ VỆ SINH";
            return true;
        }

        RayCollision mainDoor = Raylib.GetRayCollisionBox(eyeRay, GetCurrentWcDoorBox());
        if (mainDoor.Hit && mainDoor.Distance <= 2.6f && !IsRayBlockedByWall(eyeRay, mainDoor.Distance))
        {
            activeInteractId = "wc_door";
            skullPromptKey = "F";
            skullPromptText = wcDoorOpen ? "ĐÓNG CỬA NHÀ VỆ SINH" : "MỞ CỬA NHÀ VỆ SINH";
            return true;
        }

        for (int i = 0; i < 3; i++)
        {
            RayCollision sinkHit = Raylib.GetRayCollisionBox(eyeRay, wcSinkBoxes[i]);
            if (sinkHit.Hit && sinkHit.Distance <= 2.2f && !IsRayBlockedByWall(eyeRay, sinkHit.Distance))
            {
                activeInteractId = $"wc_sink_{i}";
                skullPromptKey = "F";
                skullPromptText = "MỞ VÒI RỬA TAY";
                return true;
            }

            RayCollision stallHit = Raylib.GetRayCollisionBox(eyeRay, wcStallDoorBoxes[i]);
            if (stallHit.Hit && stallHit.Distance <= 2.2f && !IsRayBlockedByWall(eyeRay, stallHit.Distance))
            {
                activeInteractId = $"wc_stall_{i}";
                skullPromptKey = "F";
                skullPromptText = wcStallDoorsOpen[i] ? "ĐÓNG CỬA BUỒNG" : "MỞ CỬA BUỒNG";
                return true;
            }

            float stallMinX = -12.50f - i * 1.45f;
            float stallMaxX = stallMinX + 1.45f;
            bool insideThisStall = playerPos.Z > -3.05f && playerPos.X >= stallMinX && playerPos.X <= stallMaxX;

            RayCollision toiletHit = Raylib.GetRayCollisionBox(eyeRay, wcToiletBoxes[i]);
            if (insideThisStall && !wcStallDoorsOpen[i] && toiletHit.Hit && toiletHit.Distance <= 2.0f && !IsRayBlockedByWall(eyeRay, toiletHit.Distance))
            {
                activeInteractId = $"wc_toilet_{i}";
                skullPromptKey = "F";
                skullPromptText = "ĐI VỆ SINH";
                return true;
            }
        }
        return false;
    }

    static bool TryExecuteWcInteraction()
    {
        if (activeInteractId == "wc_door")
        {
            wcDoorOpen = !wcDoorOpen;
            Raylib.SetSoundPitch(footstepSound, 0.75f);
            Raylib.PlaySound(footstepSound);
            return true;
        }
        if (activeInteractId == "wc_light_switch")
        {
            wcLightOn = !wcLightOn;
            Raylib.SetSoundPitch(voiceStaticSound, wcLightOn ? 1.45f : 1.15f);
            Raylib.PlaySound(voiceStaticSound);
            return true;
        }
        if (activeInteractId.StartsWith("wc_stall_"))
        {
            int idx = activeInteractId[^1] - '0';
            wcStallDoorsOpen[idx] = !wcStallDoorsOpen[idx];
            Raylib.SetSoundPitch(footstepSound, 0.85f);
            Raylib.PlaySound(footstepSound);
            return true;
        }
        if (activeInteractId.StartsWith("wc_sink_"))
        {
            activeSinkIndex = activeInteractId[^1] - '0';
            washHandsTimer = 3.5f;
            float sinkX = -11.85f - activeSinkIndex * 1.30f;
            playerPos = new Vector3(sinkX, 0f, -5.82f);
            camYaw = MathF.PI;
            camPitch = -0.18f;
            return true;
        }
        if (activeInteractId.StartsWith("wc_toilet_"))
        {
            activePeeStall = activeInteractId[^1] - '0';
            isPeeing = true;
            peeTimer = 0f;
            savedPitchBeforePee = camPitch;
            float toiletX = -11.775f - activePeeStall * 1.45f;
            playerPos = new Vector3(toiletX, 0f, -2.30f);
            camYaw = 0f;
            return true;
        }
        return false;
    }

    static void UpdateWcActions(float dt)
    {
        if (washHandsTimer > 0f)
        {
            washHandsTimer -= dt;
            if ((int)(washHandsTimer * 10f) % 3 == 0)
            {
                Raylib.SetSoundPitch(voiceStaticSound, 1.85f);
                Raylib.PlaySound(voiceStaticSound);
            }
            ApplyCameraOrientation(new Vector3(playerPos.X, 1.7f, playerPos.Z));
            if (washHandsTimer <= 0f)
            {
                washHandsTimer = 0f;
                activeSinkIndex = -1;
                Raylib.GetMouseDelta();
            }
        }

        if (isPeeing)
        {
            peeTimer += dt;
            camPitch += (-0.82f - camPitch) * Math.Min(1f, dt * 6f);
            ApplyCameraOrientation(new Vector3(playerPos.X, 1.68f, playerPos.Z));

            if (peeTimer >= 4.2f)
            {
                isPeeing = false;
                peeTimer = 0f;
                camPitch = savedPitchBeforePee;
                Raylib.SetSoundPitch(voiceStaticSound, 0.65f);
                Raylib.PlaySound(voiceStaticSound);
                Raylib.GetMouseDelta();
            }
        }
    }

    static void DrawWcHudOverlay()
    {
        if (washHandsTimer > 0f)
        {
            string sub = "Rửa mặt mũi, tay chân cho tỉnh táo... Đêm nay còn dài.";
            float w = Raylib.MeasureTextEx(viFont, sub, 22, 1f).X;
            DrawViText(sub, (int)((ScreenWidth - w) / 2), ScreenHeight - 62, 22, new Color(245, 236, 185, 255));
        }
        else if (isPeeing)
        {
            string sub = peeTimer < 2.6f ? "Mình buồn tiểu nãy giờ..." : "Nhẹ cả người rồi.";
            float w = Raylib.MeasureTextEx(viFont, sub, 23, 1f).X;
            DrawViText(sub, (int)((ScreenWidth - w) / 2), ScreenHeight - 62, 23, new Color(245, 236, 175, 255));
        }
    }
}