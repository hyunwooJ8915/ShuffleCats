using System;
using System.Collections.Generic;
using UnityEngine;

public static class EffectProcessor
{
    /// <summary>
    /// Effects 문자열을 파싱해 실제 효과를 실행합니다.
    ///
    /// 문법 요약
    ///   Deal:formula:target[:repeat]
    ///   Add:tagType:amount:target[:repeat]
    ///   Remove:tagType/Debuff[:target]
    ///   Discard:N  |  DiscardAll  |  Draw:N
    ///   Random(effectA | effectB | effectC)
    ///
    /// 타겟 키
    ///   S=자신  E=선택한 적  EA=모든 적  ER=랜덤 적
    ///   P=선택한 아군  PA=모든 아군  PR=랜덤 아군
    ///   T=이전 효과의 대상  CE=포착된 적
    ///
    /// handTargetIDs: Discard 효과가 사용할 손패 카드 InstanceID 목록.
    /// </summary>
    public static void Process(string effectCommand, Unit caster, Unit targetFromUI = null, List<int> handTargetIDs = null)
    {
        Queue<int> handQueue  = handTargetIDs != null ? new Queue<int>(handTargetIDs) : null;
        List<Unit> lastTargets = new List<Unit>();

        foreach (string cmd in SplitBySemicolon(effectCommand))
            ExecuteCommand(cmd.Trim(), caster, targetFromUI, handQueue, lastTargets);
    }

    // ─────────────────────────────────────────────
    //  파싱 유틸
    // ─────────────────────────────────────────────

    // ';' 로 분리하되 Random(...) 내부의 ';' 는 무시
    private static List<string> SplitBySemicolon(string input)
    {
        var result = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if      (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ';' && depth == 0)
            {
                result.Add(input.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }
        result.Add(input.Substring(start).Trim());
        return result;
    }

    // ─────────────────────────────────────────────
    //  커맨드 디스패치
    // ─────────────────────────────────────────────

    private static void ExecuteCommand(string cmd, Unit caster, Unit selected, Queue<int> handQueue, List<Unit> lastTargets)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return;

        // Random(A | B | C)
        if (cmd.StartsWith("Random(") && cmd.EndsWith(")"))
        {
            string inner      = cmd.Substring(7, cmd.Length - 8);
            string[] options  = inner.Split('|');
            string chosen     = options[UnityEngine.Random.Range(0, options.Length)].Trim();
            ExecuteCommand(chosen, caster, selected, handQueue, lastTargets);
            return;
        }

        string[] args = cmd.Split(':');
        if (args.Length == 0) return;
        string action = args[0].Trim();

        switch (action)
        {
            case "Deal":      ExecuteDeal(args, caster, selected, lastTargets);    break;
            case "Add":       ExecuteAdd(args, caster, selected, lastTargets);     break;
            case "Remove":    ExecuteRemove(args, caster, selected, lastTargets);  break;
            case "Discard":   ExecuteDiscard(args, handQueue);                     break;
            case "DiscardAll":BattleManager.Instance.DiscardAllHandCards();        break;
            case "Draw":      ExecuteDraw(args);                                   break;
            default: Log.Error($"[EffectProcessor] 알 수 없는 액션: {action}");   break;
        }
    }

    // ─────────────────────────────────────────────
    //  Deal : formula : target [: repeat]
    //
    //  formula = 태그명 또는 사칙연산 수식
    //    Atk        → 시전자의 공격력 (BaseAtk + Strength)
    //    Guard      → 시전자의 방어도 태그 수
    //    Atk*Focus  → 공격력 × 집중 태그 수
    //
    //  피해는 대상의 Guard 태그가 먼저 흡수합니다.
    // ─────────────────────────────────────────────

    private static void ExecuteDeal(string[] args, Unit caster, Unit selected, List<Unit> lastTargets)
    {
        if (args.Length < 2) return;
        string formula   = args[1].Trim();
        string targetKey = args.Length > 2 ? args[2].Trim() : "S";
        int repeat       = args.Length > 3 && int.TryParse(args[3].Trim(), out int r) ? r : 1;

        List<Unit> targets = TargetFinder.GetTargets(targetKey, caster, selected, lastTargets);
        SetLastTargets(lastTargets, targets);

        const float hitInterval = 0.3f;
        for (int i = 0; i < repeat; i++)
        {
            int dmg      = FormulaParser.Evaluate(formula, caster);
            float delay  = i * hitInterval;
            bool isLast  = i == repeat - 1;
            foreach (var t in targets)
                t.TakeDamage(dmg, delay, isLast);
        }
    }

    // ─────────────────────────────────────────────
    //  Add : tagType : amount : target [: repeat]
    //
    //  amount = 정수 리터럴 또는 수식 (e.g. "8", "Atk")
    //  Heal 태그는 대상의 HP를 즉시 회복합니다.
    // ─────────────────────────────────────────────

    private static void ExecuteAdd(string[] args, Unit caster, Unit selected, List<Unit> lastTargets)
    {
        if (args.Length < 4) return;
        if (!Enum.TryParse(args[1].Trim(), out ETagType tag))
        {
            Log.Error($"[EffectProcessor] 알 수 없는 태그: {args[1]}");
            return;
        }
        string amountFormula = args[2].Trim();
        string targetKey     = args[3].Trim();
        int repeat           = args.Length > 4 && int.TryParse(args[4].Trim(), out int r) ? r : 1;

        List<Unit> targets = TargetFinder.GetTargets(targetKey, caster, selected, lastTargets);
        SetLastTargets(lastTargets, targets);

        for (int i = 0; i < repeat; i++)
        {
            int amount = FormulaParser.Evaluate(amountFormula, caster);
            foreach (var t in targets) t.AddTag(tag, amount);
        }
    }

    // ─────────────────────────────────────────────
    //  Remove : tagType / Debuff [: target]
    //
    //  Debuff 키워드 → 모든 디버프 제거 (Bleed, Capture …)
    //  target 생략 시 자신(S)
    // ─────────────────────────────────────────────

    private static void ExecuteRemove(string[] args, Unit caster, Unit selected, List<Unit> lastTargets)
    {
        if (args.Length < 2) return;
        string targetKey = args.Length > 2 ? args[2].Trim() : "S";
        List<Unit> targets = TargetFinder.GetTargets(targetKey, caster, selected, lastTargets);
        SetLastTargets(lastTargets, targets);

        if (args[1].Trim() == "Debuff")
        {
            foreach (var t in targets) t.RemoveAllDebuffs();
        }
        else if (Enum.TryParse(args[1].Trim(), out ETagType removeTag))
        {
            foreach (var t in targets) t.RemoveTag(removeTag);
        }
        else
        {
            Log.Error($"[EffectProcessor] Remove 대상 불명: {args[1]}");
        }
    }

    // ─────────────────────────────────────────────
    //  Discard : N
    // ─────────────────────────────────────────────

    private static void ExecuteDiscard(string[] args, Queue<int> handQueue)
    {
        if (args.Length < 2 || !int.TryParse(args[1].Trim(), out int count)) return;
        for (int i = 0; i < count && handQueue != null && handQueue.Count > 0; i++)
            BattleManager.Instance.MoveHandToDiscard(handQueue.Dequeue());
    }

    // ─────────────────────────────────────────────
    //  Draw : N
    // ─────────────────────────────────────────────

    private static void ExecuteDraw(string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1].Trim(), out int count)) return;
        BattleManager.Instance.DrawCard(count);
    }

    // ─────────────────────────────────────────────
    //  내부 유틸
    // ─────────────────────────────────────────────

    private static void SetLastTargets(List<Unit> lastTargets, List<Unit> resolved)
    {
        lastTargets.Clear();
        lastTargets.AddRange(resolved);
    }
}
