using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 적 그룹 데이터를 담는 ScriptableObject 테이블.
/// </summary>
[CreateAssetMenu(fileName = "EnemyGroupTable", menuName = "Data/EnemyGroupTable")]
public class EnemyGroupTable : ScriptableObject
{
    public List<EnemyGroupData> groups = new();
}
