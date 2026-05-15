using UnityEngine;

/// <summary>
/// 이 컴포넌트가 붙은 루트 오브젝트를 씬 전환 시 파괴되지 않도록 유지합니다.
/// EventSystem 등 Singleton 패턴을 쓰지 않는 공용 오브젝트에 사용합니다.
/// </summary>
public class PersistOnLoad : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
