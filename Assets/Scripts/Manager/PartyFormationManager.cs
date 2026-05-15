using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파티 편성 씬 전체를 관리합니다.
///
/// 흐름
///   1. Awake: 보유 뮤버 목록으로 MewberEntryUI 생성 + 던전 버튼 생성
///   2. 플레이어가 편성하기/해제 클릭 → OnToggleAssign()
///   3. 던전 선택 → OnSelectDungeon()
///   4. 원정 시작 → 맵 생성 후 BattleSaveData 저장, Map 씬 이동
///   5. 덱 보기  → 편성된 뮤버의 카드풀 합산 → PileViewerUI 표시
/// </summary>
public class PartyFormationManager : MonoBehaviour
{
    private const int MaxParty = 3;

    [Header("뮤버 목록")]
    [SerializeField] private MewberEntryUI _entryPrefab;
    [SerializeField] private Transform     _content;

    [Header("던전 선택")]
    [SerializeField] private DungeonEntryUI      _dungeonEntryPrefab;
    [SerializeField] private Transform           _dungeonContent;
    [SerializeField] private TextMeshProUGUI     _selectedDungeonText;

    [Header("버튼")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _deckViewButton;
    [SerializeField] private Button _backButton;

    [Header("파티 상태 표시")]
    [SerializeField] private TextMeshProUGUI _partyCountText;

    [Header("덱 뷰어")]
    [SerializeField] private PileViewerUI _pileViewer;

    private readonly List<int>           _partyIDs   = new();
    private readonly List<MewberEntryUI> _entries    = new();
    private int                          _selectedDungeonID = -1;

    private void Awake()
    {
        if (_startButton   != null) _startButton.onClick.AddListener(OnStartExpedition);
        if (_deckViewButton != null) _deckViewButton.onClick.AddListener(OnDeckView);
        if (_backButton    != null) _backButton.onClick.AddListener(OnBack);

        BuildEntries();
        BuildDungeonEntries();
        RefreshUI();
    }

    // ─────────────────────────────────────────────
    //  던전 목록 생성
    // ─────────────────────────────────────────────

    private void BuildDungeonEntries()
    {
        if (_dungeonEntryPrefab == null || _dungeonContent == null) return;

        var dungeons = DataManager.Instance.GetAllDungeons();
        if (dungeons == null) return;

        foreach (var dungeon in dungeons)
        {
            if (dungeon == null) continue;
            DungeonEntryUI entry = Instantiate(_dungeonEntryPrefab, _dungeonContent);
            entry.Init(dungeon, OnSelectDungeon);
        }
    }

    // ─────────────────────────────────────────────
    //  던전 선택
    // ─────────────────────────────────────────────

    private void OnSelectDungeon(int dungeonID)
    {
        _selectedDungeonID = dungeonID;

        var dungeon = DataManager.Instance.GetDungeon(dungeonID);
        if (_selectedDungeonText != null)
            _selectedDungeonText.text = dungeon != null ? dungeon.DungeonName : "???";

        Log.Info($"[PartyFormation] 던전 선택: {dungeonID}");
        RefreshUI();
    }

    // ─────────────────────────────────────────────
    //  목록 생성
    // ─────────────────────────────────────────────

    private void BuildEntries()
    {
        if (_entryPrefab == null || _content == null) return;

        var estateData = SaveManager.Instance.EstateData;

        foreach (int id in estateData.ownedMewberIDs)
        {
            MewberData     data = DataManager.Instance.GetMewberData(id);
            MewberPoolSave pool = SaveManager.Instance.GetMewberPool(id);
            if (data == null) continue;

            MewberEntryUI entry = Instantiate(_entryPrefab, _content);
            entry.Init(data, pool, OnToggleAssign);
            _entries.Add(entry);
        }
    }

    // ─────────────────────────────────────────────
    //  편성 토글
    // ─────────────────────────────────────────────

    private void OnToggleAssign(int mewberID)
    {
        if (_partyIDs.Contains(mewberID))
        {
            _partyIDs.Remove(mewberID);
        }
        else
        {
            if (_partyIDs.Count >= MaxParty)
            {
                Log.Warning("[PartyFormation] 파티 인원이 꽉 찼습니다 (최대 3명)");
                return;
            }
            _partyIDs.Add(mewberID);
        }

        RefreshUI();
    }

    // ─────────────────────────────────────────────
    //  UI 갱신
    // ─────────────────────────────────────────────

    private void RefreshUI()
    {
        foreach (var entry in _entries)
            entry.SetAssigned(_partyIDs.Contains(entry.MewberID));

        bool canStart   = _partyIDs.Count >= 1 && _selectedDungeonID >= 0;
        bool canViewDeck = _partyIDs.Count >= 1;
        if (_startButton    != null) _startButton.interactable    = canStart;
        if (_deckViewButton != null) _deckViewButton.interactable = canViewDeck;
        if (_partyCountText != null) _partyCountText.text         = $"{_partyIDs.Count} / {MaxParty}";
    }

    // ─────────────────────────────────────────────
    //  버튼 콜백
    // ─────────────────────────────────────────────

    private void OnStartExpedition()
    {
        if (_partyIDs.Count == 0 || _selectedDungeonID < 0)
        {
            Log.Warning("[PartyFormation] 파티 또는 던전이 선택되지 않았습니다.");
            return;
        }

        var battleData = SaveManager.Instance.BattleData;
        battleData.partyMewberIDs.Clear();
        battleData.partyMewberIDs.AddRange(_partyIDs);
        battleData.selectedDungeonID = _selectedDungeonID;
        battleData.pendingNodeID     = -1;

        if (_selectedDungeonID >= 0)
        {
            var dungeon  = DataManager.Instance.GetDungeon(_selectedDungeonID);
            var weights  = DataManager.Instance.GetNodeWeights(battleData.selectedDifficulty);
            if (dungeon != null)
            {
                int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                MapGenerator.Generate(dungeon, battleData, seed, weights);
                Log.Info($"[PartyFormation] 맵 생성 완료 — 던전: {dungeon.DungeonName}, 난이도: {battleData.selectedDifficulty}, 시드: {seed}");
            }
        }

        SaveManager.Instance.SaveBattle();

        Log.Info($"[PartyFormation] 원정 시작 — 파티: {string.Join(", ", _partyIDs)}");
        StageManager.Instance.LoadStage(ESceneName.Map);
    }

    private void OnDeckView()
    {
        if (_pileViewer == null) return;

        var cards = BuildPartyDeck();
        _pileViewer.ShowCardList("현재 덱", cards);
    }

    private void OnBack()
    {
        StageManager.Instance.LoadStage(ESceneName.Estate);
    }

    // ─────────────────────────────────────────────
    //  파티 덱 합산
    // ─────────────────────────────────────────────

    private List<CardInstance> BuildPartyDeck()
    {
        var result   = new List<CardInstance>();
        int tempID   = 1;

        foreach (int mewberID in _partyIDs)
        {
            MewberPoolSave pool = SaveManager.Instance.GetMewberPool(mewberID);
            if (pool == null) continue;

            foreach (var entry in pool.cards)
                for (int i = 0; i < entry.count; i++)
                    result.Add(new CardInstance(entry.cardID, tempID++));
        }

        return result;
    }
}
