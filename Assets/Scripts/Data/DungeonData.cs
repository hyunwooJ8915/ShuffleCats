using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 한 개의 구성 정보.
/// DungeonTable에 리스트로 관리됩니다.
/// 그룹 풀은 EnemyGroupData.ID 참조로 연결합니다.
/// </summary>
[Serializable]
public class DungeonData
{
    [Header("기본 정보")]
    public int    ID;
    public string DungeonName;
    [TextArea] public string Description;

    [Header("맵 구성")]
    [Tooltip("보스 층 포함 총 층 수")]
    public int FloorCount = 10;
    [Tooltip("층당 레인(노드) 수")]
    public int MapWidth   = 3;

    [Header("적 그룹 ID 풀")]
    public List<int> NormalGroupIDs = new();
    public List<int> EliteGroupIDs  = new();
    public List<int> BossGroupIDs   = new();
}
