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

        Log.Info($"영지 데이터 저장 경로 : {_estateSavePath}");
        Log.Info($"전투 데이터 저장 경로 : {_battleSavePath}");
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

    // ─────────────────────────────────────────────
    //  뮤버 카드풀 관리
    // ─────────────────────────────────────────────

    /// <summary>
    /// 뮤버를 처음 획득할 때 호출합니다.
    /// StartBundle로 카드풀을 초기화하고 ownedMewberIDs에 등록합니다.
    /// 이미 등록된 뮤버라면 아무것도 하지 않습니다.
    /// </summary>
    public bool AcquireMewber(int mewberID)
    {
        if (EstateData.ownedMewberIDs.Contains(mewberID)) return false;

        MewberData data = DataManager.Instance.GetMewberData(mewberID);
        if (data == null)
        {
            Log.Error($"[SaveManager] 뮤버 ID {mewberID} 데이터 없음");
            return false;
        }

        EstateData.ownedMewberIDs.Add(mewberID);

        var pool = new MewberPoolSave { mewberID = mewberID };
        pool.FromDictionary(data.ParseStartBundle());
        EstateData.mewberPools.Add(pool);

        Log.Info($"[SaveManager] {data.Name} 획득 — 카드풀 초기화 완료 ({pool.TotalCount()}/{data.CardLimit})");
        return true;
    }

    /// <summary>
    /// 뮤버의 현재 카드풀을 반환합니다. 없으면 null.
    /// </summary>
    public MewberPoolSave GetMewberPool(int mewberID)
        => EstateData.mewberPools.Find(p => p.mewberID == mewberID);

    /// <summary>
    /// 뮤버의 카드풀에 카드를 추가합니다.
    /// CardLimit 초과 시 실패(false 반환).
    /// </summary>
    public bool AddCardToPool(int mewberID, int cardID, int count = 1)
    {
        MewberData data = DataManager.Instance.GetMewberData(mewberID);
        if (data == null) return false;

        MewberPoolSave pool = GetMewberPool(mewberID);
        if (pool == null) return false;

        if (pool.TotalCount() + count > data.CardLimit)
        {
            Log.Warning($"[SaveManager] {data.Name}의 카드풀 한도 초과 ({pool.TotalCount()}/{data.CardLimit})");
            return false;
        }

        CardEntry entry = pool.cards.Find(e => e.cardID == cardID);
        if (entry != null) entry.count += count;
        else               pool.cards.Add(new CardEntry { cardID = cardID, count = count });

        Log.Info($"[SaveManager] {data.Name}에 카드 {cardID} x{count} 추가 → ({pool.TotalCount()}/{data.CardLimit})");
        return true;
    }

    /// <summary>
    /// 뮤버의 카드풀에서 카드를 제거합니다. 보유량이 0이 되면 항목 자체를 삭제합니다.
    /// </summary>
    public bool RemoveCardFromPool(int mewberID, int cardID, int count = 1)
    {
        MewberPoolSave pool = GetMewberPool(mewberID);
        if (pool == null) return false;

        CardEntry entry = pool.cards.Find(e => e.cardID == cardID);
        if (entry == null || entry.count < count) return false;

        entry.count -= count;
        if (entry.count == 0) pool.cards.Remove(entry);
        return true;
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
