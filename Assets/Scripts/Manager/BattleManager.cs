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

    /// <summary>
    /// true인 동안은 아군·적군 모두 카드 상호작용이 불가합니다.
    /// 카드 한 장의 효과 처리 + 낙하 애니메이션이 완전히 끝나야 false로 돌아옵니다.
    /// </summary>
    public bool IsProcessing { get; private set; } = false;

    /// <summary>
    /// 카드 한 장이 완전히 처리될 때마다 1씩 증가하는 턴 카운터
    /// </summary>
    public int TurnCount { get; private set; } = 0;

    private HandUI _handUI;
    #endregion

    /// <summary>
    /// 외부(TutorialStarter, MapManager 등)에서 전투 준비 완료 후 호출합니다.
    /// 배틀 씬 로드 후 호출되므로, 이 시점에 씬 내 UI를 탐색합니다.
    /// </summary>
    public void BeginBattle()
    {
        SaveManager.Instance.BattleData.isBattleActive = true;
        SaveManager.Instance.SaveBattle();

        _handUI = FindFirstObjectByType<HandUI>();
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
    /// handTargetIDs: Discard 효과가 사용할 손패 카드 InstanceID 목록 (선택 순서 유지)
    /// 새로 드로우된 인스턴스 목록을 반환합니다.
    /// </summary>
    public List<CardInstance> ExecuteCard(int instanceID, Unit caster, Unit target = null, List<int> handTargetIDs = null)
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

        // 메인 카드를 먼저 손패에서 제거 (효과 실행 중 hand 카운트 정확성 유지)
        _handCards.Remove(instance);
        _discardPile.Add(instance);

        // caster가 없으면 살아있는 첫 번째 아군 유닛으로 대체
        if (caster == null)
            caster = PlayerParty.Find(u => !u.IsDead);

        // 효과 실행 전 손패 스냅샷 (Draw 효과로 추가된 카드 추적용)
        var handSnapshot = new System.Collections.Generic.HashSet<int>();
        foreach (var c in _handCards) handSnapshot.Add(c.InstanceID);

        EffectProcessor.Process(data.Effects, caster, target, handTargetIDs);

        // 효과 실행 중 추가된 카드 수집
        var effectDrawn = new List<CardInstance>();
        foreach (var c in _handCards)
            if (!handSnapshot.Contains(c.InstanceID)) effectDrawn.Add(c);

        // 최소 손패 유지 리필
        int drawNeeded = MinHandCount - _handCards.Count;
        var refillDrawn = drawNeeded > 0 ? DrawCard(drawNeeded) : new List<CardInstance>();

        var drawn = new List<CardInstance>(effectDrawn);
        drawn.AddRange(refillDrawn);

        SaveManager.Instance.BattleData.randomStep = RandomUtil.CurrentStep;
        SaveManager.Instance.SaveBattle();

        CheckBattleOver();

        return drawn;
    }
    #endregion

    #region Processing Lock & Turn Counter
    /// <summary>
    /// HandUI가 카드 처리 시작/종료 시 호출합니다.
    /// false로 전환될 때 TurnCount를 증가시킵니다.
    /// </summary>
    public void SetProcessing(bool processing)
    {
        IsProcessing = processing;
        if (!processing)
        {
            TurnCount++;
            Log.Info($"턴 {TurnCount} 완료");
        }
    }
    #endregion

    #region Hand Discard Helpers (EffectProcessor 전용)
    /// <summary>
    /// EffectProcessor의 Discard 효과에서 호출합니다.
    /// 리필 드로우 없이 카드 한 장을 손패 → 버린 카드 더미로 이동합니다.
    /// </summary>
    internal void MoveHandToDiscard(int instanceID)
    {
        CardInstance inst = _handCards.Find(c => c.InstanceID == instanceID);
        if (inst == null) return;
        _handCards.Remove(inst);
        _discardPile.Add(inst);
    }

    /// <summary>
    /// EffectProcessor의 DiscardAll 효과에서 호출합니다.
    /// 손패 전체를 버린 카드 더미로 이동합니다.
    /// </summary>
    internal void DiscardAllHandCards()
    {
        _discardPile.AddRange(_handCards);
        _handCards.Clear();
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

    #region Party Queries
    /// <summary> caster 기준으로 생존한 적군 파티 반환 </summary>
    public List<Unit> GetEnemies(Unit caster)
    {
        List<Unit> party = PlayerParty.Contains(caster) ? EnemyParty : PlayerParty;
        return party.FindAll(u => !u.IsDead);
    }

    /// <summary> caster 기준으로 생존한 랜덤 적 1명 반환 </summary>
    public Unit GetRandomEnemy(Unit caster)
    {
        List<Unit> enemies = GetEnemies(caster);
        return enemies.Count > 0 ? enemies[RandomUtil.Range(0, enemies.Count)] : null;
    }

    /// <summary> caster 기준으로 생존한 아군 파티 반환 </summary>
    public List<Unit> GetAllies(Unit caster)
    {
        List<Unit> party = PlayerParty.Contains(caster) ? PlayerParty : EnemyParty;
        return party.FindAll(u => !u.IsDead);
    }

    /// <summary> caster 기준으로 생존한 랜덤 아군 1명 반환 </summary>
    public Unit GetRandomAlly(Unit caster)
    {
        List<Unit> allies = GetAllies(caster);
        return allies.Count > 0 ? allies[RandomUtil.Range(0, allies.Count)] : null;
    }

    /// <summary>
    /// Unit.OnDead()에서 호출됩니다.
    /// 사망한 유닛을 파티에서 제거하고 전투 종료 여부를 확인합니다.
    /// </summary>
    public void NotifyUnitDead(Unit unit)
    {
        PlayerParty.Remove(unit);
        EnemyParty.Remove(unit);
        CheckBattleOver();
    }
    #endregion

    #region Battle Status
    private void CheckBattleOver()
    {
        if (IsBattleOver) return;
        // 전투 시작 전(양 파티 모두 미등록)은 스킵
        if (PlayerParty.Count == 0 && EnemyParty.Count == 0) return;

        bool allPlayersDead = PlayerParty.Count == 0 || PlayerParty.TrueForAll(u => u.IsDead);
        bool allEnemiesDead = EnemyParty.Count  == 0 || EnemyParty.TrueForAll(u => u.IsDead);

        if (allPlayersDead)
        {
            IsBattleOver = true;
            StartCoroutine(DelayedFinish(false));
        }
        else if (allEnemiesDead)
        {
            IsBattleOver = true;
            StartCoroutine(DelayedFinish(true));
        }
    }

    private System.Collections.IEnumerator DelayedFinish(bool isWin)
    {
        yield return new WaitForSeconds(1.0f);
        FinishBattle(isWin);
    }

    private void FinishBattle(bool isWin)
    {
        IsBattleOver = true;
        SaveManager.Instance.BattleData.isBattleActive = false;
        SaveManager.Instance.SaveBattle();

        if (isWin) Log.Success("전투 승리");
        else       Log.Error("전투 패배");

        FindFirstObjectByType<BattleResultUI>()?.Show(isWin);
    }
    #endregion
}
