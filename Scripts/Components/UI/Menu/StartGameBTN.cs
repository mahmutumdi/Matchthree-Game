using Events;
using Zenject;

namespace Components.UI.Menu
{
    public class StartGameBTN : UIBTN
    {
        [Inject]
        private MenuEvents _menuEvents{ get; set; }
        protected override void OnClick()
        {
            _menuEvents.StartGameBtnUAction?.Invoke();
        }
    }
}