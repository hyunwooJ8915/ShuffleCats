using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 파티 편성 씬의 던전 선택 항목 하나를 담당합니다.
/// </summary>
public class DungeonEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private Button          _selectButton;

    private int            _dungeonID;
    private Action<int>    _onSelect;

    public void Init(DungeonData data, Action<int> onSelect)
    {
        _dungeonID = data.ID;
        _onSelect  = onSelect;

        if (_nameText != null) _nameText.text = data.DungeonName;
        if (_infoText != null) _infoText.text = $"{data.FloorCount}층";

        if (_selectButton != null)
            _selectButton.onClick.AddListener(() => _onSelect?.Invoke(_dungeonID));
    }
}
