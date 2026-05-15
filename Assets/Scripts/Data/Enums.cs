
/*
    열거형 자료를 일괄 관리하는 cs파일입니다.
    (열거형을 생성할 때 E를 붙여주고, 주석으로 어떤 역할인지 남길 것)
*/


// 현재 게임 상태
public enum EGameState
{
    None, 
    Loading, 
    Estate, 
    Battle
}


// 씬 이름
public enum ESceneName
{
    Logo,
    Loading,
    Intro,
    Estate,
    PartyFormation,
    Map,
    Battle
}


// 적 AI 카드 우선도 (낮은 값 = 높은 우선)
public enum EEnemyCardType
{
    Heal,    // 회복 – HP 30% 이하일 때만 사용
    Buff,    // 아군 버프
    Debuff,  // 적군 디버프
    Attack,  // 공격
    Other    // 기타(드로우 등)
}


// 맵 노드 타입
public enum ENodeType
{
    Start,
    Battle,
    Elite,
    Boss,
    Event,
    Shop,
    Rest,
}

// 원정 난이도
public enum EDifficulty
{
    Easy,
    Normal,
    Hard,
}

// 적 그룹 난이도
public enum EEnemyGroupType
{
    Normal,
    Elite,
    Boss,
}

// 유닛 스프라이트 상태
public enum EUnitSpriteType
{
    Idle,           // 대기
    Attack,         // 공격
    Damaged,        // 피해입음
    Skill,          // 스킬사용
    FaceNormal,     // 대기-기본얼굴
    FaceEyesClosed, // 대기-눈감음
    FaceDazed,      // 대기-헤롱헤롱
}


// 태그 타입 (카드 효과 태그 / 전투 중 상태 마커)
public enum ETagType
{
    // ── 버프 ──────────────────
    Heal,       // 회복
    Guard,      // 방어도
    Strength,   // 힘
    Focus,      // 집중
    Stealth,    // 은신
    Reflect,    // 반사
    Taunt,      // 도발
    Evade,      // 회피
    // ── 디버프 ────────────────
    Bleed,      // 출혈
    Capture,    // 포착
}