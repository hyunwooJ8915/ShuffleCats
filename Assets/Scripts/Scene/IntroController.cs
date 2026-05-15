using System.Collections;
using UnityEngine;

/// <summary>
/// Intro 씬 진입 시 유저 상태에 따라 다음 씬으로 라우팅합니다.
/// 추후 인트로 영상 추가 시 Proceed() 호출을 영상 완료 콜백으로 교체하세요.
/// </summary>
public class IntroController : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 이전 씬 전환이 완전히 끝날 때까지 대기
        yield return new WaitUntil(() => !StageManager.Instance.IsTransitioning);
        // 현재는 영상 없이 즉시 전환 — 영상 추가 시 이 줄을 제거하고 콜백으로 교체
        Proceed();
    }

    public void Proceed()
    {
        var estate = SaveManager.Instance.EstateData;
        var battle = SaveManager.Instance.BattleData;

        if (!estate.isTutorialComplete)
        {
            Log.Info("[Intro] 신규 유저 → 튜토리얼 Battle");
            StageManager.Instance.LoadStage(ESceneName.Battle);
        }
        else if (battle.isBattleActive)
        {
            Log.Info("[Intro] 미완료 전투 감지 → Battle 복귀");
            StageManager.Instance.LoadStage(ESceneName.Battle);
        }
        else
        {
            Log.Info("[Intro] 기존 유저 → Estate");
            StageManager.Instance.LoadStage(ESceneName.Estate);
        }
    }
}
