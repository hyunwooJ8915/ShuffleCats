using System.Collections.Generic;
using System.Linq;

public static class TargetFinder
{
    /// <summary>
    /// 타겟 키를 해석해 대상 유닛 목록을 반환합니다.
    ///
    /// 타겟 키 목록
    ///   S   자신
    ///   E   선택한 적 (UI 지정)
    ///   EA  모든 적
    ///   ER  랜덤 적 1명
    ///   P   선택한 아군 (UI 지정)
    ///   PA  모든 아군
    ///   PR  랜덤 아군 1명
    ///   T   이전 효과의 대상과 동일 (lastTargets 재사용)
    ///   CE  포착(Capture) 태그가 있는 적
    /// </summary>
    public static List<Unit> GetTargets(string key, Unit caster, Unit selected, List<Unit> lastTargets = null)
    {
        var result = new List<Unit>();
        switch (key)
        {
            case "S":
                if (caster != null) result.Add(caster);
                break;

            case "E":   // 선택한 적
            case "P":   // 선택한 아군 — 둘 다 UI 지정 대상을 사용
                if (selected != null) result.Add(selected);
                break;

            case "T":   // 이전 효과에서 결정된 대상 재사용
                if (lastTargets != null && lastTargets.Count > 0)
                    result.AddRange(lastTargets);
                else if (selected != null)
                    result.Add(selected);
                break;

            case "ER":
                Unit rEnemy = BattleManager.Instance.GetRandomEnemy(caster);
                if (rEnemy != null) result.Add(rEnemy);
                break;

            case "EA":
                result.AddRange(BattleManager.Instance.GetEnemies(caster));
                break;

            case "PA":
            case "AA":  // 하위 호환
                result.AddRange(BattleManager.Instance.GetAllies(caster));
                break;

            case "PR":
            case "AR":  // 하위 호환
                Unit rAlly = BattleManager.Instance.GetRandomAlly(caster);
                if (rAlly != null) result.Add(rAlly);
                break;

            case "CE":  // 포착(Capture) 태그가 부여된 적
                result.AddRange(
                    BattleManager.Instance.GetEnemies(caster)
                        .Where(u => u.GetTagValue(ETagType.Capture) > 0));
                break;

            default:
                Log.Error($"[TargetFinder] 알 수 없는 타겟 키: {key}");
                break;
        }
        return result;
    }
}
