using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// DungeonData를 기반으로 맵 노드를 생성합니다.
/// 규칙:
///   - 0층: 항상 Battle (복수 노드 선택 가능)
///   - 마지막 층: Boss 1개 (center lane)
///   - 마지막 직전 층: Rest 또는 Shop
///   - 나머지: DungeonData.NodeWeights 에 따라 랜덤
///   - 연결: 교차 없는 STS식 분기
/// </summary>
public static class MapGenerator
{
    public static void Generate(DungeonData dungeon, BattleSaveData battleData, int seed, List<NodeTypeWeight> nodeWeights)
    {
        Random.InitState(seed);
        battleData.mapNodes.Clear();
        battleData.currentFloor = 0;

        int total  = dungeon.FloorCount;
        int width  = dungeon.MapWidth;
        int nodeID = 0;

        // 시작 노드 (floor -1, 센터 레인) — 항상 클리어 상태로 시작
        var startNode = new MapNodeSaveData
        {
            nodeID      = nodeID++,
            floor       = -1,
            lane        = width / 2,
            type        = ENodeType.Start,
            isCleared   = true,
            isAvailable = true,
        };

        var floorNodes = new List<List<MapNodeSaveData>>();

        for (int f = 0; f < total; f++)
        {
            var floor = new List<MapNodeSaveData>();

            if (f == total - 1)
            {
                // 보스 층: 센터 레인 1개, 오프셋 없음
                floor.Add(new MapNodeSaveData
                {
                    nodeID       = nodeID++,
                    floor        = f,
                    lane         = width / 2,
                    xOffset      = 0f,
                    type         = ENodeType.Boss,
                    enemyGroupID = PickGroupID(dungeon.BossGroupIDs)
                });
            }
            else
            {
                for (int lane = 0; lane < width; lane++)
                {
                    ENodeType type    = AssignType(f, total, nodeWeights);
                    int       groupID = GroupIDForType(type, dungeon);

                    // 레인 간격의 ±30% 범위에서 수평 불규칙 배치
                    float xOffset = (Random.value * 2f - 1f) * 0.3f;

                    floor.Add(new MapNodeSaveData
                    {
                        nodeID       = nodeID++,
                        floor        = f,
                        lane         = lane,
                        xOffset      = xOffset,
                        type         = type,
                        enemyGroupID = groupID
                    });
                }
            }

            floorNodes.Add(floor);
        }

        // 연결 생성
        for (int f = 0; f < total - 1; f++)
            Connect(floorNodes[f], floorNodes[f + 1]);

        // 0층 전체 선택 가능으로 표시 + 시작 노드에서 연결
        foreach (var n in floorNodes[0])
        {
            n.isAvailable = true;
            startNode.nextNodeIDs.Add(n.nodeID);
        }

        battleData.mapNodes.Add(startNode);
        foreach (var floor in floorNodes)
            battleData.mapNodes.AddRange(floor);
    }

    // ─────────────────────────────────────────────
    //  노드 타입 결정
    // ─────────────────────────────────────────────

    private static ENodeType AssignType(int floor, int totalFloors, List<NodeTypeWeight> weights)
    {
        if (floor == 0)               return ENodeType.Battle;
        if (floor == totalFloors - 2) return Random.value < 0.5f ? ENodeType.Rest : ENodeType.Shop;
        return WeightedRandom(weights);
    }

    private static ENodeType WeightedRandom(List<NodeTypeWeight> weights)
    {
        var pool  = weights.Where(w => w.Type != ENodeType.Boss).ToList();
        int total = pool.Sum(w => w.Weight);
        if (total <= 0) return ENodeType.Battle;

        int roll = Random.Range(0, total);
        int acc  = 0;
        foreach (var w in pool)
        {
            acc += w.Weight;
            if (roll < acc) return w.Type;
        }
        return ENodeType.Battle;
    }

    // ─────────────────────────────────────────────
    //  적 그룹 선택
    // ─────────────────────────────────────────────

    private static int GroupIDForType(ENodeType type, DungeonData dungeon) => type switch
    {
        ENodeType.Battle => PickGroupID(dungeon.NormalGroupIDs),
        ENodeType.Elite  => PickGroupID(dungeon.EliteGroupIDs),
        ENodeType.Boss   => PickGroupID(dungeon.BossGroupIDs),
        _                => -1
    };

    private static int PickGroupID(List<int> groupIDs)
    {
        if (groupIDs == null || groupIDs.Count == 0) return -1;
        return groupIDs[Random.Range(0, groupIDs.Count)];
    }

    // ─────────────────────────────────────────────
    //  연결 생성
    // ─────────────────────────────────────────────

    /// <summary>
    /// 교차 없는 단조 연결 알고리즘.
    /// from 노드를 왼쪽부터 순서대로 처리하며 to 연결 인덱스가 단조 증가하도록 보장합니다.
    /// 규칙: 인접 레인(±1)만 허용, X자 교차 불가.
    /// </summary>
    public static void Connect(List<MapNodeSaveData> from, List<MapNodeSaveData> to)
    {
        // 실제 시각 위치(lane + xOffset) 기준 정렬 — 논리 레인만으로 정렬하면 xOffset으로 인한 교차 발생
        var fromSorted = from.OrderBy(n => n.lane + n.xOffset).ToList();
        var toSorted   = to.OrderBy(n => n.lane + n.xOffset).ToList();
        var inDegree   = new int[toSorted.Count];

        // 왼쪽 from 노드가 연결한 최소 to 인덱스 추적 — 교차 방지 핵심
        int minToIdx = 0;

        for (int i = 0; i < fromSorted.Count; i++)
        {
            var f = fromSorted[i];

            // 기본 연결: 가장 가까운 to 노드 (minToIdx 이상만 허용)
            int primary = Mathf.Clamp(ClosestIndex(toSorted, f.lane + f.xOffset), minToIdx, toSorted.Count - 1);
            AddEdge(f, toSorted[primary]);
            inDegree[primary]++;

            // 45% 확률로 왼쪽 인접 레인 추가 연결 (minToIdx 이상만)
            if (Random.value < 0.45f && primary - 1 >= minToIdx)
            {
                AddEdge(f, toSorted[primary - 1]);
                inDegree[primary - 1]++;
            }

            // 45% 확률로 오른쪽 인접 레인 추가 연결
            bool didRight = false;
            if (Random.value < 0.45f && primary + 1 < toSorted.Count)
            {
                AddEdge(f, toSorted[primary + 1]);
                inDegree[primary + 1]++;
                didRight = true;
            }

            // 교차 방지 핵심: 현재 노드의 최대(오른쪽) 연결 인덱스 기준으로 갱신
            // min이 아닌 max — 이래야 다음 from 노드가 현재 노드의 오른쪽 연결과 교차 불가
            minToIdx = primary + (didRight ? 1 : 0);
        }

        // 고립 노드 보정: 진입 간선이 없는 to 노드는 가장 가까운 from 노드에서 연결
        for (int i = 0; i < toSorted.Count; i++)
        {
            if (inDegree[i] > 0) continue;
            float toVisualX = toSorted[i].lane + toSorted[i].xOffset;
            var nearest = fromSorted.OrderBy(n => Mathf.Abs((n.lane + n.xOffset) - toVisualX)).First();
            AddEdge(nearest, toSorted[i]);
            inDegree[i]++;
        }
    }

    private static int ClosestIndex(List<MapNodeSaveData> list, float visualX)
    {
        int   best     = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            float dist = Mathf.Abs((list[i].lane + list[i].xOffset) - visualX);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }
        return best;
    }

    private static bool AddEdge(MapNodeSaveData from, MapNodeSaveData to)
    {
        if (from.nextNodeIDs.Contains(to.nodeID)) return false;
        from.nextNodeIDs.Add(to.nodeID);
        return true;
    }
}
