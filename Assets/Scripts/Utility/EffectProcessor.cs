using System;
using System.Collections.Generic;

public static class EffectProcessor
{
    /// <summary>
    /// 시트의 Effects 문자열을 받아 실제 효과를 실행
    /// </summary>
    public static void Process(string effectCommand, Unit caster, Unit targetFromUI = null)
    {
        // 세미콜론(;)으로 다중 효과 분리
        string[] commands = effectCommand.Split(';');

        foreach (string cmd in commands)
        {
            // 콜론(:)으로 인자 분리 (Action : Formula : Target : Repeat)
            string[] args = cmd.Split(':');
            string action = args[0].Trim();
            string formula = args[1].Trim();
            string targetKey = args[2].Trim();
            int repeat = args.Length > 3 ? int.Parse(args[3]) : 1;

            // 타겟 결정
            List<Unit> targets = TargetFinder.GetTargets(targetKey, caster, targetFromUI);

            // 반복 횟수만큼 효과 실행
            for (int i = 0; i < repeat; i++)
            {
                int finalValue = FormulaParser.Evaluate(formula, caster);
                Execute(action, finalValue, caster, targets, args);
            }
        }
    }

    private static void Execute(string action, int value, Unit caster, List<Unit> targets, string[] originalArgs)
    {
        foreach (var target in targets)
        {
            switch (action)
            {
                case "Deal":
                    // target.TakeDamage(value, caster);
                    break;
                case "Add":
                    // originalArgs[1]은 "Guard" 같은 태그 이름
                    ETagType tagType = Enum.Parse<ETagType>(originalArgs[1]);
                    target.AddTag(tagType, value);
                    break;
                case "Draw":
                    //BattleManager.Instance.DrawCard(value);
                    break;

                    // 추가 예정
            }
        }
    }
}
