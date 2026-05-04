using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// [세이브 관리 시스템]
/// 1. 영지 데이터(EState)와 전투 데이터(Battle)를 별도 파일로 관리합니다.
/// 2. XOR 암호화를 통해 데이터 조작을 방지합니다.
/// 3. SO 시스템의 ID를 기반으로 저장하여 DataManager와 연동됩니다.
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    #region Properties
    public EstateSaveData EstateData { get; private set; }
    public BattleSaveData BattleData { get; private set; }
    #endregion

    #region PrivateVariables
    private string _estateSavePath;
    private string _battleSavePath;
    private const string EncryptionKey = "SHUFFLECATS_KEY_9384791223";  // 암호화 키
    #endregion

    #region UnityMeythods
    protected override void Awake()
    {
        base.Awake();

        _estateSavePath = Path.Combine(Application.persistentDataPath, "estate_data.dat");
        _battleSavePath = Path.Combine(Application.persistentDataPath, "battle_temp.dat");
        LoadAll();
    }
    #endregion

    #region PublicMethods
    public void SaveEstate()
    {
        EstateData.lastSavedTime = DateTime.UtcNow.ToString("o");
        string json = JsonUtility.ToJson(EstateData);
        File.WriteAllText(_estateSavePath, EncryptDecrypt(json));
        Log.Success($"영지 데이터 저장 완료, 저장 경로 : {_estateSavePath}");
    }

    public void SaveBattle()
    {
        string json = JsonUtility.ToJson(BattleData);
        File.WriteAllText(_battleSavePath, EncryptDecrypt(json));
        // 전투 로그는 너무 자주 찍히면 지저분하므로 Info로 처리
        Log.Info($"전투 임시 데이터 저장 완료, 저장 경로 : {_battleSavePath}");
    }

    public void ClearBattleSave()
    {
        if (File.Exists(_battleSavePath)) File.Delete(_battleSavePath);
        BattleData = new BattleSaveData();
        Log.Info("전투 임시 데이터 초기화");
    }
    #endregion

    #region PrivateMethods
    private void LoadAll()
    {
        // 영지 로드
        try
        {
            if (File.Exists(_estateSavePath))
                EstateData = JsonUtility.FromJson<EstateSaveData>(EncryptDecrypt(File.ReadAllText(_estateSavePath)));
        }
        catch { }

        if (EstateData == null)
            EstateData = new EstateSaveData { uid = $"8{DateTime.Now:yyMMdd}{UnityEngine.Random.Range(100, 999)}" };

        // 전투 로드
        try
        {
            if (File.Exists(_battleSavePath))
                BattleData = JsonUtility.FromJson<BattleSaveData>(EncryptDecrypt(File.ReadAllText(_battleSavePath)));
        }
        catch { }

        if (BattleData == null)
            BattleData = new BattleSaveData();
    }
        
    private string EncryptDecrypt(string text)
    {
        StringBuilder res = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
            res.Append((char)(text[i] ^ EncryptionKey[i % EncryptionKey.Length]));
        return res.ToString();
    }
    #endregion
}
