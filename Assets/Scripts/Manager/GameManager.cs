using UnityEngine;


/// <summary>
/// [게임 메인 컨트롤 매니저]
/// 1. 최상위 관리자로서 다른 매니저들의 초기화 순서를 보장합니다.
/// 2. 게임의 현재 상태(영지, 전투 등)를 관리합니다.
/// 3. 싱글톤 베이스를 상속받아 전역 접근을 허용합니다.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    #region Properties
    public EGameState CurrentState { get; private set; } = EGameState.None;
    public bool IsInitialized { get; private set; } = false;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        Log.Info("게임 시스템 초기화 시작");

        // 1. 매니저 부팅 순서 (중요한 순서는 이곳에서 제어할 것)
        // DataManager나 SaveManager는 Singleton 호출 시점에 자동 생성
        if (SaveManager.Instance != null && DataManager.Instance != null)
        {
            Log.Success("매니저 시스템 부팅 완료");
        }

        CheckUserStatus();

        IsInitialized = true;
        Log.Success("게임 시스템 초기화 완료");
    }

    private void CheckUserStatus()
    {
        if (string.IsNullOrEmpty(SaveManager.Instance.EstateData.uid))
        {
            Log.Info("신규 유저 감지");
            ChangeState(EGameState.Loading);
        }
        else
        {
            Log.Info($"기존 유저 접속 : {SaveManager.Instance.EstateData.UserName}");
        }
    }

    public void ChangeState(EGameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        Log.Info($"게임 상태 변경 : {newState}");

        switch (newState)
        {
            case EGameState.Estate:
                // 영지 진입 로직
                break;

            case EGameState.Battle:
                // 전투 진입 로직
                break;
        }
    }
}
