using Events;
using Extensions.Unity.MonoHelper;
using Zenject;

namespace Components.UI.Menu
{
    public class MusicSLIDER : UIBasicSlider
    {
        [Inject] private AudioEvents _audioEvents { get; set; }
        protected override void OnValueChanged(float val)
        {
            _audioEvents.MusicSliderUAction?.Invoke(val);
        }
    }
}