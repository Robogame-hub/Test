using UnityEngine;

namespace TankGame.Utils
{
    /// <summary>
    /// Утилита для условной компиляции Debug логов
    /// В production build логи не выполняются (0 overhead)
    /// </summary>
    public static class DebugHelper
    {
        // ИЗМЕНИТЬ НА false ДЛЯ PRODUCTION BUILD!
        // Или добавить в Player Settings → Scripting Define Symbols: TANK_DEBUG
        public const bool ENABLE_DEBUG_LOGS = true;

        [System.Diagnostics.Conditional("TANK_DEBUG")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(object message)
        {
            if (ENABLE_DEBUG_LOGS)
            {
                Debug.Log(message);
            }
        }

        [System.Diagnostics.Conditional("TANK_DEBUG")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(object message, Object context)
        {
            if (ENABLE_DEBUG_LOGS)
            {
                Debug.Log(message, context);
            }
        }

        [System.Diagnostics.Conditional("TANK_DEBUG")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message)
        {
            if (ENABLE_DEBUG_LOGS)
            {
                Debug.LogWarning(message);
            }
        }

        [System.Diagnostics.Conditional("TANK_DEBUG")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context)
        {
            if (ENABLE_DEBUG_LOGS)
            {
                Debug.LogWarning(message, context);
            }
        }

        // Error всегда логируется (даже в production)
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        public static void LogError(object message, Object context)
        {
            Debug.LogError(message, context);
        }
    }
}

