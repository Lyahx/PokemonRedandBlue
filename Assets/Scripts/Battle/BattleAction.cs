namespace PokeRed.Battle
{
    public enum BattleActionType { Fight, SwitchPokemon, UseItem, Run }

    public struct BattleAction
    {
        public BattleActionType type;
        public int moveIndex;
        public int switchIndex;
    }
}
