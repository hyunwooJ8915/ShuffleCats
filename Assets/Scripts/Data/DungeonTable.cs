using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 던전 데이터를 담는 ScriptableObject 테이블.
/// </summary>
[CreateAssetMenu(fileName = "DungeonTable", menuName = "Data/DungeonTable")]
public class DungeonTable : ScriptableObject
{
    public List<DungeonData> dungeons = new();
}
