using TMPro;
using UnityEngine;

/// <summary>
/// 유닛 위에 표시되는 상태 마커 하나.
/// 아이콘 SpriteRenderer + 스택 수 텍스트(TextMeshPro)로 구성됩니다.
/// StatusPanelUI가 생성 및 관리합니다.
/// </summary>
public class StatusMarkerUI : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _icon;
    [SerializeField] private TextMeshPro _countText;

    public ETagType TagType { get; private set; }

    public void Init(ETagType type, Sprite icon, Color color)
    {
        TagType = type;
        if (_icon != null)
        {
            if (icon != null) _icon.sprite = icon;
            _icon.color = color;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 스택 수를 갱신합니다. 0이면 마커를 숨깁니다.
    /// </summary>
    public void SetCount(int count)
    {
        gameObject.SetActive(count != 0);
        if (_countText != null)
            _countText.text = Mathf.Abs(count).ToString();
    }
}
