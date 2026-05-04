using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


/// <summary>
/// [통합 오디오 관리 매니저]
/// 1. BGM 크로스페이드 및 사운드 덕킹(Ducking)을 지원하며 유저 설정 볼륨과 연동됩니다.
/// 2. SoundData(SO) 기반 쿨다운 및 피치 랜덤화로 사운드 품질을 보장합니다.
/// 3. 오디오 믹서를 통해 마스터/BGM/SFX 전역 볼륨을 데시벨(dB) 단위로 제어합니다.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    #region Variables
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup _bgmMixerGroup;
    [SerializeField] private AudioMixerGroup _sfxMixerGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;

    [Header("Pool Settings")]
    [SerializeField] private int _initialPoolSize = 10;
    [SerializeField] private int _maxPoolSize = 30;
    [SerializeField] private int _maxConcurrentSFX = 8;
    [Range(0f, 1f)][SerializeField] private float _minDuckingMultiplier = 0.3f;

    private float _userBGMVolume = 1.0f;
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const float DUCK_VOLUME_DB = -20f;

    private Dictionary<SoundData, float> _lastPlayTimes = new Dictionary<SoundData, float>();
    private List<AudioSource> _sfxPool = new List<AudioSource>();
    private Coroutine _duckingCoroutine;

    private int _totalPlayingCount = 0;
    #endregion

    #region UnityMethods
    protected override void Awake()
    {
        base.Awake();
        InitPool();
        InitBGMSource();
    }
    #endregion

    #region VolumeControl (Global)
    private float LinearToDecibel(float linear) => Mathf.Log10(Mathf.Max(0.0001f, linear)) * 20f;

    public void SetMasterVolume(float volume) => _audioMixer.SetFloat("MasterVolume", LinearToDecibel(volume));

    public void SetBGMVolume(float volume)
    {
        _userBGMVolume = Mathf.Clamp(volume, 0.0001f, 1.0f);
        if (_duckingCoroutine == null)
            _audioMixer.SetFloat(BGM_VOLUME_PARAM, LinearToDecibel(_userBGMVolume));
    }

    public void SetSFXVolume(float volume) => _audioMixer.SetFloat("SFXVolume", LinearToDecibel(volume));
    #endregion

    #region BGMControl
    private void InitBGMSource()
    {
        if (_bgmSource == null) _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.outputAudioMixerGroup = _bgmMixerGroup;
    }

    public void PlayBGM(AudioClip clip, float duration = 1.0f)
    {
        if (_bgmSource.clip == clip) return;
        StartCoroutine(CoCrossfadeBGM(clip, duration));
    }

    private IEnumerator CoCrossfadeBGM(AudioClip nextClip, float duration)
    {
        float timer = 0;
        float startVol = _bgmSource.volume;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVol, 0, timer / duration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.clip = nextClip;
        _bgmSource.Play();

        timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0, startVol, timer / duration);
            yield return null;
        }
    }
    #endregion

    #region SFX & Ducking
    public void PlaySFX(SoundData data, bool useDucking = false)
    {
        if (data == null || data.clip == null || !CheckCooldown(data)) return;

        AudioSource source = GetAvailableSource();
        if (source == null) return;

        float duckMultiplier = data.ignoreDucking ? 1f : GetDuckingMultiplier();
        source.clip = data.clip;
        source.volume = data.volume * duckMultiplier;
        source.pitch = data.GetRandomPitch();
        source.Play();

        TrackPlayStart(data);
        StartCoroutine(CoReturnToPool(source, data.clip.length / source.pitch));

        // 덕킹 실행 로직 추가
        if (useDucking)
        {
            if (_duckingCoroutine != null) StopCoroutine(_duckingCoroutine);
            _duckingCoroutine = StartCoroutine(CoDucking());
        }
    }

    private bool CheckCooldown(SoundData data)
    {
        if (!_lastPlayTimes.TryGetValue(data, out float lastTime)) return true;
        return (Time.time - lastTime) >= data.coolDown;
    }

    private float GetDuckingMultiplier()
    {
        if (_totalPlayingCount <= _maxConcurrentSFX) return 1f;
        float ratio = (float)_maxConcurrentSFX / _totalPlayingCount;
        return Mathf.Max(ratio, _minDuckingMultiplier);
    }

    private IEnumerator CoReturnToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        _totalPlayingCount = Mathf.Max(0, _totalPlayingCount - 1);
    }

    private void TrackPlayStart(SoundData data)
    {
        _lastPlayTimes[data] = Time.time;
        _totalPlayingCount++;
    }

    private IEnumerator CoDucking()
    {
        float userVolDB = LinearToDecibel(_userBGMVolume);
        _audioMixer.SetFloat(BGM_VOLUME_PARAM, userVolDB + DUCK_VOLUME_DB);

        yield return new WaitForSeconds(0.5f);

        float timer = 0;
        float duration = 1.0f;
        float startVolDB = userVolDB + DUCK_VOLUME_DB;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float currentVol = Mathf.Lerp(startVolDB, userVolDB, timer / duration);
            _audioMixer.SetFloat(BGM_VOLUME_PARAM, currentVol);
            yield return null;
        }

        _audioMixer.SetFloat(BGM_VOLUME_PARAM, userVolDB);
        _duckingCoroutine = null;
    }
    #endregion

    #region Pooling
    private void InitPool()
    {
        for (int i = 0; i < _initialPoolSize; i++) CreateNewSource();
    }

    private AudioSource CreateNewSource()
    {
        GameObject go = new GameObject("SFX_Pool_Unit");
        go.transform.SetParent(transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = _sfxMixerGroup;
        _sfxPool.Add(source);
        return source;
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var source in _sfxPool)
            if (!source.isPlaying) return source;

        if (_sfxPool.Count < _maxPoolSize)
            return CreateNewSource();

        return null;
    }
    #endregion
}
