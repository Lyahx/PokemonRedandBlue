using UnityEngine;

namespace PokeRed.Items
{
    public enum ItemCategory { General, Ball, Potion, KeyItem, TM }

    [CreateAssetMenu(fileName = "ItemData", menuName = "PokeRed/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName = "Potion";
        public ItemCategory category = ItemCategory.General;
        public int price = 0;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Ball")]
        [Tooltip("Catch rate multiplier vs standard Poké Ball (1.0). E.g. Great Ball = 1.5, Ultra Ball = 2.0.")]
        public float ballModifier = 1f;

        [Header("Potion")]
        [Tooltip("HP restored if used out of battle. 0 = not a potion.")]
        public int healAmount = 0;
    }
}
