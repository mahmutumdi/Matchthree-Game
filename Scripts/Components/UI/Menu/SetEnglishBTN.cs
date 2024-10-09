using Events;

namespace Components.UI.Menu
{
    public class SetEnglishBTN : UIBTN
    {
        protected override void OnClick()
        {
            LocalizationEvents.SetEnglishLanguageBtnUAction?.Invoke();
        }
    }
}