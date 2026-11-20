using System.Numerics;
using Raylib_cs;

partial class Program
{
    enum HeldItemType
    {
        None,
        SupplyBox,
        JuiceCupEmpty,
        JuiceCupFull,
        Broom,
        Mop,
        CheckoutItem,
        TrashBag
    }

    sealed class FloorMess
    {
        public Vector3 Pos;
        public bool IsLiquid;
        public float Progress;
    }

    static HeldItemType heldItem = HeldItemType.None;
    static int heldBoxStock = 0;
    static float cupFillAmount = 0f;
    static Color cupLiquidColor = new(245, 155, 38, 240);
    static bool isPouringJuice = false;
    static int activeDispenserIndex = -1;
    static float handActionAnim = 0f;

    static float scanSweepAnim = 0f;
    static Color scanSweepItemColor = new(215, 65, 65, 255);
    static int baggedItemCount = 0;

    const int ShelfCount = 6;
    const int SlotsPerShelf = 48;
    static readonly bool[,] shelfSlotFilled = new bool[ShelfCount, SlotsPerShelf];
    static bool shelfStockInitialized = false;

    static readonly List<FloorMess> floorMesses = [];
    static string skullPromptKey = "";
    static string skullPromptText = "";
    static bool skullPromptBlocked = false;

    const int CurtainStripCount = 8;
    static readonly float[] curtainAngle = new float[CurtainStripCount];
    static readonly float[] curtainVel = new float[CurtainStripCount];
    static Texture2D warehouseSignTex;
    static bool hasWarehouseSignTex = false;

    static int indoorTrashBags = 3;
    static readonly bool[] dumpsterLidOpen = [false, false, false];
    static bool breakerDoorOpen = false;
    static float breakerDoorAnim = 0f;
    static float breakerRepairProgress = 0f;
    static readonly bool[] breakerSwitchesOn = [true, true, true, true];

    static void InitShiftWorkState()
    {
        heldItem = HeldItemType.None;
        heldBoxStock = 0;
        cupFillAmount = 0f;
        isPouringJuice = false;
        activeDispenserIndex = -1;
        handActionAnim = 0f;
        scanSweepAnim = 0f;
        baggedItemCount = 0;
        indoorTrashBags = 3;
        Array.Clear(dumpsterLidOpen);
        breakerDoorOpen = false;
        breakerDoorAnim = 0f;
        breakerRepairProgress = 0f;
        Array.Fill(breakerSwitchesOn, true);
        floorMesses.Clear();
        Array.Clear(curtainAngle);
        Array.Clear(curtainVel);
        navGridBuilt = false;
        ResetShopperState();

        for (int s = 0; s < ShelfCount; s++)
        {
            for (int i = 0; i < SlotsPerShelf; i++)
            {
                int h = ((s + 1) * 31 + i * 17) & 0x7FFFFFFF;
                shelfSlotFilled[s, i] = (h % 6 != 0);
            }
        }
        shelfStockInitialized = true;

        floorMesses.Add(new FloorMess { Pos = new Vector3(0.35f, 0.006f, 5.20f), IsLiquid = false });
        floorMesses.Add(new FloorMess { Pos = new Vector3(5.55f, 0.006f, 1.40f), IsLiquid = false });
        floorMesses.Add(new FloorMess { Pos = new Vector3(-2.10f, 0.006f, 2.20f), IsLiquid = false });
        floorMesses.Add(new FloorMess { Pos = new Vector3(2.85f, 0.006f, -3.60f), IsLiquid = false });

        floorMesses.Add(new FloorMess { Pos = new Vector3(-5.65f, 0.006f, 0.40f), IsLiquid = true });
        floorMesses.Add(new FloorMess { Pos = new Vector3(-8.75f, 0.006f, -1.20f), IsLiquid = true });
        floorMesses.Add(new FloorMess { Pos = new Vector3(8.35f, 0.006f, 2.60f), IsLiquid = true });
        floorMesses.Add(new FloorMess { Pos = new Vector3(1.15f, 0.006f, -0.50f), IsLiquid = true });
    }

    static int GetShelfFilledCount(int shelfIndex)
    {
        if (!shelfStockInitialized) InitShiftWorkState();
        int count = 0;
        for (int i = 0; i < SlotsPerShelf; i++)
            if (shelfSlotFilled[shelfIndex, i]) count++;
        return count;
    }

    static bool TakeOneProductFromShelf(int shelfIndex)
    {
        if (!shelfStockInitialized) InitShiftWorkState();
        if (shelfIndex < 0 || shelfIndex >= ShelfCount) return false;
        for (int i = SlotsPerShelf - 1; i >= 0; i--)
        {
            if (shelfSlotFilled[shelfIndex, i])
            {
                shelfSlotFilled[shelfIndex, i] = false;
                return true;
            }
        }
        return false;
    }

    static bool RestockOneProductToShelf(int shelfIndex)
    {
        if (!shelfStockInitialized) InitShiftWorkState();
        if (shelfIndex < 0 || shelfIndex >= ShelfCount) return false;
        for (int i = 0; i < SlotsPerShelf; i++)
        {
            if (!shelfSlotFilled[shelfIndex, i])
            {
                shelfSlotFilled[shelfIndex, i] = true;
                return true;
            }
        }
        return false;
    }

    static void UpdateWarehouseCurtainPhysics(float dt)
    {
        const float doorCenterX = 9.0f;
        const float doorWidth = 1.24f;
        const float doorZ = -7.20f;
        float stripW = doorWidth / CurtainStripCount;

        for (int i = 0; i < CurtainStripCount; i++)
        {
            float stripX = (doorCenterX - doorWidth * 0.5f) + (i + 0.5f) * stripW;
            float dx = MathF.Abs(playerPos.X - stripX);
            float dz = playerPos.Z - doorZ;

            if (dx < 0.34f && MathF.Abs(dz) < 0.38f)
            {
                float pushSign = dz >= 0f ? -1f : 1f;
                float proximity = 1f - (dx / 0.34f);
                float targetAngle = pushSign * (42f + 32f * proximity);
                curtainVel[i] += (targetAngle - curtainAngle[i]) * 26f * dt;
            }

            const float stiffness = 22.0f;
            const float damping = 4.2f;
            float accel = -stiffness * curtainAngle[i] - damping * curtainVel[i];
            curtainVel[i] += accel * dt;
            curtainAngle[i] += curtainVel[i] * dt;
            curtainAngle[i] = Math.Clamp(curtainAngle[i], -78f, 78f);
        }
    }

    static void UpdateShiftWork(float dt)
    {
        if (!shelfStockInitialized) InitShiftWorkState();

        UpdateWarehouseCurtainPhysics(dt);

        if (!lightsOn && breakerSwitchesOn[0])
        {
            breakerSwitchesOn[0] = false;
            breakerSwitchesOn[2] = false;
        }
        float targetDoor = breakerDoorOpen ? 1f : 0f;
        breakerDoorAnim += (targetDoor - breakerDoorAnim) * Math.Min(1f, dt * 10f);

        skullPromptKey = "";
        skullPromptText = "";
        skullPromptBlocked = false;
        isPouringJuice = false;
        activeDispenserIndex = -1;

        if (handActionAnim > 0f)
            handActionAnim = Math.Max(0f, handActionAnim - dt * 3.8f);

        if (scanSweepAnim > 0f)
            scanSweepAnim = Math.Max(0f, scanSweepAnim - dt * 2.4f);

        UpdateRegularShopper(dt);

        if (isTalking || isWorkCutscene || isDawnCutscene) return;

        if (heldItem != HeldItemType.None && Raylib.IsKeyPressed(KeyboardKey.Q))
        {
            heldItem = HeldItemType.None;
            heldBoxStock = 0;
            cupFillAmount = 0f;
            Raylib.SetSoundPitch(voiceStaticSound, 0.9f);
            Raylib.PlaySound(voiceStaticSound);
            return;
        }

        Vector3 lookDir = Vector3.Normalize(camera.Target - camera.Position);
        Ray eyeRay = new(camera.Position, lookDir);

        var placements = GetStoreShelfPlacements();
        for (int s = 0; s < placements.Length && s < ShelfCount; s++)
        {
            BoundingBox shelfBox = GetCustomModelBounds("shelf", placements[s].Pos, placements[s].Yaw);
            RayCollision hit = Raylib.GetRayCollisionBox(eyeRay, shelfBox);
            if (hit.Hit && hit.Distance <= 2.6f)
            {
                int cur = GetShelfFilledCount(s);
                if (cur < SlotsPerShelf)
                {
                    if (heldItem == HeldItemType.SupplyBox && heldBoxStock > 0)
                    {
                        skullPromptKey = "F";
                        skullPromptText = $"XẾP LÊN KỆ ({cur}/{SlotsPerShelf}) • THÙNG CÒN {heldBoxStock}";
                        if (Raylib.IsKeyPressed(KeyboardKey.F))
                        {
                            if (RestockOneProductToShelf(s))
                            {
                                heldBoxStock--;
                                handActionAnim = 1f;
                                Raylib.SetSoundPitch(voiceStaticSound, 1.25f);
                                Raylib.PlaySound(voiceStaticSound);
                                if (heldBoxStock <= 0) heldItem = HeldItemType.None;
                            }
                        }
                        return;
                    }
                    else if (heldItem == HeldItemType.None && cur < SlotsPerShelf - 6)
                    {
                        skullPromptKey = "F";
                        skullPromptText = $"KỆ VƠI ({cur}/{SlotsPerShelf}) • CẦN THÙNG HÀNG TRONG KHO";
                        skullPromptBlocked = true;
                        return;
                    }
                }
            }
        }

        BoundingBox warehouseBoxZone = new(new Vector3(6.6f, 0.0f, -11.0f), new Vector3(8.4f, 1.6f, -9.5f));
        RayCollision boxHit = Raylib.GetRayCollisionBox(eyeRay, warehouseBoxZone);
        if (boxHit.Hit && boxHit.Distance <= 2.7f)
        {
            if (heldItem == HeldItemType.None)
            {
                skullPromptKey = "F";
                skullPromptText = "BƯNG THÙNG HÀNG";
                if (Raylib.IsKeyPressed(KeyboardKey.F))
                {
                    heldItem = HeldItemType.SupplyBox;
                    heldBoxStock = 12;
                    handActionAnim = 1f;
                    Raylib.SetSoundPitch(voiceStaticSound, 0.85f);
                    Raylib.PlaySound(voiceStaticSound);
                }
            }
            else
            {
                skullPromptKey = "F";
                skullPromptText = "TAY ĐANG BẬN (NHẤN [Q] ĐỂ BỎ ĐỒ)";
                skullPromptBlocked = true;
            }
            return;
        }

        BoundingBox broomBox = new(new Vector3(9.65f, 0f, -6.95f), new Vector3(10.25f, 1.7f, -6.45f));
        BoundingBox mopBox = new(new Vector3(9.65f, 0f, -6.40f), new Vector3(10.25f, 1.7f, -5.90f));

        RayCollision broomHit = Raylib.GetRayCollisionBox(eyeRay, broomBox);
        if (broomHit.Hit && broomHit.Distance <= 2.6f)
        {
            if (heldItem == HeldItemType.Broom)
            {
                skullPromptKey = "F";
                skullPromptText = "CẤT CHỔI";
                if (Raylib.IsKeyPressed(KeyboardKey.F)) heldItem = HeldItemType.None;
            }
            else if (heldItem == HeldItemType.None)
            {
                skullPromptKey = "F";
                skullPromptText = "CẦM CHỔI QUÉT";
                if (Raylib.IsKeyPressed(KeyboardKey.F)) heldItem = HeldItemType.Broom;
            }
            else
            {
                skullPromptKey = "F";
                skullPromptText = "TAY ĐANG BẬN";
                skullPromptBlocked = true;
            }
            return;
        }

        RayCollision mopHit = Raylib.GetRayCollisionBox(eyeRay, mopBox);
        if (mopHit.Hit && mopHit.Distance <= 2.6f)
        {
            if (heldItem == HeldItemType.Mop)
            {
                skullPromptKey = "F";
                skullPromptText = "CẤT CÂY LAU SÀN";
                if (Raylib.IsKeyPressed(KeyboardKey.F)) heldItem = HeldItemType.None;
            }
            else if (heldItem == HeldItemType.None)
            {
                skullPromptKey = "F";
                skullPromptText = "CẦM CÂY LAU SÀN";
                if (Raylib.IsKeyPressed(KeyboardKey.F)) heldItem = HeldItemType.Mop;
            }
            else
            {
                skullPromptKey = "F";
                skullPromptText = "TAY ĐANG BẬN";
                skullPromptBlocked = true;
            }
            return;
        }

        BoundingBox cupStackBox = new(new Vector3(-7.6f, 0.95f, 0.1f), new Vector3(-7.0f, 1.35f, 0.95f));
        RayCollision cupHit = Raylib.GetRayCollisionBox(eyeRay, cupStackBox);
        if (cupHit.Hit && cupHit.Distance <= 2.5f)
        {
            if (heldItem == HeldItemType.None)
            {
                skullPromptKey = "F";
                skullPromptText = "LẤY LY NHỰA";
                if (Raylib.IsKeyPressed(KeyboardKey.F))
                {
                    heldItem = HeldItemType.JuiceCupEmpty;
                    cupFillAmount = 0f;
                    handActionAnim = 1f;
                }
            }
            else
            {
                skullPromptKey = "F";
                skullPromptText = "TAY ĐANG BẬN";
                skullPromptBlocked = true;
            }
            return;
        }

        (string Name, Color Col)[] juiceFlavors =
        [
            ("NƯỚC VIỆT QUẤT", new Color(58, 148, 232, 245)),
            ("TRÀ XANH TÁO",   new Color(72, 192, 105, 245)),
            ("SỮA DỪA TƯƠI",   new Color(242, 238, 224, 245)),
            ("NƯỚC NHO TÍM",   new Color(156, 86, 214, 245))
        ];

        for (int m = 0; m < 4; m++)
        {
            float mz = -2.2f + m * 1.35f;
            BoundingBox dispenserBox = new(
                new Vector3(-10.15f, 1.00f, mz - 0.56f),
                new Vector3(-9.15f, 1.92f, mz + 0.56f)
            );
            RayCollision dHit = Raylib.GetRayCollisionBox(eyeRay, dispenserBox);
            if (dHit.Hit && dHit.Distance <= 2.65f)
            {
                if (heldItem == HeldItemType.JuiceCupEmpty || heldItem == HeldItemType.JuiceCupFull)
                {
                    skullPromptKey = "GIỮ F";
                    skullPromptText = cupFillAmount >= 0.72f
                        ? $"ĐẦY LY {juiceFlavors[m].Name} (NHẢ TAY)"
                        : $"RÓT {juiceFlavors[m].Name}";

                    if (Raylib.IsKeyDown(KeyboardKey.F))
                    {
                        isPouringJuice = true;
                        activeDispenserIndex = m;

                        if (cupFillAmount <= 0.02f)
                            cupLiquidColor = juiceFlavors[m].Col;
                        else
                            cupLiquidColor = new Color(
                                (byte)(cupLiquidColor.R + (juiceFlavors[m].Col.R - cupLiquidColor.R) * Math.Min(1f, dt * 3.5f)),
                                (byte)(cupLiquidColor.G + (juiceFlavors[m].Col.G - cupLiquidColor.G) * Math.Min(1f, dt * 3.5f)),
                                (byte)(cupLiquidColor.B + (juiceFlavors[m].Col.B - cupLiquidColor.B) * Math.Min(1f, dt * 3.5f)),
                                (byte)245
                            );

                        cupFillAmount += dt * 0.52f;
                        handActionAnim = 0.25f;
                        if (cupFillAmount >= 0.72f)
                            heldItem = HeldItemType.JuiceCupFull;

                        if (cupFillAmount > 1.06f)
                        {
                            cupFillAmount = 1.0f;
                            floorMesses.Add(new FloorMess
                            {
                                Pos = new Vector3(playerPos.X, 0.006f, playerPos.Z),
                                IsLiquid = true
                            });
                            Raylib.SetSoundPitch(voiceStaticSound, 0.65f);
                            Raylib.PlaySound(voiceStaticSound);
                        }
                    }
                }
                else
                {
                    skullPromptKey = "F";
                    skullPromptText = $"CẦN CẦM LY NHỰA ({juiceFlavors[m].Name})";
                    skullPromptBlocked = true;
                }
                return;
            }
        }

        for (int i = floorMesses.Count - 1; i >= 0; i--)
        {
            var mess = floorMesses[i];
            BoundingBox messBox = new(mess.Pos - new Vector3(0.45f, 0.05f, 0.45f), mess.Pos + new Vector3(0.45f, 0.25f, 0.45f));
            RayCollision messHit = Raylib.GetRayCollisionBox(eyeRay, messBox);
            if (messHit.Hit && messHit.Distance <= 2.7f)
            {
                bool rightTool = (mess.IsLiquid && heldItem == HeldItemType.Mop) ||
                                 (!mess.IsLiquid && heldItem == HeldItemType.Broom);
                if (rightTool)
                {
                    skullPromptKey = "GIỮ F";
                    skullPromptText = mess.IsLiquid ? "LAU SẠCH VẾT NƯỚC" : "QUÉT SẠCH RÁC";
                    if (Raylib.IsKeyDown(KeyboardKey.F))
                    {
                        mess.Progress += dt * 0.95f;
                        handActionAnim = 0.5f + 0.5f * MathF.Sin((float)Raylib.GetTime() * 14f);
                        if (mess.Progress >= 1f)
                        {
                            floorMesses.RemoveAt(i);
                            Raylib.SetSoundPitch(voiceStaticSound, 1.4f);
                            Raylib.PlaySound(voiceStaticSound);
                        }
                    }
                }
                else
                {
                    skullPromptKey = "F";
                    skullPromptText = mess.IsLiquid ? "CẦN CÂY LAU SÀN" : "CẦN CHỔI QUÉT";
                    skullPromptBlocked = true;
                }
                return;
            }
        }

        BoundingBox indoorBinBox = new(new Vector3(6.40f, 0f, -8.10f), new Vector3(7.10f, 1.05f, -7.40f));
        RayCollision inBinHit = Raylib.GetRayCollisionBox(eyeRay, indoorBinBox);
        if (inBinHit.Hit && inBinHit.Distance <= 2.6f)
        {
            if (heldItem == HeldItemType.None && indoorTrashBags > 0)
            {
                skullPromptKey = "F";
                skullPromptText = $"LẤY TÚI RÁC RA SÂN SAU ĐỔ ({indoorTrashBags} TÚI)";
                if (Raylib.IsKeyPressed(KeyboardKey.F))
                {
                    heldItem = HeldItemType.TrashBag;
                    indoorTrashBags--;
                    handActionAnim = 1f;
                    Raylib.SetSoundPitch(voiceStaticSound, 0.85f);
                    Raylib.PlaySound(voiceStaticSound);
                }
            }
            else if (heldItem != HeldItemType.None)
            {
                skullPromptKey = "F";
                skullPromptText = "TAY ĐANG BẬN (NHẤN [Q] ĐỂ BỎ ĐỒ)";
                skullPromptBlocked = true;
            }
            return;
        }

        for (int b = 0; b < 3; b++)
        {
            float bz = -12.6f - b * 1.0f;
            BoundingBox dumpsterBox = new(new Vector3(9.90f, 0f, bz - 0.42f), new Vector3(10.80f, 1.25f, bz + 0.42f));
            RayCollision dHit = Raylib.GetRayCollisionBox(eyeRay, dumpsterBox);
            if (dHit.Hit && dHit.Distance <= 2.7f)
            {
                if (heldItem == HeldItemType.TrashBag)
                {
                    skullPromptKey = "F";
                    skullPromptText = "BỎ TÚI RÁC VÀO THÙNG";
                    if (Raylib.IsKeyPressed(KeyboardKey.F))
                    {
                        dumpsterLidOpen[b] = true;
                        heldItem = HeldItemType.None;
                        handActionAnim = 1f;
                        Raylib.SetSoundPitch(voiceStaticSound, 0.78f);
                        Raylib.PlaySound(voiceStaticSound);
                    }
                }
                else
                {
                    skullPromptKey = "F";
                    skullPromptText = dumpsterLidOpen[b] ? "ĐẬY NẮP THÙNG RÁC" : "MỞ NẮP THÙNG RÁC";
                    if (Raylib.IsKeyPressed(KeyboardKey.F))
                    {
                        dumpsterLidOpen[b] = !dumpsterLidOpen[b];
                        Raylib.SetSoundPitch(voiceStaticSound, 1.1f);
                        Raylib.PlaySound(voiceStaticSound);
                    }
                }
                return;
            }
        }

        BoundingBox breakerBox = new(new Vector3(7.20f, 1.05f, -11.65f), new Vector3(7.90f, 1.85f, -11.20f));
        RayCollision brkHit = Raylib.GetRayCollisionBox(eyeRay, breakerBox);
        if (brkHit.Hit && brkHit.Distance <= 2.6f)
        {
            if (!breakerDoorOpen)
            {
                skullPromptKey = "F";
                skullPromptText = lightsOn ? "MỞ HỘP ĐIỆN TỔNG" : "MỞ HỘP ĐIỆN (NHẢY CẦU DAO!)";
                if (Raylib.IsKeyPressed(KeyboardKey.F))
                {
                    breakerDoorOpen = true;
                    Raylib.SetSoundPitch(voiceStaticSound, 1.15f);
                    Raylib.PlaySound(voiceStaticSound);
                }
            }
            else if (!lightsOn)
            {
                if (heldItem != HeldItemType.None)
                {
                    skullPromptKey = "F";
                    skullPromptText = "CẦN RẢNH TAY ĐỂ GẠT CẦU DAO (NHẤN [Q])";
                    skullPromptBlocked = true;
                }
                else
                {
                    int pct = (int)(breakerRepairProgress * 100f);
                    skullPromptKey = "GIỮ F";
                    skullPromptText = $"SỬA CẦU CHÌ & GẠT CẦU DAO TỔNG ({pct}%)";
                    if (Raylib.IsKeyDown(KeyboardKey.F))
                    {
                        breakerRepairProgress += dt * 0.65f;
                        handActionAnim = 0.5f + 0.5f * MathF.Sin((float)Raylib.GetTime() * 18f);
                        if (breakerRepairProgress >= 1f)
                        {
                            breakerRepairProgress = 0f;
                            Array.Fill(breakerSwitchesOn, true);
                            lightsOn = true;
                            Raylib.SetSoundPitch(voiceStaticSound, 1.55f);
                            Raylib.PlaySound(voiceStaticSound);
                        }
                    }
                }
            }
            else
            {
                skullPromptKey = "F";
                skullPromptText = "ĐÓNG NẮP HỘP ĐIỆN (ĐIỆN ĐANG ỔN ĐỊNH)";
                if (Raylib.IsKeyPressed(KeyboardKey.F))
                {
                    breakerDoorOpen = false;
                    Raylib.SetSoundPitch(voiceStaticSound, 0.95f);
                    Raylib.PlaySound(voiceStaticSound);
                }
            }
            return;
        }

        BoundingBox bikeBox = new(new Vector3(3.75f, 0f, -16.35f), new Vector3(4.65f, 1.25f, -14.75f));
        RayCollision bikeHit = Raylib.GetRayCollisionBox(eyeRay, bikeBox);
        if (bikeHit.Hit && bikeHit.Distance <= 2.7f)
        {
            skullPromptKey = "F";
            skullPromptText = "XE MÁY CỦA TÚ";
            if (Raylib.IsKeyPressed(KeyboardKey.F))
            {
                StartDialogue(
                    "Tú (Suy nghĩ)",
                    "Con xe cà tàng vừa chở mình tới ca trực tối nay. Ráng làm xong ca tới sáng rồi chạy về ngủ một giấc.",
                    1,
                    ["Tiếp tục ca trực"]
                );
            }
            return;
        }
    }

    static Texture2D GetWarehouseWarningSignTexture()
    {
        if (hasWarehouseSignTex) return warehouseSignTex;

        const int w = 512;
        const int h = 160;
        RenderTexture2D rt = Raylib.LoadRenderTexture(w, h);
        Raylib.BeginTextureMode(rt);
        Raylib.ClearBackground(new Color(185, 24, 24, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(8, 8, w - 16, h - 16), 6f, new Color(250, 215, 65, 255));

        string l1 = "KHO HÀNG";
        string l2 = "KHÔNG PHẬN SỰ MIỄN VÀO";
        int w1 = (int)Raylib.MeasureTextEx(viFont, l1, 34, 1.5f).X;
        int w2 = (int)Raylib.MeasureTextEx(viFont, l2, 30, 1.2f).X;
        DrawViText(l1, (w - w1) / 2, 26, 34, new Color(255, 230, 80, 255));
        DrawViText(l2, (w - w2) / 2, 84, 30, Color.White);
        Raylib.EndTextureMode();

        Image img = Raylib.LoadImageFromTexture(rt.Texture);
        Raylib.ImageFlipVertical(ref img);
        warehouseSignTex = Raylib.LoadTextureFromImage(img);
        Raylib.SetTextureFilter(warehouseSignTex, TextureFilter.Bilinear);
        Raylib.UnloadImage(img);
        Raylib.UnloadRenderTexture(rt);
        hasWarehouseSignTex = true;
        return warehouseSignTex;
    }

    static void DrawWarehouseDoorSignAndCurtain(bool lit)
    {
        const float doorCenterX = 9.0f;
        const float doorWidth = 1.24f;
        const float doorZ = -7.20f;
        const float topY = 2.42f;
        const float stripLen = 2.28f;

        Color frameCol = lit ? new Color(165, 172, 182, 255) : new Color(55, 58, 64, 255);
        Raylib.DrawCube(new Vector3(doorCenterX, topY + 0.03f, doorZ), doorWidth + 0.12f, 0.06f, 0.24f, frameCol);

        Texture2D signTex = GetWarehouseWarningSignTexture();
        Color signTint = lit ? Color.White : new Color(90, 90, 95, 255);
        float signW = 1.18f, signH = 0.36f, signY = 2.74f, signZ = doorZ + 0.11f;

        Raylib.DrawCube(
            new Vector3(doorCenterX, signY, signZ - 0.005f),
            signW, signH, 0.015f,
            lit ? new Color(160, 20, 20, 255) : new Color(55, 10, 10, 255)
        );

        Rlgl.SetTexture(signTex.Id);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(signTint.R, signTint.G, signTint.B, signTint.A);
        Rlgl.Normal3f(0f, 0f, 1f);
        Rlgl.TexCoord2f(0f, 1f); Rlgl.Vertex3f(doorCenterX - signW * 0.5f, signY - signH * 0.5f, signZ + 0.006f);
        Rlgl.TexCoord2f(1f, 1f); Rlgl.Vertex3f(doorCenterX + signW * 0.5f, signY - signH * 0.5f, signZ + 0.006f);
        Rlgl.TexCoord2f(1f, 0f); Rlgl.Vertex3f(doorCenterX + signW * 0.5f, signY + signH * 0.5f, signZ + 0.006f);
        Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(doorCenterX - signW * 0.5f, signY + signH * 0.5f, signZ + 0.006f);
        Rlgl.End();
        Rlgl.SetTexture(0);

        float stripW = doorWidth / CurtainStripCount;
        Color pvcGreen = lit ? new Color(36, 168, 92, 175) : new Color(14, 58, 34, 175);
        Color pvcEdge = lit ? new Color(22, 115, 62, 210) : new Color(10, 40, 22, 210);

        for (int i = 0; i < CurtainStripCount; i++)
        {
            float stripX = (doorCenterX - doorWidth * 0.5f) + (i + 0.5f) * stripW;
            Rlgl.PushMatrix();
            Rlgl.Translatef(stripX, topY, doorZ);
            Rlgl.Rotatef(curtainAngle[i], 1f, 0f, 0f);
            Raylib.DrawCube(new Vector3(0f, -stripLen * 0.5f, 0f), stripW * 0.94f, stripLen, 0.014f, pvcGreen);
            Raylib.DrawCubeWires(new Vector3(0f, -stripLen * 0.5f, 0f), stripW * 0.94f, stripLen, 0.015f, pvcEdge);
            Rlgl.PopMatrix();
        }
    }

    static void DrawShiftWork3D(bool lit)
    {
        foreach (var mess in floorMesses)
        {
            float scale = Math.Max(0.15f, 1f - mess.Progress);
            if (mess.IsLiquid)
            {
                Raylib.DrawCylinder(mess.Pos, 0.36f * scale, 0.36f * scale, 0.004f, 14,
                    lit ? new Color(65, 130, 175, 165) : new Color(25, 45, 65, 165));
            }
            else
            {
                Raylib.DrawCube(mess.Pos + new Vector3(-0.08f, 0.02f, 0.04f), 0.14f * scale, 0.04f, 0.10f * scale, new Color(210, 170, 75, 255));
                Raylib.DrawCube(mess.Pos + new Vector3(0.09f, 0.015f, -0.05f), 0.11f * scale, 0.03f, 0.11f * scale, new Color(185, 70, 65, 255));
            }
        }

        if (heldItem != HeldItemType.Broom)
        {
            Raylib.DrawCylinder(new Vector3(9.95f, 0.02f, -6.72f), 0.018f, 0.018f, 1.35f, 8, new Color(165, 118, 68, 255));
            Raylib.DrawCube(new Vector3(9.95f, 0.09f, -6.72f), 0.28f, 0.16f, 0.08f, new Color(215, 185, 95, 255));
        }
        if (heldItem != HeldItemType.Mop)
        {
            Raylib.DrawCylinder(new Vector3(9.95f, 0.02f, -6.15f), 0.020f, 0.020f, 1.40f, 8, new Color(65, 125, 195, 255));
            Raylib.DrawCube(new Vector3(9.95f, 0.07f, -6.15f), 0.32f, 0.10f, 0.14f, new Color(235, 238, 242, 255));
        }

        DrawWarehouseDoorSignAndCurtain(lit);

        Color scannerBody = lit ? new Color(42, 46, 54, 255) : new Color(22, 24, 28, 255);
        Raylib.DrawCube(new Vector3(-0.82f, 1.07f, -2.06f), 0.14f, 0.03f, 0.16f, scannerBody);
        Raylib.DrawCube(new Vector3(-0.82f, 1.086f, -2.06f), 0.09f, 0.005f, 0.11f, new Color(220, 35, 35, 220));
        if (scanSweepAnim > 0.05f)
        {
            Raylib.DrawCube(new Vector3(-0.82f, 1.13f, -2.06f), 0.006f, 0.09f, 0.13f, new Color(255, 50, 50, 200));
        }

        Color spoutCol = lit ? new Color(195, 202, 212, 255) : new Color(65, 68, 74, 255);
        for (int m = 0; m < 4; m++)
        {
            float mz = -2.2f + m * 1.35f;
            Raylib.DrawCube(new Vector3(-9.38f, 1.41f, mz), 0.14f, 0.04f, 0.05f, spoutCol);
            Raylib.DrawCylinder(new Vector3(-9.33f, 1.35f, mz), 0.016f, 0.016f, 0.06f, 10, spoutCol);
        }

        if (scanSweepAnim > 0.01f)
        {
            float t = 1f - scanSweepAnim;
            float itemX = -0.58f + (-1.24f - (-0.58f)) * t;
            float arcY = 1.16f + MathF.Sin(t * MathF.PI) * 0.12f;
            float itemZ = -2.05f;
            Raylib.DrawCube(new Vector3(itemX, arcY, itemZ), 0.11f, 0.12f, 0.10f, scanSweepItemColor);
            Raylib.DrawSphere(new Vector3(itemX, arcY + 0.07f, itemZ - 0.04f), 0.042f, new Color(224, 182, 148, 255));
        }

        Color inBinCol = lit ? new Color(55, 62, 72, 255) : new Color(25, 28, 32, 255);
        Raylib.DrawCylinder(new Vector3(6.75f, 0.01f, -7.75f), 0.24f, 0.20f, 0.62f, 14, inBinCol);
        if (indoorTrashBags > 0)
            Raylib.DrawSphere(new Vector3(6.75f, 0.64f, -7.75f), 0.22f, new Color(24, 24, 28, 255));

        Color[] binBodyCols =
        [
            lit ? new Color(36, 132, 68, 255) : new Color(16, 54, 30, 255),
            lit ? new Color(36, 132, 68, 255) : new Color(16, 54, 30, 255),
            lit ? new Color(64, 70, 78, 255) : new Color(28, 30, 34, 255)
        ];
        for (int b = 0; b < 3; b++)
        {
            float bz = -12.6f - b * 1.0f;
            Raylib.DrawCube(new Vector3(10.35f, 0.54f, bz), 0.66f, 0.96f, 0.62f, binBodyCols[b]);
            Raylib.DrawCubeWires(new Vector3(10.35f, 0.54f, bz), 0.665f, 0.965f, 0.625f, new Color(20, 22, 26, 255));
            Raylib.DrawCylinder(new Vector3(10.62f, 0.08f, bz - 0.24f), 0.08f, 0.08f, 0.05f, 10, Color.Black);
            Raylib.DrawCylinder(new Vector3(10.62f, 0.08f, bz + 0.24f), 0.08f, 0.08f, 0.05f, 10, Color.Black);

            Rlgl.PushMatrix();
            Rlgl.Translatef(10.68f, 1.03f, bz);
            Rlgl.Rotatef(dumpsterLidOpen[b] ? -75f : 0f, 0f, 0f, 1f);
            Raylib.DrawCube(new Vector3(-0.34f, 0.03f, 0f), 0.70f, 0.06f, 0.66f, binBodyCols[b]);
            Rlgl.PopMatrix();
        }
        Raylib.DrawSphere(new Vector3(10.25f, 0.20f, -15.35f), 0.24f, new Color(22, 22, 26, 255));
        Raylib.DrawSphere(new Vector3(10.42f, 0.18f, -15.72f), 0.21f, new Color(26, 26, 30, 255));

        const float bx = 7.55f, by = 1.45f, bzWall = -11.36f;
        Color cabinetCol = lit ? new Color(155, 162, 172, 255) : new Color(58, 62, 68, 255);
        Raylib.DrawCube(new Vector3(bx, by, bzWall), 0.46f, 0.62f, 0.14f, cabinetCol);
        Raylib.DrawCube(new Vector3(bx, by, bzWall - 0.055f), 0.40f, 0.56f, 0.03f, new Color(28, 30, 35, 255));

        for (int sw = 0; sw < 4; sw++)
        {
            float sx = bx - 0.12f + sw * 0.08f;
            Raylib.DrawCube(new Vector3(sx, by + 0.06f, bzWall - 0.075f), 0.055f, 0.14f, 0.03f, new Color(220, 222, 228, 255));
            Color toggleCol = breakerSwitchesOn[sw] ? new Color(45, 185, 85, 255) : new Color(225, 48, 48, 255);
            float toggleY = breakerSwitchesOn[sw] ? by + 0.08f : by + 0.03f;
            Raylib.DrawCube(new Vector3(sx, toggleY, bzWall - 0.095f), 0.035f, 0.035f, 0.025f, toggleCol);
        }
        Raylib.DrawCube(new Vector3(bx, by - 0.12f, bzWall - 0.075f), 0.30f, 0.03f, 0.02f, new Color(210, 145, 55, 255));

        Rlgl.PushMatrix();
        Rlgl.Translatef(bx - 0.23f, by, bzWall - 0.075f);
        Rlgl.Rotatef(breakerDoorAnim * 105f, 0f, 1f, 0f);
        Raylib.DrawCube(new Vector3(0.23f, 0f, 0f), 0.46f, 0.62f, 0.02f, cabinetCol);
        Raylib.DrawCube(new Vector3(0.23f, 0.08f, -0.013f), 0.14f, 0.14f, 0.004f, new Color(245, 200, 38, 255));
        bool faultBlink = !lightsOn && MathF.Sin((float)Raylib.GetTime() * 14f) > 0f;
        Color ledCol = lightsOn
            ? new Color(55, 235, 95, 255)
            : (faultBlink ? new Color(255, 42, 42, 255) : new Color(95, 18, 18, 255));
        Raylib.DrawSphere(new Vector3(0.23f, 0.22f, -0.015f), 0.018f, ledCol);
        Rlgl.PopMatrix();

        if (heldItem == HeldItemType.None) return;

        Vector3 forward = Vector3.Normalize(camera.Target - camera.Position);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);
        float bob = MathF.Sin(walkBobTimer) * 0.012f - handActionAnim * 0.05f;

        if (heldItem == HeldItemType.SupplyBox)
        {
            Vector3 boxPos = camera.Position + forward * 0.56f - up * (0.28f - bob);
            Rlgl.PushMatrix();
            Rlgl.Translatef(boxPos.X, boxPos.Y, boxPos.Z);
            Rlgl.Rotatef(camYaw * 180f / MathF.PI, 0, 1, 0);
            Raylib.DrawCube(Vector3.Zero, 0.44f, 0.28f, 0.34f, new Color(178, 132, 82, 255));
            Raylib.DrawCubeWires(Vector3.Zero, 0.445f, 0.285f, 0.345f, new Color(95, 65, 35, 255));
            Rlgl.PopMatrix();
        }
        else if (heldItem == HeldItemType.JuiceCupEmpty || heldItem == HeldItemType.JuiceCupFull)
        {
            const float cupBottomR = 0.036f;
            const float cupTopR = 0.048f;
            const float cupH = 0.145f;

            Vector3 cupPos;
            if (isPouringJuice && activeDispenserIndex >= 0)
            {
                float mz = -2.2f + activeDispenserIndex * 1.35f;
                cupPos = new Vector3(-9.33f, 1.04f, mz);

                float pulse = 0.010f + 0.0025f * MathF.Sin((float)Raylib.GetTime() * 28f);
                float streamBottomY = cupPos.Y + Math.Min(0.135f, Math.Max(0.01f, cupFillAmount * 0.135f));
                float streamH = Math.Max(0.02f, 1.355f - streamBottomY);
                Raylib.DrawCylinder(
                    new Vector3(-9.33f, streamBottomY, mz),
                    pulse * 0.85f,
                    pulse * 1.15f,
                    streamH,
                    10,
                    cupLiquidColor
                );
            }
            else
            {
                cupPos = camera.Position + forward * 0.48f + right * 0.22f - up * (0.22f - bob);
            }

            float fillRatio = Math.Clamp(cupFillAmount, 0f, 1f);
            if (fillRatio > 0.015f)
            {
                float innerBottomR = cupBottomR - 0.0025f;
                float innerTopMaxR = cupTopR - 0.0025f;
                float liquidTopR = innerBottomR + (innerTopMaxR - innerBottomR) * fillRatio;
                float liquidH = fillRatio * (cupH - 0.008f);

                Raylib.DrawCylinder(
                    cupPos + new Vector3(0f, 0.003f, 0f),
                    liquidTopR,
                    innerBottomR,
                    liquidH,
                    16,
                    cupLiquidColor
                );
            }

            Raylib.DrawCylinder(cupPos, cupTopR, cupBottomR, cupH, 16, new Color(235, 244, 252, 115));
            Raylib.DrawCylinderWires(cupPos, cupTopR, cupBottomR, cupH, 16, new Color(210, 222, 235, 175));
        }
        else if (heldItem == HeldItemType.Broom || heldItem == HeldItemType.Mop)
        {
            Vector3 toolPos = camera.Position + forward * 0.52f + right * 0.26f - up * (0.35f - bob);
            Color shaftCol = heldItem == HeldItemType.Broom ? new Color(165, 118, 68, 255) : new Color(65, 125, 195, 255);
            Raylib.DrawCylinder(toolPos, 0.016f, 0.016f, 0.75f, 8, shaftCol);
        }
        else if (heldItem == HeldItemType.TrashBag)
        {
            Vector3 bagPos = camera.Position + forward * 0.52f + right * 0.24f - up * (0.34f - bob);
            Raylib.DrawSphere(bagPos, 0.19f, new Color(24, 24, 28, 255));
            Raylib.DrawCylinder(bagPos + new Vector3(0f, 0.16f, 0f), 0.05f, 0.08f, 0.09f, 8, new Color(36, 36, 42, 255));
        }
    }

    static void DrawSkullmugKeyPrompt()
    {
        if (string.IsNullOrEmpty(skullPromptKey) || isTalking || isWorkCutscene || isDawnCutscene) return;

        int keyW = skullPromptKey.Length > 2 ? 78 : 44;
        int keyH = 42;
        int textW = (int)Raylib.MeasureTextEx(viFont, skullPromptText, 22, 1f).X;
        int totalW = keyW + 14 + textW;
        int startX = (ScreenWidth - totalW) / 2;
        int startY = ScreenHeight / 2 + 28;

        Raylib.DrawRectangle(startX - 3, startY - 3, keyW + 6, keyH + 6, Color.Black);
        Raylib.DrawRectangle(startX, startY, keyW, keyH, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(new Rectangle(startX, startY, keyW, keyH), 2.5f, new Color(35, 35, 40, 255));

        int kTxtW = (int)Raylib.MeasureTextEx(viFont, skullPromptKey, 21, 1f).X;
        DrawViText(skullPromptKey, startX + (keyW - kTxtW) / 2, startY + 9, 21, Color.Black);

        if (skullPromptBlocked)
        {
            int cx = startX + keyW / 2;
            int cy = startY + keyH / 2;
            int r = 22;
            for (int t = 0; t < 4; t++)
                Raylib.DrawCircleLines(cx, cy, r - t * 0.6f, new Color(225, 28, 28, 255));
            Raylib.DrawLineEx(new Vector2(cx - 14, cy - 14), new Vector2(cx + 14, cy + 14), 4.5f, new Color(225, 28, 28, 255));
        }

        int tx = startX + keyW + 14;
        int ty = startY + 9;
        Color labelCol = skullPromptBlocked ? new Color(255, 110, 110, 255) : Color.White;
        DrawViText(skullPromptText, tx - 2, ty, 22, Color.Black);
        DrawViText(skullPromptText, tx + 2, ty, 22, Color.Black);
        DrawViText(skullPromptText, tx, ty - 2, 22, Color.Black);
        DrawViText(skullPromptText, tx, ty + 2, 22, Color.Black);
        DrawViText(skullPromptText, tx, ty, 22, labelCol);
    }

    const float NavCellSize = 0.35f;
    const float NavMinX = -9.80f;
    const float NavMaxX = 9.80f;
    const float NavMinZ = -6.85f;
    const float NavMaxZ = 12.25f;
    static readonly int NavCols = (int)MathF.Ceiling((NavMaxX - NavMinX) / NavCellSize);
    static readonly int NavRows = (int)MathF.Ceiling((NavMaxZ - NavMinZ) / NavCellSize);
    static readonly bool[,] navBlocked = new bool[NavCols, NavRows];
    static bool navGridBuilt = false;

    static void BuildStoreNavGrid()
    {
        Array.Clear(navBlocked);

        List<BoundingBox> obstacles =
        [
            new BoundingBox(new Vector3(-3.16f, 0f, -2.56f), new Vector3(0.36f, 1.5f, -1.72f)),
            new BoundingBox(new Vector3(-3.18f, 0f, -5.36f), new Vector3(-2.32f, 1.5f, -1.72f)),
            new BoundingBox(new Vector3(0.47f, 0f, -2.62f), new Vector3(1.23f, 1.5f, -1.88f)),
            new BoundingBox(new Vector3(-10.5f, 0f, -3.40f), new Vector3(-9.20f, 2.0f, 3.00f)),
            new BoundingBox(new Vector3(-8.06f, 0f, -1.36f), new Vector3(-6.64f, 1.8f, 2.36f)),
            new BoundingBox(new Vector3(1.22f, 0f, 4.44f), new Vector3(2.18f, 1.8f, 5.36f)),
            new BoundingBox(new Vector3(9.55f, 0f, -5.15f), new Vector3(10.5f, 2.6f, 5.95f)),
            new BoundingBox(new Vector3(-10.5f, 0f, 7.25f), new Vector3(-0.82f, 3.8f, 7.75f)),
            new BoundingBox(new Vector3(0.82f, 0f, 7.25f), new Vector3(10.5f, 3.8f, 7.75f))
        ];

        obstacles.AddRange(customModelColliders);
        var placements = GetStoreShelfPlacements();
        for (int s = 0; s < placements.Length && s < ShelfCount; s++)
        {
            obstacles.Add(GetCustomModelBounds("shelf", placements[s].Pos, placements[s].Yaw));
        }

        const float charRadius = 0.38f;
        for (int c = 0; c < NavCols; c++)
        {
            float wx = NavMinX + (c + 0.5f) * NavCellSize;
            for (int r = 0; r < NavRows; r++)
            {
                float wz = NavMinZ + (r + 0.5f) * NavCellSize;

                if (wz > 7.15f && MathF.Abs(wx) > 0.78f)
                {
                    navBlocked[c, r] = true;
                    continue;
                }

                bool hit = false;
                foreach (var b in obstacles)
                {
                    if (wx >= b.Min.X - charRadius && wx <= b.Max.X + charRadius &&
                        wz >= b.Min.Z - charRadius && wz <= b.Max.Z + charRadius)
                    {
                        hit = true;
                        break;
                    }
                }
                navBlocked[c, r] = hit;
            }
        }

        navGridBuilt = true;
    }

    static (int Col, int Row) WorldToNavCell(Vector3 pos)
    {
        int c = Math.Clamp((int)((pos.X - NavMinX) / NavCellSize), 0, NavCols - 1);
        int r = Math.Clamp((int)((pos.Z - NavMinZ) / NavCellSize), 0, NavRows - 1);
        return (c, r);
    }

    static Vector3 NavCellToWorld(int c, int r) =>
        new(NavMinX + (c + 0.5f) * NavCellSize, 0f, NavMinZ + (r + 0.5f) * NavCellSize);

    static (int Col, int Row) FindNearestWalkableCell(int startC, int startR)
    {
        if (!navBlocked[startC, startR]) return (startC, startR);
        for (int rad = 1; rad <= 12; rad++)
        {
            int bestC = -1, bestR = -1;
            float bestD = float.MaxValue;
            for (int dc = -rad; dc <= rad; dc++)
            {
                for (int dr = -rad; dr <= rad; dr++)
                {
                    int nc = startC + dc;
                    int nr = startR + dr;
                    if (nc < 0 || nc >= NavCols || nr < 0 || nr >= NavRows) continue;
                    if (navBlocked[nc, nr]) continue;
                    float d = dc * dc + dr * dr;
                    if (d < bestD)
                    {
                        bestD = d;
                        bestC = nc;
                        bestR = nr;
                    }
                }
            }
            if (bestC >= 0) return (bestC, bestR);
        }
        return (startC, startR);
    }

    static List<Vector3> ComputeAStarPath(Vector3 fromWorld, Vector3 toWorld)
    {
        if (!navGridBuilt) BuildStoreNavGrid();

        var start = FindNearestWalkableCell(WorldToNavCell(fromWorld).Col, WorldToNavCell(fromWorld).Row);
        var goal = FindNearestWalkableCell(WorldToNavCell(toWorld).Col, WorldToNavCell(toWorld).Row);

        if (start == goal)
            return [NavCellToWorld(goal.Col, goal.Row)];

        int total = NavCols * NavRows;
        float[] gScore = new float[total];
        int[] cameFrom = new int[total];
        bool[] closed = new bool[total];
        Array.Fill(gScore, float.MaxValue);
        Array.Fill(cameFrom, -1);

        int startIdx = start.Col * NavRows + start.Row;
        int goalIdx = goal.Col * NavRows + goal.Row;
        gScore[startIdx] = 0f;

        PriorityQueue<int, float> open = new();
        open.Enqueue(startIdx, 0f);

        int[] dCol = [1, -1, 0, 0, 1, 1, -1, -1];
        int[] dRow = [0, 0, 1, -1, 1, -1, 1, -1];
        float[] stepCost = [1f, 1f, 1f, 1f, 1.414f, 1.414f, 1.414f, 1.414f];

        while (open.Count > 0)
        {
            int cur = open.Dequeue();
            if (cur == goalIdx) break;
            if (closed[cur]) continue;
            closed[cur] = true;

            int cc = cur / NavRows;
            int cr = cur % NavRows;

            for (int i = 0; i < 8; i++)
            {
                int nc = cc + dCol[i];
                int nr = cr + dRow[i];
                if (nc < 0 || nc >= NavCols || nr < 0 || nr >= NavRows) continue;
                if (navBlocked[nc, nr]) continue;

                if (i >= 4 && (navBlocked[cc + dCol[i], cr] || navBlocked[cc, cr + dRow[i]]))
                    continue;

                int nIdx = nc * NavRows + nr;
                if (closed[nIdx]) continue;

                float candG = gScore[cur] + stepCost[i];
                if (candG < gScore[nIdx])
                {
                    gScore[nIdx] = candG;
                    cameFrom[nIdx] = cur;
                    float h = MathF.Sqrt((nc - goal.Col) * (nc - goal.Col) + (nr - goal.Row) * (nr - goal.Row));
                    open.Enqueue(nIdx, candG + h);
                }
            }
        }

        List<Vector3> path = [];
        int trace = goalIdx;
        if (cameFrom[trace] == -1 && trace != startIdx)
        {
            path.Add(NavCellToWorld(goal.Col, goal.Row));
            return path;
        }

        while (trace != -1 && trace != startIdx)
        {
            path.Add(NavCellToWorld(trace / NavRows, trace % NavRows));
            trace = cameFrom[trace];
        }
        path.Reverse();

        if (path.Count > 2)
        {
            List<Vector3> smoothed = [path[0]];
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector3 prev = smoothed[^1];
                Vector3 next = path[i + 1];
                Vector3 d1 = Vector3.Normalize(path[i] - prev);
                Vector3 d2 = Vector3.Normalize(next - path[i]);
                if (Vector3.Dot(d1, d2) < 0.995f)
                    smoothed.Add(path[i]);
            }
            smoothed.Add(path[^1]);
            return smoothed;
        }

        return path;
    }

    static Vector3 GetShelfStandPosition(int shelfIndex)
    {
        if (!navGridBuilt) BuildStoreNavGrid();
        var placements = GetStoreShelfPlacements();
        if (shelfIndex < 0 || shelfIndex >= placements.Length)
            return new Vector3(0f, 0f, 2.0f);

        BoundingBox box = GetCustomModelBounds("shelf", placements[shelfIndex].Pos, placements[shelfIndex].Yaw);
        Vector3 center = (box.Min + box.Max) * 0.5f;

        Vector3[] candidates =
        [
            new Vector3(box.Min.X - 0.58f, 0f, center.Z),
            new Vector3(box.Max.X + 0.58f, 0f, center.Z),
            new Vector3(center.X, 0f, box.Max.Z + 0.58f),
            new Vector3(center.X, 0f, box.Min.Z - 0.58f)
        ];

        Vector3 bestPos = candidates[0];
        float bestDist = float.MaxValue;
        foreach (var cand in candidates)
        {
            var cell = WorldToNavCell(cand);
            if (!navBlocked[cell.Col, cell.Row])
            {
                float d = Vector3.DistanceSquared(shopperPos, cand);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestPos = NavCellToWorld(cell.Col, cell.Row);
                }
            }
        }

        if (bestDist < float.MaxValue)
            return bestPos;

        var fallback = FindNearestWalkableCell(WorldToNavCell(candidates[0]).Col, WorldToNavCell(candidates[0]).Row);
        return NavCellToWorld(fallback.Col, fallback.Row);
    }

    enum ShopperState
    {
        Inactive,
        ToShelf,
        PickingShelf,
        ToCounter,
        WaitingCheckout,
        Leaving
    }

    enum ShopperAccessory
    {
        None,
        Cap,
        Backpack,
        CrossbodyBag,
        Headphones
    }

    enum ShopperArchetypeKind
    {
        StudentCouple,
        RudeCustomer,
        EveningGirl,
        NightRider
    }

    static ShopperState shopperState = ShopperState.Inactive;
    static ShopperArchetypeKind shopperKind = ShopperArchetypeKind.EveningGirl;
    static Vector3 shopperPos;
    static float shopperYaw = 180f;
    static float shopperWalkPhase = 0f;
    static float shopperSwing = 0f;
    static Color shopperShirtCol;
    static Color shopperHairCol;
    static Color shopperAccCol;
    static FearsBodySpec shopperSpec;
    static FaceId shopperFaceId;
    static ShopperAccessory shopperAccessory = ShopperAccessory.None;

    static bool hasCompanionShopper = false;
    static Vector3 companionPos;
    static float companionYaw = 180f;
    static float companionWalkPhase = 0f;
    static float companionSwing = 0f;
    static Color companionShirtCol;
    static Color companionHairCol;
    static Color companionAccCol;
    static FearsBodySpec companionSpec;
    static FaceId companionFaceId;
    static ShopperAccessory companionAccessory = ShopperAccessory.CrossbodyBag;

    static string shopperTitle = "Khách";
    static string shopperSpeechText = "";
    static float shopperSpeechTimer = 0f;
    static bool rudeConfronted = false;
    static int shopperRotationIndex = 0;

    static readonly Queue<Vector3> shopperWaypoints = new();
    static int shopperTargetShelf = 0;
    static int shopperShelvesRemaining = 0;
    static float shopperActionTimer = 0f;
    static float shopperSpawnCooldown = 3.5f;
    static int counterScanItems = 0;
    static int shopperBasketCount = 0;

    static bool IsShopperIdleOrLeaving() =>
        shopperState == ShopperState.Inactive || shopperState == ShopperState.Leaving;

    static void SayShopperLine(string text, float duration = 4.6f)
    {
        shopperSpeechText = text;
        shopperSpeechTimer = duration;
    }

    static void ResetShopperState()
    {
        shopperState = ShopperState.Inactive;
        hasCompanionShopper = false;
        rudeConfronted = false;
        shopperRotationIndex = 0;
        shopperWaypoints.Clear();
        counterScanItems = 0;
        shopperBasketCount = 0;
        baggedItemCount = 0;
        shopperSpeechText = "";
        shopperSpeechTimer = 0f;
        shopperSpawnCooldown = 3.0f;
    }

    static void SpawnRegularShopper()
    {
        if (!navGridBuilt) BuildStoreNavGrid();

        ShopperArchetypeKind[] order =
        [
            ShopperArchetypeKind.StudentCouple,
            ShopperArchetypeKind.RudeCustomer,
            ShopperArchetypeKind.EveningGirl,
            ShopperArchetypeKind.NightRider
        ];

        shopperKind = order[shopperRotationIndex % order.Length];
        shopperRotationIndex++;

        hasCompanionShopper = false;
        rudeConfronted = false;
        shopperPos = new Vector3(0f, 0f, 11.8f);
        shopperYaw = 180f;
        shopperBasketCount = 0;
        counterScanItems = 0;
        baggedItemCount = 0;
        shopperShelvesRemaining = rng.Next(1, 3);
        shopperState = ShopperState.ToShelf;

        if (shopperKind == ShopperArchetypeKind.StudentCouple)
        {
            shopperTitle = "Cặp đôi học sinh";
            shopperShirtCol = new Color(235, 238, 244, 255);
            shopperHairCol = new Color(28, 28, 32, 255);
            shopperAccCol = new Color(42, 78, 145, 255);
            shopperSpec = FearsBodySpec.StaffGuy;
            shopperFaceId = FaceId.None;
            shopperAccessory = ShopperAccessory.Backpack;

            hasCompanionShopper = true;
            companionPos = new Vector3(0.25f, 0f, 12.25f);
            companionYaw = 180f;
            companionShirtCol = new Color(242, 210, 220, 255);
            companionHairCol = new Color(36, 26, 24, 255);
            companionAccCol = new Color(220, 95, 125, 255);
            companionSpec = FearsBodySpec.StaffGirl;
            companionFaceId = FaceId.Girl;
            companionAccessory = ShopperAccessory.CrossbodyBag;

            SayShopperLine("Hôm nay tan học thêm trễ, hai đứa mình mua snack với trà sữa nha!", 5.0f);
        }
        else if (shopperKind == ShopperArchetypeKind.RudeCustomer)
        {
            shopperTitle = "Khách mặt hầm hầm";
            shopperShirtCol = new Color(42, 42, 46, 255);
            shopperHairCol = new Color(25, 25, 28, 255);
            shopperAccCol = new Color(185, 35, 35, 255);
            shopperSpec = FearsBodySpec.HungNpc;
            shopperFaceId = FaceId.StrangeMan;
            shopperAccessory = ShopperAccessory.Cap;

            SayShopperLine("Tiệm gì mà vắng tanh vậy, có bán đồ ăn nóng không hả?!", 5.0f);
        }
        else if (shopperKind == ShopperArchetypeKind.EveningGirl)
        {
            shopperTitle = "Cô gái tan làm";
            shopperShirtCol = new Color(168, 108, 198, 255);
            shopperHairCol = new Color(42, 30, 25, 255);
            shopperAccCol = new Color(238, 240, 246, 255);
            shopperSpec = FearsBodySpec.MaiNpc;
            shopperFaceId = FaceId.Girl;
            shopperAccessory = ShopperAccessory.Headphones;

            SayShopperLine("Chào anh ơi, giờ này tiệm mình vẫn còn bánh ngọt với nước ép chứ ạ?", 5.0f);
        }
        else
        {
            shopperTitle = "Tài xế công nghệ";
            shopperShirtCol = new Color(42, 152, 88, 255);
            shopperHairCol = new Color(30, 30, 34, 255);
            shopperAccCol = new Color(245, 198, 48, 255);
            shopperSpec = FearsBodySpec.StaffGuy;
            shopperFaceId = FaceId.None;
            shopperAccessory = ShopperAccessory.Cap;

            SayShopperLine("Chạy cuốc tối đói bụng quá, ghé làm ổ bánh mì với chai nước!", 4.8f);
        }

        EnqueueNextShelfRoute();
    }

    static void ResolveRudeShopperChoice(bool polite)
    {
        rudeConfronted = true;
        if (polite)
        {
            greetedCount++;
            SayShopperLine("Hừ, tạm được! Lần sau nhớ để bảng giá cho rõ ràng vào.", 4.5f);
        }
        else
        {
            SayShopperLine("Cái thái độ gì đây?! Quét lẹ đi rồi tao còn về!", 4.5f);
        }
    }

    static void EnqueueAStarRoute(Vector3 destination)
    {
        shopperWaypoints.Clear();
        var path = ComputeAStarPath(shopperPos, destination);
        foreach (var pt in path)
            shopperWaypoints.Enqueue(pt);
    }

    static void EnqueueNextShelfRoute()
    {
        int chosen = rng.Next(ShelfCount);
        for (int attempt = 0; attempt < ShelfCount; attempt++)
        {
            int idx = (chosen + attempt) % ShelfCount;
            if (GetShelfFilledCount(idx) > 4)
            {
                chosen = idx;
                break;
            }
        }
        shopperTargetShelf = chosen;
        Vector3 standPos = GetShelfStandPosition(chosen);
        EnqueueAStarRoute(standPos);
    }

    static void EnqueueCounterRoute()
    {
        EnqueueAStarRoute(new Vector3(-1.10f, 0f, -1.15f));
    }

    static void EnqueueExitRoute()
    {
        EnqueueAStarRoute(new Vector3(0f, 0f, 12.1f));
    }

    static void UpdateCompanionFollow(float dt)
    {
        if (!hasCompanionShopper) return;

        Vector3 desiredPos;
        if (shopperState == ShopperState.WaitingCheckout)
        {
            desiredPos = new Vector3(-0.55f, 0f, -1.15f);
        }
        else
        {
            float rad = shopperYaw * MathF.PI / 180f;
            Vector3 backDir = new(-MathF.Sin(rad), 0f, -MathF.Cos(rad));
            desiredPos = shopperPos + backDir * 0.55f;
        }

        Vector3 diff = desiredPos - companionPos;
        diff.Y = 0f;
        float dist = diff.Length();
        if (dist > 0.05f)
        {
            companionPos += Vector3.Normalize(diff) * Math.Min(dist, 2.05f * dt);
            companionYaw = MathF.Atan2(diff.X, diff.Z) * 180f / MathF.PI;
            companionWalkPhase += dt * 8.8f;
            companionSwing = 25f;
        }
        else
        {
            companionSwing = 0f;
            if (shopperState == ShopperState.WaitingCheckout)
                companionYaw = 180f;
        }
    }

    static bool MoveShopperAlongWaypoints(float dt, float speed = 1.95f)
    {
        if (shopperWaypoints.Count == 0)
        {
            shopperSwing = 0f;
            return true;
        }

        Vector3 target = shopperWaypoints.Peek();
        Vector3 dir = target - shopperPos;
        dir.Y = 0f;
        float dist = dir.Length();

        if (dist <= 0.06f)
        {
            shopperPos = new Vector3(target.X, 0f, target.Z);
            shopperWaypoints.Dequeue();
            return shopperWaypoints.Count == 0;
        }

        shopperPos += Vector3.Normalize(dir) * Math.Min(dist, speed * dt);
        shopperYaw = MathF.Atan2(dir.X, dir.Z) * 180f / MathF.PI;
        shopperWalkPhase += dt * 8.5f;
        shopperSwing = 26f;
        return false;
    }

    static void UpdateRegularShopper(float dt)
    {
        if (shopperSpeechTimer > 0f)
            shopperSpeechTimer = Math.Max(0f, shopperSpeechTimer - dt);

        if (isWorkCutscene || isDawnCutscene || gameEnded) return;

        if (shopperState == ShopperState.Inactive)
        {
            bool canSpawn = !isDoorLocked && !npcActive && !isTalking &&
                            ((storyStep == 0 && clockMinutes < 1438f) || (storyStep >= 1 && storyStep <= 2));
            if (canSpawn)
            {
                shopperSpawnCooldown -= dt;
                if (shopperSpawnCooldown <= 0f)
                {
                    SpawnRegularShopper();
                }
            }
            return;
        }

        UpdateCompanionFollow(dt);

        if (shopperState == ShopperState.ToShelf)
        {
            if (MoveShopperAlongWaypoints(dt))
            {
                var placements = GetStoreShelfPlacements();
                if (shopperTargetShelf >= 0 && shopperTargetShelf < placements.Length)
                {
                    Vector3 toShelf = placements[shopperTargetShelf].Pos - shopperPos;
                    shopperYaw = MathF.Atan2(toShelf.X, toShelf.Z) * 180f / MathF.PI;
                }
                shopperActionTimer = 1.8f;
                shopperState = ShopperState.PickingShelf;
                if (shopperKind == ShopperArchetypeKind.StudentCouple)
                    SayShopperLine("Lấy thêm gói bim bim cay với hai chai nước nha!", 3.5f);
            }
        }
        else if (shopperState == ShopperState.PickingShelf)
        {
            shopperSwing = 0f;
            shopperActionTimer -= dt;
            if (shopperActionTimer <= 0f)
            {
                int takeCount = shopperKind == ShopperArchetypeKind.StudentCouple ? rng.Next(3, 5) : rng.Next(2, 4);
                for (int k = 0; k < takeCount; k++)
                {
                    if (TakeOneProductFromShelf(shopperTargetShelf))
                        shopperBasketCount++;
                }

                if (rng.NextDouble() < 0.35 && floorMesses.Count < 8)
                {
                    floorMesses.Add(new FloorMess
                    {
                        Pos = new Vector3(shopperPos.X, 0.006f, shopperPos.Z),
                        IsLiquid = rng.Next(2) == 0
                    });
                }

                shopperShelvesRemaining--;
                if (shopperShelvesRemaining > 0)
                {
                    EnqueueNextShelfRoute();
                    shopperState = ShopperState.ToShelf;
                }
                else
                {
                    EnqueueCounterRoute();
                    shopperState = ShopperState.ToCounter;
                }
            }
        }
        else if (shopperState == ShopperState.ToCounter)
        {
            if (MoveShopperAlongWaypoints(dt))
            {
                shopperYaw = 180f;
                counterScanItems = Math.Max(1, shopperBasketCount);
                baggedItemCount = 0;
                shopperState = ShopperState.WaitingCheckout;

                if (shopperKind == ShopperArchetypeKind.RudeCustomer)
                    SayShopperLine("Ê nhóc, quét lẹ mấy món này coi! Tao không có thời gian đứng chờ!", 6.0f);
                else if (shopperKind == ShopperArchetypeKind.StudentCouple)
                    SayShopperLine($"Anh ơi tính tiền giúp tụi em {counterScanItems} món này với ạ!", 5.5f);
                else
                    SayShopperLine($"Anh tính tiền giúp em {counterScanItems} món này nhé!", 5.5f);
            }
        }
        else if (shopperState == ShopperState.WaitingCheckout)
        {
            shopperSwing = 0f;

            if (!isTalking && skullPromptKey == "")
            {
                Vector3 lookDir = Vector3.Normalize(camera.Target - camera.Position);
                Ray eyeRay = new(camera.Position, lookDir);

                BoundingBox rudeBox = new(shopperPos - new Vector3(0.4f, 0f, 0.4f), shopperPos + new Vector3(0.4f, 1.85f, 0.4f));
                RayCollision rudeHit = (shopperKind == ShopperArchetypeKind.RudeCustomer && !rudeConfronted)
                    ? Raylib.GetRayCollisionBox(eyeRay, rudeBox)
                    : new RayCollision { Hit = false };

                BoundingBox scanZone = new(new Vector3(-1.35f, 0.95f, -2.35f), new Vector3(-0.35f, 1.45f, -1.65f));
                RayCollision hit = Raylib.GetRayCollisionBox(eyeRay, scanZone);

                if (rudeHit.Hit && rudeHit.Distance <= 2.9f)
                {
                    skullPromptKey = "F";
                    skullPromptText = "ĐỐI ĐÁP VỚI KHÁCH GÂY SỰ";
                    if (Raylib.IsKeyPressed(KeyboardKey.F))
                    {
                        StartDialogue(
                            "Khách mặt hầm hầm",
                            "Nhìn gì mà nhìn?! Quán xá thì lèo tèo mấy món, nhân viên thì lề mề. Tính tiền lẹ coi!",
                            1,
                            [
                                "Dạ anh thông cảm, để em quét mã ngay cho anh ạ.",
                                "Anh nói chuyện lịch sự chút đi, em đang tính tiền cho anh đây."
                            ],
                            mode: "rude_shopper"
                        );
                    }
                }
                else if (hit.Hit && hit.Distance <= 2.7f && counterScanItems > 0)
                {
                    skullPromptKey = "F";
                    skullPromptText = $"QUÉT MÃ & BỎ TÚI (CÒN {counterScanItems} MÓN)";
                    if (Raylib.IsKeyPressed(KeyboardKey.F))
                    {
                        Color[] packCols =
                        [
                            new(215, 65, 65, 255),
                            new(55, 145, 205, 255),
                            new(225, 175, 55, 255),
                            new(75, 165, 95, 255)
                        ];
                        scanSweepItemColor = packCols[(counterScanItems - 1) % packCols.Length];
                        scanSweepAnim = 1.0f;
                        baggedItemCount++;
                        counterScanItems--;
                        handActionAnim = 1f;
                        Raylib.SetSoundPitch(voiceStaticSound, 1.65f);
                        Raylib.PlaySound(voiceStaticSound);

                        if (counterScanItems <= 0)
                        {
                            if (shopperKind == ShopperArchetypeKind.RudeCustomer)
                                SayShopperLine("Lần sau làm ăn cho nhanh nhẹn lên!", 3.8f);
                            else if (shopperKind == ShopperArchetypeKind.StudentCouple)
                                SayShopperLine("Tụi em cảm ơn anh nhiều ạ! Đi thôi cậu ơi!", 4.2f);
                            else
                                SayShopperLine("Cảm ơn anh nhiều nha, chúc anh buổi tối vui vẻ!", 4.0f);

                            EnqueueExitRoute();
                            shopperState = ShopperState.Leaving;
                        }
                    }
                }
            }
        }
        else if (shopperState == ShopperState.Leaving)
        {
            if (MoveShopperAlongWaypoints(dt, 2.15f))
            {
                shopperState = ShopperState.Inactive;
                hasCompanionShopper = false;
                baggedItemCount = 0;
                shopperSpawnCooldown = clockMinutes < 1435f ? 4.2f : 8.5f;
            }
        }
    }

    static void DrawSingleCharacterAccessories(Vector3 pos, float yaw, float hScale, ShopperAccessory acc, Color accCol, bool holdingBag)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(pos.X, pos.Y, pos.Z);
        Rlgl.Rotatef(yaw, 0f, 1f, 0f);

        if (acc == ShopperAccessory.Backpack)
        {
            Raylib.DrawCube(new Vector3(0f, 1.18f * hScale, -0.17f), 0.28f, 0.34f, 0.14f, accCol);
            Raylib.DrawCube(new Vector3(0f, 1.12f * hScale, -0.24f), 0.22f, 0.18f, 0.04f, new Color(35, 35, 40, 255));
        }
        else if (acc == ShopperAccessory.CrossbodyBag)
        {
            Raylib.DrawCube(new Vector3(0.19f, 0.96f * hScale, -0.04f), 0.08f, 0.16f, 0.20f, accCol);
            Raylib.DrawCube(new Vector3(0f, 1.22f * hScale, 0.12f), 0.26f, 0.03f, 0.02f, accCol);
        }
        else if (acc == ShopperAccessory.Cap)
        {
            float headY = 1.69f * hScale;
            Raylib.DrawCylinder(new Vector3(0f, headY, 0f), 0.135f, 0.135f, 0.07f, 12, accCol);
            Raylib.DrawCube(new Vector3(0f, headY + 0.005f, 0.13f), 0.18f, 0.015f, 0.14f, accCol);
        }
        else if (acc == ShopperAccessory.Headphones)
        {
            float earY = 1.60f * hScale;
            Raylib.DrawCylinder(new Vector3(-0.14f, earY, 0f), 0.045f, 0.045f, 0.03f, 10, accCol);
            Raylib.DrawCylinder(new Vector3(0.14f, earY, 0f), 0.045f, 0.045f, 0.03f, 10, accCol);
            Raylib.DrawCube(new Vector3(0f, 1.73f * hScale, 0f), 0.28f, 0.025f, 0.03f, accCol);
        }

        if (holdingBag)
        {
            Raylib.DrawCube(new Vector3(-0.24f, 0.78f * hScale, 0.08f), 0.16f, 0.22f, 0.12f, new Color(235, 240, 245, 230));
        }

        Rlgl.PopMatrix();
    }

    static void DrawShopper3D()
    {
        if (shopperState == ShopperState.Inactive) return;

        Props.DrawFearsCharacter(
            shopperPos,
            shopperYaw,
            shopperShirtCol,
            shopperHairCol,
            shopperSpec,
            shopperWalkPhase,
            shopperSwing,
            faceId: shopperFaceId
        );
        DrawSingleCharacterAccessories(
            shopperPos,
            shopperYaw,
            shopperSpec.HeightScale,
            shopperAccessory,
            shopperAccCol,
            shopperState == ShopperState.Leaving && baggedItemCount > 0
        );

        if (hasCompanionShopper)
        {
            Props.DrawFearsCharacter(
                companionPos,
                companionYaw,
                companionShirtCol,
                companionHairCol,
                companionSpec,
                companionWalkPhase,
                companionSwing,
                faceId: companionFaceId
            );
            DrawSingleCharacterAccessories(
                companionPos,
                companionYaw,
                companionSpec.HeightScale,
                companionAccessory,
                companionAccCol,
                false
            );
        }

        Color[] packCols =
        [
            new(215, 65, 65, 255),
            new(55, 145, 205, 255),
            new(225, 175, 55, 255),
            new(75, 165, 95, 255)
        ];

        if (shopperState == ShopperState.WaitingCheckout && counterScanItems > 0)
        {
            for (int i = 0; i < counterScanItems && i < 6; i++)
            {
                float ox = -0.56f + (i % 2) * 0.14f;
                float oz = -2.06f + (i / 2) * 0.13f;
                Raylib.DrawCube(new Vector3(ox, 1.11f, oz), 0.10f, 0.12f, 0.09f, packCols[i % packCols.Length]);
            }
        }

        if (shopperState == ShopperState.WaitingCheckout && baggedItemCount > 0)
        {
            float bagH = 0.14f + Math.Min(4, baggedItemCount) * 0.025f;
            Vector3 bagPos = new(-1.24f, 1.05f + bagH * 0.5f, -2.06f);
            Raylib.DrawCube(bagPos, 0.22f, bagH, 0.16f, new Color(238, 244, 250, 215));
            Raylib.DrawCubeWires(bagPos, 0.222f, bagH, 0.162f, new Color(165, 180, 195, 255));
            Raylib.DrawCube(bagPos + new Vector3(0f, bagH * 0.5f + 0.03f, -0.06f), 0.10f, 0.06f, 0.012f, new Color(225, 232, 240, 235));
            Raylib.DrawCube(bagPos + new Vector3(0f, bagH * 0.5f + 0.03f, 0.06f), 0.10f, 0.06f, 0.012f, new Color(225, 232, 240, 235));
        }
    }

    static void DrawShopperSpeechBubbleHUD()
    {
        if (shopperState == ShopperState.Inactive || shopperSpeechTimer <= 0f || string.IsNullOrEmpty(shopperSpeechText))
            return;

        Vector3 headWorld = shopperPos + new Vector3(0f, 1.95f * shopperSpec.HeightScale, 0f);
        Vector3 toHead = headWorld - camera.Position;
        Vector3 camForward = Vector3.Normalize(camera.Target - camera.Position);
        if (Vector3.Dot(toHead, camForward) <= 0.1f) return;

        Vector2 screenPos = Raylib.GetWorldToScreen(headWorld, camera);
        string fullLine = $"{shopperTitle}: \"{shopperSpeechText}\"";
        int txtW = (int)Raylib.MeasureTextEx(viFont, fullLine, 19, 1f).X;
        int padX = 14;
        int boxW = txtW + padX * 2;
        int boxH = 36;
        int bx = Math.Clamp((int)screenPos.X - boxW / 2, 16, ScreenWidth - boxW - 16);
        int by = Math.Clamp((int)screenPos.Y - 46, 95, ScreenHeight - 140);

        Color borderCol = shopperKind == ShopperArchetypeKind.RudeCustomer
            ? new Color(255, 110, 95, 235)
            : new Color(120, 195, 255, 220);

        Raylib.DrawRectangle(bx, by, boxW, boxH, new Color(15, 18, 24, 215));
        Raylib.DrawRectangleLinesEx(new Rectangle(bx, by, boxW, boxH), 1.5f, borderCol);
        DrawViText(fullLine, bx + padX, by + 8, 19, Color.RayWhite);
    }
}