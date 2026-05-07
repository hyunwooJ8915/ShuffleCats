using System.Collections;
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

    private List<int> _drawPile => SaveManager.Instance.BattleData.drawPile;
    private List<int> _handCards => SaveManager.Instance.BattleData.handCards;
    private List<int> _discardPile => SaveManager.Instance.BattleData.discardPile;

    private const int MaxHandCount = 10;
    private const int MinHandCount = 5;

    public bool IsBattleOver { get; private set; } = false;
    #endregion

    private void Start()
    {
        // 테스트용 
        List<int> testHand = new List<int> { 10001, 10001, 10001, 10001, 10001};

        var handUI = FindFirstObjectByType<HandUI>();
        if (handUI != null) handUI.RefreshHand(testHand);
    }

    #region Battle Initializer
    public void PrpareBattle(int seed, List<int> startDeck)
    {
        IsBattleOver = false;

        RandomUtil.Initialize(seed, SaveManager.Instance.BattleData.randomStep);

        if (_drawPile.Count == 0 && _handCards.Count == 0 && _discardPile.Count == 0)
        {
            _drawPile.AddRange(startDeck);
            _drawPile.Shuffle(); // RandomManager 활용 확장 메서드
            DrawCard(MinHandCount);
        }

        Log.Success("전투 데이터 로드 및 초기화 완료");
    }
    #endregion

    #region Core Logic
    /// <summary> 카드를 사용하고 결과를 처리합니다. </summary>
    public void ExecuteCard(int cardID, Unit caster, Unit target = null)
    {
        if (IsBattleOver) return;

        CardData data = DataManager.Instance.GetCard(cardID);
        if (data == null) return;

        // 효과 프로세서 실행 (시트 명령어 해석)
        EffectProcessor.Process(data.Effects, caster, target);

        // 덱 상태 업데이트
        _handCards.Remove(cardID);
        _discardPile.Add(cardID);

        // 실시간 리필 로직 (기획: 최소 5장 유지)
        if (_handCards.Count < MinHandCount)
        {
            DrawCard(1);
        }

        // 상태 저장 (난수 단계 포함)
        SaveManager.Instance.BattleData.randomStep = RandomUtil.CurrentStep;
        SaveManager.Instance.SaveBattle();

        // 게임 종료 체크
        CheckBattleOver();
    }
    #endregion

    #region Deck Management
    public void DrawCard(int count)
    {
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

            int cardID = _drawPile[0];
            _drawPile.RemoveAt(0);
            _handCards.Add(cardID);

            Log.Info($"카드 드로우: {cardID} (남은 덱: {_drawPile.Count})");
        }
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
