using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [실시간 데이터 매니저]
/// 1. SO에 저장된 리스트 데이터를 검색 최적화를 위해 Dictionary 구조로 재구성합니다.
/// 2. O(1)의 속도로 데이터를 조회하며, 싱글톤 패턴을 통해 어디서든 접근 가능하게 합니다.
/// 3. 대규모 데이터 처리 시 초기 로딩 시점에 단 한 번 변환하여 메모리 효율을 높입니다.
/// </summary>
public class DataManager : Singleton<DataManager>
{
    #region SerializeFields
    [Header("Table Assets")] // 데이터 테이블, Dict 추가해준 뒤 InitData에서 추가 로직 작성 까먹지 말 것
    [SerializeField] private CardTable   cardTable;
    [SerializeField] private MewberTable mewberTable;

    [Header("던전 & 적")]
    [SerializeField] private DungeonTable       dungeonTable;
    [SerializeField] private EnemyTable         enemyTable;
    [SerializeField] private EnemyGroupTable    enemyGroupTable;
    [SerializeField] private NodeWeightSettings nodeWeightSettings;
    #endregion

    #region Variables
    private Dictionary<int, CardData>       cardDict       = new();
    private Dictionary<int, MewberData>    mewberDict     = new();
    private Dictionary<int, DungeonData>   dungeonDict    = new();
    private Dictionary<int, EnemyData>     enemyDict      = new();
    private Dictionary<int, EnemyGroupData> enemyGroupDict = new();
    public bool IsInitialized { get; private set; } = false;
    #endregion

    #region UnityMethods
    protected override void Awake()
    {
        base.Awake();
        EnsureTablesLoaded();
        InitData();
    }
    #endregion

    #region PrivateMethods
    /// <summary>
    /// 인스펙터에서 SO 참조가 비어있는 경우(예: Singleton이 동적 생성된 케이스)
    /// Resources 폴더에서 자동 로드합니다. 빌드/에디터 모두 동일하게 동작.
    /// </summary>
    private void EnsureTablesLoaded()
    {
        UnityEngine.Debug.Log($"[DataManager] EnsureTablesLoaded 시작. cardTable={(cardTable != null ? "있음" : "null")}, mewberTable={(mewberTable != null ? "있음" : "null")}");

        // 진단: Resources/ 안에 어떤 CardTable/MewberTable이 있는지 전부 나열
        var allCards = Resources.LoadAll<CardTable>("");
        UnityEngine.Debug.Log($"[DataManager] Resources 안 CardTable 개수: {allCards.Length}");
        foreach (var t in allCards) UnityEngine.Debug.Log($"  - {t.name} (cards: {t.cards.Count})");

        var allMewbers = Resources.LoadAll<MewberTable>("");
        UnityEngine.Debug.Log($"[DataManager] Resources 안 MewberTable 개수: {allMewbers.Length}");
        foreach (var t in allMewbers) UnityEngine.Debug.Log($"  - {t.name} (mewbers: {t.mewbers.Count})");

        if (cardTable == null)
        {
            cardTable = Resources.Load<CardTable>("Data/CardTable");
            UnityEngine.Debug.Log($"[DataManager] Resources.Load(\"Data/CardTable\") 결과: {(cardTable != null ? "성공" : "null")}");
            // 폴백: 어떤 이름이든 첫 번째 CardTable
            if (cardTable == null && allCards.Length > 0)
            {
                cardTable = allCards[0];
                UnityEngine.Debug.LogWarning($"[DataManager] 경로 기반 로드 실패 → 첫 번째 발견 CardTable({allCards[0].name})로 폴백");
            }
            if (cardTable == null) Log.Error("CardTable을 찾을 수 없습니다. Resources/Data/CardTable.asset 경로를 확인하세요.");
        }

        if (mewberTable == null)
        {
            mewberTable = Resources.Load<MewberTable>("Data/MewberTable");
            UnityEngine.Debug.Log($"[DataManager] Resources.Load(\"Data/MewberTable\") 결과: {(mewberTable != null ? "성공" : "null")}");
            if (mewberTable == null && allMewbers.Length > 0)
            {
                mewberTable = allMewbers[0];
                UnityEngine.Debug.LogWarning($"[DataManager] 경로 기반 로드 실패 → 첫 번째 발견 MewberTable({allMewbers[0].name})로 폴백");
            }
            if (mewberTable == null) Log.Error("MewberTable을 찾을 수 없습니다. Resources/Data/MewberTable.asset 경로를 확인하세요.");
        }

        if (dungeonTable == null)
        {
            dungeonTable = Resources.Load<DungeonTable>("Data/DungeonTable");
            if (dungeonTable == null) Log.Warning("DungeonTable을 찾을 수 없습니다. Resources/Data/DungeonTable.asset 경로를 확인하세요.");
        }

        if (enemyTable == null)
        {
            enemyTable = Resources.Load<EnemyTable>("Data/EnemyTable");
            if (enemyTable == null) Log.Warning("EnemyTable을 찾을 수 없습니다. Resources/Data/EnemyTable.asset 경로를 확인하세요.");
        }

        if (enemyGroupTable == null)
        {
            enemyGroupTable = Resources.Load<EnemyGroupTable>("Data/EnemyGroupTable");
            if (enemyGroupTable == null) Log.Warning("EnemyGroupTable을 찾을 수 없습니다. Resources/Data/EnemyGroupTable.asset 경로를 확인하세요.");
        }

        if (nodeWeightSettings == null)
        {
            nodeWeightSettings = Resources.Load<NodeWeightSettings>("Data/NodeWeightSettings");
            if (nodeWeightSettings == null) Log.Warning("NodeWeightSettings를 찾을 수 없습니다. Resources/Data/NodeWeightSettings.asset 경로를 확인하세요.");
        }
    }

    private void InitData()
    {
        if (IsInitialized) return;

        cardDict.Clear();
        if (cardTable != null)
            foreach (var card in cardTable.cards)
                cardDict[card.ID] = card;

        mewberDict.Clear();
        if (mewberTable != null)
            foreach (var mewber in mewberTable.mewbers)
                mewberDict[mewber.ID] = mewber;

        dungeonDict.Clear();
        if (dungeonTable != null)
            foreach (var d in dungeonTable.dungeons)
                dungeonDict[d.ID] = d;

        enemyDict.Clear();
        if (enemyTable != null)
            foreach (var e in enemyTable.enemies)
                enemyDict[e.ID] = e;

        enemyGroupDict.Clear();
        if (enemyGroupTable != null)
            foreach (var g in enemyGroupTable.groups)
                enemyGroupDict[g.ID] = g;

        IsInitialized = true;
        Log.Success("전체 게임 데이터 로드 완료");
    }
    #endregion

    #region PublicMethods
    // 피드백 : 매니저에 있을 필요가 없다.
    public CardData GetCard(int id)
    {
        if (cardDict.TryGetValue(id, out var data)) return data;

        Log.Warning($"ID {id}에 해당하는 카드가 데이터 시트가 없습니다.");
        return null;
    }

    public MewberData GetMewberData(int id)
    {
        if (mewberDict.TryGetValue(id, out var data)) return data;

        Log.Warning($"ID {id}에 해당하는 뮤버가 데이터 시트가 없습니다.");
        return null;
    }

    public DungeonData GetDungeon(int id)
    {
        if (dungeonDict.TryGetValue(id, out var d)) return d;
        Log.Warning($"던전 ID {id} 없음");
        return null;
    }

    public List<DungeonData> GetAllDungeons() => dungeonTable != null ? dungeonTable.dungeons : new();

    public EnemyData GetEnemy(int id)
    {
        if (enemyDict.TryGetValue(id, out var e)) return e;
        Log.Warning($"적 ID {id} 없음");
        return null;
    }

    public EnemyGroupData GetEnemyGroup(int id)
    {
        if (enemyGroupDict.TryGetValue(id, out var g)) return g;
        Log.Warning($"적 그룹 ID {id} 없음");
        return null;
    }

    public List<NodeTypeWeight> GetNodeWeights(EDifficulty difficulty)
        => nodeWeightSettings != null ? nodeWeightSettings.GetWeights(difficulty) : new();

    /// <summary>MewberTable의 첫 번째 뮤버를 반환합니다. 튜토리얼 초기 지급용.</summary>
    public MewberData GetFirstMewber()
    {
        if (mewberTable == null || mewberTable.mewbers.Count == 0)
        {
            Log.Error("[DataManager] MewberTable이 비어 있습니다.");
            return null;
        }
        return mewberTable.mewbers[0];
    }
    #endregion
}