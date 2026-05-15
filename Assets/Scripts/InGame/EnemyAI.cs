using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실시간으로 손패의 적 카드를 자동 사용하는 AI 컨트롤러.
///
/// 비해이비어트리 우선도:
///   1. 회복(Heal)  – 아군 중 HP 30% 이하인 유닛이 있을 때만 사용
///   2. 버프(Buff)  – 회복이 불필요하거나 없을 때
///   3. 디버프(Debuff)
///   4. 공격(Attack)
///   5. 기타(Other)
///
/// 회복 카드는 HP 30%가 넘으면 절대 사용하지 않으므로,
/// 체력이 충분할 때 회복 카드가 손패에 쌓여도 아껴두는 지능적 플레이가 자연스럽게 구현됩니다.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Tooltip("적 AI 난이도 설정 에셋. 비어있으면 기본값을 사용합니다.")]
    [SerializeField] private EnemyDifficultySettings _difficulty;

    // 에셋이 없을 때 사용하는 기본값 (지연 초기화)
    private static EnemyDifficultySettings _default;
    private EnemyDifficultySettings D
    {
        get
        {
            if (_difficulty != null) return _difficulty;
            if (_default == null) _default = CreateDefaultSettings();
            return _default;
        }
    }

    private static EnemyDifficultySettings CreateDefaultSettings()
    {
        var s = ScriptableObject.CreateInstance<EnemyDifficultySettings>();
        s.ActionInterval   = 3f;
        s.InitialDelay     = 2f;
        s.IntervalVariance = 0.5f;
        s.HealHpThreshold  = 0.3f;
        return s;
    }

    private HandUI _handUI;
    private float _timer;

    private void Start()
    {
        _handUI = FindFirstObjectByType<HandUI>();
        _timer  = -D.InitialDelay; // 음수로 시작해서 InitialDelay만큼 첫 행동 지연
    }

    private void Update()
    {
        if (BattleManager.Instance == null || BattleManager.Instance.IsBattleOver) return;

        _timer += Time.deltaTime;
        if (_timer < D.ActionInterval) return;

        _timer = -RandomUtil.Range(0f, D.IntervalVariance); // 다음 간격에 랜덤 오프셋
        TryPlayCard();
    }

    // ─────────────────────────────────────────────
    //  비해이비어트리 진입점
    // ─────────────────────────────────────────────

    private void TryPlayCard()
    {
        if (BattleManager.Instance.IsProcessing)
        {
            Log.Info("[EnemyAI] 대기 중 (IsProcessing=true)");
            return;
        }

        if (_handUI == null)
        {
            Log.Warning("[EnemyAI] HandUI 참조 없음 — FindFirstObjectByType 재시도");
            _handUI = FindFirstObjectByType<HandUI>();
            if (_handUI == null) return;
        }

        var candidates = CollectEnemyCandidates();
        Log.Info($"[EnemyAI] 손패 전체 {BattleManager.Instance.HandCards.Count}장, 적 카드 후보 {candidates.Count}장, 적 파티 {BattleManager.Instance.EnemyParty.Count}명");
        if (candidates.Count == 0) return;

        (CardInstance inst, Unit caster, Unit target) = SelectAction(candidates);
        if (inst == null)
        {
            Log.Warning("[EnemyAI] SelectAction 결과 null (살아있는 적 없음?)");
            return;
        }

        Log.Info($"[EnemyAI] 카드 사용 시도: CardID={inst.CardID} InstanceID={inst.InstanceID}, caster={caster?.UnitName}, target={target?.UnitName}");
        _handUI.PlayEnemyCard(inst, caster, target);
    }

    // ─────────────────────────────────────────────
    //  후보 카드 수집
    // ─────────────────────────────────────────────

    private struct Candidate
    {
        public CardInstance Inst;
        public CardData     Data;
        public EEnemyCardType Type;
    }

    private List<Candidate> CollectEnemyCandidates()
    {
        var list = new List<Candidate>();
        foreach (var inst in BattleManager.Instance.HandCards)
        {
            if (!EnemyCardClassifier.IsEnemyCard(inst.CardID)) continue;
            CardData data = DataManager.Instance.GetCard(inst.CardID);
            if (data == null) continue;
            list.Add(new Candidate
            {
                Inst = inst,
                Data = data,
                Type = EnemyCardClassifier.Classify(data.Effects)
            });
        }
        return list;
    }

    // ─────────────────────────────────────────────
    //  비해이비어트리: 행동 결정
    // ─────────────────────────────────────────────

    private (CardInstance, Unit, Unit) SelectAction(List<Candidate> candidates)
    {
        // 1. 회복 브랜치: 아군 중 HP 임계값 이하인 유닛 존재 시에만 진입
        Unit woundedAlly = FindLowestHPEnemy(threshold: D.HealHpThreshold);
        if (woundedAlly != null)
        {
            var healCard = FindFirst(candidates, EEnemyCardType.Heal);
            if (healCard.Inst != null)
            {
                // 시전자 = 체력이 가장 낮은 적군 유닛 (회복 대상이 자신)
                Unit target = NeedsExplicitTarget(healCard.Data) ? GetRandomPlayer() : null;
                return (healCard.Inst, woundedAlly, target);
            }
        }

        // 2-5. 버프 → 디버프 → 공격 → 기타
        EEnemyCardType[] priority = { EEnemyCardType.Buff, EEnemyCardType.Debuff,
                                      EEnemyCardType.Attack, EEnemyCardType.Other };
        foreach (var type in priority)
        {
            var card = FindFirst(candidates, type);
            if (card.Inst == null) continue;

            Unit caster = GetRandomLivingEnemy();
            if (caster == null) return (null, null, null);

            Unit target = NeedsExplicitTarget(card.Data) ? GetRandomPlayer() : null;
            return (card.Inst, caster, target);
        }

        return (null, null, null);
    }

    // ─────────────────────────────────────────────
    //  헬퍼
    // ─────────────────────────────────────────────

    private Candidate FindFirst(List<Candidate> candidates, EEnemyCardType type)
    {
        foreach (var c in candidates)
            if (c.Type == type) return c;
        return default;
    }

    /// <summary> 살아있는 적군 중 HP 비율이 threshold 이하인 유닛 중 가장 낮은 HP 반환 </summary>
    private Unit FindLowestHPEnemy(float threshold)
    {
        Unit result = null;
        float lowestRatio = threshold;

        foreach (Unit u in BattleManager.Instance.EnemyParty)
        {
            if (u.CurrentHP <= 0) continue;
            float ratio = u.MaxHp > 0 ? (float)u.CurrentHP / u.MaxHp : 1f;
            if (ratio <= lowestRatio)
            {
                lowestRatio = ratio;
                result = u;
            }
        }
        return result;
    }

    private Unit GetRandomLivingEnemy()
    {
        var living = new List<Unit>();
        foreach (Unit u in BattleManager.Instance.EnemyParty)
            if (u.CurrentHP > 0) living.Add(u);

        if (living.Count == 0) return null;
        return living[RandomUtil.Range(0, living.Count)];
    }

    /// <summary> 적군(AI) 기준으로 플레이어(아군) 중 랜덤 1명 반환 </summary>
    private Unit GetRandomPlayer()
    {
        var living = new List<Unit>();
        foreach (Unit u in BattleManager.Instance.PlayerParty)
            if (u.CurrentHP > 0) living.Add(u);

        if (living.Count == 0) return null;
        return living[RandomUtil.Range(0, living.Count)];
    }

    /// <summary> 카드 Effects에 "E" 타겟이 있으면 명시적 Unit 타겟 필요 </summary>
    private bool NeedsExplicitTarget(CardData data)
        => TargetTypeHelper.RequiresTarget(data.Effects);
}
