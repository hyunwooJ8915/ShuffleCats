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
    [SerializeField] private CardTable cardTable;
    [SerializeField] private MewberTable mewberTable;
    #endregion

    #region Variables
    private Dictionary<int, CardData> cardDict = new Dictionary<int, CardData>();   // 피드백 : 매니저에 있을 필요가 없다.
    private Dictionary<int, MewberData> mewberDict = new Dictionary<int, MewberData>();
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
    }

    private void InitData()
    {
        if (IsInitialized) return;
        if (cardTable == null || mewberTable == null) return;

        cardDict.Clear();
        foreach (var card in cardTable.cards)
            cardDict[card.ID] = card;

        mewberDict.Clear();
        foreach (var mewber in mewberTable.mewbers)
            mewberDict[mewber.ID] = mewber;

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
    #endregion
}