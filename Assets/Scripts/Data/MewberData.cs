using System.Collections.Generic;

/// <summary>
/// [데이터 구조 정의]
/// 1. 구글 시트의 각 행(Row)과 1:1로 매칭되는 뮤버 데이터 클래스입니다.
/// 2. 변수명은 구글 시트의 헤더(첫 줄) 이름과 반드시 일치해야 자동 매칭됩니다.
/// </summary>

[System.Serializable]
public class MewberData
{
    public int ID;
    public string Name;
    public int Health;
    public int Attack;
    public int CardLimit;                       // 뮤버 하나가 들고 있을 수 있는 카드 한도
    public Dictionary<int, int> StartBundle;    // 기본 지급 카드 (ex : 10001:3;10002:1  →  10001번 카드 3장, 10002번 카드 1장)
}