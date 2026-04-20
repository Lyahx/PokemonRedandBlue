using TMPro;
using UnityEngine;

namespace PokeRed.Dialogue
{
    public class DialogueBox : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;

        public void SetVisible(bool v) { if (root != null) root.SetActive(v); }
        public void SetText(string s)  { if (label != null) label.text = s; }
    }
}
