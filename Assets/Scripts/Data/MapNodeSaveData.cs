using System;
using System.Collections.Generic;

/// <summary>
/// 맵 노드 하나의 저장 데이터.
/// BattleSaveData.mapNodes 리스트에 저장됩니다.
/// </summary>
[Serializable]
public class MapNodeSaveData
{
    public int       nodeID;
    public int       floor;
    public int       lane;
    public float     xOffset = 0f;  // 레인 내 수평 불규칙 배치용 오프셋
    public ENodeType type;
    public int       enemyGroupID = -1;  // 전투/엘리트/보스 노드에만 사용
    public bool      isCleared;
    public bool      isAvailable;
    public List<int> nextNodeIDs = new();
}
