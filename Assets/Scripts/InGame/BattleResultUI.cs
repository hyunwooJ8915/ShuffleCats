using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 종료 시 표시되는 결과 패널.
/// - 승리/패배 텍스트 표시
/// - 나가기 버튼: 튜토리얼 또는 보스 층이면 Estate, 아니면 Map으로 이동
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    [SerializeField] private GameObject        _panel;
    [SerializeField] private TextMeshProUGUI   _resultText;
    [SerializeField] private TextMeshProUGUI   _subText;
    [SerializeField] private Button            _exitButton;

    private bool _isWin;

    private void Awake()
    {
        _panel.SetActive(false);
        _exitButton.onClick.AddListener(OnExit);
    }

    public void Show(bool isWin)
    {
        _isWin = isWin;
        _resultText.text = isWin ? "승리" : "패배";
        _subText.text    = isWin ? "전투에서 승리했습니다." : "전투에서 패배했습니다.";
        _panel.SetActive(true);
    }

    private void OnExit()
    {
        bool isTutorial = !SaveManager.Instance.EstateData.isTutorialComplete;
        bool isBoss     = GetCurrentNodeType() == ENodeType.Boss;

        if (isTutorial && _isWin)
        {
            SaveManager.Instance.EstateData.isTutorialComplete = true;
            SaveManager.Instance.ClearBattleSave();
            SaveManager.Instance.SaveEstate();
            StageManager.Instance.LoadStage(ESceneName.Estate);
        }
        else if (isBoss || !_isWin)
        {
            SaveManager.Instance.ClearBattleSave();
            StageManager.Instance.LoadStage(ESceneName.Estate);
        }
        else
        {
            StageManager.Instance.LoadStage(ESceneName.Map);
        }
    }

    private ENodeType GetCurrentNodeType()
    {
        int nodeID = SaveManager.Instance.BattleData.pendingNodeID;
        if (nodeID < 0) return ENodeType.Battle;

        var node = SaveManager.Instance.BattleData.mapNodes
                              .Find(n => n.nodeID == nodeID);
        return node != null ? node.type : ENodeType.Battle;
    }
}
