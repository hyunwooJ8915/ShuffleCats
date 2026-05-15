using UnityEngine;

/// <summary>
/// 전투 씬의 유닛 배치 슬롯을 관리합니다.
/// 빈 GameObject를 슬롯으로 인스펙터에 등록하면 됩니다.
/// </summary>
public class BattleField : MonoBehaviour
{
    [SerializeField] private Transform[] _playerSlots;
    [SerializeField] private Transform[] _enemySlots;

    public int PlayerSlotCount => _playerSlots.Length;
    public int EnemySlotCount  => _enemySlots.Length;

    public Transform GetPlayerSlot(int index)
    {
        if (index < 0 || index >= _playerSlots.Length)
        {
            Log.Warning($"[BattleField] 플레이어 슬롯 {index} 범위 초과");
            return null;
        }
        return _playerSlots[index];
    }

    public Transform GetEnemySlot(int index)
    {
        if (index < 0 || index >= _enemySlots.Length)
        {
            Log.Warning($"[BattleField] 적 슬롯 {index} 범위 초과");
            return null;
        }
        return _enemySlots[index];
    }
}
