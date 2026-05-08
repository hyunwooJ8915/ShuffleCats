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

    [Header("Pile Anchors (선택)")]
    [Tooltip("뽑을 카드 더미 아이콘의 RectTransform. 드로우 카드가 이 위치에서 손패로 날아옵니다.")]
    [SerializeField] private RectTransform _drawPileAnchor;
    [Tooltip("드로우 시작 시 카드 스케일 (작게 시작 → UpdateLayout이 1.0으로 확대)")]
    [SerializeField] private float _drawSpawnScale = 0.6f;

    [Header("Discard Animation")]
    [Tooltip("바운스 시 위로 튕길 Y 거리(양수)")]
    [SerializeField] private float _discardBounceHeight = 120f;
    [Tooltip("바운스 진행 시간(초)")]
    [SerializeField] private float _discardBounceDuration = 0.15f;
    [Tooltip("카드 사용 시 떨어질 Y 거리(로컬 기준, 음수면 아래로 낙하)")]
    [SerializeField] private float _discardFallDistance = -1500f;
    [Tooltip("낙하 진행 시간(초)")]
    [SerializeField] private float _discardDuration = 0.55f;

    private List<CardUI> _activeCards = new List<CardUI>();
    public bool IsTargetingMode { get; private set; }

    // 현재 드래그 중인 카드가 있는지 확인하는 프로퍼티
    public bool IsAnyCardDragging { get; private set; }
    public RectTransform Rect => GetComponent<RectTransform>();

    public void RefreshHand(List<CardInstance> hand)
    {
        foreach (var card in _activeCards) CardPool.Instance.ReturnCard(card);
        _activeCards.Clear();

        for (int i = 0; i < hand.Count; i++)
            SpawnFromDrawPile(hand[i]);

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
        IsAnyCardDragging = false;
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
        int instanceID = card.InstanceID;
        _activeCards.Remove(card);

        // 덱 데이터 업데이트 (버리기 + 드로우) 후 새로 뽑힌 인스턴스 수령
        List<CardInstance> drawn = BattleManager.Instance.ExecuteCard(instanceID, null, target);

        // 나머지 카드 즉시 재정렬
        UpdateLayout();

        // 카드 퇴장 연출: 위로 살짝 튕긴 뒤 화면 하단으로 낙하
        card.transform.DOKill();
        card.SetSortingOrder(2000);   // 떨어지는 동안 다른 카드 위로 보이도록
        card.SetInteractable(false);  // 낙하 중인 카드 다시 못 잡도록 차단

        float startY = card.transform.localPosition.y;
        float startX = card.transform.localPosition.x;
        float tilt = UnityEngine.Random.Range(-25f, 25f);
        float drift = UnityEngine.Random.Range(-60f, 60f);
        float totalDuration = _discardBounceDuration + _discardDuration;

        // 회전과 가로 표류는 전체 듀레이션에 걸쳐 천천히 진행
        card.transform.DOLocalRotate(new Vector3(0, 0, tilt), totalDuration);
        card.transform.DOLocalMoveX(startX + drift, totalDuration).SetEase(Ease.OutQuad);

        // Y축은 시퀀스: 살짝 위로 튕긴 후 → 가속하며 낙하
        Sequence fallSeq = DOTween.Sequence();
        fallSeq.Append(card.transform
            .DOLocalMoveY(startY + _discardBounceHeight, _discardBounceDuration)
            .SetEase(Ease.OutQuad));
        fallSeq.Append(card.transform
            .DOLocalMoveY(_discardFallDistance, _discardDuration)
            .SetEase(Ease.InCubic));
        fallSeq.OnComplete(() =>
        {
            CardPool.Instance.ReturnCard(card);

            // 새로 뽑힌 카드들은 뽑을 카드 더미 위치에서 솟아오르도록 배치
            foreach (CardInstance inst in drawn) SpawnFromDrawPile(inst);
            UpdateLayout();
        });
    }

    /// <summary> 손패에 카드 한 장을 추가합니다. (외부에서 직접 추가할 때 사용) </summary>
    public void AddCard(CardInstance instance)
    {
        SpawnFromDrawPile(instance);
        UpdateLayout();
    }

    /// <summary>
    /// 카드 한 장을 풀에서 가져와 _drawPileAnchor 위치에 배치한 뒤 _activeCards에 추가합니다.
    /// 호출 후 UpdateLayout()이 실행되면 anchor → 손패 슬롯으로 자연스럽게 보간됩니다.
    /// </summary>
    private void SpawnFromDrawPile(CardInstance instance)
    {
        CardUI newCard = CardPool.Instance.GetCard(transform);
        newCard.Init(instance);
        newCard.transform.DOKill();

        if (_drawPileAnchor != null)
        {
            // anchor의 World Position을 HandUI의 Local로 변환
            Vector3 localFrom = transform.InverseTransformPoint(_drawPileAnchor.position);
            newCard.transform.localPosition = localFrom;
            newCard.transform.localRotation = Quaternion.identity;
            newCard.transform.localScale = Vector3.one * _drawSpawnScale;
        }

        _activeCards.Add(newCard);
    }
}