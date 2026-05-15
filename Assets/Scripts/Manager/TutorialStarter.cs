using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Battle 씬에 배치합니다. 신규 유저일 때만 활성화합니다.
/// 최초 지급된 뮤버로 파티를 자동 편성하고 전투를 시작합니다.
/// 전투 종료 후 isTutorialComplete = true로 저장합니다.
/// </summary>
public class TutorialStarter : MonoBehaviour
{
    [Tooltip("튜토리얼 전투에 사용할 랜덤 시드")]
    [SerializeField] private int _seed = 42;

    private void Start()
    {
        if (!SaveManager.Instance.EstateData.isTutorialComplete)
        {
            SetupTutorialParty();
            SetupTutorialDeck();
        }

        FindFirstObjectByType<BattleSetup>()?.Execute();
    }

    private void SetupTutorialParty()
    {
        MewberData starter = DataManager.Instance.GetFirstMewber();
        if (starter == null)
        {
            Log.Error("[TutorialStarter] 스타터 뮤버를 찾을 수 없습니다.");
            return;
        }

        var battleData = SaveManager.Instance.BattleData;
        battleData.partyMewberIDs.Clear();
        battleData.partyMewberIDs.Add(starter.ID);
        battleData.pendingEnemyGroupID = 600;

        Log.Info($"[TutorialStarter] 파티 자동 편성: {starter.Name} / 적 그룹: 600");
    }

    private void SetupTutorialDeck()
    {
        // 카드풀 존재 여부만 검증합니다. 실제 덱 준비는 BattleSetup.PrepareDeck()이 담당합니다.
        MewberData starter = DataManager.Instance.GetFirstMewber();
        if (starter == null) return;

        MewberPoolSave pool = SaveManager.Instance.GetMewberPool(starter.ID);
        if (pool == null)
            Log.Error("[TutorialStarter] 스타터 뮤버의 카드풀이 없습니다.");
        else
            Log.Info($"[TutorialStarter] 카드풀 확인 완료 ({pool.TotalCount()}장)");
    }

    /// <summary>
    /// BattleManager가 전투 종료를 알릴 때 호출합니다.
    /// </summary>
    public void OnTutorialComplete()
    {
        SaveManager.Instance.EstateData.isTutorialComplete = true;
        SaveManager.Instance.ClearBattleSave();
        SaveManager.Instance.SaveEstate();

        Log.Success("[TutorialStarter] 튜토리얼 완료 → Estate 이동");
        StageManager.Instance.LoadStage(ESceneName.Estate);
    }
}
