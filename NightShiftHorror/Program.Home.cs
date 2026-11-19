using System.Numerics;
using Raylib_cs;

partial class Program
{
    static int homeSelection = 0;
    static float homeOrbitAngle = 0f;

    static bool DrawHomeScreen(float dt)
    {
        const int ButtonX = 94;
        const int ButtonY = 390;
        const int ButtonW = 340;
        const int ButtonH = 58;
        const int ButtonGap = 14;

        homeOrbitAngle += dt * 0.13f;
        camera.Position = new Vector3(
            MathF.Sin(homeOrbitAngle) * 17.5f,
            4.0f + MathF.Sin(homeOrbitAngle * 0.7f) * 0.45f,
            MathF.Cos(homeOrbitAngle) * 17.5f
        );
        camera.Target = new Vector3(0f, 2.0f, 0f);
        camera.Up = Vector3.UnitY;
        camera.FovY = 52f;

        Vector2 mousePosition = Raylib.GetMousePosition();
        for (int i = 0; i < 2; i++)
        {
            Rectangle bounds = new(ButtonX, ButtonY + i * (ButtonH + ButtonGap), ButtonW, ButtonH);
            if (Raylib.CheckCollisionPointRec(mousePosition, bounds))
            {
                homeSelection = i;
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    if (i == 0)
                    {
                        StartGameFromHome();
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S) ||
            Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            homeSelection = 1 - homeSelection;
            Raylib.PlaySound(voiceStaticSound);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            if (homeSelection == 0)
            {
                StartGameFromHome();
            }
            else
            {
                return true;
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(8, 11, 17, 255));
        Raylib.BeginMode3D(camera);
        DrawHomeStoreExterior();
        Raylib.EndMode3D();

        Raylib.DrawRectangleGradientH(0, 0, 760, ScreenHeight,
            new Color(5, 7, 11, 242), new Color(5, 7, 11, 0));
        Raylib.DrawRectangleGradientV(0, 0, ScreenWidth, 190,
            new Color(4, 6, 10, 125), new Color(4, 6, 10, 0));
        DrawVignette(55);

        DrawViText("TRỰC ĐÊM", 92, 130, 62, new Color(232, 224, 207, 255));
        Raylib.DrawRectangle(96, 211, 72, 4, new Color(184, 48, 45, 255));
        DrawViText("NIGHT SHIFT HORROR", 96, 232, 19, new Color(177, 182, 188, 255));
        DrawViText("Một ca làm. Một cửa hàng vắng. Đừng tin mọi thứ bạn thấy.", 96, 286, 18,
            new Color(193, 194, 190, 255));

        for (int i = 0; i < 2; i++)
        {
            int y = ButtonY + i * (ButtonH + ButtonGap);
            bool selected = i == homeSelection;
            Color fill = selected ? new Color(142, 35, 38, 235) : new Color(12, 16, 22, 210);
            Color border = selected ? new Color(220, 91, 77, 255) : new Color(94, 99, 104, 210);
            string label = i == 0 ? "BẮT ĐẦU CA TRỰC" : "THOÁT";
            float labelWidth = Raylib.MeasureTextEx(viFont, label, 21, 1f).X;

            Raylib.DrawRectangle(ButtonX, y, ButtonW, ButtonH, fill);
            Raylib.DrawRectangleLines(ButtonX, y, ButtonW, ButtonH, border);
            if (selected)
                Raylib.DrawRectangle(ButtonX, y, 4, ButtonH, new Color(241, 187, 113, 255));
            DrawViText(label, ButtonX + (ButtonW - (int)labelWidth) / 2, y + 18, 21,
                selected ? Color.RayWhite : new Color(185, 188, 190, 255));
        }

        DrawViText("[Lên/Xuống] Chọn   [Enter] Xác nhận", 96, 550, 17, new Color(134, 139, 145, 255));
        DrawViText("ĐƯỜNG SỐ 9  •  23:48", ScreenWidth - 300, ScreenHeight - 47, 17,
            new Color(181, 181, 171, 230));
        Raylib.EndDrawing();
        return false;
    }

    static void StartGameFromHome()
    {
        gameScene = -1;
        phoneNotifStep = 0;
        phoneStepTimer = 0.7f;
        phoneChoiceStage = 0;
        phoneSelectedOption = 0;
        phoneEndHoldTimer = 0f;
        phoneSentReply = "";
        fadeAlpha = 1f;
        homeOrbitAngle = 0f;
        Raylib.DisableCursor();
        Raylib.GetMouseDelta();
    }

    static void ReturnToHome()
    {
        ResetGame();
        gameScene = -2;
        homeSelection = 0;
        homeOrbitAngle = 0f;
        Raylib.EnableCursor();
    }

    static void DrawHomeStoreExterior()
    {
        Color asphalt = new(25, 29, 34, 255);
        Color wall = new(65, 69, 70, 255);

        Raylib.DrawPlane(new Vector3(0f, -0.07f, 0f), new Vector2(70f, 70f), new Color(14, 17, 22, 255));
        Raylib.DrawCube(new Vector3(0f, 0.02f, 8.0f), 44f, 0.12f, 13f, asphalt);
        Raylib.DrawCube(new Vector3(0f, 0.095f, 1.0f), 23f, 0.12f, 7f, new Color(40, 42, 42, 255));
        Raylib.DrawCube(new Vector3(0f, 0.17f, -4.55f), 13f, 0.34f, 10f, new Color(46, 47, 45, 255));

        for (int x = -18; x <= 18; x += 6)
            Raylib.DrawCube(new Vector3(x, 0.095f, 9.0f), 2.4f, 0.012f, 0.12f, new Color(192, 177, 129, 175));

        Raylib.DrawCube(new Vector3(0f, 2.55f, -0.2f), 12f, 5.1f, 9.2f, wall);
        Raylib.DrawCube(new Vector3(0f, 5.15f, -0.2f), 12.35f, 0.28f, 9.5f, new Color(23, 25, 29, 255));
        Raylib.DrawCube(new Vector3(0f, 4.9f, 4.48f), 12.15f, 0.22f, 0.35f, new Color(149, 39, 38, 255));

        Raylib.DrawCube(new Vector3(-3.85f, 2.45f, 4.48f), 3.5f, 2.65f, 0.11f, new Color(64, 87, 91, 255));
        Raylib.DrawCube(new Vector3(3.85f, 2.45f, 4.48f), 3.5f, 2.65f, 0.11f, new Color(57, 78, 82, 255));
        Raylib.DrawCube(new Vector3(-3.85f, 2.45f, 4.55f), 3.25f, 2.42f, 0.035f, new Color(129, 149, 142, 95));
        Raylib.DrawCube(new Vector3(3.85f, 2.45f, 4.55f), 3.25f, 2.42f, 0.035f, new Color(112, 132, 130, 88));

        Raylib.DrawCube(new Vector3(0f, 2.0f, 4.53f), 1.75f, 3.8f, 0.16f, new Color(35, 39, 42, 255));
        Raylib.DrawCube(new Vector3(0.42f, 2.0f, 4.63f), 0.72f, 3.48f, 0.035f, new Color(73, 91, 91, 190));
        Raylib.DrawCube(new Vector3(-0.57f, 2.0f, 4.65f), 0.045f, 0.82f, 0.06f, new Color(191, 176, 128, 255));

        Raylib.DrawCube(new Vector3(0f, 4.13f, 4.54f), 5.4f, 0.92f, 0.24f, new Color(21, 57, 70, 255));
        Raylib.DrawCube(new Vector3(0f, 3.62f, 4.53f), 11.4f, 0.12f, 0.5f, new Color(157, 36, 34, 255));

        Raylib.DrawCube(new Vector3(-5.95f, 2.35f, -0.2f), 0.12f, 4.0f, 7.5f, new Color(50, 67, 68, 255));
        Raylib.DrawCube(new Vector3(5.95f, 2.35f, -0.2f), 0.12f, 4.0f, 7.5f, new Color(45, 59, 62, 255));
        Raylib.DrawCube(new Vector3(-6.02f, 2.4f, -0.2f), 0.035f, 2.15f, 3.6f, new Color(93, 120, 119, 105));
        Raylib.DrawCube(new Vector3(6.02f, 2.4f, -0.2f), 0.035f, 2.15f, 3.6f, new Color(84, 107, 111, 95));

        Raylib.DrawCube(new Vector3(-4.3f, 3.7f, 4.55f), 0.38f, 0.14f, 0.18f,
            new Color(255, 202, 128, 235));
        Raylib.DrawCube(new Vector3(4.3f, 3.7f, 4.55f), 0.38f, 0.14f, 0.18f,
            new Color(255, 202, 128, 235));

        Raylib.DrawCube(new Vector3(-9.0f, 2.2f, 4.0f), 0.12f, 4.4f, 0.12f, new Color(52, 55, 59, 255));
        Raylib.DrawCube(new Vector3(-9.0f, 4.45f, 4.0f), 0.85f, 0.48f, 0.85f, new Color(211, 194, 144, 255));
        Raylib.DrawCube(new Vector3(-9.0f, 0.08f, 4.0f), 1.55f, 0.05f, 1.55f, new Color(211, 177, 115, 36));

        Raylib.DrawCubeWires(new Vector3(0f, 2.55f, -0.2f), 12f, 5.1f, 9.2f, new Color(111, 115, 113, 110));
    }
}
