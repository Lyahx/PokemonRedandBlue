# PokeRed

Pokemon Red/Blue tarzı bir oyun — Unity 6 (C#) ile yeniden yapım.

## Kapsam (MVP)

- Pallet Town → Route 1 → Viridian City → Viridian Forest → Pewter City (ilk gym)
- Izgara tabanlı hareket, NPC diyalogları, vahşi karşılaşmalar, turn-based savaş
- Party sistemi (max 6), seviye atlama, tip etkinlik tablosu (Gen 1)
- Tek oyunculu, JSON kayıt/yükleme

## Projeyi Açmak

1. **Unity Hub**'ı aç
2. `Add` → `Add project from disk` → bu klasörü seç (`C:\Users\MSI\Desktop\PokeRed`)
3. Unity sürümü: **6000.4.2f1**
4. İlk açılışta Unity eksik `ProjectSettings/` dosyalarını otomatik üretir
5. `Window → Package Manager` üzerinden doğrula:
   - 2D Sprite
   - 2D Tilemap Editor
   - TextMeshPro (built-in)

## Klasör Yapısı

```
Assets/
  Scripts/
    Core/       — GameManager, GameState, Direction, InputReader
    World/      — GridMover, Warp, SpawnPoint, EncounterZone, EncounterTable
    Player/     — PlayerController
    NPC/        — NPCController, IInteractable, SignInteractable
    Dialogue/   — DialogueManager, DialogueBox
    Pokemon/    — PokemonData/MoveData (SO), PokemonInstance, Party, TypeChart, DamageCalc
    Battle/     — BattleSystem, BattleUI, IBattleUI
    UI/         — MainMenuController
    Save/       — SaveSystem, PokemonDataRegistry
    Debug/      — DebugBootstrap, BattleTrigger (B tuşu test savaşı)
  Data/         — ScriptableObject veriler
  Scenes/       — Unity sahneleri
  Sprites/      — Grafik varlıklar
  Prefabs/      — Player, NPC, Dialogue UI vb.
Packages/
ProjectSettings/
```

## Hızlı Test

1. Unity'de sahne oluştur (`Assets/Scenes/Test.unity`)
2. Sahneye bir GameObject ekle → `GameManager` + `DebugBootstrap` scriptini ekle
3. Canvas + `BattleUI` + `BattleSystem` referansları
4. Play bas → `B` tuşuna bas → vahşi savaş açılır

## Sonraki Adımlar

- [ ] PalletTown tilemap + player prefab
- [ ] BattleUI sahne prefab'ı (Canvas + butonlar)
- [ ] İlk gym (Pewter) + Brock NPC
- [ ] Save slot UI

## Notlar

- Orijinal sprite/müzik kullanıcı tarafından eklenecek — kod tarafı placeholder ile çalışır.
- `Assets/Create/PokeRed/...` menüsünden ScriptableObject üretilebilir.
- Savaş sistemi Gen 1 hasar formülünü kullanır.
