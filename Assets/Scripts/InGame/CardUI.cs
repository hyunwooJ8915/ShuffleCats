using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Components")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descText;
    [SerializeField] private Image _cardIllustration;
    [SerializeField] private GameObject _goldMark;

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

    public void Init(int cardID)
    {
        CardID = cardID;
        CardData data = DataManager.Instance.GetCard(cardID);
        if (data == null) return;

        _nameText.text = data.Name;
        _descText.text = data.Description;
    }

    public void SetSortingOrder(int order)
    {
        if (_canvas != null) _canvas.sortingOrder = order;
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
                // 캔버스 모드가 Camera라면 RectTransformUtility를 쓰는 게 가장 정확합니다.
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    Hand.Rect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector3 mouseWorldPos);

                TargetArrow.Instance.UpdateArrow(transform.position, mouseWorldPos);
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