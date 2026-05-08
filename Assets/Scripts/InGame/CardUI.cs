using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Components")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descText;
    [SerializeField] private Image _cardIllustration;
    [SerializeField] private GameObject _goldMark;

    public int InstanceID { get; private set; }
    public int CardID { get; private set; }
    public bool IsDragging { get; private set; }
    public bool IsHovering { get; private set; }

    private HandUI _handUI;
    private Canvas _canvas;
    private CanvasGroup _group;
    private RectTransform _rect;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _group = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();

        // 캐싱 시도
        _handUI = GetComponentInParent<HandUI>();
    }

    // _handUI가 null일 경우를 대비한 안전장치 프로퍼티
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
        CardID = instance.CardID;

        // 풀에서 재사용될 때 잔여 트윈/상태가 남아있을 수 있으므로 리셋
        transform.DOKill();
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        IsDragging = false;
        IsHovering = false;
        if (_group != null) _group.blocksRaycasts = true;
        OffOverrideSorting();

        CardData data = DataManager.Instance.GetCard(CardID);
        if (data == null) return;

        _nameText.text = data.Name;
        _descText.text = data.Description;
    }

    /// <summary> 외부에서 카드의 인터랙션 가능 여부를 토글 (떨어지는 중 잡지 못하게 등) </summary>
    public void SetInteractable(bool on)
    {
        if (_group != null) _group.blocksRaycasts = on;
    }

    public void SetSortingOrder(int order)
    {
        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = order;
        }
    }

    public void OffOverrideSorting()
    {
        if (_canvas != null)
        {
            _canvas.overrideSorting = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsDragging || Hand == null) return; // Hand가 null이면 리턴
        IsHovering = true;
        Hand.UpdateLayout();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsDragging || Hand == null) return;
        IsHovering = false;
        Hand.UpdateLayout();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Hand == null) return;
        IsDragging = true;
        _group.blocksRaycasts = false;
        Hand.SetAnyCardDragging(true); // 핸드 전체에 드래그 시작 보고
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Hand == null) return;

        if (eventData.position.y > Screen.height * 0.35f)
        {
            if (!Hand.IsTargetingMode) Hand.SetTargetingMode(true, this);

            if (TargetArrow.Instance != null)
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

            // 마우스가 일정 높이 이하일 때만 수동 위치 갱신
            if (eventData.position.y > Screen.height * 0.15f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    Hand.Rect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

                // X좌표 고정, Y좌표만 마우스 추적
                _rect.localPosition = new Vector3(_rect.localPosition.x, localPos.y, 0f);
            }
            else
            {
                // 너무 낮으면 원위치 복귀 정렬 루틴 작동
                Hand.UpdateLayout();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Hand == null) return;
        IsDragging = false;
        IsHovering = false;
        _group.blocksRaycasts = true;
        Hand.HandleCardDrop(this, eventData); // 여기서 SetAnyCardDragging(false) 처리됨
    }
}