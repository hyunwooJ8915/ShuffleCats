using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTable", menuName = "Data/EnemyTable")]
public class EnemyTable : ScriptableObject
{
    public List<EnemyData> enemies = new List<EnemyData>();
}
