using System.Collections.Generic;
using System;

// ─────────────────────────────────────────────
//  카드풀 직렬화 보조 타입
//  JsonUtility는 Dictionary를 지원하지 않으므로 List<CardEntry>로 저장합니다.
// ─────────────────────────────────────────────

[Serializable]
public class CardEntry
{
    public int cardID;
    public int count;
}

/// <summary>
/// 뮤버 한 명의 카드풀 저장 데이터.
/// CardLimit 이하의 총 장수를 초과해 카드를 보유할 수 없습니다.
/// </summary>
[Serializable]
public class MewberPoolSave
{
    public int             mewberID;
    public List<CardEntry> cards = new List<CardEntry>();

    /// <summary>현재 보유 카드 총 장수</summary>
    public int TotalCount()
    {
        int total = 0;
        foreach (var e in cards) total += e.count;
        return total;
    }

    /// <summary>List → Dictionary 변환 (런타임 조회용)</summary>
    public Dictionary<int, int> ToDictionary()
    {
        var dict = new Dictionary<int, int>();
        foreach (var e in cards) dict[e.cardID] = e.count;
        return dict;
    }

    /// <summary>Dictionary → List 변환 (저장용)</summary>
    public void FromDictionary(Dictionary<int, int> dict)
    {
        cards.Clear();
        foreach (var kv in dict)
            cards.Add(new CardEntry { cardID = kv.Key, count = kv.Value });
    }
}

// ─────────────────────────────────────────────
//  영지 세이브
// ─────────────────────────────────────────────

[Serializable]
public class EstateSaveData
{
    public string      uid;
    public string      UserName        = "Kedy";
    public int         gold            = 0;
    public List<int>   ownedMewberIDs  = new List<int>();
    public int         highestFloor    = 0;
    public bool        isTutorialComplete = false;
    public string      lastSavedTime;

    /// <summary>뮤버별 카드풀 (뮤버 획득 시 StartBundle로 초기화)</summary>
    public List<MewberPoolSave> mewberPools = new List<MewberPoolSave>();
}

[Serializable]
public class BattleSaveData
{
    // 파티 편성 ───────────────────────────────
    public List<int> partyMewberIDs = new List<int>();  // 최대 3명

    // 던전 & 맵 ───────────────────────────────
    public int         selectedDungeonID  = -1;
    public int         pendingNodeID      = -1;
    public EDifficulty selectedDifficulty = EDifficulty.Normal;
    public List<MapNodeSaveData> mapNodes = new List<MapNodeSaveData>();
    public int pendingEnemyGroupID = -1;  // 선택된 노드의 적 그룹 (Battle 씬 진입 전 세팅)

    // 전투 정보 ───────────────────────────────
    public bool isBattleActive = false;                                                 // 전투 여부
    public int currentFloor;                                                            // 현재 층
    public List<MewberBattleState> playerParty = new List<MewberBattleState>();         // 플레이어블 캐릭터 상태
    public List<EnemyBattleState> enemyParty = new List<EnemyBattleState>();            // 적군 캐릭터 상태

    public List<CardInstance> drawPile = new List<CardInstance>();                      // 뽑을 카드 더미
    public List<CardInstance> handCards = new List<CardInstance>();                     // 현재 손패
    public List<CardInstance> discardPile = new List<CardInstance>();                   // 버린 카드 더미

    public int nextInstanceID = 1;                                                      // 카드 인스턴스 식별자 발급용 카운터

    public List<EnemyActionState> pendingEnemyActions = new List<EnemyActionState>();   // 적군이 찜한 카드 (Behavior Tree)

    // 강종 치트 방지 ──────────────────────────
    public int seed;        // 초기 시드 값
    public int randomStep;  // 난수 발생 횟수
}

[Serializable]
public class MewberBattleState
{
    public int mewberID;
    public int currentHP;
    public int maxHP;    
    public int position;
    public bool isDead;
}

[Serializable]
public class EnemyBattleState
{
    public int enemyID;
    public int currentHP;
    public int maxHP;
    public int position;
    public bool isDead;
}

[Serializable]
public class EnemyActionState
{
    public int enemyIndex;      // 행동 주체 적군
    public int cardHandIndex;   // 손패 중 몇 번째 카드를 쓰려했는지 정보
    public int targetIndex;     // 사용 대상
    public bool isTargetPlayer; // 타겟이 아군인지 적군인지 구분
}
