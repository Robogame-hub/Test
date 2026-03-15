using UnityEngine;
using TankGame.Tank;
using TankGame.Audio;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Диктор танка с вероятностным воспроизведением реплик. Работает только у локального игрока.
    /// </summary>
    [DisallowMultipleComponent]
    public class TankAnnouncer : MonoBehaviour
    {
        [Header("Audio Source")]
        [Tooltip("Источник звука диктора")]
        [SerializeField] private AudioSource announcerAudioSource;
        [Tooltip("Громкость диктора")]
        [SerializeField] [Range(0f, 1f)] private float announcerVolume = 1f;
        [Tooltip("Глобальная пауза между любыми репликами диктора (сек)")]
        [SerializeField] private float globalAnnouncerCooldown = 3f;

        [Header("Target Spotted")]
        [SerializeField] private AudioClip[] targetSpottedClips;
        [SerializeField] [Range(0f, 1f)] private float targetSpottedChance = 0.25f;
        [SerializeField] private float targetSpottedCooldown = 8f;

        [Header("Reloading")]
        [SerializeField] private AudioClip[] reloadingClips;
        [SerializeField] [Range(0f, 1f)] private float reloadingChance = 0.5f;
        [SerializeField] private float reloadingCooldown = 6f;

        [Header("Low HP")]
        [SerializeField] private AudioClip[] lowHpClips;
        [SerializeField] [Range(0f, 1f)] private float lowHpChance = 0.8f;
        [SerializeField] private float lowHpCooldown = 15f;

        [Header("Need Reload")]
        [SerializeField] private AudioClip[] needReloadClips;
        [SerializeField] [Range(0f, 1f)] private float needReloadChance = 0.7f;
        [SerializeField] private float needReloadCooldown = 7f;

        [Header("Out Of Ammo")]
        [SerializeField] private AudioClip[] outOfAmmoClips;
        [SerializeField] [Range(0f, 1f)] private float outOfAmmoChance = 1f;
        [SerializeField] private float outOfAmmoCooldown = 12f;

        private float lastAnyAnnouncerTime = -999f;
        private float lastTargetSpottedTime = -999f;
        private float lastReloadingTime = -999f;
        private float lastLowHpTime = -999f;
        private float lastNeedReloadTime = -999f;
        private float lastOutOfAmmoTime = -999f;
        private TankController _tankController;

        private void Awake()
        {
            _tankController = GetComponent<TankController>() ?? GetComponentInParent<TankController>();
            if (announcerAudioSource == null)
            {
                GameObject announcerAudioObject = new GameObject("AnnouncerAudioSource");
                announcerAudioObject.transform.SetParent(transform, false);
                announcerAudioSource = announcerAudioObject.AddComponent<AudioSource>();
            }

            if (announcerAudioSource != null)
            {
                announcerAudioSource.playOnAwake = false;
                announcerAudioSource.loop = false;
                announcerAudioSource.spatialBlend = 0f;
                announcerAudioSource.volume = SfxVolumeUtility.GetScaled(announcerVolume);
            }
        }

        public bool TryPlayTargetSpotted()
        {
            return TryPlayEvent(targetSpottedClips, targetSpottedChance, targetSpottedCooldown, ref lastTargetSpottedTime);
        }

        public bool TryPlayReloading()
        {
            return TryPlayEvent(reloadingClips, reloadingChance, reloadingCooldown, ref lastReloadingTime);
        }

        public bool TryPlayLowHp()
        {
            return TryPlayEvent(lowHpClips, lowHpChance, lowHpCooldown, ref lastLowHpTime);
        }

        public bool TryPlayNeedReload()
        {
            return TryPlayEvent(needReloadClips, needReloadChance, needReloadCooldown, ref lastNeedReloadTime);
        }

        public bool TryPlayOutOfAmmo()
        {
            return TryPlayEvent(outOfAmmoClips, outOfAmmoChance, outOfAmmoCooldown, ref lastOutOfAmmoTime);
        }

        private bool TryPlayEvent(AudioClip[] clips, float chance, float cooldown, ref float lastEventTime)
        {
            if (_tankController != null && !_tankController.IsLocalPlayer)
                return false;
            if (announcerAudioSource == null)
                return false;

            if (announcerAudioSource.isPlaying)
                return false;

            if (clips == null || clips.Length == 0)
                return false;

            if (Time.time - lastAnyAnnouncerTime < globalAnnouncerCooldown)
                return false;

            if (Time.time - lastEventTime < cooldown)
                return false;

            if (Random.value > chance)
                return false;

            AudioClip clip = PickRandomClip(clips);
            if (clip == null)
                return false;

            announcerAudioSource.volume = SfxVolumeUtility.GetScaled(announcerVolume);
            announcerAudioSource.PlayOneShot(clip);

            lastAnyAnnouncerTime = Time.time;
            lastEventTime = Time.time;
            return true;
        }

        private static AudioClip PickRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            int startIndex = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                int idx = (startIndex + i) % clips.Length;
                if (clips[idx] != null)
                    return clips[idx];
            }

            return null;
        }
    }
}
