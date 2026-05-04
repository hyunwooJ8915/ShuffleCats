using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Data/SoundData")]
public class SoundData : ScriptableObject
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 1.5f)] public float minPitch = 0.95f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.05f;

    public float coolDown = 0.05f;
    public bool ignoreDucking = false;

    public float GetRandomPitch() => Random.Range(minPitch, maxPitch);
}
