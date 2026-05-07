using System.Collections.Generic;
using UnityEngine;

public class CardPool : Singleton<CardPool>
{
    [SerializeField] private GameObject _cardPrefab;
    private Stack<CardUI> _pool = new Stack<CardUI>();

    public CardUI GetCard(Transform parent)
    {
        CardUI card = _pool.Count > 0 ? _pool.Pop() : Instantiate(_cardPrefab).GetComponent<CardUI>();
        card.transform.SetParent(parent);
        card.transform.localScale = Vector3.one;
        card.gameObject.SetActive(true);
        return card;
    }

    public void ReturnCard(CardUI card)
    {
        card.gameObject.SetActive(false);
        card.transform.SetParent(transform);
        _pool.Push(card);
    }
}
