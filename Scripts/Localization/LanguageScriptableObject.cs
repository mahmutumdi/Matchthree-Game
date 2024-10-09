using UnityEngine;

namespace Localization
{
    [CreateAssetMenu(fileName = nameof(LanguageScriptableObject), menuName = EnvVar.LocalizationSettingsPath, order = 0)]
    public class LanguageScriptableObject : ScriptableObject
    {
        public string languageName;
        public string[] keys;
        public string[] values;

        public string GetTranslation(string key)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == key)
                {
                    return values[i];
                }
            }
            return null;
        }
    }
}