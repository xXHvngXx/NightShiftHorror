using System.Numerics;
using Raylib_cs;

partial class Program
{
    static bool isReturnHomeCutscene = false;
    static float returnHomeCutsceneTimer = 0f;

    static bool IsMorningAfterShift => storyStep >= 10 || dawnProgress >= 0.99f;

    static void StartReturnHomeCutscene()
    {
        isTalking = false;
        StopDialogueVoice();
        isTalkingToNpc = false;
        dialogueStage = 0;
        isReturnHomeCutscene = true;
        returnHomeCutsceneTimer = 0f;
        currentObjective = "Nổ máy xe chạy về nhà sau ca trực đêm";
        Raylib.SetSoundPitch(footstepSound, 0.85f);
        Raylib.PlaySound(footstepSound);
        Raylib.GetMouseDelta();
    }

    static void UpdateReturnHomeCutscene(float dt)
    {
        returnHomeCutsceneTimer += dt;

        // Di chuyển nhẹ camera theo hướng xe máy nổ máy lăn bánh rời sân sau ra đường lớn
        Vector3 bikeFocus = new(4.20f, 1.35f, -15.45f);
        Vector3 exitRoadFocus = new(6.50f, 1.55f, -18.80f);
        float t = Math.Clamp(returnHomeCutsceneTimer / 5.5f, 0f, 1f);
        float smoothT = t * t * (3f - 2f * t);

        Vector3 camStart = new(4.95f, 1.62f, -14.10f);
        Vector3 camEnd = new(5.40f, 1.68f, -17.60f);
        Vector3 camPos = Vector3.Lerp(camStart, camEnd, smoothT);
        camPos.Y += MathF.Sin(returnHomeCutsceneTimer * 24f) * 0.012f; // Rung nhẹ tiếng động cơ

        Vector3 targetPos = Vector3.Lerp(bikeFocus, exitRoadFocus, smoothT);
        camera.Position = camPos;
        camera.Target = targetPos;
        camera.Up = Vector3.UnitY;

        if (returnHomeCutsceneTimer >= 5.5f)
        {
            isReturnHomeCutscene = false;
            returnHomeCutsceneTimer = 0f;
            TriggerEnding(2);
        }
    }
}
