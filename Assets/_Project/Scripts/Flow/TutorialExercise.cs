using System;
using UnityEngine;

namespace CrisisVR
{
    [Serializable]
    public class TutorialExercise
    {
        public TutorialStep step;
        [TextArea] public string title;
        [TextArea] public string instruction;
        [TextArea] public string help;
    }
}
