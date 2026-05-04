using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int BaseAtk {  get; private set; }
    public int MaxHp { get; private set; }
    public int CurrentHP { get; private set; }

    public Dictionary<ETagType, int> Tags = new Dictionary<ETagType, int>();

    public int GetTagValue(ETagType type)
    {
        return Tags.TryGetValue(type, out int value) ? value : 0;
    }

    public void AddTag(ETagType type, int amount)
    {
        if (Tags.ContainsKey(type)) Tags[type] += amount;
        else Tags[type] = amount;

        Log.Info($"{name}의 {type} 태그가 {amount}만큼 변동되었습니다. (현재: {Tags[type]})");
    }
}
