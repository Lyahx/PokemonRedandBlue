using System;
using System.Collections.Generic;
using System.IO;
using PokeRed.Core;
using PokeRed.Pokemon;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokeRed.Save
{
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [SerializeField] private string fileName = "save.json";
        [SerializeField] private Transform player;
        [SerializeField] private PokemonDataRegistry registry;

        private string Path => System.IO.Path.Combine(Application.persistentDataPath, fileName);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public bool Exists() => File.Exists(Path);

        public void Save()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var data = new SaveData
            {
                playerName = gm.PlayerName,
                money      = gm.Money,
                sceneName  = SceneManager.GetActiveScene().name,
                posX       = player != null ? player.position.x : 0f,
                posY       = player != null ? player.position.y : 0f,
                savedAtTicks = DateTime.UtcNow.Ticks
            };

            foreach (var mon in gm.PlayerParty.members)
                data.party.Add(ToSaved(mon));

            File.WriteAllText(Path, JsonUtility.ToJson(data, true));
            Debug.Log($"Saved → {Path}");
        }

        public bool Load()
        {
            if (!Exists()) return false;
            var json = File.ReadAllText(Path);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return false;

            var gm = GameManager.Instance;
            if (gm == null) return false;

            gm.PlayerName = data.playerName;
            gm.Money      = data.money;
            gm.PlayerParty.members.Clear();
            foreach (var sp in data.party)
            {
                var restored = FromSaved(sp);
                if (restored != null) gm.PlayerParty.members.Add(restored);
            }

            if (player != null) player.position = new Vector3(data.posX, data.posY, player.position.z);

            if (!string.IsNullOrEmpty(data.sceneName) && SceneManager.GetActiveScene().name != data.sceneName)
                SceneManager.LoadScene(data.sceneName);

            return true;
        }

        private SavedPokemon ToSaved(PokemonInstance p)
        {
            var sp = new SavedPokemon
            {
                speciesAssetName = p.species != null ? p.species.name : "",
                nickname = p.nickname,
                level = p.level, exp = p.exp, currentHP = p.currentHP,
                status = (int)p.status,
                ivHP = p.ivHP, ivAtk = p.ivAtk, ivDef = p.ivDef,
                ivSpAtk = p.ivSpAtk, ivSpDef = p.ivSpDef, ivSpeed = p.ivSpeed
            };
            foreach (var m in p.moves)
                sp.moves.Add(new SavedMoveSlot { moveAssetName = m.move != null ? m.move.name : "", currentPP = m.currentPP });
            return sp;
        }

        private PokemonInstance FromSaved(SavedPokemon sp)
        {
            if (registry == null) return null;
            var species = registry.FindSpecies(sp.speciesAssetName);
            if (species == null) return null;

            var p = new PokemonInstance
            {
                species = species,
                nickname = sp.nickname,
                level = sp.level, exp = sp.exp, currentHP = sp.currentHP,
                status = (StatusCondition)sp.status,
                ivHP = sp.ivHP, ivAtk = sp.ivAtk, ivDef = sp.ivDef,
                ivSpAtk = sp.ivSpAtk, ivSpDef = sp.ivSpDef, ivSpeed = sp.ivSpeed,
                moves = new List<MoveSlot>()
            };
            foreach (var m in sp.moves)
            {
                var move = registry.FindMove(m.moveAssetName);
                if (move != null) p.moves.Add(new MoveSlot(move) { currentPP = m.currentPP });
            }
            return p;
        }
    }
}
