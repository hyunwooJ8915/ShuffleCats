using System.Diagnostics;

/// <summary>
/// [로그 관리 매니저]
/// 1. 모든 디버그 로그를 일괄적으로 켜고 끕니다.
/// 2. 배포 빌드 시 로그를 자동으로 제거하여 성능을 최적화합니다.
/// 3. 로그 앞에 [INFO], [ERROR] 등의 태그를 붙여 가독성을 높입니다.
/// </summary>
public static class Log
{
    // 개발 중에는 true, 배포 시에는 false로 설정하거나 
    // 아래 [Conditional] 기능을 사용해 빌드 옵션에서 제어
    private const bool IsEnabled = true;

    // "DEBUG_MODE"라는 심볼이 정의되어 있을 때만 이 메서드 실행
    // 유니티 빌드 설정의 'Scripting Define Symbols'에서 제어 가능
    [Conditional("UNITY_EDITOR"), Conditional("DEBUG_MODE")]
    public static void Info(object message)
    {
        if (IsEnabled)
            UnityEngine.Debug.Log($"<color=#FFFFFF>[INFO]</color> {message}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEBUG_MODE")]
    public static void Success(object message)
    {
        if (IsEnabled)
            UnityEngine.Debug.Log($"<color=#00FF00>[SUCCESS]</color> {message}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEBUG_MODE")]
    public static void Warning(object message)
    {
        if (IsEnabled)
            UnityEngine.Debug.LogWarning($"<color=#FFFF00>[WARNING]</color> {message}");
    }

    // 에러 로그는 배포 빌드에서도 확인해야 하는 경우가 많아 Conditional 배제
    public static void Error(object message)
    {
        UnityEngine.Debug.LogError($"<color=#FF0000>[ERROR]</color> {message}");
    }
}
