using UnityEngine;

namespace TankGame.Tank.AI
{
    public enum TankAIState
    {
        Idle = 0,
        Patrol = 1,
        Chase = 2,
        Search = 3
    }

    public struct TankAIBrainInput
    {
        public float Time;
        public Vector3 SelfPosition;
        public bool HasTarget;
        public bool TargetVisible;
        public bool TargetInDetectionRange;
        public Vector3 TargetPosition;
    }

    public struct TankAIBrainDecision
    {
        public TankAIState State;
        public bool HasDestination;
        public Vector3 Destination;
        public bool ShouldAim;
        public Vector3 AimPoint;
    }

    /// <summary>
    /// Pure AI state machine. It does not know about Unity components and returns only intent.
    /// </summary>
    public sealed class TankAIBrain
    {
        private readonly float chaseMemoryDuration;
        private readonly float searchDuration;
        private readonly float searchReachDistance;

        private TankAIState currentState = TankAIState.Patrol;
        private bool hasLastSeenPosition;
        private Vector3 lastSeenTargetPosition;
        private float lastSeenTime;
        private float searchStartTime;

        public TankAIState State => currentState;

        public TankAIBrain(float chaseMemoryDuration, float searchDuration, float searchReachDistance)
        {
            this.chaseMemoryDuration = Mathf.Max(0f, chaseMemoryDuration);
            this.searchDuration = Mathf.Max(0f, searchDuration);
            this.searchReachDistance = Mathf.Max(0.5f, searchReachDistance);
        }

        public void Reset()
        {
            currentState = TankAIState.Patrol;
            hasLastSeenPosition = false;
            lastSeenTargetPosition = Vector3.zero;
            lastSeenTime = 0f;
            searchStartTime = 0f;
        }

        public TankAIBrainDecision Evaluate(TankAIBrainInput input)
        {
            bool targetDetected = input.HasTarget && input.TargetVisible && input.TargetInDetectionRange;
            if (targetDetected)
            {
                currentState = TankAIState.Chase;
                hasLastSeenPosition = true;
                lastSeenTargetPosition = input.TargetPosition;
                lastSeenTime = input.Time;
            }
            else
            {
                UpdateStateWithoutVision(input);
            }

            TankAIBrainDecision decision = new TankAIBrainDecision
            {
                State = currentState,
                HasDestination = false,
                Destination = Vector3.zero,
                ShouldAim = false,
                AimPoint = Vector3.zero
            };

            switch (currentState)
            {
                case TankAIState.Chase:
                    if (input.HasTarget)
                    {
                        decision.HasDestination = true;
                        decision.Destination = input.TargetPosition;
                        decision.ShouldAim = true;
                        decision.AimPoint = input.TargetPosition;
                    }
                    break;

                case TankAIState.Search:
                    if (hasLastSeenPosition)
                    {
                        decision.HasDestination = true;
                        decision.Destination = lastSeenTargetPosition;
                        decision.ShouldAim = true;
                        decision.AimPoint = lastSeenTargetPosition;
                    }
                    break;

                case TankAIState.Patrol:
                case TankAIState.Idle:
                default:
                    break;
            }

            return decision;
        }

        private void UpdateStateWithoutVision(TankAIBrainInput input)
        {
            if (!input.HasTarget)
            {
                currentState = TankAIState.Patrol;
                hasLastSeenPosition = false;
                return;
            }

            switch (currentState)
            {
                case TankAIState.Chase:
                {
                    float sinceLastSeen = input.Time - lastSeenTime;
                    if (hasLastSeenPosition && sinceLastSeen <= chaseMemoryDuration)
                    {
                        currentState = TankAIState.Search;
                        searchStartTime = input.Time;
                    }
                    else
                    {
                        currentState = TankAIState.Patrol;
                    }
                    break;
                }

                case TankAIState.Search:
                {
                    bool reachedLastSeen = hasLastSeenPosition &&
                                           (input.SelfPosition - lastSeenTargetPosition).sqrMagnitude <=
                                           searchReachDistance * searchReachDistance;

                    bool searchTimedOut = (input.Time - searchStartTime) >= searchDuration;
                    if (reachedLastSeen || searchTimedOut)
                        currentState = TankAIState.Patrol;
                    break;
                }

                case TankAIState.Patrol:
                case TankAIState.Idle:
                default:
                    currentState = TankAIState.Patrol;
                    break;
            }
        }
    }
}
