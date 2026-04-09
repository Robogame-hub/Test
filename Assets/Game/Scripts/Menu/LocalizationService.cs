using System;
using System.Collections.Generic;
using UnityEngine;

namespace TankGame.Menu
{
    public static class LocalizationService
    {
        private const string KeyLanguage = "Game.Language";
        private const string LocalizationConfigPath = "Menu/LocalizationConfig";

        private static Dictionary<string, LocalizationTranslationEntry> translationsByKey;
        private static Dictionary<GameLanguage, string> languageNativeNames;
        private static bool isLoaded;

        public static event Action LanguageChanged;

        public static GameLanguage CurrentLanguage
        {
            get => (GameLanguage)PlayerPrefs.GetInt(KeyLanguage, (int)GameLanguage.Russian);
            set
            {
                PlayerPrefs.SetInt(KeyLanguage, (int)value);
                PlayerPrefs.Save();
                LanguageChanged?.Invoke();
            }
        }

        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            EnsureLoaded();
            if (translationsByKey == null || !translationsByKey.TryGetValue(key, out LocalizationTranslationEntry entry))
                return key;

            string localized = entry.Get(CurrentLanguage);
            return string.IsNullOrEmpty(localized) ? key : localized;
        }

        public static string GetLanguageNativeName(GameLanguage language)
        {
            EnsureLoaded();
            if (languageNativeNames != null && languageNativeNames.TryGetValue(language, out string localizedName) && !string.IsNullOrEmpty(localizedName))
                return localizedName;

            return language.ToString();
        }

        public static int GetLanguageCount()
        {
            return Enum.GetValues(typeof(GameLanguage)).Length;
        }

        public static GameLanguage GetNextLanguage(GameLanguage current)
        {
            int count = GetLanguageCount();
            int next = ((int)current + 1) % count;
            return (GameLanguage)next;
        }

        public static GameLanguage GetPreviousLanguage(GameLanguage current)
        {
            int count = GetLanguageCount();
            int previous = (int)current - 1;
            if (previous < 0)
                previous = count - 1;
            return (GameLanguage)previous;
        }

        private static void EnsureLoaded()
        {
            if (isLoaded)
                return;

            isLoaded = true;
            translationsByKey = new Dictionary<string, LocalizationTranslationEntry>(StringComparer.Ordinal);
            languageNativeNames = new Dictionary<GameLanguage, string>();

            TextAsset configAsset = Resources.Load<TextAsset>(LocalizationConfigPath);
            if (configAsset == null)
            {
                Debug.LogWarning($"[LocalizationService] Missing localization config at Resources/{LocalizationConfigPath}.json");
                return;
            }

            LocalizationConfigData config = JsonUtility.FromJson<LocalizationConfigData>(configAsset.text);
            if (config == null)
            {
                Debug.LogWarning("[LocalizationService] Failed to parse localization config JSON.");
                return;
            }

            if (config.entries != null)
            {
                for (int i = 0; i < config.entries.Length; i++)
                {
                    LocalizationTranslationEntry entry = config.entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                        continue;

                    translationsByKey[entry.key] = entry;
                }
            }

            if (config.languageNames != null)
            {
                for (int i = 0; i < config.languageNames.Length; i++)
                {
                    LocalizationLanguageNameEntry entry = config.languageNames[i];
                    if (entry == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(entry.nativeName))
                        continue;

                    languageNativeNames[entry.language] = entry.nativeName;
                }
            }
        }
    }

    [Serializable]
    public sealed class LocalizationConfigData
    {
        public LocalizationTranslationEntry[] entries;
        public LocalizationLanguageNameEntry[] languageNames;
    }

    [Serializable]
    public sealed class LocalizationTranslationEntry
    {
        public string key;
        public string russian;
        public string english;
        public string french;
        public string german;
        public string japanese;

        public string Get(GameLanguage language)
        {
            switch (language)
            {
                case GameLanguage.Russian:
                    return russian;
                case GameLanguage.English:
                    return english;
                case GameLanguage.French:
                    return french;
                case GameLanguage.German:
                    return german;
                case GameLanguage.Japanese:
                    return japanese;
                default:
                    return russian;
            }
        }
    }

    [Serializable]
    public sealed class LocalizationLanguageNameEntry
    {
        public GameLanguage language;
        public string nativeName;
    }
}


