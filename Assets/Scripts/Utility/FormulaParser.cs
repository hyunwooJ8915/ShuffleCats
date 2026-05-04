using System;
using UnityEngine;

public static class FormulaParser
{
    public static int Evaluate(string formula, Unit caster)
    {
        string processedFormula = formula;

        // Atk 키워드 처리 (기본 스탯 + 힘 태그)
        if (processedFormula.Contains("Atk"))
        {
            // 유닛의 기본 스탯(baseAtk)과 현재 부여된 힘(Strength) 태그를 합산
            int totalAtk = caster.BaseAtk + caster.GetTagValue(ETagType.Strength);
            processedFormula = processedFormula.Replace("Atk", totalAtk.ToString());
        }

        // 나머지 태그들 처리 (Focus, Guard 등)
        foreach (ETagType tagType in Enum.GetValues(typeof(ETagType)))
        {
            string tagName = tagType.ToString();
            if (processedFormula.Contains(tagName))
            {
                processedFormula = processedFormula.Replace(tagName, caster.GetTagValue(tagType).ToString());
            }
        }

        // 최종 수식 계산
        try
        {
            var result = new System.Data.DataTable().Compute(processedFormula, null);
            return Mathf.RoundToInt(System.Convert.ToSingle(result));
        }
        catch
        {
            Log.Error($"수식 해석 오류: {formula} -> {processedFormula}");
            return 0;
        }
    }
}
