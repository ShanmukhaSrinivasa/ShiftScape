using System;
using UnityEngine;

namespace ShiftScape.Game
{
    // Sound effects synthesized at startup, so the game ships no audio files.
    public static class SoundSynth
    {
        const int SampleRate = 44100;
        const float TwoPi = Mathf.PI * 2f;

        public static AudioClip Click() => Make("Click", 0.04f, (t, _) =>
            Mathf.Sin(TwoPi * 1800f * t) * Mathf.Exp(-t * 120f) * 0.5f);

        // Filtered noise swell with a soft low thump: the ground sliding.
        public static AudioClip Slide()
        {
            var rng = new System.Random(7);
            float low = 0f;
            return Make("Slide", 0.17f, (t, d) =>
            {
                float noise = (float)rng.NextDouble() * 2f - 1f;
                low += Mathf.Lerp(0.22f, 0.04f, t / d) * (noise - low);
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / d)) * Mathf.Exp(-t * 6f);
                float thump = Mathf.Sin(TwoPi * 150f * t) * Mathf.Exp(-t * 30f) * 0.35f;
                return low * 2.2f * envelope + thump;
            });
        }

        // Two dull low knocks: the line won't budge.
        public static AudioClip Blocked() => Make("Blocked", 0.22f, (t, _) =>
        {
            float first = Knock(t, 130f);
            float second = t > 0.09f ? Knock(t - 0.09f, 104f) : 0f;
            return (first + second) * 0.55f;
        });

        public static AudioClip Step() => Make("Step", 0.08f, (t, _) =>
            (Mathf.Sin(TwoPi * 660f * t) + 0.3f * Mathf.Sin(TwoPi * 1320f * t)) * Mathf.Exp(-t * 55f) * 0.35f);

        public static AudioClip Crystal() => Make("Crystal", 0.45f, (t, _) =>
        {
            float vibrato = 1f + 0.004f * Mathf.Sin(TwoPi * 7f * t);
            float tone = Mathf.Sin(TwoPi * 1568f * vibrato * t) + 0.6f * Mathf.Sin(TwoPi * 2093f * vibrato * t)
                         + 0.25f * Mathf.Sin(TwoPi * 3136f * t);
            return tone * Mathf.Exp(-t * 9f) * 0.3f;
        });

        // C major arpeggio up an octave.
        public static AudioClip Win()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            const float spacing = 0.1f;
            return Make("Win", 1.1f, (t, _) =>
            {
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float local = t - i * spacing;
                    if (local < 0f) continue;
                    float tone = Mathf.Sin(TwoPi * notes[i] * local) + 0.35f * Mathf.Sin(TwoPi * notes[i] * 2f * local);
                    float hold = i == notes.Length - 1 ? 3.5f : 7f;
                    sum += tone * Mathf.Exp(-local * hold) * Mathf.Clamp01(local / 0.005f);
                }
                return sum * 0.28f;
            });
        }

        static float Knock(float t, float frequency)
        {
            float body = Mathf.Sin(TwoPi * frequency * t);
            body = Mathf.Clamp(body * 2.5f, -1f, 1f);
            return body * Mathf.Exp(-t * 38f);
        }

        // sample(t, duration) returns a value in roughly [-1, 1]; a short fade-out avoids clicks at the end.
        static AudioClip Make(string name, float duration, Func<float, float, float> sample)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                float fade = Mathf.Clamp01((duration - t) / 0.01f);
                data[i] = Mathf.Clamp(sample(t, duration) * fade, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
