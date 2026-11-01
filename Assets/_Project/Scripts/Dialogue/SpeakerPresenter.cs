using System;
using UnityEngine;
using Yarn.Unity;

namespace CrisisVR
{
    // Presentador de Yarn que no muestra texto: marca a quien habla. En la videollamada enciende el borde
    // del recuadro (Lucía, Andrés, Mateo) y en el aula hace cabecear a Miguel o a Rodrigo.
    public sealed class SpeakerPresenter : DialoguePresenterBase
    {
        [Serializable]
        public struct Speaker
        {
            public string characterName;
            public GameObject highlight;
            public Act1ProfessorMotion motion;
        }

        [SerializeField] Speaker[] speakers = Array.Empty<Speaker>();

        public string CurrentSpeaker { get; private set; }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            SetSpeaker(line.CharacterName);
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            SetSpeaker(null);
            return YarnTask.CompletedTask;
        }

        void SetSpeaker(string characterName)
        {
            CurrentSpeaker = characterName;
            foreach (var speaker in speakers)
            {
                bool speaking = !string.IsNullOrEmpty(characterName)
                    && string.Equals(speaker.characterName, characterName, StringComparison.OrdinalIgnoreCase);
                if (speaker.highlight != null) speaker.highlight.SetActive(speaking);
                if (speaker.motion != null) speaker.motion.SetSpeaking(speaking);
            }
        }
    }
}
