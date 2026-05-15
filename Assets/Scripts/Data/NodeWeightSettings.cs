using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeTypeWeight
{
    public ENodeType           Type;
    [Range(0, 100)] public int Weight;
}

[Serializable]
public class DifficultyWeightEntry
{
    public EDifficulty          Difficulty;
    public List<NodeTypeWeight> Weights = new();
}

/// <summary>
/// 난이도별 노드 타입 분포를 정의하는 ScriptableObject.
/// MapGenerator는 선택된 난이도의 Weights를 참조합니다.
/// </summary>
[CreateAssetMenu(fileName = "NodeWeightSettings", menuName = "Data/NodeWeightSettings")]
public class NodeWeightSettings : ScriptableObject
{
    public List<DifficultyWeightEntry> Entries = new();

    public List<NodeTypeWeight> GetWeights(EDifficulty difficulty)
    {
        foreach (var e in Entries)
            if (e.Difficulty == difficulty) return e.Weights;

        // 폴백: Normal, 없으면 첫 번째
        foreach (var e in Entries)
            if (e.Difficulty == EDifficulty.Normal) return e.Weights;

        return Entries.Count > 0 ? Entries[0].Weights : new List<NodeTypeWeight>();
    }
}
