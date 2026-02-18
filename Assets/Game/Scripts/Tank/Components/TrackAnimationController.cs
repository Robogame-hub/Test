using UnityEngine;

namespace TankGame.Tank.Components
{
    /// <summary>
    /// Управляет анимацией левой и правой гусениц.
    /// Позволяет вручную настроить вклад движения/поворота для каждой гусеницы.
    /// Поддерживает "инерцию" анимации от фактической скорости танка после отпускания ввода.
    /// </summary>
    [RequireComponent(typeof(TankMovement), typeof(TankEngine))]
    public sealed class TrackAnimationController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Animator левой гусеницы")]
        [SerializeField] private Animator leftTrackAnimator;
        [Tooltip("Animator правой гусеницы")]
        [SerializeField] private Animator rightTrackAnimator;
        [SerializeField] private TankMovement movement;
        [SerializeField] private TankEngine engine;

        [Header("Manual Track Mix (Input)")]
        [Tooltip("Вклад forward-input в левую гусеницу")]
        [SerializeField] private float leftForwardFactor = 1f;
        [Tooltip("Вклад turn-input в левую гусеницу")]
        [SerializeField] private float leftTurnFactor = 0f;
        [Tooltip("Вклад forward-input в правую гусеницу")]
        [SerializeField] private float rightForwardFactor = 1f;
        [Tooltip("Вклад turn-input в правую гусеницу")]
        [SerializeField] private float rightTurnFactor = 0f;

        [Header("Manual Turn Split (Direction Specific)")]
        [Tooltip("RightForvardLeft: вклад правой гусеницы при повороте влево")]
        [SerializeField] private float rightForvardLeft = 1f;
        [Tooltip("LeftRorwardleft: вклад левой гусеницы при повороте влево")]
        [SerializeField] private float leftRorwardleft = -1f;
        [Tooltip("RightForvardRight: вклад правой гусеницы при повороте вправо")]
        [SerializeField] private float rightForvardRight = -1f;
        [Tooltip("LeftRorwardRight: вклад левой гусеницы при повороте вправо")]
        [SerializeField] private float leftRorwardRight = 1f;

        [Header("Animation Settings")]
        [Tooltip("Максимальная скорость воспроизведения клипа при полном газе")]
        [SerializeField] private float maxAnimatorSpeed = 2f;
        [Tooltip("Скорость сглаживания (реакция на смену направления)")]
        [SerializeField] private float speedResponse = 12f;
        [Tooltip("Переводить Animator в AnimatePhysics при старте")]
        [SerializeField] private bool useAnimatePhysics = true;
        [Tooltip("Минимальная скорость анимации при инерции (чтобы не гасла мгновенно)")]
        [SerializeField] private float inertiaMinAnimation = 0.15f;
        [Tooltip("Множитель скорости анимации на заднем ходе")]
        [SerializeField] private float backwardAnimationMultiplier = 0.75f;

        [Header("Thresholds")]
        [Tooltip("Ввод ниже этого значения считается нулём (гусеница стоит)")]
        [SerializeField] private float deadZone = 0.08f;
        [Tooltip("Скорость ниже этого значения считается остановкой")]
        [SerializeField] private float stopVelocityThreshold = 0.05f;

        [Header("Animator Parameters")]
        [Tooltip("Float-параметр для левой гусеницы в Animator. +1 вперед, -1 назад.")]
        [SerializeField] private string leftSpeedParameter  = "LeftTrackSpeed";
        [Tooltip("Float-параметр для правой гусеницы в Animator. +1 вперед, -1 назад.")]
        [SerializeField] private string rightSpeedParameter = "RightTrackSpeed";

        [Header("Invert")]
        [Tooltip("Включите, если при нажатии W гусеницы крутятся назад")]
        [SerializeField] private bool invertForward = false;
        [Tooltip("Включите, если при повороте стороны гусениц перепутаны")]
        [SerializeField] private bool invertTurn = false;

        private float currentLeftSpeed;
        private float currentRightSpeed;
        private int   leftParamHash;
        private int   rightParamHash;
        private float lastLeftSign = 1f;
        private float lastRightSign = 1f;

        private void Awake()
        {
            if (movement == null)
                movement = GetComponent<TankMovement>();
            if (engine == null)
                engine = GetComponent<TankEngine>();

            if (useAnimatePhysics)
            {
                SetupAnimator(leftTrackAnimator);
                SetupAnimator(rightTrackAnimator);
            }

            leftParamHash  = string.IsNullOrWhiteSpace(leftSpeedParameter)  ? 0 : Animator.StringToHash(leftSpeedParameter);
            rightParamHash = string.IsNullOrWhiteSpace(rightSpeedParameter) ? 0 : Animator.StringToHash(rightSpeedParameter);
        }

        /// <summary>
        /// Вызывается из TankController.FixedUpdate.
        /// verticalInput:   +1 = W (вперёд),  -1 = S (назад).
        /// horizontalInput: +1 = D (вправо),  -1 = A (влево).
        /// </summary>
        public void UpdateTrackAnimation(float verticalInput, float horizontalInput)
        {
            bool engineRunning = engine == null || engine.IsEngineRunning;
            float forward = Mathf.Abs(verticalInput) < deadZone ? 0f : verticalInput;
            float turn = Mathf.Abs(horizontalInput) < deadZone ? 0f : horizontalInput;

            if (invertForward)
                forward = -forward;
            if (invertTurn)
                turn = -turn;

            float targetLeft;
            float targetRight;
            bool isBackwardByInput = forward < 0f;

            if (engineRunning && (Mathf.Abs(forward) > 0f || Mathf.Abs(turn) > 0f))
            {
                // Базовый forward-вклад.
                targetLeft = forward * leftForwardFactor;
                targetRight = forward * rightForwardFactor;

                // Поворот раздельно для лево/право с ручными коэффициентами.
                if (turn < 0f)
                {
                    float turnAbs = Mathf.Abs(turn);
                    targetLeft += turnAbs * leftRorwardleft;
                    targetRight += turnAbs * rightForvardLeft;
                }
                else if (turn > 0f)
                {
                    float turnAbs = Mathf.Abs(turn);
                    targetLeft += turnAbs * leftRorwardRight;
                    targetRight += turnAbs * rightForvardRight;
                }

                // Дополнительные общие коэффициенты (для быстрого глобального тюнинга).
                targetLeft += turn * leftTurnFactor;
                targetRight += turn * rightTurnFactor;

                targetLeft = Mathf.Clamp(targetLeft, -1f, 1f);
                targetRight = Mathf.Clamp(targetRight, -1f, 1f);
            }
            else
            {
                // Двигатель выключен ИЛИ ввода нет: используем фактическую инерцию танка.
                Vector3 localVelocity = transform.InverseTransformDirection(movement.CurrentVelocity);
                float forwardVelocityNorm = Mathf.Clamp(localVelocity.z / Mathf.Max(movement.MoveSpeed, 0.01f), -1f, 1f);
                if (invertForward)
                    forwardVelocityNorm = -forwardVelocityNorm;

                // Чтобы клип не замирал мгновенно: поддерживаем минимальный темп на инерции.
                if (Mathf.Abs(forwardVelocityNorm) > stopVelocityThreshold && Mathf.Abs(forwardVelocityNorm) < inertiaMinAnimation)
                {
                    forwardVelocityNorm = Mathf.Sign(forwardVelocityNorm) * inertiaMinAnimation;
                }

                targetLeft = Mathf.Clamp(forwardVelocityNorm * leftForwardFactor, -1f, 1f);
                targetRight = Mathf.Clamp(forwardVelocityNorm * rightForwardFactor, -1f, 1f);
                isBackwardByInput = forwardVelocityNorm < 0f;
            }

            float directionAnimMultiplier = isBackwardByInput ? backwardAnimationMultiplier : 1f;

            float delta = speedResponse * Time.fixedDeltaTime;
            currentLeftSpeed = Mathf.MoveTowards(currentLeftSpeed, targetLeft * maxAnimatorSpeed * directionAnimMultiplier, delta);
            currentRightSpeed = Mathf.MoveTowards(currentRightSpeed, targetRight * maxAnimatorSpeed * directionAnimMultiplier, delta);

            ApplyToAnimator(leftTrackAnimator, currentLeftSpeed, leftParamHash, ref lastLeftSign);
            ApplyToAnimator(rightTrackAnimator, currentRightSpeed, rightParamHash);
        }

        private void ApplyToAnimator(Animator animator, float signedSpeed, int paramHash, ref float lastSign)
        {
            if (animator == null)
                return;

            if (Mathf.Abs(signedSpeed) < deadZone)
            {
                animator.speed = 0f;
                if (paramHash != 0)
                    animator.SetFloat(paramHash, 0f);
                return;
            }

            animator.speed = Mathf.Abs(signedSpeed);

            if (paramHash != 0)
            {
                lastSign = Mathf.Sign(signedSpeed);
                animator.SetFloat(paramHash, lastSign);
            }
        }

        private void ApplyToAnimator(Animator animator, float signedSpeed, int paramHash)
        {
            float temp = 1f;
            ApplyToAnimator(animator, signedSpeed, paramHash, ref temp);
        }

        private static void SetupAnimator(Animator animator)
        {
            if (animator == null)
                return;

            animator.updateMode = AnimatorUpdateMode.Fixed;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }
}
