using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeRed.Pokemon
{
    [Serializable]
    public class MoveSlot
    {
        public MoveData move;
        public int currentPP;

        public MoveSlot(MoveData m)
        {
            move = m;
            currentPP = m != null ? m.pp : 0;
        }
    }

    [Serializable]
    public class PokemonInstance
    {
        public PokemonData species;
        public string nickname;
        public int level = 5;
        public int exp;
        public int currentHP;
        public StatusCondition status = StatusCondition.None;

        public int ivHP, ivAtk, ivDef, ivSpAtk, ivSpDef, ivSpeed;
        public List<MoveSlot> moves = new();

        public int MaxHP    => CalcHP();
        public int Attack   => CalcStat(species.baseAtk,    ivAtk);
        public int Defense  => CalcStat(species.baseDef,    ivDef);
        public int SpAttack => CalcStat(species.baseSpAtk,  ivSpAtk);
        public int SpDefense=> CalcStat(species.baseSpDef,  ivSpDef);
        public int Speed    => CalcStat(species.baseSpeed,  ivSpeed);
        public string DisplayName => string.IsNullOrEmpty(nickname) ? species.speciesName : nickname;
        public bool IsFainted => currentHP <= 0;

        public static PokemonInstance Create(PokemonData data, int level)
        {
            var p = new PokemonInstance { species = data, level = Mathf.Max(1, level) };
            p.RollIVs();
            p.exp = data.ExpForLevel(p.level);
            p.currentHP = p.MaxHP;
            p.LearnDefaultMoves();
            return p;
        }

        public void RollIVs()
        {
            ivHP = UnityEngine.Random.Range(0, 16);
            ivAtk = UnityEngine.Random.Range(0, 16);
            ivDef = UnityEngine.Random.Range(0, 16);
            ivSpAtk = UnityEngine.Random.Range(0, 16);
            ivSpDef = UnityEngine.Random.Range(0, 16);
            ivSpeed = UnityEngine.Random.Range(0, 16);
        }

        public void LearnDefaultMoves()
        {
            moves.Clear();
            if (species == null) return;
            foreach (var lm in species.learnset)
            {
                if (lm.level > level || lm.move == null) continue;
                if (moves.Count >= 4) moves.RemoveAt(0);
                moves.Add(new MoveSlot(lm.move));
            }
        }

        public int TakeDamage(int amount)
        {
            int dealt = Mathf.Clamp(amount, 0, currentHP);
            currentHP -= dealt;
            if (currentHP <= 0) { currentHP = 0; status = StatusCondition.Faint; }
            return dealt;
        }

        public void Heal(int amount) => currentHP = Mathf.Min(MaxHP, currentHP + amount);
        public void FullHeal() { currentHP = MaxHP; status = StatusCondition.None; foreach (var m in moves) if (m.move != null) m.currentPP = m.move.pp; }

        public bool GainExp(int amount, out int levelsGained)
        {
            levelsGained = 0;
            exp += amount;
            while (level < 100 && exp >= species.ExpForLevel(level + 1))
            {
                level++;
                levelsGained++;
                currentHP = MaxHP; // simple: full HP on level up
                // New level-up move?
                foreach (var lm in species.learnset)
                    if (lm.level == level && lm.move != null)
                    {
                        if (moves.Count < 4) moves.Add(new MoveSlot(lm.move));
                    }
            }
            return levelsGained > 0;
        }

        private int CalcHP()
        {
            if (species == null) return 0;
            return Mathf.FloorToInt(((2 * species.baseHP + ivHP) * level) / 100f) + level + 10;
        }

        private int CalcStat(int baseStat, int iv)
        {
            return Mathf.FloorToInt(((2 * baseStat + iv) * level) / 100f) + 5;
        }
    }
}
