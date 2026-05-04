using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 내장 SceneManager와 구분하기 위해 필요

/// <summary>
/// [스테이지 및 씬 전환 매니저]
/// 1. 씬 전환 시 페이드 인/아웃 연출을 수행합니다.
/// 2. 씬 전환 직전 세이브 데이터를 자동 저장하여 데이터 유실을 방지합니다.
/// 3. 유니티 내장 SceneManager를 래핑하여 사용 편의성을 높입니다.
/// </summary>
public class StageManager : Singleton<StageManager>
{
    #region Variables
    [Header("Fade UI")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private float _fadeDuration = 0.5f;

    private bool _isTransitioning = false;
    #endregion

    #region PublicMethods
    /// <summary>
    /// 페이드 효과와 함께 다음 스테이지(씬)으로 이동
    /// </summary>
    /// <param name="sceneName">이동할 씬 이름</param>
    /// <param name="shouldSave">이동 전 저장 여부</param>
    public void LoadStage(ESceneName sceneEnum, bool shouldSave = true)
    {
        if (_isTransitioning)
        {
            Log.Warning("이미 씬 전환 진행 중");
            return;
        }
        string sceneName = sceneEnum.ToString();
        StartCoroutine(CoTransitionStage(sceneName, shouldSave));
    }
    #endregion

    #region PrivateMethods
    private IEnumerator CoTransitionStage(string sceneName, bool shouldSave)
    {
        _isTransitioning = true;

        // 페이드 아웃
        yield return CoFade(0, 1);

        if (shouldSave && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveEstate();
            Log.Info($"[{sceneName}] 이동 전 자동 저장 완료");
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        _isTransitioning = false;
    }

    private IEnumerator CoFade(float startAlpha, float endAlpha)
    {
        if (_fadeCanvasGroup == null)
        {
            Log.Error("Fade CanvasGroup이 설정되지 않음");
            yield break;
        }

        float timer = 0f;
        _fadeCanvasGroup.alpha = startAlpha;

        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer);
            yield return null;
        }

        _fadeCanvasGroup.alpha = endAlpha;
    }

    // 차후 연출을 위해 임시 구현
    public void LoadStageWithEffect(ESceneName sceneName, Func<IEnumerator> specialEffect = null)
    {
        StartCoroutine(CoTransitionWithEffect(sceneName, specialEffect));
    }

    private IEnumerator CoTransitionWithEffect(ESceneName sceneName, Func<IEnumerator> specialEffect)
    {
        yield return CoFade(0, 1);

        if (specialEffect != null)
        {
            yield return specialEffect(); // 주입된 특수 연출 실행 및 대기
        }

        // 씬 로드 로직
    }
    #endregion
}
