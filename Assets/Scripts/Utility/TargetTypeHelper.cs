/// <summary>
/// Effects 문자열을 분석하여 카드의 대상 타입 정보를 제공하는 유틸리티.
/// "E"가 하나라도 포함되면 명시적 타겟팅 필요, 나머지는 자동 시전.
/// </summary>
public static class TargetTypeHelper
{
    /// <summary>
    /// 카드가 명시적인 대상 지정(타겟팅 화살표)을 필요로 하는지 반환.
    /// Effects에 "E" 키가 하나라도 있으면 true.
    /// </summary>
    public static bool RequiresTarget(string effects)
    {
        if (string.IsNullOrEmpty(effects)) return false;

        foreach (string effect in effects.Split(';'))
        {
            string[] parts = effect.Trim().Split(':');
            if (parts.Length >= 3)
            {
                string targetKey = parts[2].Trim();
                if (targetKey == "E" || targetKey == "P")
                    return true;
            }
        }
        return false;
    }
}
