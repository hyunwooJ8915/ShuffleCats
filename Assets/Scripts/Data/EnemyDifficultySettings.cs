using UnityEngine;

/// <summary>
/// 적 AI 난이도 설정 에셋.
/// Project 창에서 우클릭 → Create → ShuffleCats → Enemy Difficulty Settings 로 생성합니다.
/// EnemyAI 컴포넌트에 원하는 에셋을 연결하면 난이도를 즉시 교체할 수 있습니다.
/// </summary>
[CreateAssetMenu(menuName = "ShuffleCats/Enemy Difficulty Settings", fileName = "EnemyDifficultySettings")]
public class EnemyDifficultySettings : ScriptableObject
{
    [Header("행동 타이밍")]
    [Tooltip("카드 한 장 사용 사이의 기본 간격 (초)")]
    [Min(0.1f)] public float ActionInterval = 3f;

    [Tooltip("전투 시작 후 첫 행동까지의 대기 시간 (초)")]
    [Min(0f)]   public float InitialDelay = 2f;

    [Tooltip("매 간격에 추가되는 최대 무작위 오프셋 (초) – 행동 패턴을 자연스럽게 만듦")]
    [Min(0f)]   public float IntervalVariance = 0.5f;

    [Header("행동 판단")]
    [Tooltip("아군 HP가 이 비율 이하일 때 회복 카드를 우선 사용합니다. (0 ~ 1)")]
    [Range(0f, 1f)] public float HealHpThreshold = 0.3f;
}
