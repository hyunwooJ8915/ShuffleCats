using System.Collections.Generic;

/// <summary>
/// 구글 시트 Enemys 탭과 1:1 매칭되는 적 개체 데이터.
/// Bundle  포맷 : "카드ID:장수;카드ID:장수;…"  예) "40001:3;40002:1"
/// InitialStatus 포맷 : "태그:값;태그:값;…"    예) "Guard:5;Strength:2"
/// Reward  포맷 : "gold:값;cardID:값;…"         예) "gold:10"
/// </summary>
[System.Serializable]
public class EnemyData
{
    public int    ID;
    public string Name;
    public int    Health;
    public int    Attack;
    public string Bundle;

    // 스프라이트 주소 (Addressables 별칭)
    public string SpriteIdle;
    public string SpriteAttack;
    public string SpriteDamaged;
    public string SpriteSkill;
    public string SpriteFaceNormal;
    public string SpriteFaceEyesClosed;
    public string SpriteFaceDazed;

    public string InitialStatus;
    public string Reward;

    /// <summary>Bundle 문자열 → Dictionary(카드ID, 장수)</summary>
    public Dictionary<int, int> ParseBundle()
    {
        var result = new Dictionary<int, int>();
        if (string.IsNullOrEmpty(Bundle)) return result;

        foreach (string pair in Bundle.Split(';'))
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

    /// <summary>InitialStatus 문자열 → Dictionary(태그명, 값)</summary>
    public Dictionary<string, int> ParseInitialStatus()
    {
        var result = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(InitialStatus)) return result;

        foreach (string pair in InitialStatus.Split(';'))
        {
            string[] kv = pair.Trim().Split(':');
            if (kv.Length == 2 && int.TryParse(kv[1], out int value))
                result[kv[0].Trim()] = value;
        }
        return result;
    }
}
