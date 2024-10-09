using Events;
using Zenject;

namespace Components.UI.Game
{
    public class PauseGameBTN : UIBTN
    {
        [Inject]
        private GameMenuEvents _gameMenuEvents{ get; set; }
        
        protected override void OnClick()
        {
            _gameMenuEvents.PauseGameBtnUAction?.Invoke();
        }
    }
}