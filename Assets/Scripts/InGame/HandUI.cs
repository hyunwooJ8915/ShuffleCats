using System.Collections.Generic;
using TMPro;
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

    [Header("Hand Selection Popup")]
    [Tooltip("손패 선택 모드 진입 시 표시할 팝업 GameObject")]
    [SerializeField] private GameObject _selectionPopup;
    [Tooltip("팝업 안의 안내 텍스트")]
    [SerializeField] private TextMeshProUGUI _selectionPopupText;

    private List<CardUI> _activeCards = new List<CardUI>();
    public bool IsTargetingMode { get; private set; }
    public bool IsAnyCardDragging { get; private set; }
    public RectTransform Rect => GetComponent<RectTransform>();

    // ─────────────────────────────────────────────
    //  손패 선택 모드
    // ─────────────────────────────────────────────

    public bool IsSelectionMode { get; private set; }
    private int _selectionRequired;
    private List<CardUI> _selectedCards = new List<CardUI>();
    private System.Action<List<int>, List<CardUI>> _onSelectionComplete;

    // ─────────────────────────────────────────────
    //  손패 초기화 / 레이아웃
    // ─────────────────────────────────────────────

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

        float currentSpacing = Mathf.Min(_maxSpacing, _maxWidth / Mathf.Max(1, count - 1));
        float totalWidth     = (count - 1) * currentSpacing;

        for (int i = 0; i < count; i++)
        {
            CardUI card = _activeCards[i];

            if (card.IsDragging && IsTargetingMode) continue;

            float normalizeIdx = (count > 1) ? (i / (float)(count - 1) - 0.5f) * 2f : 0f;
            float posX         = normalizeIdx * (totalWidth * 0.5f);
            float posY         = (1f - (normalizeIdx * normalizeIdx)) * _curveStrength;
            float rotZ         = normalizeIdx * -_maxRotation;
            float targetScale  = 1.0f;
            int sortingOrder = (i + 1) * 10;

            if (card.IsHovering && !IsAnyCardDragging)
            {
                // 호버가 선택 상태보다 우선 (선택 모드 중에도 활성화)
                targetScale  = 1.25f;
                posY        += 160f;
                rotZ         = 0f;
                sortingOrder = 500;
            }
            else if (IsSelectionMode && card.IsSelected)
            {
                // 호버 중이 아닐 때만 선택 범프 적용
                posY        += 80f;
                sortingOrder = 500;
            }

            card.transform.DOKill();
            card.transform.DOLocalMove(new Vector3(posX, posY, 0f), 0.25f).SetEase(Ease.OutCubic);
            card.transform.DOLocalRotate(new Vector3(0, 0, rotZ), 0.25f);
            card.transform.DOScale(targetScale, 0.2f);
            card.SetSortingOrder(sortingOrder);
        }
    }

    public void SetTargetingMode(bool active, CardUI card)
    {
        IsTargetingMode = active;

        if (TargetArrow.Instance != null)
            TargetArrow.Instance.SetActive(active && card != null && card.RequiresTarget);

        if (active)
        {
            card.transform.DOKill();
            card.transform.DOLocalMove(new Vector3(0, 200f, 0), 0.2f).SetEase(Ease.OutBack);
            card.transform.DOLocalRotate(Vector3.zero, 0.2f);
            card.transform.DOScale(1.0f, 0.2f);
            card.SetSortingOrder(1000);
        }
    }

    public void SetAnyCardDragging(bool dragging)
    {
        IsAnyCardDragging = dragging;
        if (!dragging) UpdateLayout();
    }

    // ─────────────────────────────────────────────
    //  카드 드롭 처리 (아군 카드)
    // ─────────────────────────────────────────────

    public void HandleCardDrop(CardUI card, PointerEventData eventData)
    {
        IsTargetingMode = false;
        IsAnyCardDragging = false;
        if (TargetArrow.Instance != null) TargetArrow.Instance.SetActive(false);

        bool droppedHigh = eventData.position.y > Screen.height * 0.35f;

        if (!card.RequiresTarget && droppedHigh)
        {
            TryExecuteOrSelect(card, null);
        }
        else
        {
            RaycastHit2D hit    = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(eventData.position), Vector2.zero);
            Unit unitTarget = hit.collider?.GetComponent<Unit>();

            if (unitTarget != null && droppedHigh)
                TryExecuteOrSelect(card, unitTarget);
            else
                UpdateLayout();
        }
    }

    /// <summary>
    /// 손패 선택 효과(Discard:N)가 있으면 선택 모드를 먼저 진행하고,
    /// 없으면 즉시 카드를 실행합니다.
    /// </summary>
    private void TryExecuteOrSelect(CardUI card, Unit unitTarget)
    {
        CardData data        = DataManager.Instance.GetCard(card.CardID);
        int discardCount = data != null ? HandTargetHelper.GetDiscardCount(data.Effects) : 0;
        int selectable   = _activeCards.Count - 1; // 자신 제외

        BattleManager.Instance.SetProcessing(true); // ── 처리 락 ON ──

        if (discardCount > 0 && selectable > 0)
        {
            // 가운데 대기 카드: 숨김 처리 (손패 가림 방지)
            _activeCards.Remove(card);
            card.transform.DOKill();
            card.transform.DOLocalMove(new Vector3(0, 200f, 0), 0.2f).SetEase(Ease.OutBack);
            card.transform.DOLocalRotate(Vector3.zero, 0.2f);
            card.SetSortingOrder(1000);
            card.SetInteractable(false);
            card.SetVisible(false); // ← 숨김

            UpdateLayout();
            ShowSelectionPopup();

            int pendingID   = card.InstanceID;
            int actualCount = Mathf.Min(discardCount, _activeCards.Count);

            StartHandSelection(actualCount, (List<int> selectedIDs, List<CardUI> selectedUIs) =>
            {
                HideSelectionPopup();
                card.SetVisible(true); // ← 낙하 직전 다시 표시

                // 선택된 카드 낙하 (처리 락 해제 없이)
                foreach (var sel in selectedUIs)
                    PlayFallAnimation(sel, new List<CardInstance>(), releaseLock: false);

                List<CardInstance> drawn = BattleManager.Instance.ExecuteCard(pendingID, null, unitTarget, selectedIDs);
                UpdateLayout();
                PlayFallAnimation(card, drawn, releaseLock: true);
            });
        }
        else
        {
            ExecuteCardEffect(card, unitTarget);
        }
    }

    // ─────────────────────────────────────────────
    //  손패 선택 모드
    // ─────────────────────────────────────────────

    private void StartHandSelection(int count, System.Action<List<int>, List<CardUI>> onComplete)
    {
        IsSelectionMode      = true;
        _selectionRequired   = count;
        _selectedCards.Clear();
        _onSelectionComplete = onComplete;
        UpdateLayout();
    }

    /// <summary> CardUI.OnPointerClick → 여기서 선택/해제 처리 </summary>
    public void OnCardClicked(CardUI card)
    {
        if (!IsSelectionMode) return;

        if (card.IsSelected)
        {
            card.SetSelected(false);
            _selectedCards.Remove(card);
            UpdateLayout();
        }
        else if (_selectedCards.Count < _selectionRequired)
        {
            card.SetSelected(true);
            _selectedCards.Add(card);
            UpdateLayout();

            if (_selectedCards.Count >= _selectionRequired)
                ConfirmHandSelection();
        }
    }

    private void ConfirmHandSelection()
    {
        IsSelectionMode = false;

        var selectedIDs = new List<int>();
        var selectedUIs = new List<CardUI>(_selectedCards);

        foreach (var c in _selectedCards)
        {
            selectedIDs.Add(c.InstanceID);
            c.SetSelected(false);
            _activeCards.Remove(c);
        }
        _selectedCards.Clear();

        var callback     = _onSelectionComplete;
        _onSelectionComplete = null;
        callback?.Invoke(selectedIDs, selectedUIs);
    }

    // ─────────────────────────────────────────────
    //  선택 팝업
    // ─────────────────────────────────────────────

    private void ShowSelectionPopup()
    {
        if (_selectionPopup == null) return;
        _selectionPopup.SetActive(true);
        if (_selectionPopupText != null)
            _selectionPopupText.text = "카드를 선택하세요.";
    }

    private void HideSelectionPopup()
    {
        if (_selectionPopup == null) return;
        _selectionPopup.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  카드 실행 (아군)
    // ─────────────────────────────────────────────

    private void ExecuteCardEffect(CardUI card, Unit target)
    {
        CardData data = DataManager.Instance.GetCard(card.CardID);

        // DiscardAll 효과가 있으면 실행 전에 손패 스냅샷 (재생 카드 제외)
        // → 실행 후 비교하면 리필 카드와 뒤섞여 정확히 감지할 수 없음
        List<CardUI> handToDiscard = null;
        if (data != null && data.Effects.Contains("DiscardAll"))
        {
            handToDiscard = new List<CardUI>(_activeCards);
            handToDiscard.Remove(card);
        }

        _activeCards.Remove(card);
        List<CardInstance> drawn = BattleManager.Instance.ExecuteCard(card.InstanceID, null, target);

        if (handToDiscard != null && handToDiscard.Count > 0)
        {
            foreach (var c in handToDiscard) _activeCards.Remove(c);
            StartCoroutine(PlayDiscardAllAnimation(card, handToDiscard, drawn));
        }
        else
        {
            UpdateLayout();
            PlayFallAnimation(card, drawn, releaseLock: true);
        }
    }

    // 재생 카드 → 손패 오른쪽부터 한 장씩 낙하 → 드로우 카드 한 장씩 생성
    private System.Collections.IEnumerator PlayDiscardAllAnimation(
        CardUI mainCard, List<CardUI> handDiscards, List<CardInstance> drawn)
    {
        const float stagger = 0.08f;
        float fallTotal = _discardBounceDuration + _discardDuration;

        PlayFallAnimation(mainCard, new List<CardInstance>(), releaseLock: false);
        yield return new WaitForSeconds(stagger);

        for (int i = handDiscards.Count - 1; i >= 0; i--)
        {
            PlayFallAnimation(handDiscards[i], new List<CardInstance>(), releaseLock: false);
            if (i > 0) yield return new WaitForSeconds(stagger);
        }

        yield return new WaitForSeconds(fallTotal);

        foreach (var inst in drawn)
        {
            if (!_activeCards.Exists(c => c.InstanceID == inst.InstanceID))
                SpawnFromDrawPile(inst);
            UpdateLayout();
            yield return new WaitForSeconds(0.15f);
        }

        BattleManager.Instance.SetProcessing(false);
    }

    // ─────────────────────────────────────────────
    //  적 카드 사용 (EnemyAI 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// EnemyAI가 선택한 카드를 즉시 실행하고 낙하 연출을 재생합니다.
    /// </summary>
    public void PlayEnemyCard(CardInstance instance, Unit caster, Unit target)
    {
        CardUI card = _activeCards.Find(c => c.InstanceID == instance.InstanceID);
        if (card == null)
        {
            Log.Warning($"[HandUI] PlayEnemyCard: InstanceID={instance.InstanceID} 카드를 _activeCards에서 찾지 못함 (활성 카드 수={_activeCards.Count})");
            return;
        }

        BattleManager.Instance.SetProcessing(true); // ── 처리 락 ON ──

        _activeCards.Remove(card);
        List<CardInstance> drawn = BattleManager.Instance.ExecuteCard(instance.InstanceID, caster, target);
        UpdateLayout();
        PlayFallAnimation(card, drawn, releaseLock: true);
    }

    // ─────────────────────────────────────────────
    //  공용 유틸
    // ─────────────────────────────────────────────

    public void AddCard(CardInstance instance)
    {
        SpawnFromDrawPile(instance);
        UpdateLayout();
    }

    private void SpawnFromDrawPile(CardInstance instance)
    {
        CardUI newCard = CardPool.Instance.GetCard(transform);
        newCard.Init(instance);
        newCard.transform.DOKill();

        if (_drawPileAnchor != null)
        {
            Vector3 localFrom = transform.InverseTransformPoint(_drawPileAnchor.position);
            newCard.transform.localPosition = localFrom;
            newCard.transform.localRotation = Quaternion.identity;
            newCard.transform.localScale    = Vector3.one * _drawSpawnScale;
        }

        _activeCards.Add(newCard);
    }

    // ─────────────────────────────────────────────
    //  공통 낙하 연출
    //  releaseLock = true  → 애니메이션 완료 시 처리 락 해제 + 턴 카운트 증가
    //  releaseLock = false → 동시 낙하하는 보조 카드용 (선택 버리기 카드 등)
    // ─────────────────────────────────────────────

    private void PlayFallAnimation(CardUI card, List<CardInstance> drawn, bool releaseLock)
    {
        card.transform.DOKill();
        card.SetSortingOrder(0);
        card.SetInteractable(false);

        float startY = card.transform.localPosition.y;
        float startX = card.transform.localPosition.x;
        float tilt   = UnityEngine.Random.Range(-25f, 25f);
        float drift  = UnityEngine.Random.Range(-60f, 60f);
        float total  = _discardBounceDuration + _discardDuration;

        card.transform.DOLocalRotate(new Vector3(0, 0, tilt), total);
        card.transform.DOLocalMoveX(startX + drift, total).SetEase(Ease.OutQuad);

        Sequence seq = DOTween.Sequence();
        seq.Append(card.transform
            .DOLocalMoveY(startY + _discardBounceHeight, _discardBounceDuration)
            .SetEase(Ease.OutQuad));
        seq.Append(card.transform
            .DOLocalMoveY(_discardFallDistance, _discardDuration)
            .SetEase(Ease.InCubic));
        seq.OnComplete(() =>
        {
            CardPool.Instance.ReturnCard(card);
            foreach (CardInstance inst in drawn)
            {
                // SyncDiscardedCards 등에서 이미 스폰된 카드는 건너뜀
                if (_activeCards.Exists(c => c.InstanceID == inst.InstanceID)) continue;
                SpawnFromDrawPile(inst);
            }
            UpdateLayout();

            if (releaseLock)
                BattleManager.Instance.SetProcessing(false); // ── 처리 락 OFF, 턴 카운트 +1 ──
        });
    }
}
