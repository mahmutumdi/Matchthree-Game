using Events;

namespace Components.UI.Menu
{
    public class SetTurkishBTN : UIBTN
    {
        protected override void OnClick()
        {
            LocalizationEvents.SetTurkishLanguageBtnUAction?.Invoke();
        }
    }
}