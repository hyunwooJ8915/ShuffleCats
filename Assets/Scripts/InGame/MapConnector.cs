using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 두 맵 노드 사이를 잇는 선 UI입니다.
/// Image를 회전·신축해서 직선을 표현합니다.
/// MapManager가 노드 배치 후 생성합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MapConnector : MonoBehaviour
{
    [SerializeField] private Image _line;

    public void Connect(RectTransform from, RectTransform to)
    {
        Vector2 fromPos = from.anchoredPosition;
        Vector2 toPos   = to.anchoredPosition;
        Vector2 dir     = toPos - fromPos;

        var rect = GetComponent<RectTransform>();
        rect.anchoredPosition = (fromPos + toPos) * 0.5f;
        rect.sizeDelta        = new Vector2(dir.magnitude, rect.sizeDelta.y);
        rect.localRotation    = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    public void SetColor(Color color)
    {
        if (_line != null) _line.color = color;
    }
}
