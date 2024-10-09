using Events;
using Zenject;

namespace Components.UI.Game
{
    public class ContinueGameBTN : UIBTN
    {
        [Inject]
        private GameMenuEvents _gameMenuEvents { get; set; }
        
        protected override void OnClick()
        {
            _gameMenuEvents.ContinueGameBtnUAction?.Invoke();
        }
    }
}