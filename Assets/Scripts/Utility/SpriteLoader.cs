using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Addressables 기반 스프라이트 비동기 로더.
/// 동일 주소는 캐싱하여 중복 로드를 방지하고, Release로 메모리를 반환합니다.
/// </summary>
public class SpriteLoader : Singleton<SpriteLoader>
{
    private readonly Dictionary<string, Sprite>                        _cache   = new();
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>>  _handles = new();
    private readonly Dictionary<string, List<Action<Sprite>>>          _pending = new();

    /// <summary>
    /// 주소로 스프라이트를 비동기 로드합니다.
    /// 이미 캐싱된 경우 callback을 즉시 호출합니다.
    /// </summary>
    public void Load(string address, Action<Sprite> callback)
    {
        if (string.IsNullOrEmpty(address))
        {
            callback?.Invoke(null);
            return;
        }

        if (_cache.TryGetValue(address, out Sprite cached))
        {
            callback?.Invoke(cached);
            return;
        }

        // 이미 로드 중인 주소면 콜백만 등록
        if (_pending.TryGetValue(address, out var waitList))
        {
            waitList.Add(callback);
            return;
        }

        _pending[address] = new List<Action<Sprite>> { callback };

        var handle = Addressables.LoadAssetAsync<Sprite>(address);
        _handles[address] = handle;

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                _cache[address] = op.Result;
                foreach (var cb in _pending[address])
                    cb?.Invoke(op.Result);
            }
            else
            {
                Log.Warning($"[SpriteLoader] 로드 실패: {address}");
                foreach (var cb in _pending[address])
                    cb?.Invoke(null);
            }
            _pending.Remove(address);
        };
    }

    /// <summary>
    /// 특정 주소의 스프라이트를 메모리에서 해제합니다.
    /// </summary>
    public void Release(string address)
    {
        if (!_handles.TryGetValue(address, out var handle)) return;

        _cache.Remove(address);
        Addressables.Release(handle);
        _handles.Remove(address);
    }

    /// <summary>
    /// 캐시된 모든 스프라이트를 해제합니다. (씬 전환 시 호출)
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var handle in _handles.Values)
            Addressables.Release(handle);

        _cache.Clear();
        _handles.Clear();
        _pending.Clear();
    }
}
