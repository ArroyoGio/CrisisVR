using UnityEngine;

namespace CrisisVR
{
    /// <summary>
    /// Sonido de tecleo del teclado físico de Acto 1. Escucha el evento Pressed de cada tecla
    /// (no modifica la pulsación, el retorno ni la háptica de Act1KeyboardKey) y reproduce un
    /// clic con ligera variación de tono y volumen. ESPACIO, ENTER y BORRAR usan su propio sonido.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class Act1KeyboardSound : MonoBehaviour
    {
        const int BackspaceKey = 24;
        const int EnterKey = 49;
        const int SpaceKey = 80;

        [SerializeField] AudioClip[] letterClips;
        [SerializeField] AudioClip spaceClip;
        [SerializeField] AudioClip enterClip;
        [SerializeField] AudioClip backspaceClip;
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;
        [SerializeField, Range(0f, 0.3f)] float pitchJitter = 0.08f;
        [SerializeField, Range(0f, 0.3f)] float volumeJitter = 0.1f;

        AudioSource source;
        Act1KeyboardKey[] keys;
        int lastLetterClip = -1;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.clip = null; // Act1KeyboardKey reproduce source.clip si existe: se deja vacío para no duplicar el sonido.
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 0.15f;
            source.maxDistance = 4f;
            keys = GetComponentsInChildren<Act1KeyboardKey>(true);
        }

        void OnEnable()
        {
            if (keys == null)
                return;
            foreach (Act1KeyboardKey key in keys)
                if (key != null)
                    key.Pressed += OnKeyPressed;
        }

        void OnDisable()
        {
            if (keys == null)
                return;
            foreach (Act1KeyboardKey key in keys)
                if (key != null)
                    key.Pressed -= OnKeyPressed;
        }

        void OnKeyPressed(Act1KeyboardKey key)
        {
            if (key == null || source == null)
                return;

            AudioClip clip = PickClip(key.PhysicalKeyIndex);
            if (clip == null)
                return;

            source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            source.PlayOneShot(clip, volume * (1f + Random.Range(-volumeJitter, volumeJitter)));
        }

        AudioClip PickClip(int index)
        {
            if (index == SpaceKey && spaceClip != null) return spaceClip;
            if (index == EnterKey && enterClip != null) return enterClip;
            if (index == BackspaceKey && backspaceClip != null) return backspaceClip;
            if (letterClips == null || letterClips.Length == 0)
                return null;

            int next = Random.Range(0, letterClips.Length);
            if (letterClips.Length > 1 && next == lastLetterClip)
                next = (next + 1) % letterClips.Length;
            lastLetterClip = next;
            return letterClips[next];
        }
    }
}
