using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [실시간 데이터 매니저]
/// 1. SO에 저장된 리스트 데이터를 검색 최적화를 위해 Dictionary 구조로 재구성합니다.
/// 2. O(1)의 속도로 데이터를 조회하며, 싱글톤 패턴을 통해 어디서든 접근 가능하게 합니다.
/// 3. 대규모 데이터 처리 시 초기 로딩 시점에 단 한 번 변환하여 메모리 효율을 높입니다.
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    #region SerializeFields
    [Header("Table Assets")] // 데이터 테이블, Dict 추가해준 뒤 InitData에서 추가 로직 작성 까먹지 말 것
    [SerializeField] private CardTable cardTable;
    [SerializeField] private MewberTable mewberTable;
    #endregion

    #region Variables
    private Dictionary<int, CardData> cardDict = new Dictionary<int, CardData>();
    private Dictionary<int, MewberData> mewberDict = new Dictionary<int, MewberData>();
    public bool IsInitialized { get; private set; } = false;
    #endregion

    #region UnityMethods
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region PrivateMethods
    private void InitData()
    {
        if (IsInitialized) return;

        cardDict.Clear();
        foreach (var card in cardTable.cards)
        {
            if (!cardDict.ContainsKey(card.ID))
                cardDict.Add(card.ID, card);
        }

        mewberDict.Clear();
        foreach (var mewber in mewberTable.mewbers)
        {
            if (!mewberDict.ContainsKey(mewber.ID))
                mewberDict.Add(mewber.ID, mewber);
        }

        IsInitialized = true;
        Log.Success("전체 게임 데이터 로드 완료");
    }
    #endregion

    #region PublicMethods
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