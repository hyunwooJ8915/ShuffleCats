using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class HandUI : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private float _maxWidth = 1000f;
    [SerializeField] private float _curveStrength = 50f;
    [SerializeField] private float _maxRotation = 20f;
    [SerializeField] private float _maxSpacing = 150f;

    private List<CardUI> _activeCards = new List<CardUI>();
    public bool IsTargetingMode { get; private set; }

    // 현재 드래그 중인 카드가 있는지 확인하는 프로퍼티
    public bool IsAnyCardDragging { get; private set; }
    public RectTransform Rect => GetComponent<RectTransform>();

    public void RefreshHand(List<int> handIDs)
    {
        foreach (var card in _activeCards) CardPool.Instance.ReturnCard(card);
        _activeCards.Clear();

        for (int i = 0; i < handIDs.Count; i++)
        {
            CardUI card = CardPool.Instance.GetCard(transform);
            card.Init(handIDs[i]);
            _activeCards.Add(card);
        }
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        int count = _activeCards.Count;
        if (count == 0) return;

        // 기본 간격 및 가로폭 계산
        float currentSpacing = Mathf.Min(_maxSpacing, _maxWidth / Mathf.Max(1, count - 1));
        float totalWidth = (count - 1) * currentSpacing;

        for (int i = 0; i < count; i++)
        {
            CardUI card = _activeCards[i];

            // 타겟팅 모드일 때 해당 카드는 중앙(200) 위치를 유지해야 하므로 정렬 루프에서 제외
            if (card.IsDragging && IsTargetingMode) continue;

            // 기본 부채꼴 위치/회전값 계산
            float normalizeIdx = (count > 1) ? (i / (float)(count - 1) - 0.5f) * 2f : 0f;
            float posX = normalizeIdx * (totalWidth * 0.5f);
            float posY = (1f - (normalizeIdx * normalizeIdx)) * _curveStrength;
            float rotZ = normalizeIdx * -_maxRotation;

            float targetScale = 1.0f;
            int sortingOrder = i * 10;

            // 마우스 오버 상태 연출 (단, 다른 카드를 드래그 중이 아닐 때만)
            if (card.IsHovering && !IsAnyCardDragging)
            {
                targetScale = 1.25f;
                posY += 160f;
                rotZ = 0f;    // 가독성을 위해 각도 직각 고정
                sortingOrder = 500;
            }

            // 기존 트윈과 충돌 방지
            card.transform.DOKill();

            // 최종 연출 적용
            card.transform.DOLocalMove(new Vector3(posX, posY, 0f), 0.25f).SetEase(Ease.OutCubic);
            card.transform.DOLocalRotate(new Vector3(0, 0, rotZ), 0.25f);
            card.transform.DOScale(targetScale, 0.2f);

            // 시각적 레이어 순서 적용
            card.SetSortingOrder(sortingOrder);
        }
    }

    public void SetTargetingMode(bool active, CardUI card)
    {
        IsTargetingMode = active;
        if (TargetArrow.Instance != null) TargetArrow.Instance.SetActive(active);

        if (active)
        {
            // 타겟팅 모드 진입 시 카드를 중앙 고정 위치로 즉시 이동
            card.transform.DOKill(); // 기존 움직임 제거
            card.transform.DOLocalMove(new Vector3(0, 200f, 0), 0.2f).SetEase(Ease.OutBack);
            card.transform.DOLocalRotate(Vector3.zero, 0.2f);
            card.transform.DOScale(1.0f, 0.2f); // 고정 중일 땐 기본 크기 유지
            card.SetSortingOrder(1000);
        }
    }

    // 드래그 시작/종료 시 호출하여 상태 기록
    public void SetAnyCardDragging(bool dragging)
    {
        IsAnyCardDragging = dragging;
        if (!dragging) UpdateLayout();
    }

    public void HandleCardDrop(CardUI card, PointerEventData eventData)
    {
        IsTargetingMode = false;
        IsAnyCardDragging = false; // 드래그 종료 기록
        if (TargetArrow.Instance != null) TargetArrow.Instance.SetActive(false);

        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(eventData.position), Vector2.zero);
        Unit target = hit.collider?.GetComponent<Unit>();

        if (target != null && eventData.position.y > Screen.height * 0.35f)
        {
            ExecuteCardEffect(card, target);
        }
        else
        {
            UpdateLayout();
        }
    }

    private void ExecuteCardEffect(CardUI card, Unit target)
    {
        _activeCards.Remove(card);
        card.transform.DOLocalMoveY(1200f, 0.5f).SetEase(Ease.InBack);
        card.transform.DOScale(0, 0.4f).OnComplete(() => {
            CardPool.Instance.ReturnCard(card);
            UpdateLayout();
        });
    }
}