using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파티 편성 씬의 스크롤 목록에 뮤버 한 명을 표시하는 슬롯.
/// PartyFormationManager가 생성 및 관리합니다.
/// </summary>
public class MewberEntryUI : MonoBehaviour
{
    [Header("표시")]
    [SerializeField] private Image            _portrait;
    [SerializeField] private TextMeshProUGUI  _nameText;
    [SerializeField] private TextMeshProUGUI  _cardPoolText;
    [SerializeField] private GameObject       _assignedBadge;   // "편성됨!" 표시

    [Header("버튼")]
    [SerializeField] private Button           _assignButton;
    [SerializeField] private TextMeshProUGUI  _assignButtonText;

    public int MewberID { get; private set; }
    private bool _isAssigned;
    private Action<int> _onToggle;   // 편성/해제 요청 → Manager로 위임

    public void Init(MewberData data, MewberPoolSave pool, Action<int> onToggle)
    {
        MewberID = data.ID;
        _onToggle = onToggle;

        if (_nameText != null)
            _nameText.text = data.Name;

        if (_cardPoolText != null)
        {
            int current = pool?.TotalCount() ?? 0;
            _cardPoolText.text = $"카드풀 {current}/{data.CardLimit}";
        }

        // 초상화 로드
        if (_portrait != null && !string.IsNullOrEmpty(data.SpriteFaceNormal))
        {
            SpriteLoader.Instance.Load(data.SpriteFaceNormal, sprite =>
            {
                if (_portrait != null) _portrait.sprite = sprite;
            });
        }

        if (_assignButton != null)
            _assignButton.onClick.AddListener(() => _onToggle?.Invoke(MewberID));

        SetAssigned(false);
    }

    /// <summary>편성 상태를 갱신합니다. PartyFormationManager가 호출합니다.</summary>
    public void SetAssigned(bool assigned)
    {
        _isAssigned = assigned;

        if (_assignedBadge != null)    _assignedBadge.SetActive(assigned);
        if (_assignButtonText != null) _assignButtonText.text = assigned ? "편성 해제" : "편성하기";
    }
}
