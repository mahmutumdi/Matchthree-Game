using UnityEngine;

namespace Localization
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance;

        public LanguageScriptableObject turkishLanguage;
        public LanguageScriptableObject englishLanguage;

        private LanguageScriptableObject currentLanguage;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Load saved language preference
            int savedLanguage = PlayerPrefs.GetInt("Language", (int)Languages.English);
            SetLanguage((Languages)savedLanguage);
        }

        public void SetLanguage(Languages language)
        {
            switch (language)
            {
                case Languages.Turkish:
                    currentLanguage = turkishLanguage;
                    break;
                case Languages.English:
                    currentLanguage = englishLanguage;
                    break;
                default:
                    currentLanguage = englishLanguage;
                    break;
            }
            PlayerPrefs.SetInt("Language", (int)language);
            PlayerPrefs.Save();
        }

        public string GetTranslation(string key)
        {
            if (currentLanguage == null)
            {
                Debug.LogWarning("Current language is not set.");
                return key;
            }
            return currentLanguage.GetTranslation(key);
        }
    }
}