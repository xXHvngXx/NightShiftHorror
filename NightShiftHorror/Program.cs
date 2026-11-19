using System.Numerics;
using Raylib_cs;

partial class Program
{
    const int ScreenWidth = 1280;
    const int ScreenHeight = 720;
    const float NormalFov = 65.0f;
    const float TalkNpcFov = 58.0f;
    const float StepInterval = 0.46f;
    const float TypingSpeed = 38f;

    static RenderTexture2D sceneTarget;
    static Font viFont;
    static readonly Random rng = new();

    static Vector3 playerPos = new(-1.2f, 0f, -3.5f);
    static float camYaw = 0f;
    static float camPitch = -0.05f;
    static float currentBobOffset = 0f;

    static Camera3D camera = new()
    {
        Position = new Vector3(0.0f, 1.32f, -1.15f),
        Target = new Vector3(0.0f, 1.25f, 0.8f),
        Up = new Vector3(0.0f, 1.0f, 0.0f),
        FovY = NormalFov,
        Projection = CameraProjection.Perspective
    };

    static float stepTimer = 0f;
    static float walkBobTimer = 0f;

    static bool lieToMom = false;
    static bool readPoster = false;
    static bool readLogbook = false;
    static bool readMargin = false;
    static bool nameTag = false;
    static bool talkedMai = false;
    static bool recordingOn = false;
    static bool sharedLocation = false;
    static bool spoofAlertShown = false;
    static bool calledRealDung = false;
    static int greetedCount = 0;

    static int gameScene = -1;
    static int cafeIndex = 0;
    static int cafeSubStep = 0;
    static bool isFadingOutCafe = false;
    static bool isCafeCutscene = false;
    static float cafeCutsceneTimer = 0f;
    static int introIndex = 0;
    static float fadeAlpha = 1.0f;

    static bool isPaused = false;
    static int pauseSelection = 0;
    static readonly string[] pauseOptions = ["Tiếp tục ca trực", "Chơi lại từ đầu", "Thoát trò chơi"];

    static int storyStep = -1;
    static float clockMinutes = 1428f;
    static string currentTime = "23:48";
    static string currentObjective = "Nói chuyện với Quản lý để nhận bàn giao ca";
    static bool isDoorLocked = false;
    static bool lightsOn = true;
    static bool noticedAnomaly = false;
    static float flickerTimer = 0f;
    static float phoneRingTimer = 0f;
    static float eventDelayTimer = 0f;
    static float heartbeatTimer = 0f;
    static float cameraShakeTimer = 0f;
    static int endingType = 0;

    static bool isWorkCutscene = false;
    static float workCutsceneTimer = 0f;

    static bool isDawnCutscene = false;
    static float dawnCutsceneTimer = 0f;
    static float dawnProgress = 0f;

    static bool npcActive = false;
    static Vector3 npcPos = new(0f, 0f, 13.5f);
    static Vector3 npcTarget = new(0f, 0f, 13.5f);
    static Color npcShirtColor = Color.Blue;
    static Color npcHairColor = new(35, 35, 40, 255);
    static bool npcLeaving = false;
    static bool npcShouldLeaveAfterDialogue = false;
    static float npcYaw = 180f, npcWalkPhase = 0f, npcSwing = 0f;
    static FaceId npcFaceId = FaceId.None;
    static FearsBodySpec npcSpec = FearsBodySpec.Manager;

    static float npcGazeWeight = 0f;
    static float npcHeadYaw = 0f;
    static float npcHeadPitch = 0f;
    static float npcHeadRoll = 0f;
    static Vector2 npcEyeGaze = Vector2.Zero;

    static bool gameEnded = false;
    static bool isBadEnding = false;
    static string endingTitle = "";
    static string endingText = "";

    static bool isTalking = false;
    static bool isTalkingToNpc = false;
    static int dialogueStage = 0;
    static string currentSpeaker = "";
    static string fullText = "";
    static float visibleChars = 0f;
    static int lastSoundCharIndex = -1;
    static string[] currentOptions = [];
    static int selectedOption = 0;
    static string activeInteractId = "";
    static string subChoiceMode = "";

    static void Main()
    {
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Ca Trực Đêm - Night Shift Horror");
        DisableIme();
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(60);
        Raylib.DisableCursor();

        sceneTarget = Raylib.LoadRenderTexture(ScreenWidth, ScreenHeight);
        Raylib.SetTextureFilter(sceneTarget.Texture, TextureFilter.Bilinear);

        Props.LoadFaceTextures();
        Props.LoadWorldTextures();

        string viChars =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~•" +
            "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư" +
            "ẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ";

        int[] codepoints = viChars.Select(c => (int)c).Distinct().ToArray();
        viFont = Raylib.LoadFontEx(@"C:\Windows\Fonts\segoeui.ttf", 26, codepoints, codepoints.Length);
        Raylib.SetTextureFilter(viFont.Texture, TextureFilter.Bilinear);

        InitAudio();
        UpdateFirstPersonLook(cafeSeatPos);

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            if (!gameEnded && Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                isPaused = !isPaused;
                pauseSelection = 0;
                if (isPaused)
                {
                    Raylib.EnableCursor();
                    if (Raylib.IsSoundPlaying(cafeAmbience)) Raylib.PauseSound(cafeAmbience);
                }
                else
                {
                    Raylib.DisableCursor();
                    Raylib.GetMouseDelta();
                    if (gameScene == 0 && cafeSubStep < 2) Raylib.ResumeSound(cafeAmbience);
                }
            }

            if (isPaused)
            {
                if (UpdateAndDrawPauseMenu()) break;
                continue;
            }

            if (gameScene == -1)
            {
                DrawPhoneIntroScene(dt);
                continue;
            }

            if (gameScene == 0)
            {
                UpdateAndDrawCafeScene(dt);
                continue;
            }

            if (gameScene == 1)
            {
                UpdateAndDrawIntroMonologueScene(dt);
                continue;
            }

            UpdateAndDrawStoreGameplay(dt);
        }

        Props.UnloadFaceTextures();
        Props.UnloadWorldTextures();
        Raylib.UnloadRenderTexture(sceneTarget);
        Raylib.UnloadFont(viFont);
        UnloadAudio();
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    static unsafe void DisableIme()
    {
        NativeMethods.ImmAssociateContext((IntPtr)Raylib.GetWindowHandle(), IntPtr.Zero);
    }

    static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("imm32.dll")]
        public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
    }
}