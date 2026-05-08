using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

/// <summary>
/// [구글 시트 동기화 툴]
/// 1. UnityWebRequest를 이용해 특정 GID(탭 ID)의 시트 데이터를 CSV로 다운로드합니다.
/// 2. 다운로드된 데이터를 SO(ScriptableObject) 에셋에 덮어씌워 빌드 없이 실시간 갱신합니다.
/// 3. 사용법: Tools > Sync All 메뉴를 클릭하여 프로젝트의 모든 시트를 일괄 업데이트합니다.
/// </summary>
public class SheetImporterEditor : Editor
{
    // 여기에 시트 정보들을 등록 (SheetID, GID, 저장경로, 데이터타입)
    private static readonly string SheetID = "107RcCZk4NK1Y0OZPOeW7GwCrK7ND8Sn5VLTmsc-iT_I";

    private static List<(string gid, string path, System.Type type, string fieldName)> configs 
        = new List<(string, string, System.Type, string)> 
        {
            ("0", "Assets/Resources/Data/CardTable.asset", typeof(CardData), "cards"),
            ("505215350", "Assets/Resources/Data/MewberTable.asset", typeof(MewberData), "mewbers")
        };

    [MenuItem("Tools/Sync All Google Sheets")]
    public static void SyncAll()
    {
        // 에디터에서 진행률 표시바 출력
        EditorUtility.DisplayProgressBar("구글 시트 동기화", "데이터를 가져오는 중...", 0);

        try
        {
            foreach (var config in configs)
            {
                DownloadAndSave(config.gid, config.path, config.type, config.fieldName);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void DownloadAndSave(string gid, string path, System.Type type, string fieldName)
    {
        string url = $"https://docs.google.com/spreadsheets/d/{SheetID}/export?format=csv&gid={gid}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone) { } // 동기식 대기

            if (www.result == UnityWebRequest.Result.Success)
            {
                string csv = www.downloadHandler.text;

                // 리플렉션으로 ParseCSV<T> 메서드 호출
                var method = typeof(CSVSerializer).GetMethod("ParseCSV").MakeGenericMethod(type);
                var dataList = method.Invoke(null, new object[] { csv });

                // SO 로드 및 데이터 갱신
                Object table = AssetDatabase.LoadAssetAtPath(path, typeof(ScriptableObject));
                if (table != null)
                {
                    table.GetType().GetField(fieldName).SetValue(table, dataList);
                    EditorUtility.SetDirty(table);
                    Log.Success($"[성공] {path} 갱신 완료");
                }
            }
            else
            {
                Log.Error($"[실패] {gid} 시트 로드 오류: {www.error}");
            }
        }
        AssetDatabase.SaveAssets();
    }
}
