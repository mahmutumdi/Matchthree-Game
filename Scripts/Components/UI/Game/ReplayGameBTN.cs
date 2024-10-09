using Events;
using Zenject;

namespace Components.UI.Game
{
    public class ReplayGameBTN : UIBTN
    {
        [Inject]
        private GameMenuEvents _gameMenuEvents{ get; set; }
        protected override void OnClick()
        {
            _gameMenuEvents.ReplayGameBtnUAction?.Invoke();
        }
    }
}