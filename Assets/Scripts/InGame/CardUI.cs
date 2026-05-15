using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Visual Components")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descText;
    [SerializeField] private Image _cardIllustration;
    [SerializeField] private GameObject _goldMark;
    [Tooltip("적 카드임을 표시하는 마커 (적 카드일 때 활성화)")]
    [SerializeField] private GameObject _enemyMark;
    [Tooltip("손패 선택 모드에서 선택됐을 때 표시하는 마커")]
    [SerializeField] private GameObject _selectionMark;

    public int InstanceID { get; private set; }
    public int CardID { get; private set; }
    public bool IsDragging { get; private set; }
    public bool IsHovering { get; private set; }
    public bool IsSelected { get; private set; }

    // Effects 파싱 결과
    public bool RequiresTarget { get; private set; }
    public bool IsEnemyCard { get; private set; }

    private HandUI _handUI;
    private Canvas _canvas;
    private CanvasGroup _group;
    private RectTransform _rect;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _group  = GetComponent<CanvasGroup>();
        _rect   = GetComponent<RectTransform>();
        _handUI = GetComponentInParent<HandUI>();
    }

    private HandUI Hand
    {
        get
        {
            if (_handUI == null) _handUI = GetComponentInParent<HandUI>();
            return _handUI;
        }
    }

    public void Init(CardInstance instance)
    {
        InstanceID = instance.InstanceID;
        CardID     = instance.CardID;

        transform.DOKill();
        transform.localRotation = Quaternion.identity;
        transform.localScale    = Vector3.one;
        IsDragging  = false;
        IsHovering  = false;
        IsSelected  = false;
        if (_group != null)
        {
            _group.blocksRaycasts = true;
            _group.alpha = 1f; // 풀 재사용 시 숨김 상태 초기화
        }
        OffOverrideSorting();
        if (_selectionMark != null) _selectionMark.SetActive(false);

        CardData data = DataManager.Instance.GetCard(CardID);
        if (data == null) return;

        _nameText.text = data.Name;
        _descText.text = data.Description;

        RequiresTarget = TargetTypeHelper.RequiresTarget(data.Effects);
        IsEnemyCard    = EnemyCardClassifier.IsEnemyCard(CardID);
        if (_enemyMark != null) _enemyMark.SetActive(IsEnemyCard);
    }

    public void SetInteractable(bool on)
    {
        if (_group != null) _group.blocksRaycasts = on;
    }

    public void SetSortingOrder(int order)
    {
        if (_canvas == null) return;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder    = order;
    }

    public void OffOverrideSorting()
    {
        if (_canvas != null) _canvas.overrideSorting = false;
    }

    /// <summary> 손패 선택 모드에서 이 카드의 선택 상태를 설정합니다. </summary>
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (_selectionMark != null) _selectionMark.SetActive(selected);
    }

    /// <summary> 카드 시각을 보이거나 숨깁니다. (알파값만 조정, 트윈 대상은 유지) </summary>
    public void SetVisible(bool visible)
    {
        if (_group != null) _group.alpha = visible ? 1f : 0f;
    }

    // ─────────────────────────────────────────────
    //  포인터 이벤트
    // ─────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsDragging || Hand == null) return;
        IsHovering = true;
        Hand.UpdateLayout();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsDragging || Hand == null) return;
        IsHovering = false;
        Hand.UpdateLayout();
    }

    // 손패 선택 모드에서 카드 클릭 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsDragging || Hand == null) return;
        Hand.OnCardClicked(this);
    }

    // ─────────────────────────────────────────────
    //  드래그 이벤트
    // ─────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 적 카드 또는 처리 중(락)에는 드래그 불가
        if (Hand == null || IsEnemyCard) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsProcessing) return;
        IsDragging = true;
        _group.blocksRaycasts = false;
        Hand.SetAnyCardDragging(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // BeginDrag에서 IsDragging이 설정되지 않은 경우(적 카드 등) 무시
        if (Hand == null || !IsDragging) return;

        if (eventData.position.y > Screen.height * 0.35f)
        {
            if (!Hand.IsTargetingMode) Hand.SetTargetingMode(true, this);

            if (RequiresTarget && TargetArrow.Instance != null)
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    Hand.Rect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector3 mouseWorldPos);

                TargetArrow.Instance.UpdateArrow(transform.position, mouseWorldPos, eventData.position);
            }
        }
        else
        {
            if (Hand.IsTargetingMode)
            {
                Hand.SetTargetingMode(false, this);
                Hand.UpdateLayout();
            }

            if (eventData.position.y > Screen.height * 0.15f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Hand.Rect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
                _rect.localPosition = new Vector3(_rect.localPosition.x, localPos.y, 0f);
            }
            else
            {
                Hand.UpdateLayout();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Hand == null || !IsDragging) return;
        IsDragging = false;
        IsHovering = false;
        _group.blocksRaycasts = true;
        Hand.HandleCardDrop(this, eventData);
    }
}
