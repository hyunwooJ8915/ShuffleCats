using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// [전투 제어 메인 매니저]
/// 1. 공유 덱(Draw, Hand, Discard)의 생명 주기를 관리합니다.
/// 2. 유저와 적군의 행동(카드 사용)을 처리하고, 세이버와 동기화 합니다.
/// 3. 승리/패배 조건 및 실시간 리필 로직을 담당합니다.
/// </summary>
public class BattleManager : Singleton<BattleManager>
{
    #region Variables & Properties
    [Header("Party Settings")]
    public List<Unit> PlayerParty = new List<Unit>();
    public List<Unit> EnemyParty = new List<Unit>();

    private List<CardInstance> _drawPile => SaveManager.Instance.BattleData.drawPile;
    private List<CardInstance> _handCards => SaveManager.Instance.BattleData.handCards;
    private List<CardInstance> _discardPile => SaveManager.Instance.BattleData.discardPile;

    public IReadOnlyList<CardInstance> HandCards => _handCards;
    public IReadOnlyList<CardInstance> DrawPile => _drawPile;
    public IReadOnlyList<CardInstance> DiscardPile => _discardPile;

    private const int MaxHandCount = 10;
    private const int MinHandCount = 5;

    public bool IsBattleOver { get; private set; } = false;

    private HandUI _handUI;
    #endregion

    private void Start()
    {
        _handUI = FindFirstObjectByType<HandUI>();

        // 테스트용: 샘플 덱으로 전투 초기화
        List<int> testDeck = new List<int> { 10001, 10002, 10003, 10101, 10102,
                                             10103, 10201, 10202, 10203, 10301,
                                             10302, 10303, 10401, 10402, 10403,
                                             10501, 10502, 10503, 10601, 10602,
                                             10603, 10701, 10702, 10703, 10801,
                                             10802, 10803, 10901, 10902, 10903 };
        PrepareBattle(42, testDeck);
        _handUI?.RefreshHand(new List<CardInstance>(_handCards));
    }

    #region Battle Initializer
    public void PrepareBattle(int seed, List<int> startDeck)
    {
        IsBattleOver = false;

        RandomUtil.Initialize(seed, SaveManager.Instance.BattleData.randomStep);

        if (_drawPile.Count == 0 && _handCards.Count == 0 && _discardPile.Count == 0)
        {
            foreach (int cardID in startDeck)
                _drawPile.Add(NewInstance(cardID));

            _drawPile.Shuffle(); // RandomManager 활용 확장 메서드
            DrawCard(MinHandCount);
        }

        Log.Success("전투 데이터 로드 및 초기화 완료");
    }

    private CardInstance NewInstance(int cardID)
    {
        int id = SaveManager.Instance.BattleData.nextInstanceID++;
        return new CardInstance(cardID, id);
    }
    #endregion

    #region Core Logic
    /// <summary>
    /// 손패의 특정 인스턴스 카드를 사용합니다.
    /// 새로 드로우된 인스턴스 목록을 반환합니다.
    /// </summary>
    public List<CardInstance> ExecuteCard(int instanceID, Unit caster, Unit target = null)
    {
        if (IsBattleOver) return new List<CardInstance>();

        CardInstance instance = _handCards.Find(c => c.InstanceID == instanceID);
        if (instance == null)
        {
            Log.Warning($"손패에 InstanceID {instanceID} 카드가 없음");
            return new List<CardInstance>();
        }

        CardData data = DataManager.Instance.GetCard(instance.CardID);
        if (data == null) return new List<CardInstance>();

        // caster가 있을 때만 효과 프로세서 실행
        if (caster != null)
            EffectProcessor.Process(data.Effects, caster, target);

        // 덱 상태 업데이트
        _handCards.Remove(instance);
        _discardPile.Add(instance);

        int drawNeeded = MinHandCount - _handCards.Count;
        List<CardInstance> drawn = drawNeeded > 0 ? DrawCard(drawNeeded) : new List<CardInstance>();

        // 상태 저장 (난수 단계 포함)
        SaveManager.Instance.BattleData.randomStep = RandomUtil.CurrentStep;
        SaveManager.Instance.SaveBattle();

        // 게임 종료 체크
        CheckBattleOver();

        return drawn;
    }
    #endregion

    #region Deck Management
    public List<CardInstance> DrawCard(int count)
    {
        var drawn = new List<CardInstance>();
        for (int i = 0; i < count; i++)
        {
            if (_handCards.Count >= MaxHandCount) break;

            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0)
                {
                    Log.Warning("뽑을 카드 더미 소진");
                    break;
                }
                ReshuffleDiscardToDraw();
            }

            CardInstance instance = _drawPile[0];
            _drawPile.RemoveAt(0);
            _handCards.Add(instance);
            drawn.Add(instance);

            Log.Info($"카드 드로우: {instance.CardID}#{instance.InstanceID} (덱: {_drawPile.Count} | 버림: {_discardPile.Count})");
        }
        return drawn;
    }

    private void ReshuffleDiscardToDraw()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        _drawPile.Shuffle();
        Log.Info("버린 카드를 섞어 새로운 뽑기 더미 생성");
    }
    #endregion

    #region Battle Status
    private void CheckBattleOver()
    {
        if (PlayerParty.Count == 0 || EnemyParty.Count == 0) return;
        // 모든 아군 사망 여부 체크
        bool allPlayersDead = PlayerParty.TrueForAll(p => p.CurrentHP <= 0);
        // 모든 적군 사망 여부 체크
        bool allEnemiesDead = EnemyParty.TrueForAll(e => e.CurrentHP <= 0);

        if (allPlayersDead) FinishBattle(false);
        else if (allEnemiesDead) FinishBattle(true);
    }

    private void FinishBattle(bool isWin)
    {
        IsBattleOver = true;
        if (isWin) Log.Success("전투 승리");
        else Log.Error("전투 패배");

        // 전투 종료 시 임시 데이터 삭제 및 영지 데이터 업데이트 로직 연결
    }
    #endregion
}
