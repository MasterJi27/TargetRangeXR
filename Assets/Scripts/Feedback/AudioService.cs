using System;
using System.Collections.Generic;
using UnityEngine;

namespace TargetRange.Feedback
{
    public enum SfxId
    {
        UiClick, UiHover, Shot, DryFire, Hit, Grab, GameStart, GameEnd, Tick, Bullseye
    }

    [Serializable]
    public struct SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        public Vector2 pitchRange;
    }

    /// <summary>
    /// Plays one shot sounds through a small pool of AudioSources. Gun and impact sounds are 3D (they come from
    /// where the gun or the target is, which matters in VR); UI sounds are 2D. Also owns the room ambience loop.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private SfxEntry[] effects;
        [SerializeField] private AudioClip ambience;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.22f;
        [SerializeField, Min(2)] private int voiceCount = 10;

        private readonly Dictionary<SfxId, SfxEntry> lookup = new Dictionary<SfxId, SfxEntry>();
        private AudioSource[] voices;
        private int nextVoice;

        /// <summary>Raised every time a sound effect is played. Used by the demo recorder to build the video's audio track.</summary>
        public event Action<SfxId> Played;

        private void Awake()
        {
            foreach (SfxEntry entry in effects) lookup[entry.id] = entry;

            voices = new AudioSource[voiceCount];
            for (int i = 0; i < voices.Length; i++)
            {
                var voiceObject = new GameObject("Voice " + i);
                voiceObject.transform.SetParent(transform, false);
                voices[i] = voiceObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].rolloffMode = AudioRolloffMode.Linear;
                voices[i].minDistance = 2f;
                voices[i].maxDistance = 40f;
            }

            if (ambience == null) return;
            AudioSource loop = gameObject.AddComponent<AudioSource>();
            loop.clip = ambience;
            loop.loop = true;
            loop.volume = ambienceVolume;
            loop.spatialBlend = 0f;
            loop.Play();
        }

        /// <summary>Plays a 2D sound (UI, round start and end).</summary>
        public void Play(SfxId id) => PlayInternal(id, null);

        /// <summary>Plays a 3D sound at a world position (gunshot, impact).</summary>
        public void PlayAt(SfxId id, Vector3 position) => PlayInternal(id, position);

        private void PlayInternal(SfxId id, Vector3? position)
        {
            if (!lookup.TryGetValue(id, out SfxEntry entry) || entry.clip == null) return;

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = UnityEngine.Random.Range(entry.pitchRange.x, entry.pitchRange.y);
            voice.spatialBlend = position.HasValue ? 1f : 0f;
            if (position.HasValue) voice.transform.position = position.Value;
            voice.PlayOneShot(entry.clip, entry.volume);
            Played?.Invoke(id);
        }
    }
}
