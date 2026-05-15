using System.Collections.Generic;

/// <summary>
/// 카드 ID 범위와 Effects 문자열로 적 카드 여부 및 AI 우선 타입을 결정합니다.
///
/// Effects 포맷:
///   Deal : {Formula}    : {Target} [: {Repeat}]
///   Add  : {Tag}        : {Value}  : {Target}  [: {Repeat}]
///   Remove:{Tag}        : {Target}
///   Draw : {Count}      : {Target}
/// </summary>
public static class EnemyCardClassifier
{
    // 20000-29999: 적 전용 카드
    public static bool IsEnemyCard(int cardID) => cardID >= 20000 && cardID < 30000;

    private static readonly HashSet<string> BuffTags = new HashSet<string>
        { "Guard", "Strength", "Focus", "Evade", "Stealth", "Reflect", "Taunt" };

    private static readonly HashSet<string> DebuffTags = new HashSet<string>
        { "Bleed", "Capture" };

    /// <summary>
    /// Effects 문자열을 분석해 AI 행동 우선도 타입을 반환합니다.
    /// 다중 효과 카드는 가장 높은 우선 타입(enum 값이 작을수록 우선)을 따릅니다.
    /// </summary>
    public static EEnemyCardType Classify(string effects)
    {
        if (string.IsNullOrEmpty(effects) || effects == "-")
            return EEnemyCardType.Other;

        EEnemyCardType best = EEnemyCardType.Other;

        foreach (string segment in SplitEffects(effects))
        {
            EEnemyCardType t = ClassifySegment(segment);
            if (t < best) best = t;
        }
        return best;
    }

    // 세미콜론 분리 + Random(...) 내부 파이프 분리
    private static IEnumerable<string> SplitEffects(string effects)
    {
        foreach (string part in effects.Split(';'))
        {
            string s = part.Trim();
            if (s.StartsWith("Random(") && s.EndsWith(")"))
            {
                string inner = s.Substring(7, s.Length - 8);
                foreach (string sub in inner.Split('|'))
                    yield return sub.Trim();
            }
            else
            {
                yield return s;
            }
        }
    }

    private static EEnemyCardType ClassifySegment(string segment)
    {
        string[] p = segment.Split(':');
        if (p.Length < 2) return EEnemyCardType.Other;

        string action = p[0].Trim();

        switch (action)
        {
            case "Deal":
                return EEnemyCardType.Attack;

            case "Add":
                // Add : Tag : Value : Target
                if (p.Length < 4) return EEnemyCardType.Other;
                string tag   = p[1].Trim();
                string value = p[2].Trim();

                if (tag == "Heal") return EEnemyCardType.Heal;
                if (DebuffTags.Contains(tag)) return EEnemyCardType.Debuff;
                if (BuffTags.Contains(tag))
                    return value.StartsWith("-") ? EEnemyCardType.Debuff : EEnemyCardType.Buff;
                return EEnemyCardType.Other;

            case "Remove":
                // 디버프 제거는 버프 효과로 취급
                return EEnemyCardType.Buff;

            default:
                return EEnemyCardType.Other;
        }
    }
}
