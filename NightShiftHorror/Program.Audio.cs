using System.Text;
using Raylib_cs;

partial class Program
{
    static Sound cafeAmbience;
    static Sound footstepSound;
    static Sound voiceStaticSound;
    static Sound phoneRingSound;
    static Sound phoneVibrateSound;
    static Sound knockSound;
    static Sound scareSound;
    static Sound ambientHumSound;
    static Sound heartbeatSound;
    static Sound motorbikeEngineSound;
    static Sound motorbikeHornSound;

    static void InitAudio()
    {
        cafeAmbience = CreateCafeAmbienceSound();
        footstepSound = CreateFootstepSound();
        voiceStaticSound = CreateStaticBlipSound();
        phoneRingSound = CreatePhoneRingSound();
        phoneVibrateSound = CreatePhoneVibrateSound();
        knockSound = CreateKnockSound();
        scareSound = CreateScareSound();
        ambientHumSound = CreateAmbientHumSound();
        heartbeatSound = CreateHeartbeatSound();
        motorbikeEngineSound = CreateMotorbikeEngineSound();
        motorbikeHornSound = CreateMotorbikeHornSound();

        Raylib.SetSoundVolume(footstepSound, 0.75f);
        Raylib.SetSoundVolume(voiceStaticSound, 0.35f);
        Raylib.SetSoundVolume(phoneRingSound, 0.55f);
        Raylib.SetSoundVolume(phoneVibrateSound, 0.45f);
        Raylib.SetSoundVolume(knockSound, 0.85f);
        Raylib.SetSoundVolume(scareSound, 0.85f);
        Raylib.SetSoundVolume(ambientHumSound, 0.22f);
        Raylib.SetSoundVolume(heartbeatSound, 0.65f);
        Raylib.SetSoundVolume(motorbikeEngineSound, 0.35f);
        Raylib.SetSoundVolume(motorbikeHornSound, 0.55f);

    }

    static void UnloadAudio()
    {
        Raylib.UnloadSound(cafeAmbience);
        Raylib.UnloadSound(footstepSound);
        Raylib.UnloadSound(voiceStaticSound);
        Raylib.UnloadSound(phoneRingSound);
        Raylib.UnloadSound(phoneVibrateSound);
        Raylib.UnloadSound(knockSound);
        Raylib.UnloadSound(scareSound);
        Raylib.UnloadSound(ambientHumSound);
        Raylib.UnloadSound(heartbeatSound);
        Raylib.UnloadSound(motorbikeEngineSound);
        Raylib.UnloadSound(motorbikeHornSound);
    }

    static Sound CreateFootstepSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.13f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];
        Random rand = new(42);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = MathF.Exp(-t * 32f);
            float freq = 115f - t * 280f;
            float thump = MathF.Sin(2f * MathF.PI * Math.Max(40f, freq) * t);
            float grit = ((float)rand.NextDouble() * 2f - 1f) * 0.25f;
            samples[i] = (short)((thump * 0.85f + grit) * envelope * short.MaxValue);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreateStaticBlipSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.032f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];
        Random rand = new(99);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration);
            float buzz = MathF.Sign(MathF.Sin(2f * MathF.PI * 190f * t)) * 0.4f;
            float staticNoise = ((float)rand.NextDouble() * 2f - 1f) * 0.6f;
            samples[i] = (short)((buzz + staticNoise) * envelope * short.MaxValue * 0.6f);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreatePhoneRingSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.45f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float mod = MathF.Sin(2f * MathF.PI * 22f * t) > 0 ? 1f : 0.2f;
            float tone = (MathF.Sin(2f * MathF.PI * 440f * t) + MathF.Sin(2f * MathF.PI * 480f * t)) * 0.45f;
            samples[i] = (short)(tone * mod * short.MaxValue * 0.6f);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreatePhoneVibrateSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.22f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = MathF.Sin(MathF.PI * (t / duration));
            float buzz = MathF.Sin(2f * MathF.PI * 135f * t) * 0.65f + MathF.Sin(2f * MathF.PI * 270f * t) * 0.25f;
            samples[i] = (short)(buzz * env * short.MaxValue * 0.55f);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreateKnockSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.55f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float wave = 0f;
            foreach (float hitTime in new[] { 0.0f, 0.18f, 0.36f })
            {
                if (t >= hitTime)
                {
                    float dtHit = t - hitTime;
                    wave += MathF.Sin(2f * MathF.PI * 140f * dtHit) * MathF.Exp(-dtHit * 38f);
                }
            }
            samples[i] = (short)(Math.Clamp(wave, -1f, 1f) * short.MaxValue * 0.9f);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreateScareSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.7f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];
        Random rand = new(7);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = MathF.Exp(-t * 3.5f);
            float screech = MathF.Sin(2f * MathF.PI * (620f - t * 400f) * t) * 0.5f;
            float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.5f;
            samples[i] = (short)((screech + noise) * env * short.MaxValue);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreateAmbientHumSound()
    {
        const int sampleRate = 22050;
        const float duration = 2.0f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];
        Random rand = new(12);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float hum = MathF.Sin(2f * MathF.PI * 60f * t) * 0.45f + MathF.Sin(2f * MathF.PI * 120f * t) * 0.2f;
            float air = ((float)rand.NextDouble() * 2f - 1f) * 0.12f;
            samples[i] = (short)((hum + air) * short.MaxValue * 0.35f);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreateHeartbeatSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.55f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float beat1 = MathF.Sin(2f * MathF.PI * 52f * t) * MathF.Exp(-t * 24f);
            float beat2 = t > 0.18f ? MathF.Sin(2f * MathF.PI * 48f * (t - 0.18f)) * MathF.Exp(-(t - 0.18f) * 28f) * 0.75f : 0f;
            samples[i] = (short)(Math.Clamp(beat1 + beat2, -1f, 1f) * short.MaxValue * 0.85f);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreateCafeAmbienceSound()
    {
        const int sampleRate = 22050;
        const int seconds = 8;
        int totalSamples = sampleRate * seconds;
        short[] samples = new short[totalSamples];
        Random rng = new(42);

        float[] voiceFreqs = { 135f, 165f, 195f, 230f, 270f, 310f };
        float[] talkRates = { 3.1f, 4.3f, 2.7f, 3.8f, 4.9f, 3.4f };

        float pink = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float loopEnv = MathF.Sin(MathF.PI * t / seconds);
            loopEnv = MathF.Min(1f, loopEnv * 12f);

            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            pink = 0.94f * pink + 0.06f * white;
            float sample = pink * 0.25f;

            for (int v = 0; v < voiceFreqs.Length; v++)
            {
                float sentenceWave = MathF.Sin(2f * MathF.PI * (v + 1) * 0.25f * t + v * 1.3f);
                float syllableWave = MathF.Max(0f, MathF.Sin(2f * MathF.PI * talkRates[v] * t + v * 2.1f));
                float env = MathF.Max(0f, sentenceWave) * syllableWave;

                float f0 = voiceFreqs[v] + MathF.Sin(t * 5f + v) * 14f;
                float formant = MathF.Sin(2f * MathF.PI * f0 * t) * 0.6f
                              + MathF.Sin(2f * MathF.PI * (f0 * 2.02f) * t) * 0.3f;

                sample += formant * env * 0.045f;
            }

            float clinkTime = (t % 4.0f) - 2.3f;
            if (clinkTime > 0f && clinkTime < 0.08f)
            {
                float clinkEnv = MathF.Exp(-clinkTime * 65f);
                sample += MathF.Sin(2f * MathF.PI * 2450f * clinkTime) * clinkEnv * 0.06f;
            }

            samples[i] = (short)(Math.Clamp(sample * loopEnv, -0.9f, 0.9f) * short.MaxValue);
        }

        Sound snd = BuildSoundFromPcm(samples, sampleRate);
        Raylib.SetSoundVolume(snd, 0.22f);
        return snd;
    }

    static Sound BuildSoundFromPcm(short[] samples, int sampleRate)
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        int dataBytes = samples.Length * 2;
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataBytes);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataBytes);

        foreach (short s in samples)
            writer.Write(s);

        Wave wave = Raylib.LoadWaveFromMemory(".wav", ms.ToArray());
        Sound sound = Raylib.LoadSoundFromWave(wave);
        Raylib.UnloadWave(wave);
        return sound;
    }

    static Sound CreateMotorbikeEngineSound()
    {
        const int sampleRate = 22050;
        const float duration = 1.0f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];
        Random rand = new(55);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Tiếng nổ máy 4 thì trầm ấm + tiếng ống xả
            float pulse = MathF.Sin(2f * MathF.PI * 28f * t);
            float harmonic = MathF.Sin(2f * MathF.PI * 56f * t) * 0.45f
                           + MathF.Sin(2f * MathF.PI * 84f * t) * 0.25f;
            float exhaust = ((float)rand.NextDouble() * 2f - 1f) * 0.14f;
            float wave = (pulse * 0.65f + harmonic + exhaust) * 0.55f;
            samples[i] = (short)(Math.Clamp(wave, -1f, 1f) * short.MaxValue);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }

    static Sound CreateMotorbikeHornSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.24f;
        int sampleCount = (int)(sampleRate * duration);
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = MathF.Min(1f, t * 40f) * MathF.Min(1f, (duration - t) * 35f);
            float tone = MathF.Sign(MathF.Sin(2f * MathF.PI * 410f * t)) * 0.35f
                       + MathF.Sin(2f * MathF.PI * 515f * t) * 0.35f;
            samples[i] = (short)(tone * env * short.MaxValue * 0.7f);
        }

        return BuildSoundFromPcm(samples, sampleRate);
    }
}