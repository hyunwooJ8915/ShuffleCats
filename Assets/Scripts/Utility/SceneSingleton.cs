using UnityEngine;

/// <summary>
/// [씬 한정 싱글톤 베이스]
/// Singleton<T>와 달리 DontDestroyOnLoad를 호출하지 않습니다.
/// 배틀 씬처럼 씬에 종속된 단일 인스턴스에 사용합니다.
/// </summary>
public class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
            _instance = this as T;
        else if (_instance != this)
            Destroy(gameObject);
    }
}
