using UnityEngine;

namespace TankGame.Audio
{
    public static class SfxVolumeUtility
    {
        public static float GetScaled(float baseVolume)
        {
            float sfx = 1f;
            TankGame.Menu.AudioSettings settings = TankGame.Menu.AudioSettings.Instance;
            if (settings != null)
                sfx = settings.SfxVolume;

            return Mathf.Clamp01(baseVolume) * Mathf.Clamp01(sfx);
        }
    }
}
