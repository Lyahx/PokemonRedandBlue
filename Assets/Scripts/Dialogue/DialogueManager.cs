using System;
using System.Collections;
using System.Collections.Generic;
using PokeRed.Core;
using UnityEngine;

namespace PokeRed.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [SerializeField] private DialogueBox box;
        [SerializeField] private float charsPerSecond = 40f;

        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;

        private Queue<string> queue = new();
        private bool isTyping;
        private string current;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Show(IEnumerable<string> lines)
        {
            queue.Clear();
            foreach (var l in lines) queue.Enqueue(l);
            if (queue.Count == 0) return;

            GameManager.Instance?.SetState(GameState.Dialogue);
            box.SetVisible(true);
            OnDialogueStarted?.Invoke();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (queue.Count > 0)
            {
                current = queue.Dequeue();
                yield return TypeLine(current);
                // Wait for input
                while (!InputReader.Interact) yield return null;
            }
            End();
        }

        private IEnumerator TypeLine(string line)
        {
            isTyping = true;
            box.SetText("");
            float charInterval = 1f / Mathf.Max(1f, charsPerSecond);
            for (int i = 0; i < line.Length; i++)
            {
                if (InputReader.Interact) { box.SetText(line); break; }
                box.SetText(line.Substring(0, i + 1));
                yield return new WaitForSeconds(charInterval);
            }
            box.SetText(line);
            isTyping = false;
        }

        private void End()
        {
            box.SetVisible(false);
            OnDialogueEnded?.Invoke();
            GameManager.Instance?.SetState(GameState.Overworld);
        }
    }
}
