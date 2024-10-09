using DG.Tweening;
using Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace Installers
{
    public class MenuSceneInstaller : MonoInstaller<MainSceneInstaller>
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject _settingsAboutPanel;
        [Inject] private MenuEvents _menuEvents { get; set; }
        
        public override void InstallBindings()
        {
            Container.BindInstance(_camera);
        }
        
        private void Awake()
        {
            _settingsAboutPanel.SetActive(false);
        }
        
        void Start()
        {
            //onenable ondisable yap

            _menuEvents.StartGameBtnUAction = OnStartGameBTN;
            _menuEvents.SettingsAboutBtnUAction = OnSettingsAboutBTN;
            _menuEvents.ExitSettingsAboutBtnUAction = OnExitSettingsAboutBTN;
        }

        void OnStartGameBTN()
        {
            DOVirtual.DelayedCall(0.5f, () => SceneManager.LoadScene(EnvVar.MainSceneName));
        }

        void OnSettingsAboutBTN()
        {
            DOVirtual.DelayedCall(0.5f, () => _settingsAboutPanel.SetActive(true));
        }

        private void OnExitSettingsAboutBTN()
        {
            _settingsAboutPanel.SetActive(false);
            
        }

    }
}