using System.Collections.Generic;

namespace PokeRed.Pokemon
{
    [System.Serializable]
    public class Party
    {
        public const int MaxSize = 6;
        public List<PokemonInstance> members = new();

        public int Count => members.Count;
        public bool IsFull => members.Count >= MaxSize;
        public bool HasUsable
        {
            get { foreach (var m in members) if (!m.IsFainted) return true; return false; }
        }

        public PokemonInstance Leader => members.Count > 0 ? members[0] : null;

        public bool Add(PokemonInstance p)
        {
            if (IsFull) return false;
            members.Add(p);
            return true;
        }

        public PokemonInstance FirstUsable()
        {
            foreach (var m in members) if (!m.IsFainted) return m;
            return null;
        }

        public void HealAll() { foreach (var m in members) m.FullHeal(); }
    }
}
