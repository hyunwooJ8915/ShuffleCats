using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 시작 시 SaveData의 파티 구성을 읽어 슬롯에 유닛을 스폰하고 덱을 준비합니다.
/// TutorialStarter 또는 MapManager 등 전투 진입 지점에서 데이터 세팅 후 Execute()를 호출합니다.
/// </summary>
public class BattleSetup : MonoBehaviour
{
    [SerializeField] private BattleField _battleField;
    [SerializeField] private GameObject  _unitPrefab;
    [SerializeField] private int         _seed = 42;

    public void Execute()
    {
        SpawnPlayerParty();
        SpawnEnemyParty();
        PrepareDeck();
        BattleManager.Instance.BeginBattle();
    }

    // ─────────────────────────────────────────────
    //  유닛 스폰
    // ─────────────────────────────────────────────

    private void SpawnPlayerParty()
    {
        var partyIDs = SaveManager.Instance.BattleData.partyMewberIDs;
        for (int i = 0; i < partyIDs.Count; i++)
        {
            Transform slot = _battleField.GetPlayerSlot(i);
            if (slot == null) break;

            MewberData data = DataManager.Instance.GetMewberData(partyIDs[i]);
            if (data == null) continue;

            Unit unit = Instantiate(_unitPrefab, slot.position, slot.rotation)
                            .GetComponent<Unit>();
            unit.Register(true);
            unit.Init(data);
        }
    }

    private void SpawnEnemyParty()
    {
        int groupID = SaveManager.Instance.BattleData.pendingEnemyGroupID;
        if (groupID < 0) return;

        EnemyGroupData group = DataManager.Instance.GetEnemyGroup(groupID);
        if (group == null) return;

        for (int i = 0; i < group.EnemyIDs.Count; i++)
        {
            Transform slot = _battleField.GetEnemySlot(i);
            if (slot == null) break;

            EnemyData data = DataManager.Instance.GetEnemy(group.EnemyIDs[i]);
            if (data == null) continue;

            Unit unit = Instantiate(_unitPrefab, slot.position, slot.rotation)
                            .GetComponent<Unit>();
            unit.Register(false);
            unit.Init(data);
        }
    }

    // ─────────────────────────────────────────────
    //  덱 준비 (플레이어 카드 + 적 카드 합산)
    // ─────────────────────────────────────────────

    private void PrepareDeck()
    {
        var cardIDs = new List<int>();

        // 플레이어 카드
        int beforePlayer = cardIDs.Count;
        foreach (int mewberID in SaveManager.Instance.BattleData.partyMewberIDs)
        {
            MewberPoolSave pool = SaveManager.Instance.GetMewberPool(mewberID);
            if (pool == null) { Log.Warning($"[PrepareDeck] 뮤버 {mewberID} 카드풀 없음"); continue; }
            foreach (var entry in pool.cards)
                for (int i = 0; i < entry.count; i++)
                    cardIDs.Add(entry.cardID);
        }
        Log.Info($"[PrepareDeck] 아군 카드 {cardIDs.Count - beforePlayer}장");

        // 적 카드
        int groupID = SaveManager.Instance.BattleData.pendingEnemyGroupID;
        Log.Info($"[PrepareDeck] 적 그룹 ID = {groupID}");

        if (groupID >= 0)
        {
            EnemyGroupData group = DataManager.Instance.GetEnemyGroup(groupID);
            if (group == null) { Log.Warning($"[PrepareDeck] 적 그룹 {groupID} 없음 → EnemyGroupTable 확인 필요"); }
            else
            {
                Log.Info($"[PrepareDeck] 적 그룹 '{group.GroupName}' EnemyIDs({group.EnemyIDs.Count}): {string.Join(", ", group.EnemyIDs)}");
                foreach (int enemyID in group.EnemyIDs)
                {
                    EnemyData enemy = DataManager.Instance.GetEnemy(enemyID);
                    if (enemy == null) { Log.Warning($"[PrepareDeck] 적 ID {enemyID} 없음 → EnemyTable 확인 필요"); continue; }

                    var bundle = enemy.ParseBundle();
                    Log.Info($"[PrepareDeck] {enemy.Name}(ID:{enemyID}) Bundle=\"{enemy.Bundle}\" → {bundle.Count}종");
                    foreach (var kv in bundle)
                        for (int i = 0; i < kv.Value; i++)
                            cardIDs.Add(kv.Key);
                }
            }
        }

        Log.Info($"[PrepareDeck] 전체 덱 {cardIDs.Count}장 → PrepareBattle 호출");
        BattleManager.Instance.PrepareBattle(_seed, cardIDs);
    }
}
