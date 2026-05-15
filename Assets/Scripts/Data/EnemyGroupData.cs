using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 번의 전투에서 스폰할 적 그룹 데이터.
/// EnemyGroupTable에 리스트로 관리됩니다.
/// </summary>
[Serializable]
public class EnemyGroupData
{
    public int             ID;
    public string          GroupName;
    public EEnemyGroupType GroupType;
    [Tooltip("스폰할 적의 MewberData ID 목록")]
    public List<int>       EnemyIDs = new();
}
