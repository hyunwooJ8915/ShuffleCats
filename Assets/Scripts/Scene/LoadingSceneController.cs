using System.Collections;
using UnityEngine;

public class LoadingSceneController : MonoBehaviour
{    
    void Start()
    {
        StartCoroutine(LoadGameData());
    }

    IEnumerator LoadGameData()
    {
        Log.Info("데이터 로딩 시작...");

        while (!DataManager.Instance.IsInitialized) yield return null;

        Log.Success("데이터 로딩 확인 완료");


        // 리소스 로드

        // 네트워크 체크

        // yield return new WaitForSeconds(0.5f); // 로딩 연출용 텀 (필요 시 추가)

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }
}
