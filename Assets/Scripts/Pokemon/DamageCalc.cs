using UnityEngine;

namespace PokeRed.Pokemon
{
    public struct DamageResult
    {
        public int   damage;
        public float effectiveness;
        public bool  criticalHit;
        public bool  missed;
    }

    public static class DamageCalc
    {
        public static bool RollHit(MoveData move, PokemonInstance attacker, PokemonInstance defender)
        {
            if (move.accuracy >= 100) return true;
            return Random.Range(0, 100) < move.accuracy;
        }

        public static bool RollCrit(PokemonInstance attacker)
        {
            int threshold = Mathf.Max(1, attacker.Speed / 2);
            return Random.Range(0, 256) < threshold;
        }

        public static DamageResult Compute(MoveData move, PokemonInstance attacker, PokemonInstance defender)
        {
            var r = new DamageResult();

            if (move.category == MoveCategory.Status || move.power <= 0)
            {
                r.effectiveness = 1f;
                return r;
            }

            if (!RollHit(move, attacker, defender)) { r.missed = true; return r; }

            bool crit = RollCrit(attacker);
            r.criticalHit = crit;

            int atkStat = move.category == MoveCategory.Physical ? attacker.Attack  : attacker.SpAttack;
            int defStat = move.category == MoveCategory.Physical ? defender.Defense : defender.SpDefense;

            int level = crit ? attacker.level * 2 : attacker.level;

            float baseDmg = (((2f * level / 5f + 2f) * move.power * atkStat / Mathf.Max(1, defStat)) / 50f) + 2f;

            float stab = (move.type == attacker.species.type1 || move.type == attacker.species.type2) ? 1.5f : 1f;
            float eff  = TypeChart.Effectiveness(move.type, defender.species.type1, defender.species.type2);
            float rand = Random.Range(0.85f, 1.0f);

            r.effectiveness = eff;
            r.damage = Mathf.Max(1, Mathf.FloorToInt(baseDmg * stab * eff * rand));
            if (eff == 0f) r.damage = 0;
            return r;
        }

        public static int ExpGain(PokemonInstance fainted, bool trainerBattle)
        {
            float a = trainerBattle ? 1.5f : 1f;
            return Mathf.FloorToInt((a * fainted.species.baseExpYield * fainted.level) / 7f);
        }
    }
}
