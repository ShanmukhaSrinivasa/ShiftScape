using UnityEngine;

namespace ShiftScape.Game
{
    public sealed class GameAudio : MonoBehaviour
    {
        const string MutedKey = "shiftscape.muted";
        const int Voices = 6;

        AudioSource[] sources;
        int nextSource;
        AudioClip click, slide, blocked, step, crystal, win;

        public bool Muted { get; private set; }

        public static GameAudio Create() => new GameObject("Audio").AddComponent<GameAudio>();

        void Awake()
        {
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }

            click = SoundSynth.Click();
            slide = SoundSynth.Slide();
            blocked = SoundSynth.Blocked();
            step = SoundSynth.Step();
            crystal = SoundSynth.Crystal();
            win = SoundSynth.Win();

            Muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            AudioListener.volume = Muted ? 0f : 1f;
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : 1f;
            PlayerPrefs.SetInt(MutedKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Click() => Play(click, 1f, 0.6f);
        public void Slide() => Play(slide, Random.Range(0.9f, 1.1f), 0.8f);
        public void Blocked() => Play(blocked, 1f, 0.8f);
        // Each step and each crystal climbs in pitch, so a long walk sounds like it's building up.
        public void Step(int index) => Play(step, 1f + 0.06f * Mathf.Min(index, 12), 0.5f);
        public void Crystal(int index) => Play(crystal, 1f + 0.12f * index, 0.7f);
        public void Win() => Play(win, 1f, 0.8f);

        void Play(AudioClip clip, float pitch, float volume)
        {
            if (Muted) return;
            AudioSource source = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            source.pitch = pitch;
            source.volume = volume;
            source.clip = clip;
            source.Play();
        }
    }
}
