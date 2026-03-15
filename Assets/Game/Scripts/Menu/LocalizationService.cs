using System;
using System.Collections.Generic;
using UnityEngine;

namespace TankGame.Menu
{
    public static class LocalizationService
    {
        private const string KeyLanguage = "Game.Language";

        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            ["menu.title"] = new[] { "ТАНКИ", "TANKS", "CHARS", "PANZER", "???" },
            ["menu.play"] = new[] { "Сетевая игра", "Network game", "Jeu en reseau", "Netzwerkspiel", "???" },
            ["menu.sandbox"] = new[] { "Одиночная игра", "Single player", "Jeu solo", "Einzelspieler", "???????" },
            ["menu.settings"] = new[] { "Настройки", "Settings", "Parametres", "Einstellungen", "??" },
            ["menu.exit"] = new[] { "Выход", "Exit", "Quitter", "Beenden", "??" },
            ["menu.back"] = new[] { "Назад", "Back", "Retour", "Zuruck", "??" },
            ["menu.start_match"] = new[] { "Начать матч", "Start match", "Demarrer le match", "Match starten", "??" },
            ["pause.title"] = new[] { "Пауза", "Pause", "Pause", "Pause", "???" },
            ["pause.restart"] = new[] { "Рестарт", "Restart", "Redemarrer", "Neustart", "???" },
            ["pause.main_menu"] = new[] { "Выход в главное меню", "Back to main menu", "Retour au menu principal", "Zum Hauptmenu", "???" },
            ["pause.desktop"] = new[] { "Выход на рабочий стол", "Exit to desktop", "Quitter le jeu", "Zum Desktop", "???" },

            ["lobby.title"] = new[] { "Лобби", "Lobby", "Lobby", "Lobby", "???" },
            ["lobby.rooms"] = new[] { "Комнаты", "Rooms", "Salles", "Raume", "???" },
            ["lobby.nickname"] = new[] { "Ник", "Nickname", "Pseudo", "Nickname", "??????" },
            ["lobby.no_rooms"] = new[] { "Комнат пока нет", "No rooms yet", "Aucune salle", "Keine Raume", "?????????" },
            ["lobby.refresh"] = new[] { "Обновить", "Refresh", "Actualiser", "Aktualisieren", "??" },
            ["lobby.create_room"] = new[] { "Создать комнату", "Create room", "Creer une salle", "Raum erstellen", "?????" },
            ["lobby.play_solo"] = new[] { "Играть одному", "Play solo", "Jouer solo", "Solo spielen", "?????" },
            ["lobby.join"] = new[] { "Войти", "Join", "Rejoindre", "Beitreten", "??" },

            ["sandbox.title"] = new[] { "Условия матча", "Match settings", "Parametres du match", "Match-Einstellungen", "??" },
            ["sandbox.bot_count"] = new[] { "Боты", "Bots", "Bots", "Bots", "??" },

            ["settings.title"] = new[] { "Настройки", "Settings", "Parametres", "Einstellungen", "??" },
            ["settings.sensitivity"] = new[] { "Чувствительность", "Sensitivity", "Sensibilite", "Empfindlichkeit", "??" },
            ["settings.master_sens"] = new[] { "Общая", "Master", "General", "Gesamt", "??" },
            ["settings.horizontal_sens"] = new[] { "Горизонталь", "Horizontal", "Horizontal", "Horizontal", "??" },
            ["settings.vertical_sens"] = new[] { "Вертикаль", "Vertical", "Vertical", "Vertikal", "??" },
            ["settings.sound"] = new[] { "Звук", "Sound", "Son", "Sound", "????" },
            ["settings.master_volume"] = new[] { "Общая громкость", "Master volume", "Volume principal", "Gesamtlautstarke", "??????" },
            ["settings.music_volume"] = new[] { "Музыка", "Music", "Musique", "Musik", "??" },
            ["settings.sfx_volume"] = new[] { "Эффекты", "SFX", "Effets", "Effekte", "???" },
            ["settings.language"] = new[] { "Язык", "Language", "Langue", "Sprache", "??" },

            ["tank.select"] = new[] { "Выбор танка", "Tank selection", "Selection du char", "Panzerwahl", "????" },
            ["tank.speed"] = new[] { "Скорость", "Speed", "Vitesse", "Geschwindigkeit", "??" },
            ["tank.armor"] = new[] { "Броня", "Armor", "Armure", "Panzerung", "??" },
            ["tank.firepower"] = new[] { "Огневая мощь", "Firepower", "Puissance de feu", "Feuerkraft", "??" },
            ["tank.handling"] = new[] { "Управление", "Handling", "Maniabilite", "Handling", "???" }
        };

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

            if (!Table.TryGetValue(key, out string[] values) || values == null || values.Length == 0)
                return key;

            int index = Mathf.Clamp((int)CurrentLanguage, 0, values.Length - 1);
            return values[index];
        }

        public static string GetLanguageNativeName(GameLanguage language)
        {
            switch (language)
            {
                case GameLanguage.Russian:
                    return "Русский";
                case GameLanguage.English:
                    return "English";
                case GameLanguage.French:
                    return "Francais";
                case GameLanguage.German:
                    return "Deutsch";
                case GameLanguage.Japanese:
                    return "???";
                default:
                    return "Русский";
            }
        }
    }
}


