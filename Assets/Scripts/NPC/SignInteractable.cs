using System.Collections.Generic;
using PokeRed.Core;
using PokeRed.Dialogue;
using UnityEngine;

namespace PokeRed.NPC
{
    public class SignInteractable : MonoBehaviour, IInteractable
    {
        [TextArea] [SerializeField] private List<string> lines = new();

        public void Interact(Direction from)
        {
            if (lines.Count > 0)
                DialogueManager.Instance?.Show(lines);
        }
    }
}
