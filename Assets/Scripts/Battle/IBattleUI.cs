using System.Collections;
using PokeRed.Pokemon;

namespace PokeRed.Battle
{
    public interface IBattleUI
    {
        IEnumerator ShowMessage(string text);
        IEnumerator ShowIntro(PokemonInstance playerMon, PokemonInstance enemyMon, bool wild);
        IEnumerator ShowHPChange(PokemonInstance target, int before, int after);
        IEnumerator ShowFaint(PokemonInstance target);
        IEnumerator AskPlayerAction(PokemonInstance active, System.Action<BattleAction> onChoice);
        IEnumerator HideBattle();
    }
}
