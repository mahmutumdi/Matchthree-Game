using Events;
using UnityEngine;
using Zenject;

namespace Components.UI.Menu
{
    public class ExitSettingsAboutBTN : UIBTN
    {
        [Inject]
        private MenuEvents _menuEvents{ get; set; }
        
        protected override void OnClick()
        {
            _menuEvents.ExitSettingsAboutBtnUAction?.Invoke();
        }
    }
}