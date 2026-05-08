/// <summary>
/// [데이터 구조 정의]
/// 1. 구글 시트의 각 행(Row)과 1:1로 매칭되는 카드 데이터 클래스입니다.
/// 2. 변수명은 구글 시트의 헤더(첫 줄) 이름과 반드시 일치해야 자동 매칭됩니다.
/// </summary>
[System.Serializable]
public class CardData
{
    public int ID;
    public int OwnerID;
    public string Name;
    public string Effects;
    public string Description;
}
