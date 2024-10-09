using DG.Tweening;
using Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Installers
{
    public class MainSceneInstaller : MonoInstaller<MainSceneInstaller>
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject _gameSettingsPanel;
        [Inject] private GameMenuEvents _gameMenuEvents { get; set; }

        public override void InstallBindings()
        {
            Container.BindInstance(_camera);
        }
        
        private void Awake()
        {
            _gameSettingsPanel.SetActive(false);
        }
        
        void Start()
        {
            _gameMenuEvents.PauseGameBtnUAction = OnPauseGameBTN;
            _gameMenuEvents.ContinueGameBtnUAction = OnContinueGameBTN;
            _gameMenuEvents.ReplayGameBtnUAction = OnReplayGameBTN;
            _gameMenuEvents.ExitGameBtnUAction = OnExitGameBTN;
        }

        private void OnPauseGameBTN()
        {
            DOVirtual.DelayedCall(0.25f, () => _gameSettingsPanel.SetActive(true));
        }

        private void OnContinueGameBTN()
        {
            DOVirtual.DelayedCall(0.25f, () =>
            {
                _gameSettingsPanel.SetActive(false);
            });
        }

        private void OnReplayGameBTN()
        {
            DOVirtual.DelayedCall(0.25f, () =>
            {
                _gameSettingsPanel.SetActive(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });
        }
        private void OnExitGameBTN()
        {
            DOVirtual.DelayedCall(0.25f, () => SceneManager.LoadScene(EnvVar.MenuSceneName));
        }
    }
}