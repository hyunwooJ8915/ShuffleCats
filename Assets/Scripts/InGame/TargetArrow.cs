using System.Collections.Generic;
using UnityEngine;

public class TargetArrow : Singleton<TargetArrow>
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _dotPrefab;
    [SerializeField] private GameObject _headPrefab;

    [Header("Settings")]
    [SerializeField] private int _dotCount = 15;
    [SerializeField] private float _curveHeight = 0.5f;
    [SerializeField] private float _dotScaleMultiplier = 1.0f; // 마디 크기 조절용

    private List<GameObject> _dots = new List<GameObject>();
    private GameObject _head;
    private Unit _currentTarget;

    protected override void Awake()
    {
        base.Awake();
        // 마디 생성 및 초기화
        for (int i = 0; i < _dotCount; i++)
        {
            GameObject dot = Instantiate(_dotPrefab, transform);
            dot.SetActive(false);
            _dots.Add(dot);
        }
        _head = Instantiate(_headPrefab, transform);
        _head.SetActive(false);
    }

    public void SetActive(bool active)
    {
        foreach (var dot in _dots) dot.SetActive(active);
        _head.SetActive(active);

        if(!active)
        {
            _currentTarget?.OnUntargeted();
            _currentTarget = null;
        }
    }

    public void UpdateArrow(Vector3 startPos, Vector3 endPos, Vector2 screenPos)
    {
        SetActive(true);

        // UI 환경에서는 Z를 0으로 고정하는 것이 가장 안전합니다.
        startPos.z = 0;
        endPos.z = 0;

        Vector3 midPoint = (startPos + endPos) * 0.5f;
        float dist = Vector3.Distance(startPos, endPos);

        // 제어점: 중간 지점에서 위쪽으로 곡선 높이 설정
        Vector3 controlPoint = midPoint + Vector3.up * (dist * _curveHeight);

        for (int i = 0; i < _dotCount; i++)
        {
            float t = (i + 1) / (float)(_dotCount + 1);
            Vector3 pos = GetBezierPoint(t, startPos, controlPoint, endPos);

            _dots[i].transform.position = pos;
            _dots[i].transform.localScale = Vector3.one * _dotScaleMultiplier;

            // 회전: 다음 마디를 바라보게 설정
            float nextT = (i + 1.1f) / (float)(_dotCount + 1);
            Vector3 nextPos = (i == _dotCount - 1) ? endPos : GetBezierPoint(nextT, startPos, controlPoint, endPos);
            LookAtTarget(_dots[i].transform, nextPos);
        }

        // 머리(화살촉) 설정
        _head.transform.position = endPos;
        _head.transform.localScale = Vector3.one * _dotScaleMultiplier * 1.5f;
        LookAtTarget(_head.transform, endPos + (endPos - _dots[_dotCount - 1].transform.position));

        CheckUnitUnderArrow(screenPos);
    }

    private void CheckUnitUnderArrow(Vector3 screenPos)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        Collider2D col = Physics2D.OverlapPoint(worldPos);
        Unit unit = col?.GetComponent<Unit>();

        if (unit != _currentTarget)
        {
            // 이전 타겟 해제
            if (_currentTarget != null)
            {
                _currentTarget.OnUntargeted();
                Debug.Log($"Target Lost: {_currentTarget.name}");
            }

            _currentTarget = unit;

            // 새 타겟 강조
            if (_currentTarget != null)
            {
                _currentTarget.OnTargeted();
                Debug.Log($"Target Found: {_currentTarget.name}");
            }
        }
    }

    private void LookAtTarget(Transform tf, Vector3 target)
    {
        Vector3 dir = target - tf.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // 화살표 이미지가 위를 보고 있다면 -90, 오른쪽을 보고 있다면 0 등으로 조절
        tf.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private Vector3 GetBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }
}