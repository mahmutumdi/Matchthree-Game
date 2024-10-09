using System;
using System.Collections.Generic;
using Events;
using Extensions.Unity.MonoHelper;
using UnityEngine;
using Zenject;
using Settings;
using AudioSettings = Settings.AudioSettings;

namespace Components.UI
{
    public class AudioManager : EventListenerMono
    {
        [SerializeField] private AudioSource _musicAudioSource;
        [Inject] private AudioEvents _audioEvents { get; set; }
        [Inject] private AudioSettings AudioSettings {get; set;}
        private Settings _mySettings;

        void Awake()
        {
            _mySettings = AudioSettings.AudioManagerSettings;
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            SetAudioClip("music_harp_peaceful_loop");
        }

        public void SetAudioClip(string clipName)
        {
            AudioClip clip = _mySettings.AudioClips.Find(c => c.name == clipName);
            if (clip != null)
            {
                _musicAudioSource.clip = clip;
                _musicAudioSource.Play();
            }
            else
            {
                Debug.LogWarning($"Audio clip with name {clipName} not found in ProjectSettings.");
            }
        }

        protected override void RegisterEvents()
        {
            _audioEvents.MusicSliderUAction += MusicSliderValueChanged;
        }

        private void MusicSliderValueChanged(float val)
        {
            _musicAudioSource.volume = val;
        }

        protected override void UnRegisterEvents()
        {
            _audioEvents.MusicSliderUAction -= MusicSliderValueChanged;
        }

        [Serializable]
        public class Settings
        {
            [SerializeField] private List<AudioClip> _audioClips;
            public List<AudioClip> AudioClips => _audioClips;
        }
    }
}