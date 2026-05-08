using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [덱 더미 뷰어]
/// 1. 버튼 클릭 시 뽑을 카드 더미 또는 버린 카드 더미를 패널에 정렬해 보여줍니다.
/// 2. 카드는 CardPool에서 빌려와 Layout Group이 붙은 _content 아래에 배치합니다.
///    (Grid Layout Group / Vertical Layout Group 등 어느 것이든 정상 동작)
/// 3. 표시되는 카드는 부모가 HandUI가 아니므로 드래그/호버 인터랙션이 자동으로 비활성화됩니다.
/// </summary>
public class PileViewerUI : MonoBehaviour
{
    public enum EPileType { Draw, Discard }

    [Header("UI Refs")]
    [SerializeField] private GameObject _panel;             // 뷰어 전체 패널 (열고 닫는 루트)
    [SerializeField] private RectTransform _content;        // Layout Group이 붙은 카드 컨테이너
    [SerializeField] private TextMeshProUGUI _titleText;    // 제목 + 카드 수 표시

    [Header("Trigger Buttons")]
    [SerializeField] private Button _drawPileButton;        // "뽑을 카드" 버튼
    [SerializeField] private Button _discardPileButton;     // "버린 카드" 버튼
    [SerializeField] private Button _closeButton;           // 닫기 버튼

    private readonly List<CardUI> _displayCards = new List<CardUI>();

    private void Awake()
    {
        if (_drawPileButton != null) _drawPileButton.onClick.AddListener(ShowDrawPile);
        if (_discardPileButton != null) _discardPileButton.onClick.AddListener(ShowDiscardPile);
        if (_closeButton != null) _closeButton.onClick.AddListener(Hide);

        if (_panel != null) _panel.SetActive(false);
    }

    #region Public API
    public void ShowDrawPile() => Show(EPileType.Draw);
    public void ShowDiscardPile() => Show(EPileType.Discard);

    public void Show(EPileType type)
    {
        if (BattleManager.Instance == null) return;

        IReadOnlyList<CardInstance> source = (type == EPileType.Draw)
            ? BattleManager.Instance.DrawPile
            : BattleManager.Instance.DiscardPile;

        string title = (type == EPileType.Draw) ? "뽑을 카드" : "버린 카드";
        Populate(title, source);

        if (_panel != null) _panel.SetActive(true);
    }

    public void Hide()
    {
        ClearDisplay();
        if (_panel != null) _panel.SetActive(false);
    }
    #endregion

    #region Internal
    private void Populate(string title, IReadOnlyList<CardInstance> source)
    {
        ClearDisplay();

        if (_titleText != null) _titleText.text = $"{title} ({source.Count})";

        // CardID 오름차순 정렬 → 같은 카드끼리 모여 보이도록
        var sorted = new List<CardInstance>(source);
        sorted.Sort((a, b) => a.CardID.CompareTo(b.CardID));

        foreach (CardInstance inst in sorted)
        {
            CardUI card = CardPool.Instance.GetCard(_content);
            card.Init(inst);
            _displayCards.Add(card);

            card.OffOverrideSorting();
        }
    }

    private void ClearDisplay()
    {
        foreach (var c in _displayCards) CardPool.Instance.ReturnCard(c);
        _displayCards.Clear();
    }
    #endregion
}
