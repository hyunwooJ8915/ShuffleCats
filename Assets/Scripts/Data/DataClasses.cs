using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [데이터 구조 정의]
/// 1. 구글 시트의 각 행(Row)과 1:1로 매칭되는 데이터 클래스를 정의합니다.
/// 2. ScriptableObject(SO)를 통해 파싱된 데이터를 유니티 에셋 형태로 로컬에 저장합니다.
/// 3. 주의: 변수명은 구글 시트의 헤더(첫 줄) 이름과 반드시 일치해야 자동 매칭됩니다.
/// </summary>

#region CardData
[System.Serializable]
public class CardData
{
    public int ID;
    public int OwnerID;
    public string Name;
    public string Effects;
    public string Description;
}

[CreateAssetMenu(fileName = "CardTable", menuName = "Data/CardTable")]
public class CardTable : ScriptableObject
{
    public List<CardData> cards = new List<CardData>();
}
#endregion

#region MewberData
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

[CreateAssetMenu(fileName = "MewberTable", menuName = "Data/MewberTable")]
public class MewberTable : ScriptableObject
{
    public List<MewberData> mewbers = new List<MewberData>();
}
#endregion
