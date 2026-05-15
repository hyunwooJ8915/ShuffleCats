/// <summary>
/// Effects 문자열에서 손패 카드를 대상으로 하는 효과를 분석합니다.
/// Discard:N:S → 손패에서 플레이어가 N장 직접 선택
/// DiscardAll:*:S → 손패 전체 버리기 (선택 불필요)
/// </summary>
public static class HandTargetHelper
{
    /// <summary>
    /// 플레이어가 손패에서 카드를 직접 선택해야 하는지 여부.
    /// (Discard 효과가 있고 N > 0 인 경우)
    /// </summary>
    public static bool NeedsInteractiveSelection(string effects)
        => GetDiscardCount(effects) > 0;

    /// <summary>
    /// 플레이어가 버릴 카드로 선택해야 할 총 장수를 반환합니다.
    /// 여러 Discard 효과가 있으면 합산합니다.
    /// </summary>
    public static int GetDiscardCount(string effects)
    {
        if (string.IsNullOrEmpty(effects)) return 0;
        int total = 0;

        foreach (string raw in effects.Split(';'))
        {
            string[] parts = raw.Trim().Split(':');
            if (parts.Length >= 2 && parts[0].Trim() == "Discard")
            {
                if (int.TryParse(parts[1].Trim(), out int n))
                    total += n;
            }
        }
        return total;
    }
}
