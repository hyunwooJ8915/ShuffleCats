using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Map 씬 전체를 관리합니다.
///
/// 흐름
///   1. Awake: BattleSaveData의 mapNodes로 MapNodeUI 생성
///   2. 플레이어가 노드 선택 → OnSelectNode()
///   3. 노드 타입에 따라 씬 전환 (Battle / Event / Shop / Rest)
///   4. 전투 씬에서 돌아올 때: MarkCleared() → 다음 층 isAvailable 갱신 → RefreshAllNodes()
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("노드 UI")]
    [SerializeField] private MapNodeUI     _nodePrefab;
    [SerializeField] private RectTransform _mapRoot;

    [Header("커넥터")]
    [SerializeField] private MapConnector  _connectorPrefab;

    [Header("레이아웃")]
    [SerializeField] private float _laneSpacing     = 200f;
    [SerializeField] private float _floorSpacing    = 200f;
    [SerializeField] private float _verticalPadding = 200f;

    private readonly Dictionary<int, MapNodeUI>       _nodeUIs   = new();
    private readonly Dictionary<int, RectTransform>   _nodeRects = new();

    private void Awake()
    {
        // 전투 씬에서 복귀했을 때 클리어 처리
        int pending = SaveManager.Instance.BattleData.pendingNodeID;
        if (pending >= 0)
        {
            MarkCleared(pending);
            SaveManager.Instance.BattleData.pendingNodeID = -1;
        }

        BuildMap();
    }

    // ─────────────────────────────────────────────
    //  맵 생성
    // ─────────────────────────────────────────────

    private void BuildMap()
    {
        if (_nodePrefab == null || _mapRoot == null) return;

        var nodes = SaveManager.Instance.BattleData.mapNodes;
        if (nodes == null || nodes.Count == 0)
        {
#if UNITY_EDITOR
            Log.Warning("[MapManager] mapNodes가 비어있습니다. 에디터 테스트용 더미 맵을 생성합니다.");
            nodes = GenerateEditorDummyMap(SaveManager.Instance.BattleData);
#else
            Log.Warning("[MapManager] mapNodes가 비어있습니다.");
            return;
#endif
        }

        // 층/레인 범위 계산 (Start 노드가 floor -1이므로 minFloor 추적)
        int minFloor = 0;
        int maxFloor = 0;
        int maxLane  = 0;
        foreach (var n in nodes)
        {
            if (n.floor < minFloor) minFloor = n.floor;
            if (n.floor > maxFloor) maxFloor = n.floor;
            if (n.lane  > maxLane)  maxLane  = n.lane;
        }

        float totalWidth  = maxLane * _laneSpacing;
        float totalHeight = (maxFloor - minFloor) * _floorSpacing + _verticalPadding * 2f;

        _mapRoot.sizeDelta = new Vector2(_mapRoot.sizeDelta.x, totalHeight);

        // 노드 배치
        foreach (var data in nodes)
        {
            MapNodeUI ui = Instantiate(_nodePrefab, _mapRoot);

            float x = data.lane * _laneSpacing - totalWidth * 0.5f + data.xOffset * _laneSpacing;
            float y = (data.floor - minFloor) * _floorSpacing - totalHeight * 0.5f + _verticalPadding;
            var   rect = ui.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);

            ui.Init(data, OnSelectNode);
            _nodeUIs[data.nodeID]   = ui;
            _nodeRects[data.nodeID] = rect;
        }

        // 커넥터 배치 (노드보다 뒤에 렌더링되도록 sibling 0으로 이동)
        if (_connectorPrefab != null)
        {
            foreach (var data in nodes)
            {
                if (!_nodeRects.TryGetValue(data.nodeID, out var fromRect)) continue;

                foreach (int nextID in data.nextNodeIDs)
                {
                    if (!_nodeRects.TryGetValue(nextID, out var toRect)) continue;

                    MapConnector connector = Instantiate(_connectorPrefab, _mapRoot);
                    connector.Connect(fromRect, toRect);
                    connector.transform.SetAsFirstSibling();
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  노드 선택
    // ─────────────────────────────────────────────

    private void OnSelectNode(int nodeID)
    {
        var battleData = SaveManager.Instance.BattleData;
        var node       = battleData.mapNodes.Find(n => n.nodeID == nodeID);
        if (node == null || !node.isAvailable || node.isCleared) return;

        battleData.pendingNodeID       = node.nodeID;
        battleData.pendingEnemyGroupID = node.enemyGroupID;
        battleData.currentFloor        = node.floor;
        SaveManager.Instance.SaveBattle();

        Log.Info($"[MapManager] 노드 선택 — ID:{nodeID} 타입:{node.type} 층:{node.floor}");

        switch (node.type)
        {
            case ENodeType.Battle:
            case ENodeType.Elite:
            case ENodeType.Boss:
                StageManager.Instance.LoadStage(ESceneName.Battle);
                break;

            case ENodeType.Rest:
                // TODO: 휴식 씬 또는 팝업
                Log.Info("[MapManager] 휴식 노드 — 미구현");
                MarkCleared(nodeID);
                break;

            case ENodeType.Shop:
                // TODO: 상점 씬
                Log.Info("[MapManager] 상점 노드 — 미구현");
                MarkCleared(nodeID);
                break;

            case ENodeType.Event:
                // TODO: 이벤트 씬
                Log.Info("[MapManager] 이벤트 노드 — 미구현");
                MarkCleared(nodeID);
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  클리어 처리 (전투/이벤트 씬에서 복귀 후 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 노드를 클리어 처리하고 다음 층 노드를 선택 가능하게 만듭니다.
    /// 전투 씬 복귀 시 BattleManager 또는 외부에서 호출하세요.
    /// </summary>
    public void MarkCleared(int nodeID)
    {
        var battleData = SaveManager.Instance.BattleData;
        var node       = battleData.mapNodes.Find(n => n.nodeID == nodeID);
        if (node == null) return;

        node.isCleared = true;

        foreach (int nextID in node.nextNodeIDs)
        {
            var next = battleData.mapNodes.Find(n => n.nodeID == nextID);
            if (next != null) next.isAvailable = true;
        }

        SaveManager.Instance.SaveBattle();
        RefreshAllNodes(battleData.mapNodes);

        Log.Info($"[MapManager] 노드 {nodeID} 클리어 → 다음 노드 {node.nextNodeIDs.Count}개 개방");
    }

#if UNITY_EDITOR
    // ─────────────────────────────────────────────
    //  에디터 전용 더미 맵 생성 (씬 단독 실행 테스트용)
    // ─────────────────────────────────────────────

    [Header("에디터 테스트 (빌드 제외)")]
    [SerializeField] private int _editorTestSeed = 42;

    private List<MapNodeSaveData> GenerateEditorDummyMap(BattleSaveData battleData)
    {
        // 3레인 × 6층 (0~4 일반, 5 보스)
        var types = new[]
        {
            new[] { ENodeType.Battle, ENodeType.Battle, ENodeType.Battle },
            new[] { ENodeType.Battle, ENodeType.Elite,  ENodeType.Event  },
            new[] { ENodeType.Event,  ENodeType.Battle, ENodeType.Shop   },
            new[] { ENodeType.Shop,   ENodeType.Battle, ENodeType.Elite  },
            new[] { ENodeType.Rest,   ENodeType.Rest,   ENodeType.Rest   },
            new[] { ENodeType.Boss,   ENodeType.Boss,   ENodeType.Boss   },
        };

        Random.InitState(_editorTestSeed);

        int nodeID = 0;
        var floorNodes = new List<List<MapNodeSaveData>>();

        // 시작 노드 (floor -1)
        var startNode = new MapNodeSaveData
        {
            nodeID      = nodeID++,
            floor       = -1,
            lane        = 1,
            type        = ENodeType.Start,
            isCleared   = true,
            isAvailable = true,
        };

        for (int f = 0; f < types.Length; f++)
        {
            var floor = new List<MapNodeSaveData>();
            int laneCount = f == types.Length - 1 ? 1 : types[f].Length;

            for (int lane = 0; lane < laneCount; lane++)
            {
                bool isBoss = f == types.Length - 1;
                floor.Add(new MapNodeSaveData
                {
                    nodeID  = nodeID++,
                    floor   = f,
                    lane    = isBoss ? 1 : lane,
                    xOffset = isBoss ? 0f : (Random.value * 2f - 1f) * 0.3f,
                    type    = types[f][lane],
                });
            }
            floorNodes.Add(floor);
        }

        // MapGenerator와 동일한 분기 연결 로직 사용
        for (int f = 0; f < floorNodes.Count - 1; f++)
            MapGenerator.Connect(floorNodes[f], floorNodes[f + 1]);

        // 시작 노드 → 0층 전체 연결
        foreach (var n in floorNodes[0])
        {
            n.isAvailable = true;
            startNode.nextNodeIDs.Add(n.nodeID);
        }

        battleData.mapNodes.Clear();
        battleData.mapNodes.Add(startNode);
        foreach (var floor in floorNodes)
            battleData.mapNodes.AddRange(floor);

        return battleData.mapNodes;
    }
#endif

    private void RefreshAllNodes(List<MapNodeSaveData> nodes)
    {
        foreach (var data in nodes)
        {
            if (_nodeUIs.TryGetValue(data.nodeID, out var ui))
                ui.Refresh(data);
        }
    }
}
