using System;
using System.Collections.Generic;

/// <summary>
/// [시드 기반 난수 관리 매니저]
/// 1. System.Random을 사용하여 고정된 시드(Seed)로부터 예측 가능한 난수를 생성합니다.
/// 2. CurrentStep을 통해 난수 소모량을 기록하여, 강제 종료 후 재접속 시에도 동일한 난수 결과를 보장합니다.
/// 3. 리스트 확장 메서드(Shuffle)를 포함하여 덱 섞기 등 전투 로직의 공정성을 관리합니다.
/// </summary>
public static class RandomUtil
{
    private static Random _rng;

    // 현재까지 난수를 몇 번 사용했는지 기록 (세이브 용도)
    public static int CurrentStep { get; private set; }

    /// <summary>
    /// 전투 시작 시 또는 로드 시 난수 생성기를 초기화합니다.
    /// </summary>
    /// <param name="seed">고정 시드값</param>
    /// <param name="startStep">이미 진행된 난수 단계 (로드 시 사용)</param>
    public static void Initialize(int seed, int startStep = 0)
    {
        _rng = new Random(seed);
        CurrentStep = 0;

        // 저장된 Step만큼 난수를 미리 소모
        for (int i = 0; i < startStep; i++)
        {
            _rng.Next();
            CurrentStep++;
        }
    }

    /// <summary>
    /// Random.Range 대신 사용할 시드 버전 난수 생성기 (int 오버로딩)
    /// </summary>
    public static int Range(int min, int max)
    {
        CurrentStep++;
        return _rng.Next(min, max);
    }

    /// <summary>
    /// Random.Range 대신 사용할 시드 버전 난수 생성기 (float 오버로딩)
    /// </summary>
    public static float Range(float min, float max)
    {
        CurrentStep++;
        return (float)(_rng.NextDouble() * (max - min) + min);
    }



    /// <summary>
    /// 리스트 자체에서 바로 호출 가능한 확장 매서드 셔플
    /// </summary>
    public static void Shuffle<T>(this List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            // RandomManager.Range를 사용하여 시드에 종속된 난수를 발생시킴
            int k = RandomUtil.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
