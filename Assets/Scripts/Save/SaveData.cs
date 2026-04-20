using System;
using System.Collections.Generic;

namespace PokeRed.Save
{
    [Serializable]
    public class SavedMoveSlot
    {
        public string moveAssetName;
        public int currentPP;
    }

    [Serializable]
    public class SavedPokemon
    {
        public string speciesAssetName;
        public string nickname;
        public int level;
        public int exp;
        public int currentHP;
        public int status;
        public int ivHP, ivAtk, ivDef, ivSpAtk, ivSpDef, ivSpeed;
        public List<SavedMoveSlot> moves = new();
    }

    [Serializable]
    public class SavedItem
    {
        public string itemAssetName;
        public int    count;
    }

    [Serializable]
    public class SaveData
    {
        public string playerName = "RED";
        public int    money      = 3000;
        public string sceneName  = "PalletTown";
        public float  posX, posY;
        public List<SavedPokemon> party = new();
        public List<SavedItem>    bag   = new();
        public long   savedAtTicks;
    }
}
