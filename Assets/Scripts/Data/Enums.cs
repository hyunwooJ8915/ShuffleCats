
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
    Estate,
    Battle
}


// 태그 타입 (카드 효과 태그)
public enum ETagType
{
    Heal,
    Guard,
    Strength,
    Focus,
    Stealth,
    Bleed,
    Capture,
    Evade,
}