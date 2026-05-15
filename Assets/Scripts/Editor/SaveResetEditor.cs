using UnityEditor;
using UnityEngine;
using System.IO;

public static class SaveResetEditor
{
    private const string EstatePath = "estate_data.dat";
    private const string BattlePath = "battle_temp.dat";

    [MenuItem("Tools/Reset Save/All")]
    private static void ResetAll()
    {
        if (!Confirm("전체 세이브 데이터(영지 + 전투)를 초기화합니다.")) return;
        Delete(EstatePath);
        Delete(BattlePath);
        Log("전체 세이브 초기화 완료");
    }

    [MenuItem("Tools/Reset Save/Estate Only")]
    private static void ResetEstate()
    {
        if (!Confirm("영지 데이터를 초기화합니다.")) return;
        Delete(EstatePath);
        Log("영지 세이브 초기화 완료");
    }

    [MenuItem("Tools/Reset Save/Battle Only")]
    private static void ResetBattle()
    {
        if (!Confirm("전투 임시 데이터를 초기화합니다.")) return;
        Delete(BattlePath);
        Log("전투 세이브 초기화 완료");
    }

    [MenuItem("Tools/Reset Save/Open Save Folder")]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    private static void Delete(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static bool Confirm(string message)
        => EditorUtility.DisplayDialog("세이브 초기화", message, "확인", "취소");

    private static void Log(string message)
        => Debug.Log($"[SaveReset] {message} — 경로: {Application.persistentDataPath}");
}
