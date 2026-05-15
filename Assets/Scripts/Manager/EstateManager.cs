using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Estate 씬을 관리합니다.
/// 현재는 원정 가기 버튼(PartyFormation 이동)만 구현되어 있습니다.
/// </summary>
public class EstateManager : MonoBehaviour
{
    [SerializeField] private Button _expeditionButton;

    private void Awake()
    {
        if (_expeditionButton != null)
            _expeditionButton.onClick.AddListener(OnExpedition);
    }

    private void OnExpedition()
    {
        StageManager.Instance.LoadStage(ESceneName.PartyFormation);
    }
}
