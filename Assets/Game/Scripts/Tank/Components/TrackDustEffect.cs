using UnityEngine;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Управляет эффектом пыли из-под гусениц. Интенсивность зависит от скорости движения.
    /// </summary>
    [RequireComponent(typeof(TankMovement))]
    public class TrackDustEffect : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Системы частиц пыли (можно несколько — под левой/правой гусеницей)")]
        [SerializeField] private ParticleSystem[] dustParticles;

        [Header("Speed Control")]
        [Tooltip("Минимальная скорость (0–1) для появления пыли")]
        [SerializeField] [Range(0f, 1f)] private float minSpeedForDust = 0.05f;
        [Tooltip("Скорость сглаживания изменения интенсивности")]
        [SerializeField] private float emissionLerpSpeed = 8f;

        [Header("Emission")]
        [Tooltip("Минимальная интенсивность (rate) при медленном движении")]
        [SerializeField] private float minEmissionRate = 0f;
        [Tooltip("Максимальная интенсивность при полном газе")]
        [SerializeField] private float maxEmissionRate = 30f;

        private TankMovement movement;
        private ParticleSystem.EmissionModule[] emissionModules;
        private float currentEmissionRate;

        private void Awake()
        {
            movement = GetComponent<TankMovement>();

            if (dustParticles != null && dustParticles.Length > 0)
            {
                emissionModules = new ParticleSystem.EmissionModule[dustParticles.Length];
                for (int i = 0; i < dustParticles.Length; i++)
                {
                    if (dustParticles[i] != null)
                        emissionModules[i] = dustParticles[i].emission;
                }
            }
        }

        private void Update()
        {
            if (movement == null || emissionModules == null || emissionModules.Length == 0)
                return;

            float speedFactor = movement.GetMovementFactor();
            float targetRate = speedFactor <= minSpeedForDust
                ? 0f
                : Mathf.Lerp(minEmissionRate, maxEmissionRate, Mathf.InverseLerp(minSpeedForDust, 1f, speedFactor));

            currentEmissionRate = Mathf.Lerp(currentEmissionRate, targetRate, Time.deltaTime * emissionLerpSpeed);

            // Обновляем интенсивность
            for (int i = 0; i < emissionModules.Length; i++)
            {
                if (dustParticles[i] == null)
                    continue;

                emissionModules[i].rateOverTime = currentEmissionRate;
            }

            // Поворачиваем пыль «назад» относительно движения танка.
            Vector3 vel = movement.CurrentVelocity;
            vel.y = 0f;

            Vector3 backDir;
            if (vel.sqrMagnitude > 0.0001f)
            {
                backDir = -vel.normalized;
            }
            else
            {
                // Когда скорость мала, считаем что пыль должна идти просто назад от корпуса.
                backDir = -transform.forward;
                backDir.y = 0f;
                if (backDir.sqrMagnitude > 0.0001f)
                    backDir.Normalize();
            }

            if (backDir.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(backDir, Vector3.up);
                for (int i = 0; i < dustParticles.Length; i++)
                {
                    if (dustParticles[i] == null)
                        continue;
                    dustParticles[i].transform.rotation = rot;
                }
            }
        }
    }
}
