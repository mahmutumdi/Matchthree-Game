using Events;
using Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Components.UI;
using AudioSettings = Settings.AudioSettings;

namespace Installers
{
    public class ProjectInstaller : MonoInstaller<ProjectInstaller>
    {
        private ProjectEvents _projectEvents;
        private InputEvents _inputEvents;
        private GridEvents _gridEvents;
        private MenuEvents _menuEvents;
        private GameMenuEvents _gameMenuEvents;
        private ProjectSettings _projectSettings;
        private Settings.AudioSettings _audioSettings;
        private AudioEvents _audioEvents;
        private AudioManager _audioManager;

        public override void InstallBindings()
        {
            InstallEvents();
            InstallSettings();
            InstallAudioManager();
        }

        private void InstallSettings()
        {
            _projectSettings = Resources.Load<ProjectSettings>(EnvVar.ProjectSettingsPath);
            Container.BindInstance(_projectSettings).AsSingle();

            _audioSettings = Resources.Load<AudioSettings>(EnvVar.AudioSettingsPath);
            Container.BindInstance(_audioSettings).AsSingle();

        }

        private void InstallEvents()
        {
            _projectEvents = new ProjectEvents();
            Container.BindInstance(_projectEvents).AsSingle();

            _inputEvents = new InputEvents();
            Container.BindInstance(_inputEvents).AsSingle();

            _gridEvents = new GridEvents();
            Container.BindInstance(_gridEvents).AsSingle();

            _menuEvents = new MenuEvents();
            Container.BindInstance(_menuEvents).AsSingle();

            _gameMenuEvents = new GameMenuEvents();
            Container.BindInstance(_gameMenuEvents).AsSingle();

            _audioEvents = new AudioEvents();
            Container.BindInstance(_audioEvents).AsSingle();
        }

        private void InstallAudioManager()
        {
            _audioManager = new AudioManager();
            Container.BindInstance(_audioManager).AsSingle();
        }


        private void Awake()
        {
            RegisterEvents();
        }

        public override void Start()
        {
            _projectEvents.ProjectStarted?.Invoke();
        }

        private static void LoadScene(string sceneName) {SceneManager.LoadScene(sceneName);}

        private void RegisterEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
        {
            if(loadedScene.name == EnvVar.LoginSceneName)
            {
                LoadScene(EnvVar.MenuSceneName);
            }
        }
    }
}