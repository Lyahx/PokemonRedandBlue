using UnityEngine;

namespace PokeRed.Pokemon
{
    [CreateAssetMenu(fileName = "MoveData", menuName = "PokeRed/Move")]
    public class MoveData : ScriptableObject
    {
        public string moveName = "Tackle";
        public PokemonType type = PokemonType.Normal;
        public MoveCategory category = MoveCategory.Physical;
        [Range(0, 250)] public int power = 40;
        [Range(0, 100)] public int accuracy = 100;
        [Range(1, 40)]  public int pp = 35;
        [Range(-7, 7)]  public int priority = 0;
        [TextArea] public string description;
    }
}
