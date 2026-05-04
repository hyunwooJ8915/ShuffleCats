using System.Collections.Generic;

public static class TargetFinder
{
    public static List<Unit> GetTargets(string key, Unit caster, Unit selected)
    {
        List<Unit> result = new List<Unit>();

        switch (key)
        {
            case "S": // 시전자 자신
                result.Add(caster);
                break;
            case "E": // 선택한 적
                if (selected != null) result.Add(selected);
                break;
            case "ER": // 랜덤 적
                //result.Add(BattleManager.Instance.GetRandomEnemy(caster));
                break;
            case "EA": // 모든 적
                //result.AddRange(BattleManager.Instance.GetEnemies(caster));
                break;
                // PA, PR 등 추가
        }
        return result;
    }
}