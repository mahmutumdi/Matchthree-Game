using Components;
using UnityEngine;
using Components.UI;

namespace Settings
{
    [CreateAssetMenu(fileName = nameof(AudioSettings), menuName = EnvVar.AudioSettingsPath, order = 0)]
    public class AudioSettings : ScriptableObject
    {
        [SerializeField] private AudioManager.Settings _audioManagerSettings;
        public AudioManager.Settings AudioManagerSettings => _audioManagerSettings;
    }
}