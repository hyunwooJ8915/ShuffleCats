using System.Collections.Generic;

/// <summary>
/// [데이터 구조 정의]
/// 1. 구글 시트의 각 행(Row)과 1:1로 매칭되는 뮤버 데이터 클래스입니다.
/// 2. 변수명은 구글 시트의 헤더(첫 줄) 이름과 반드시 일치해야 자동 매칭됩니다.
/// </summary>
[System.Serializable]
public class MewberData
{
    public int    ID;
    public string Name;
    public int    Health;
    public int    Attack;
    public int    CardLimit;    // 원정 중 이 뮤버가 가질 수 있는 카드풀 최대 수량

    /// <summary>
    /// 기본 지급 카드풀 원본 문자열.
    /// 포맷: "카드ID:장수;카드ID:장수;…"  예) "10001:3;10002:1;10003:1"
    /// Dictionary는 JsonUtility가 직렬화하지 못하므로 string으로 저장합니다.
    /// </summary>
    // ── 스프라이트 주소 (Addressables 별칭) ──────────
    public string SpriteIdle;
    public string SpriteAttack;
    public string SpriteDamaged;
    public string SpriteSkill;
    public string SpriteFaceNormal;
    public string SpriteFaceEyesClosed;
    public string SpriteFaceDazed;

    public string StartBundle;

    /// <summary>
    /// StartBundle 문자열을 파싱해 Dictionary(카드ID → 장수)로 반환합니다.
    /// </summary>
    public Dictionary<int, int> ParseStartBundle()
    {
        var result = new Dictionary<int, int>();
        if (string.IsNullOrEmpty(StartBundle)) return result;

        foreach (string pair in StartBundle.Split(';'))
        {
            string[] kv = pair.Trim().Split(':');
            if (kv.Length == 2 &&
                int.TryParse(kv[0], out int cardID) &&
                int.TryParse(kv[1], out int count))
            {
                result[cardID] = count;
            }
        }
        return result;
    }
}
