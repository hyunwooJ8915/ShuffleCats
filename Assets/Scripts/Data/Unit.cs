using DG.Tweening;
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

    public void OnTargeted()
    {
        // 화살표가 닿았을 때 살짝 커지거나 색깔이 변하는 연출
        transform.DOKill();
        transform.DOScale(1.15f, 0.15f).SetEase(Ease.OutBack);
    }

    public void OnUntargeted()
    {
        // 조준을 뗐을 때 원래대로
        transform.DOKill();
        transform.DOScale(1f, 0.15f);
    }

    public void TakeDamage(int amount)
    {
        CurrentHP -= amount;
        // 피격 흔들림 연출
        transform.DOShakePosition(0.2f, 0.5f);

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            // 사망 처리 로직
        }
    }
}
