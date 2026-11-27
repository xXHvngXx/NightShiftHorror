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

        Raylib.StopSound(ambientHumSound);
        Raylib.SetSoundPitch(motorbikeEngineSound, 0.95f);
        Raylib.PlaySound(motorbikeEngineSound);
        Raylib.GetMouseDelta();
    }

    static void UpdateReturnHomeCutscene(float dt)
    {
        returnHomeCutsceneTimer += dt;

        Vector3 bikeFocus = new(4.20f, 1.35f, -15.45f);
        Vector3 exitRoadFocus = new(6.50f, 1.55f, -18.80f);
        float t = Math.Clamp(returnHomeCutsceneTimer / 5.5f, 0f, 1f);
        float smoothT = t * t * (3f - 2f * t);

        Vector3 camStart = new(4.95f, 1.62f, -14.10f);
        Vector3 camEnd = new(5.40f, 1.68f, -17.60f);
        Vector3 camPos = Vector3.Lerp(camStart, camEnd, smoothT);
        camPos.Y += MathF.Sin(returnHomeCutsceneTimer * 24f) * 0.012f;

        Vector3 targetPos = Vector3.Lerp(bikeFocus, exitRoadFocus, smoothT);
        camera.Position = camPos;
        camera.Target = targetPos;
        camera.Up = Vector3.UnitY;

        if (returnHomeCutsceneTimer >= 5.5f)
        {
            Raylib.StopSound(motorbikeEngineSound);
            isReturnHomeCutscene = false;
            returnHomeCutsceneTimer = 0f;
            TriggerEnding(2);
        }
    }
}