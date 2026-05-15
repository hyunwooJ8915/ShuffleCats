using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 전투에 참여하는 유닛 한 명.
///
/// 초기화 순서
///   1. 씬에 배치 → Start()에서 BattleManager 파티에 자동 등록
///   2. BattleSetup 등 외부에서 Init(MewberData) 호출
///   3. 세이브 데이터가 있으면 ApplyBattleState() 로 HP 덮어쓰기
/// </summary>
public class Unit : MonoBehaviour
{
    [Header("파티 설정")]
    [Tooltip("true = 플레이어 파티 / false = 적 파티")]
    [SerializeField] private bool _isPlayerUnit = true;

    [Header("UI 연결")]
    [SerializeField] private SpriteRenderer _bodyRenderer;   // Idle / Attack / Damaged / Skill
    [SerializeField] private SpriteRenderer _faceRenderer;   // FaceNormal / FaceEyesClosed / FaceDazed
    [SerializeField] private StatusPanelUI  _statusPanel;
    [Tooltip("HP 채움 SpriteRenderer. Draw Mode = Sliced 이어야 합니다.")]
    [SerializeField] private SpriteRenderer _hpBarFill;
    [SerializeField] private TextMeshPro    _nameText;
    [SerializeField] private TextMeshPro    _hpText;

    [Header("눈깜빡임")]
    [SerializeField] private float _blinkIntervalMin = 2f;
    [SerializeField] private float _blinkIntervalMax = 5f;
    [SerializeField] private float _blinkDuration    = 0.12f;

    private float _hpBarMaxWidth;

    // ── 스프라이트 캐시 ────────────────────────────
    private readonly Dictionary<EUnitSpriteType, Sprite> _sprites = new();
    private Coroutine _blinkCoroutine;
    private EUnitSpriteType _currentFace = EUnitSpriteType.FaceNormal;

    // ── 식별 ──────────────────────────────────────
    public int    MewberID    { get; private set; }
    public string UnitName    { get; private set; }
    public bool   IsPlayerUnit => _isPlayerUnit;

    // ── 전투 스탯 ─────────────────────────────────
    public int  BaseAtk   { get; private set; }
    public int  MaxHp     { get; private set; }
    public int  CurrentHP { get; private set; }
    public bool IsDead    => CurrentHP <= 0;

    // ── 태그(버프/디버프) ──────────────────────────
    public Dictionary<ETagType, int> Tags = new Dictionary<ETagType, int>();

    // ─────────────────────────────────────────────
    //  수명 주기
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (_hpBarFill != null)
            _hpBarMaxWidth = _hpBarFill.size.x;
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance == null) return;
        BattleManager.Instance.PlayerParty.Remove(this);
        BattleManager.Instance.EnemyParty.Remove(this);
    }

    /// <summary>
    /// BattleSetup에서 Instantiate 직후 호출합니다.
    /// _isPlayerUnit을 설정하고 BattleManager 파티에 등록합니다.
    /// </summary>
    public void Register(bool isPlayer)
    {
        _isPlayerUnit = isPlayer;
        if (isPlayer) BattleManager.Instance.PlayerParty.Add(this);
        else          BattleManager.Instance.EnemyParty.Add(this);
    }

    // ─────────────────────────────────────────────
    //  초기화
    // ─────────────────────────────────────────────

    public void Init(MewberData data)
    {
        MewberID  = data.ID;
        UnitName  = data.Name;
        BaseAtk   = data.Attack;
        MaxHp     = data.Health;
        CurrentHP = data.Health;

        Tags.Clear();
        _statusPanel?.ClearAll();

        if (_nameText != null) _nameText.text = data.Name;
        RefreshHPUI();
        LoadSprites(data.SpriteIdle, data.SpriteAttack, data.SpriteDamaged, data.SpriteSkill,
                    data.SpriteFaceNormal, data.SpriteFaceEyesClosed, data.SpriteFaceDazed);

        Log.Info($"[Unit] {UnitName} 초기화 완료 — HP:{MaxHp}  ATK:{BaseAtk}");
    }

    public void Init(EnemyData data)
    {
        MewberID  = data.ID;
        UnitName  = data.Name;
        BaseAtk   = data.Attack;
        MaxHp     = data.Health;
        CurrentHP = data.Health;

        Tags.Clear();
        _statusPanel?.ClearAll();

        foreach (var kv in data.ParseInitialStatus())
            if (System.Enum.TryParse(kv.Key, out ETagType tag))
                AddTag(tag, kv.Value);

        if (_nameText != null) _nameText.text = data.Name;
        RefreshHPUI();
        LoadSprites(data.SpriteIdle, data.SpriteAttack, data.SpriteDamaged, data.SpriteSkill,
                    data.SpriteFaceNormal, data.SpriteFaceEyesClosed, data.SpriteFaceDazed);

        Log.Info($"[Unit] {UnitName} 초기화 완료 — HP:{MaxHp}  ATK:{BaseAtk}");
    }

    // ─────────────────────────────────────────────
    //  저장 / 복원
    // ─────────────────────────────────────────────

    public void ApplyBattleState(MewberBattleState state)
    {
        CurrentHP = state.currentHP;
        MaxHp     = state.maxHP;
        RefreshHPUI();
    }

    public void ApplyBattleState(EnemyBattleState state)
    {
        CurrentHP = state.currentHP;
        MaxHp     = state.maxHP;
        RefreshHPUI();
    }

    public MewberBattleState ExportAsMewberState(int position = 0)
    {
        return new MewberBattleState
        {
            mewberID  = MewberID,
            currentHP = CurrentHP,
            maxHP     = MaxHp,
            position  = position,
            isDead    = IsDead
        };
    }

    public EnemyBattleState ExportAsEnemyState(int position = 0)
    {
        return new EnemyBattleState
        {
            enemyID   = MewberID,
            currentHP = CurrentHP,
            maxHP     = MaxHp,
            position  = position,
            isDead    = IsDead
        };
    }

    // ─────────────────────────────────────────────
    //  태그 (버프 / 디버프)
    // ─────────────────────────────────────────────

    public int GetTagValue(ETagType type)
        => Tags.TryGetValue(type, out int v) ? v : 0;

    public void AddTag(ETagType type, int amount)
    {
        if (type == ETagType.Heal)
        {
            RestoreHP(amount);
            return;
        }

        if (Tags.ContainsKey(type)) Tags[type] += amount;
        else                        Tags[type]  = amount;

        if (Tags[type] < 0) Tags[type] = 0;

        Log.Info($"{UnitName}의 [{type}] 태그 {(amount >= 0 ? "+" : "")}{amount} → 현재 {Tags[type]}");
        _statusPanel?.UpdateStatus(type, Tags[type]);
    }

    public void RemoveTag(ETagType type)
    {
        if (!Tags.ContainsKey(type)) return;
        Tags[type] = 0;
        Log.Info($"{UnitName}의 [{type}] 태그 제거");
        _statusPanel?.UpdateStatus(type, 0);
    }

    public void RemoveAllDebuffs()
    {
        RemoveTag(ETagType.Bleed);
        RemoveTag(ETagType.Capture);
    }

    // ─────────────────────────────────────────────
    //  HP 변동
    // ─────────────────────────────────────────────

    /// <param name="visualDelay">피격 모션·HP 바 애니메이션을 시작할 때까지의 지연(초). 연타 효과의 타격 간격으로 사용합니다.</param>
    /// <param name="isLastHit">연타 중 마지막 타격일 때만 true. ReturnToIdle을 마지막에만 호출하여 스프라이트 충돌을 방지합니다.</param>
    public void TakeDamage(int amount, float visualDelay = 0f, bool isLastHit = true)
    {
        if (amount <= 0 || IsDead) return;

        int guard = GetTagValue(ETagType.Guard);
        if (guard > 0)
        {
            int absorbed = Mathf.Min(guard, amount);
            AddTag(ETagType.Guard, -absorbed);
            amount -= absorbed;
        }

        if (amount <= 0) return;

        int hpBefore = CurrentHP;
        CurrentHP = Mathf.Max(CurrentHP - amount, 0);
        int hpAfter = CurrentHP;
        Log.Info($"{UnitName} 피해 -{amount} → {CurrentHP}/{MaxHp}");

        if (CurrentHP <= 0) OnDead();

        DOVirtual.DelayedCall(visualDelay, () =>
        {
            AnimateHPBar(hpBefore, hpAfter);
            SetBodyAction(EUnitSpriteType.Damaged);
            transform.DOKill();
            transform.DOShakePosition(0.2f, 0.5f);
            if (isLastHit && !IsDead) DOVirtual.DelayedCall(0.3f, ReturnToIdle);
        });
    }

    private void AnimateHPBar(int from, int to)
    {
        if (_hpBarFill != null)
        {
            float wFrom = MaxHp > 0 ? _hpBarMaxWidth * (float)from / MaxHp : 0f;
            float wTo   = MaxHp > 0 ? _hpBarMaxWidth * (float)to   / MaxHp : 0f;
            _hpBarFill.DOKill();
            _hpBarFill.size = new Vector2(wFrom, _hpBarFill.size.y);
            DOTween.To(
                ()  => _hpBarFill.size.x,
                x   => _hpBarFill.size = new Vector2(x, _hpBarFill.size.y),
                wTo, 0.2f
            ).SetEase(Ease.OutQuad);
        }
        if (_hpText != null)
            _hpText.text = $"{to}/{MaxHp}";
    }

    public void RestoreHP(int amount)
    {
        if (amount <= 0 || IsDead) return;
        CurrentHP = Mathf.Min(CurrentHP + amount, MaxHp);
        RefreshHPUI();
        Log.Info($"{UnitName} 회복 +{amount} → {CurrentHP}/{MaxHp}");
    }

    // ─────────────────────────────────────────────
    //  스프라이트 제어 (외부 호출용)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 바디를 액션 상태로 전환합니다. (Attack / Damaged / Skill)
    /// 페이스가 숨겨지고 깜빡임이 멈춥니다.
    /// </summary>
    public void SetBodyAction(EUnitSpriteType type)
    {
        SetBodySprite(type);
        StopBlink();
        if (_faceRenderer != null) _faceRenderer.enabled = false;
    }

    /// <summary>
    /// 대기 상태로 복귀합니다. 페이스가 표시되고 깜빡임이 재개됩니다.
    /// </summary>
    public void ReturnToIdle()
    {
        SetBodySprite(EUnitSpriteType.Idle);
        if (_faceRenderer != null) _faceRenderer.enabled = true;
        ApplyFaceSprite(_currentFace);
        StartBlink();
    }

    /// <summary>
    /// 페이스 상태를 변경합니다. (FaceNormal / FaceDazed 등)
    /// Dazed처럼 상태이상으로 인한 표정 변경 시 호출합니다.
    /// </summary>
    public void SetFace(EUnitSpriteType type)
    {
        _currentFace = type;
        // Dazed 등 고정 표정이면 깜빡임 중단
        if (type == EUnitSpriteType.FaceDazed)
            StopBlink();
        else
            StartBlink();

        ApplyFaceSprite(type);
    }

    // ─────────────────────────────────────────────
    //  전투 피드백
    // ─────────────────────────────────────────────

    public void OnTargeted()
    {
        transform.DOKill();
        transform.DOScale(1.15f, 0.15f).SetEase(Ease.OutBack);
    }

    public void OnUntargeted()
    {
        transform.DOKill();
        transform.DOScale(1f, 0.15f);
    }

    // ─────────────────────────────────────────────
    //  내부
    // ─────────────────────────────────────────────

    private void OnDead()
    {
        Log.Info($"{UnitName} 사망");
        StopBlink();
        transform.DOKill();
        BattleManager.Instance.NotifyUnitDead(this);
    }

    private void RefreshHPUI()
    {
        if (_hpBarFill != null)
        {
            float ratio = MaxHp > 0 ? (float)CurrentHP / MaxHp : 0f;
            _hpBarFill.size = new Vector2(_hpBarMaxWidth * ratio, _hpBarFill.size.y);
        }
        if (_hpText != null)
            _hpText.text = $"{CurrentHP}/{MaxHp}";
    }

    private void LoadSprites(string idle, string attack, string damaged, string skill,
                             string faceNormal, string faceEyesClosed, string faceDazed)
    {
        _sprites.Clear();

        void Load(EUnitSpriteType type, string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            SpriteLoader.Instance.Load(address, sprite =>
            {
                _sprites[type] = sprite;
                if (type == EUnitSpriteType.Idle)       ReturnToIdle();
                if (type == EUnitSpriteType.FaceNormal) ApplyFaceSprite(EUnitSpriteType.FaceNormal);
            });
        }

        Load(EUnitSpriteType.Idle,           idle);
        Load(EUnitSpriteType.Attack,         attack);
        Load(EUnitSpriteType.Damaged,        damaged);
        Load(EUnitSpriteType.Skill,          skill);
        Load(EUnitSpriteType.FaceNormal,     faceNormal);
        Load(EUnitSpriteType.FaceEyesClosed, faceEyesClosed);
        Load(EUnitSpriteType.FaceDazed,      faceDazed);
    }

    private void SetBodySprite(EUnitSpriteType type)
    {
        if (_bodyRenderer == null) return;
        if (_sprites.TryGetValue(type, out Sprite sprite))
            _bodyRenderer.sprite = sprite;
    }

    private void ApplyFaceSprite(EUnitSpriteType type)
    {
        if (_faceRenderer == null) return;
        if (_sprites.TryGetValue(type, out Sprite sprite))
            _faceRenderer.sprite = sprite;
    }

    private void StartBlink()
    {
        if (_faceRenderer == null) return;
        StopBlink();
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlink()
    {
        if (_blinkCoroutine == null) return;
        StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = null;
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(_blinkIntervalMin, _blinkIntervalMax));
            ApplyFaceSprite(EUnitSpriteType.FaceEyesClosed);
            yield return new WaitForSeconds(_blinkDuration);
            ApplyFaceSprite(_currentFace == EUnitSpriteType.FaceNormal
                ? EUnitSpriteType.FaceNormal
                : _currentFace);
        }
    }
}
