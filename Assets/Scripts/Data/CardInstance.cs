using System;

/// <summary>
/// 덱 안에 존재하는 개별 카드 한 장의 식별 단위.
/// CardID(데이터)는 같아도 InstanceID는 매 카드마다 고유합니다.
/// </summary>
[Serializable]
public class CardInstance
{
    public int InstanceID;
    public int CardID;

    public CardInstance(int cardID, int instanceID)
    {
        CardID = cardID; 
        InstanceID = instanceID;        
    }
}
