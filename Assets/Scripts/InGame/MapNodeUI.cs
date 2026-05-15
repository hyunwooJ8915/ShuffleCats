using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 씬에서 노드 하나를 표시하는 버튼 UI입니다.
/// </summary>
public class MapNodeUI : MonoBehaviour
{
    [SerializeField] private Button          _button;
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private Image           _clearedOverlay;  // 클리어 시 반투명으로 표시
    [SerializeField] private Image           _availableGlow;   // 선택 가능 시 하이라이트

    public int NodeID { get; private set; }

    private Action<int> _onSelect;

    public void Init(MapNodeSaveData data, Action<int> onSelect)
    {
        NodeID    = data.nodeID;
        _onSelect = onSelect;

        if (_typeText != null)
            _typeText.text = NodeTypeLabel(data.type);

        Refresh(data);

        if (_button != null)
            _button.onClick.AddListener(() => _onSelect?.Invoke(NodeID));
    }

    public void Refresh(MapNodeSaveData data)
    {
        if (data.type == ENodeType.Start)
        {
            if (_button         != null) _button.interactable            = false;
            if (_clearedOverlay != null) _clearedOverlay.gameObject.SetActive(false);
            if (_availableGlow  != null) _availableGlow.gameObject.SetActive(false);
            return;
        }

        bool canSelect = data.isAvailable && !data.isCleared;

        if (_button         != null) _button.interactable            = canSelect;
        if (_clearedOverlay != null) _clearedOverlay.gameObject.SetActive(data.isCleared);
        if (_availableGlow  != null) _availableGlow.gameObject.SetActive(canSelect);
    }

    private static string NodeTypeLabel(ENodeType type) => type switch
    {
        ENodeType.Start  => "출발",
        ENodeType.Battle => "전투",
        ENodeType.Elite  => "엘리트",
        ENodeType.Boss   => "보스",
        ENodeType.Event  => "이벤트",
        ENodeType.Shop   => "상점",
        ENodeType.Rest   => "휴식",
        _                => "?"
    };
}
