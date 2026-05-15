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

    private void Start()
    {
        // 모든 Awake 완료 후 실행 → SaveManager.EstateData 접근 안전
        CheckUserStatus();
    }

    private void InitializeSystem()
    {
        Log.Info("게임 시스템 초기화 시작");

        if (SaveManager.Instance != null && DataManager.Instance != null)
            Log.Success("매니저 시스템 부팅 완료");

        IsInitialized = true;
        Log.Success("게임 시스템 초기화 완료");
    }

    private void CheckUserStatus()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.EstateData == null)
        {
            Log.Error("세이브 데이터를 불러올 수 없습니다!");
            return;
        }

        var estate = SaveManager.Instance.EstateData;

        if (!estate.isTutorialComplete)
        {
            Log.Info("신규 유저 감지 → 튜토리얼 흐름 시작");
            GrantStarterMewber();
        }
        else
        {
            Log.Info($"기존 유저 접속 : {estate.UserName}");
        }

        // 신규/기존 유저 모두 Intro를 거쳐 라우팅 — IntroController가 분기 처리
        StageManager.Instance.LoadStage(ESceneName.Intro, shouldSave: false);
    }

    /// <summary>최초 1회 MewberTable 첫 번째 뮤버를 자동 지급합니다.</summary>
    private void GrantStarterMewber()
    {
        if (SaveManager.Instance.EstateData.ownedMewberIDs.Count > 0) return;

        MewberData starter = DataManager.Instance.GetFirstMewber();
        if (starter == null) return;

        SaveManager.Instance.AcquireMewber(starter.ID);
        SaveManager.Instance.SaveEstate();
        Log.Success($"[GameManager] 초기 뮤버 지급: {starter.Name}");
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
