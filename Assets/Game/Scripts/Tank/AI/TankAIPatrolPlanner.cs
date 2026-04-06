using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TankGame.Tank.AI
{
    /// <summary>
    /// Builds and maintains a stable cyclic patrol route across the map.
    /// </summary>
    public sealed class TankAIPatrolPlanner
    {
        private readonly List<Vector3> patrolPoints = new List<Vector3>(32);
        private readonly NavMeshPath pathBuffer = new NavMeshPath();

        private int currentIndex;
        private float bestDistanceToCurrentPoint = float.PositiveInfinity;
        private float lastProgressTime;
        private bool hasProgressWindow;

        public int PatrolPointCount => patrolPoints.Count;
        public bool HasRoute => patrolPoints.Count > 0;

        public void Reset()
        {
            patrolPoints.Clear();
            currentIndex = 0;
            ResetProgressWindow();
        }

        public void RebuildFromCheckpoints(IReadOnlyList<NavMeshCheckpointNode> checkpoints, Vector3 selfPosition, float sampleRadius)
        {
            patrolPoints.Clear();
            currentIndex = 0;

            if (checkpoints == null || checkpoints.Count == 0)
            {
                ResetProgressWindow();
                return;
            }

            if (!TrySample(selfPosition, sampleRadius, out Vector3 selfOnMesh))
                selfOnMesh = selfPosition;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                NavMeshCheckpointNode checkpoint = checkpoints[i];
                if (checkpoint == null)
                    continue;

                if (!TrySample(checkpoint.transform.position, sampleRadius, out Vector3 sampledPoint))
                    continue;

                if (!TryCalculateCompletePath(selfOnMesh, sampledPoint))
                    continue;

                if (ContainsClosePoint(sampledPoint, 0.75f))
                    continue;

                patrolPoints.Add(sampledPoint);
            }

            if (patrolPoints.Count <= 1)
            {
                ResetProgressWindow();
                return;
            }

            SortPointsForCoverage(patrolPoints);
            RotateRouteToNearestStart(patrolPoints, selfPosition);
            ResetProgressWindow();
        }

        public void RebuildFallbackRoute(
            Vector3 center,
            int desiredPointCount,
            float radius,
            float sampleRadius,
            float minPointSpacing)
        {
            patrolPoints.Clear();
            currentIndex = 0;

            int pointCount = Mathf.Clamp(desiredPointCount, 3, 16);
            float safeRadius = Mathf.Max(6f, radius);
            float spacing = Mathf.Max(2f, minPointSpacing);

            int maxAttempts = pointCount * 10;
            for (int i = 0; i < maxAttempts && patrolPoints.Count < pointCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * safeRadius;
                Vector3 candidate = new Vector3(center.x + randomCircle.x, center.y, center.z + randomCircle.y);

                if (!TrySample(candidate, sampleRadius, out Vector3 sampledPoint))
                    continue;

                if (ContainsClosePoint(sampledPoint, spacing))
                    continue;

                patrolPoints.Add(sampledPoint);
            }

            if (patrolPoints.Count > 1)
            {
                SortPointsForCoverage(patrolPoints);
                RotateRouteToNearestStart(patrolPoints, center);
            }

            ResetProgressWindow();
        }

        public bool TryGetCurrentDestination(
            Vector3 selfPosition,
            float currentTime,
            float reachDistance,
            float stuckTimeout,
            out Vector3 destination)
        {
            destination = Vector3.zero;
            if (patrolPoints.Count == 0)
                return false;

            int attempts = 0;
            float reachThreshold = Mathf.Max(0.4f, reachDistance);
            float timeout = Mathf.Max(1f, stuckTimeout);

            while (attempts < patrolPoints.Count)
            {
                destination = patrolPoints[currentIndex];
                float distance = Vector3.Distance(selfPosition, destination);

                if (distance <= reachThreshold)
                {
                    AdvanceToNextPoint();
                    attempts++;
                    continue;
                }

                if (!hasProgressWindow)
                {
                    hasProgressWindow = true;
                    bestDistanceToCurrentPoint = distance;
                    lastProgressTime = currentTime;
                    return true;
                }

                if (distance < bestDistanceToCurrentPoint - 0.2f)
                {
                    bestDistanceToCurrentPoint = distance;
                    lastProgressTime = currentTime;
                    return true;
                }

                if (currentTime - lastProgressTime > timeout)
                {
                    AdvanceToNextPoint();
                    attempts++;
                    continue;
                }

                return true;
            }

            destination = Vector3.zero;
            return false;
        }

        public void SkipCurrentPoint()
        {
            AdvanceToNextPoint();
        }

        private void AdvanceToNextPoint()
        {
            if (patrolPoints.Count <= 0)
                return;

            currentIndex = (currentIndex + 1) % patrolPoints.Count;
            ResetProgressWindow();
        }

        private void ResetProgressWindow()
        {
            hasProgressWindow = false;
            bestDistanceToCurrentPoint = float.PositiveInfinity;
            lastProgressTime = 0f;
        }

        private bool ContainsClosePoint(Vector3 point, float minDistance)
        {
            float minDistanceSqr = minDistance * minDistance;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if ((patrolPoints[i] - point).sqrMagnitude <= minDistanceSqr)
                    return true;
            }

            return false;
        }

        private bool TrySample(Vector3 sourcePosition, float sampleRadius, out Vector3 sampledPosition)
        {
            if (NavMesh.SamplePosition(sourcePosition, out NavMeshHit hit, Mathf.Max(0.5f, sampleRadius), NavMesh.AllAreas))
            {
                sampledPosition = hit.position;
                return true;
            }

            sampledPosition = Vector3.zero;
            return false;
        }

        private bool TryCalculateCompletePath(Vector3 from, Vector3 to)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, pathBuffer))
                return false;

            return pathBuffer.status == NavMeshPathStatus.PathComplete;
        }

        private static void SortPointsForCoverage(List<Vector3> points)
        {
            if (points == null || points.Count <= 2)
                return;

            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < points.Count; i++)
                centroid += points[i];
            centroid /= points.Count;

            points.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.z - centroid.z, a.x - centroid.x);
                float angleB = Mathf.Atan2(b.z - centroid.z, b.x - centroid.x);
                return angleA.CompareTo(angleB);
            });
        }

        private static void RotateRouteToNearestStart(List<Vector3> points, Vector3 selfPosition)
        {
            if (points == null || points.Count <= 1)
                return;

            int nearestIndex = 0;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                float sqrDistance = (points[i] - selfPosition).sqrMagnitude;
                if (sqrDistance < nearestDistance)
                {
                    nearestDistance = sqrDistance;
                    nearestIndex = i;
                }
            }

            if (nearestIndex == 0)
                return;

            var reordered = new List<Vector3>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                int index = (nearestIndex + i) % points.Count;
                reordered.Add(points[index]);
            }

            points.Clear();
            points.AddRange(reordered);
        }
    }
}
