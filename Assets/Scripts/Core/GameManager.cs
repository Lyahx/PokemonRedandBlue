using System;
using PokeRed.Pokemon;
using UnityEngine;

namespace PokeRed.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState state = GameState.Overworld;
        public GameState State => state;
        public event Action<GameState> OnStateChanged;

        public Party PlayerParty { get; private set; } = new Party();
        public string PlayerName  = "RED";
        public int    Money       = 3000;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState next)
        {
            if (state == next) return;
            state = next;
            OnStateChanged?.Invoke(state);
        }
    }
}
