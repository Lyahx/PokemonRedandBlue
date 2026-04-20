using UnityEngine;

namespace PokeRed.Pokemon
{
    /// <summary>Gen 1 catch formula, simplified.</summary>
    public static class CatchCalculator
    {
        public static bool TryCatch(PokemonInstance target, float ballModifier, out int shakes)
        {
            int catchRate = target.species != null ? target.species.catchRate : 45;
            int maxHP = Mathf.Max(1, target.MaxHP);
            int currentHP = Mathf.Max(1, target.currentHP);

            int statusBonus = target.status switch
            {
                StatusCondition.Sleep or StatusCondition.Freeze => 25,
                StatusCondition.Paralysis or StatusCondition.Poison or StatusCondition.Burn => 12,
                _ => 0
            };

            int ballValue = Mathf.Clamp(Mathf.RoundToInt(catchRate * ballModifier), 1, 255);

            // Gen1 check
            int rand = Random.Range(0, 256);
            if (rand < statusBonus) { shakes = 4; return true; }

            // f = (maxHP * 255) / (ballValue * 1 * currentHP) (simplified: no ball-specific rates here)
            int f = Mathf.FloorToInt((maxHP * 255f) / (ballValue * Mathf.Max(1, currentHP / 4f)));
            f = Mathf.Max(1, f);

            shakes = 0;
            for (int i = 0; i < 4; i++)
            {
                int r = Random.Range(0, 256);
                if (r >= f) return false;
                shakes = i + 1;
            }
            return true;
        }
    }
}
