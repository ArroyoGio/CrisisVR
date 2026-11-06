using System;
using System.Text;
using UnityEngine;

namespace CrisisVR
{
    /// <summary>
    /// Entrada restringida a respuestas predefinidas para el teclado físico de Acto 1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Act1GuidedKeyboard : MonoBehaviour
    {
        const int BackspaceKey = 24;
        const int EnterKey = 49;
        const int SpaceKey = 80;

        static readonly string NumberRow = "1234567890-=";
        static readonly string QwertyRow = "qwertyuiop[]";
        static readonly string HomeRow = "asdfghjkl;'\\";
        static readonly string BottomRow = "zxcvbnm,./-=";

        readonly StringBuilder display = new StringBuilder(320);
        Act1KeyboardKey[] keys;
        Act1LaptopScreen screen;
        Act1LaptopHingeGrabTransformer lid;
        string input = string.Empty;
        string inputError = string.Empty;
        bool showing;
        bool confirmed;

        void Awake()
        {
            keys = GetComponentsInChildren<Act1KeyboardKey>(true);
            screen = FindAnyObjectByType<Act1LaptopScreen>();
            lid = FindAnyObjectByType<Act1LaptopHingeGrabTransformer>();
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
            if (keys != null)
                foreach (Act1KeyboardKey key in keys)
                    if (key != null)
                        key.Pressed -= OnKeyPressed;

            if (showing && screen != null)
                screen.HideGuidedKeyboardContent();
            showing = false;
        }

        void Update()
        {
            bool shouldShow = screen != null && screen.IsAwaitingDecision && IsLaptopOpen();
            if (shouldShow == showing)
                return;

            showing = shouldShow;
            if (showing)
                RenderInput();
            else if (screen != null)
                screen.HideGuidedKeyboardContent();
        }

        bool IsLaptopOpen()
        {
            return lid != null && Quaternion.Angle(lid.transform.localRotation, Quaternion.identity) > 5f;
        }

        void OnKeyPressed(Act1KeyboardKey key)
        {
            if (!showing || confirmed || key == null)
                return;

            int index = key.PhysicalKeyIndex;
            if (index == BackspaceKey)
            {
                if (input.Length > 0)
                    input = input.Substring(0, input.Length - 1);
                inputError = string.Empty;
                RenderInput();
                return;
            }

            if (index == EnterKey)
            {
                ConfirmResponse();
                return;
            }

            char character = GetCharacter(index);
            if (index == SpaceKey)
                character = ' ';
            if (character == '\0')
                return;

            string candidate = input + character;
            if (IsAllowedPrefix(candidate))
            {
                input = candidate;
                inputError = string.Empty;
            }
            else
            {
                inputError = ReportTexts.Act1GuidedKeyboardInvalidKey;
            }

            RenderInput();
        }

        static char GetCharacter(int index)
        {
            if (index >= 13 && index <= 23)
                return NumberRow[index - 13];
            if (index >= 25 && index <= 36)
                return QwertyRow[index - 25];
            if (index >= 37 && index <= 48)
                return HomeRow[index - 37];
            if (index >= 50 && index <= 61)
                return BottomRow[index - 50];
            return '\0';
        }

        static bool IsAllowedPrefix(string candidate)
        {
            string[] responses = ReportTexts.Act1GuidedKeyboardResponses;
            for (int i = 0; i < responses.Length; i++)
                if (responses[i].StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        void ConfirmResponse()
        {
            string[] responses = ReportTexts.Act1GuidedKeyboardResponses;
            for (int i = 0; i < responses.Length; i++)
            {
                if (!string.Equals(input, responses[i], StringComparison.OrdinalIgnoreCase))
                    continue;

                confirmed = true;
                inputError = string.Empty;
                screen.ShowGuidedKeyboardContent(ReportTexts.Act1GuidedKeyboardConfirmed + "\n" + responses[i]);
                return;
            }

            inputError = ReportTexts.Act1GuidedKeyboardIncomplete;
            RenderInput();
        }

        void RenderInput()
        {
            if (screen == null)
                return;

            display.Clear();
            display.Append(ReportTexts.Act1GuidedKeyboardPrompt).AppendLine();
            string[] responses = ReportTexts.Act1GuidedKeyboardResponses;
            for (int i = 0; i < responses.Length; i++)
                display.Append((char)('A' + i)).Append(") ").Append(responses[i]).AppendLine();
            display.Append(ReportTexts.Act1GuidedKeyboardInstruction).AppendLine();
            display.Append("> ").Append(input);
            if (!string.IsNullOrEmpty(inputError))
                display.AppendLine().Append(inputError);

            screen.ShowGuidedKeyboardContent(display.ToString());
        }
    }
}
