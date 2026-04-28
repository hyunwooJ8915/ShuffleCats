using System.Collections.Generic;
using System;

[Serializable]
public class EstateSaveData
{
    public string uid;
    public string UserName = "Kedy";
    public int gold = 0;
    public List<int> ownedMewberIDs = new List<int>();
    public int highestFloor = 0;
    public string lastSavedTime;
}

[Serializable]
public class BattleSaveData
{
    // 전투 정보 ───────────────────────────────
    public bool isBattleActive = false;                                                 // 전투 여부
    public int currentFloor;                                                            // 현재 층
    public List<MewberBattleState> playerParty = new List<MewberBattleState>();         // 플레이어블 캐릭터 상태
    public List<EnemyBattleState> enemyParty = new List<EnemyBattleState>();            // 적군 캐릭터 상태

    public List<int> drawPile = new List<int>();                                        // 뽑을 카드 더미
    public List<int> handCards = new List<int>();                                       // 현재 손패
    public List<int> discardPile = new List<int>();                                     // 버린 카드 더미

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
