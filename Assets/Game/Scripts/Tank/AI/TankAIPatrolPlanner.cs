using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TankGame.Tank.AI
{
    /// <summary>
    /// Patrol planner that supports:
    /// 1) checkpoint-graph patrol (next point is chosen on arrival),
    /// 2) fallback free patrol points when no graph route is available.
    /// </summary>
    public sealed class TankAIPatrolPlanner
    {
        private sealed class PatrolGraphNode
        {
            public NavMeshCheckpointNode Checkpoint;
            public Vector3 Position;
            public readonly List<int> Outgoing = new List<int>(4);
        }

        private readonly List<PatrolGraphNode> graphNodes = new List<PatrolGraphNode>(32);
        private readonly Dictionary<NavMeshCheckpointNode, int> graphIndexByCheckpoint = new Dictionary<NavMeshCheckpointNode, int>(32);
        private readonly List<int> candidateBuffer = new List<int>(8);

        private readonly List<Vector3> fallbackPatrolPoints = new List<Vector3>(32);
        private readonly NavMeshPath pathBuffer = new NavMeshPath();

        private bool useCheckpointGraph;
        private int currentGraphNodeIndex = -1;
        private int previousGraphNodeIndex = -1;
        private int currentFallbackIndex;
        private int routeVariantSeed;

        private float bestDistanceToCurrentPoint = float.PositiveInfinity;
        private float lastProgressTime;
        private bool hasProgressWindow;

        public int PatrolPointCount => useCheckpointGraph ? graphNodes.Count : fallbackPatrolPoints.Count;
        public bool HasRoute => useCheckpointGraph
            ? graphNodes.Count > 0 && currentGraphNodeIndex >= 0 && currentGraphNodeIndex < graphNodes.Count
            : fallbackPatrolPoints.Count > 0;

        public void SetRouteVariantSeed(int seed)
        {
            routeVariantSeed = seed;
        }

        public void Reset()
        {
            graphNodes.Clear();
            graphIndexByCheckpoint.Clear();
            fallbackPatrolPoints.Clear();
            useCheckpointGraph = false;
            currentGraphNodeIndex = -1;
            previousGraphNodeIndex = -1;
            currentFallbackIndex = 0;
            ResetProgressWindow();
        }

        public void RebuildFromCheckpoints(
            IReadOnlyList<NavMeshCheckpointNode> checkpoints,
            Vector3 selfPosition,
            float sampleRadius,
            float maxCheckpointOffset)
        {
            graphNodes.Clear();
            graphIndexByCheckpoint.Clear();
            fallbackPatrolPoints.Clear();
            useCheckpointGraph = false;
            currentGraphNodeIndex = -1;
            previousGraphNodeIndex = -1;

            if (checkpoints == null || checkpoints.Count == 0)
            {
                ResetProgressWindow();
                return;
            }

            if (!TrySample(selfPosition, sampleRadius, out Vector3 selfOnMesh))
                selfOnMesh = selfPosition;

            BuildGraphNodes(checkpoints, selfOnMesh, sampleRadius, maxCheckpointOffset);
            if (graphNodes.Count <= 0)
            {
                ResetProgressWindow();
                return;
            }

            int startIndex = FindNearestStartNodeIndex(selfOnMesh);
            if (startIndex < 0)
            {
                ResetProgressWindow();
                return;
            }

            useCheckpointGraph = true;
            currentGraphNodeIndex = startIndex;
            previousGraphNodeIndex = -1;
            ResetProgressWindow();
        }

        public void RebuildFallbackRoute(
            Vector3 center,
            int desiredPointCount,
            float radius,
            float sampleRadius,
            float minPointSpacing)
        {
            graphNodes.Clear();
            graphIndexByCheckpoint.Clear();
            fallbackPatrolPoints.Clear();
            useCheckpointGraph = false;
            currentGraphNodeIndex = -1;
            previousGraphNodeIndex = -1;
            currentFallbackIndex = 0;

            int pointCount = Mathf.Clamp(desiredPointCount, 3, 16);
            float safeRadius = Mathf.Max(6f, radius);
            float spacing = Mathf.Max(2f, minPointSpacing);

            int maxAttempts = pointCount * 10;
            for (int i = 0; i < maxAttempts && fallbackPatrolPoints.Count < pointCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * safeRadius;
                Vector3 candidate = new Vector3(center.x + randomCircle.x, center.y, center.z + randomCircle.y);

                if (!TrySample(candidate, sampleRadius, out Vector3 sampledPoint))
                    continue;

                if (ContainsCloseFallbackPoint(sampledPoint, spacing))
                    continue;

                fallbackPatrolPoints.Add(sampledPoint);
            }

            if (fallbackPatrolPoints.Count > 1)
            {
                SortPointsForCoverage(fallbackPatrolPoints);
                RotateRouteToNearestStart(fallbackPatrolPoints, center);
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
            if (useCheckpointGraph)
                return TryGetCheckpointDestination(selfPosition, currentTime, reachDistance, stuckTimeout, out destination);

            return TryGetFallbackDestination(selfPosition, currentTime, reachDistance, stuckTimeout, out destination);
        }

        public void SkipCurrentPoint()
        {
            if (useCheckpointGraph)
            {
                if (!HasRoute)
                    return;

                Vector3 selectionOrigin = graphNodes[currentGraphNodeIndex].Position;
                if (TrySelectNextGraphNode(selectionOrigin, true, out int nextIndex))
                {
                    previousGraphNodeIndex = currentGraphNodeIndex;
                    currentGraphNodeIndex = nextIndex;
                }

                ResetProgressWindow();
                return;
            }

            AdvanceFallbackPoint();
        }

        private bool TryGetCheckpointDestination(
            Vector3 selfPosition,
            float currentTime,
            float reachDistance,
            float stuckTimeout,
            out Vector3 destination)
        {
            destination = Vector3.zero;
            if (!HasRoute)
                return false;

            float reachThreshold = Mathf.Max(0.03f, reachDistance);
            int guard = graphNodes.Count + 2;

            while (guard-- > 0)
            {
                destination = graphNodes[currentGraphNodeIndex].Position;
                float distanceToCurrent = Vector3.Distance(selfPosition, destination);
                if (distanceToCurrent > reachThreshold)
                    break;

                if (!TrySelectNextGraphNode(selfPosition, true, out int nextIndex))
                    break;

                previousGraphNodeIndex = currentGraphNodeIndex;
                currentGraphNodeIndex = nextIndex;
                ResetProgressWindow();
            }

            destination = graphNodes[currentGraphNodeIndex].Position;
            float distance = Vector3.Distance(selfPosition, destination);

            if (!hasProgressWindow)
            {
                hasProgressWindow = true;
                bestDistanceToCurrentPoint = distance;
                lastProgressTime = currentTime;
                return true;
            }

            if (distance < bestDistanceToCurrentPoint - 0.15f)
            {
                bestDistanceToCurrentPoint = distance;
                lastProgressTime = currentTime;
                return true;
            }

            return true;
        }

        private bool TryGetFallbackDestination(
            Vector3 selfPosition,
            float currentTime,
            float reachDistance,
            float stuckTimeout,
            out Vector3 destination)
        {
            destination = Vector3.zero;
            if (fallbackPatrolPoints.Count == 0)
                return false;

            int attempts = 0;
            float reachThreshold = Mathf.Max(0.1f, reachDistance);
            float timeout = Mathf.Max(1f, stuckTimeout);

            while (attempts < fallbackPatrolPoints.Count)
            {
                destination = fallbackPatrolPoints[currentFallbackIndex];
                float distance = Vector3.Distance(selfPosition, destination);

                if (distance <= reachThreshold)
                {
                    AdvanceFallbackPoint();
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
                    AdvanceFallbackPoint();
                    attempts++;
                    continue;
                }

                return true;
            }

            destination = Vector3.zero;
            return false;
        }

        private void AdvanceFallbackPoint()
        {
            if (fallbackPatrolPoints.Count <= 0)
                return;

            currentFallbackIndex = (currentFallbackIndex + 1) % fallbackPatrolPoints.Count;
            ResetProgressWindow();
        }

        private void ResetProgressWindow()
        {
            hasProgressWindow = false;
            bestDistanceToCurrentPoint = float.PositiveInfinity;
            lastProgressTime = 0f;
        }

        private void BuildGraphNodes(
            IReadOnlyList<NavMeshCheckpointNode> checkpoints,
            Vector3 selfOnMesh,
            float sampleRadius,
            float maxCheckpointOffset)
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                NavMeshCheckpointNode checkpoint = checkpoints[i];
                if (checkpoint == null || graphIndexByCheckpoint.ContainsKey(checkpoint))
                    continue;

                if (!TrySampleCheckpointPosition(
                        checkpoint.transform.position,
                        sampleRadius,
                        maxCheckpointOffset,
                        out Vector3 sampledPoint))
                {
                    continue;
                }

                if (!TryCalculateCompletePath(selfOnMesh, sampledPoint))
                    continue;

                var node = new PatrolGraphNode
                {
                    Checkpoint = checkpoint,
                    Position = sampledPoint
                };

                int index = graphNodes.Count;
                graphNodes.Add(node);
                graphIndexByCheckpoint.Add(checkpoint, index);
            }

            for (int i = 0; i < graphNodes.Count; i++)
            {
                NavMeshCheckpointNode currentCheckpoint = graphNodes[i].Checkpoint;
                NavMeshCheckpointNode[] nextNodes = currentCheckpoint != null ? currentCheckpoint.NextNodes : null;
                if (nextNodes == null || nextNodes.Length == 0)
                    continue;

                for (int j = 0; j < nextNodes.Length; j++)
                {
                    NavMeshCheckpointNode nextCheckpoint = nextNodes[j];
                    if (nextCheckpoint == null)
                        continue;

                    if (!graphIndexByCheckpoint.TryGetValue(nextCheckpoint, out int nextIndex))
                        continue;

                    if (nextIndex == i || graphNodes[i].Outgoing.Contains(nextIndex))
                        continue;

                    if (!TryCalculateCompletePath(graphNodes[i].Position, graphNodes[nextIndex].Position))
                        continue;

                    graphNodes[i].Outgoing.Add(nextIndex);
                }
            }
        }

        private int FindNearestStartNodeIndex(Vector3 selfOnMesh)
        {
            int bestIndex = -1;
            float bestPathDistance = float.PositiveInfinity;
            float bestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < graphNodes.Count; i++)
            {
                float sqrDistance = (graphNodes[i].Position - selfOnMesh).sqrMagnitude;
                if (TryGetPathLength(selfOnMesh, graphNodes[i].Position, out float pathDistance))
                {
                    bool pathIsBetter = pathDistance < bestPathDistance - 0.01f;
                    bool samePathButCloser = Mathf.Abs(pathDistance - bestPathDistance) <= 0.01f && sqrDistance < bestSqrDistance;
                    if (pathIsBetter || samePathButCloser)
                    {
                        bestPathDistance = pathDistance;
                        bestSqrDistance = sqrDistance;
                        bestIndex = i;
                    }

                    continue;
                }

                if (bestIndex < 0 && sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private bool TrySelectNextGraphNode(Vector3 selfPosition, bool preferNotPrevious, out int nextIndex)
        {
            nextIndex = -1;
            if (!HasRoute)
                return false;

            if (TrySelectNextGraphNodeFromOutgoing(selfPosition, preferNotPrevious, out nextIndex))
                return true;

            if (preferNotPrevious && TrySelectNextGraphNodeFromOutgoing(selfPosition, false, out nextIndex))
                return true;

            return TryFindNearestReachableGraphNode(selfPosition, currentGraphNodeIndex, out nextIndex);
        }

        private bool TrySelectNextGraphNodeFromOutgoing(Vector3 selfPosition, bool avoidPreviousNode, out int nextIndex)
        {
            nextIndex = -1;
            if (currentGraphNodeIndex < 0 || currentGraphNodeIndex >= graphNodes.Count)
                return false;

            List<int> outgoing = graphNodes[currentGraphNodeIndex].Outgoing;
            if (outgoing == null || outgoing.Count == 0)
                return false;

            float bestPathDistance = float.PositiveInfinity;
            candidateBuffer.Clear();

            for (int i = 0; i < outgoing.Count; i++)
            {
                int candidate = outgoing[i];
                if (candidate < 0 || candidate >= graphNodes.Count || candidate == currentGraphNodeIndex)
                    continue;

                if (avoidPreviousNode && outgoing.Count > 1 && candidate == previousGraphNodeIndex)
                    continue;

                Vector3 candidatePosition = graphNodes[candidate].Position;
                if (!TryGetPathLength(selfPosition, candidatePosition, out float pathDistance))
                    continue;

                if (pathDistance < bestPathDistance - 0.05f)
                {
                    bestPathDistance = pathDistance;
                    candidateBuffer.Clear();
                    candidateBuffer.Add(candidate);
                }
                else if (Mathf.Abs(pathDistance - bestPathDistance) <= 0.05f)
                {
                    candidateBuffer.Add(candidate);
                }
            }

            if (candidateBuffer.Count <= 0)
                return false;

            nextIndex = SelectCandidateBySeed(currentGraphNodeIndex, candidateBuffer);
            return nextIndex >= 0;
        }

        private bool TryFindNearestReachableGraphNode(Vector3 selfPosition, int excludedIndex, out int nextIndex)
        {
            nextIndex = -1;
            float bestPathDistance = float.PositiveInfinity;
            float bestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < graphNodes.Count; i++)
            {
                if (i == excludedIndex)
                    continue;

                Vector3 candidatePosition = graphNodes[i].Position;
                float sqrDistance = (candidatePosition - selfPosition).sqrMagnitude;

                if (TryGetPathLength(selfPosition, candidatePosition, out float pathDistance))
                {
                    bool pathIsBetter = pathDistance < bestPathDistance - 0.01f;
                    bool samePathButCloser = Mathf.Abs(pathDistance - bestPathDistance) <= 0.01f && sqrDistance < bestSqrDistance;
                    if (pathIsBetter || samePathButCloser)
                    {
                        bestPathDistance = pathDistance;
                        bestSqrDistance = sqrDistance;
                        nextIndex = i;
                    }
                }
                else if (nextIndex < 0 && sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    nextIndex = i;
                }
            }

            return nextIndex >= 0;
        }

        private int SelectCandidateBySeed(int nodeIndex, List<int> candidates)
        {
            if (candidates == null || candidates.Count <= 0)
                return -1;
            if (candidates.Count == 1)
                return candidates[0];

            unchecked
            {
                int hash = routeVariantSeed;
                hash = (hash * 397) ^ nodeIndex;
                hash ^= (hash >> 16);
                int index = Mathf.Abs(hash) % candidates.Count;
                return candidates[index];
            }
        }

        private bool ContainsCloseFallbackPoint(Vector3 point, float minDistance)
        {
            float minDistanceSqr = minDistance * minDistance;
            for (int i = 0; i < fallbackPatrolPoints.Count; i++)
            {
                if ((fallbackPatrolPoints[i] - point).sqrMagnitude <= minDistanceSqr)
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

        private bool TrySampleCheckpointPosition(
            Vector3 checkpointPosition,
            float sampleRadius,
            float maxCheckpointOffset,
            out Vector3 sampledPosition)
        {
            float allowedOffset = Mathf.Max(0.3f, maxCheckpointOffset);
            float allowedOffsetSqr = allowedOffset * allowedOffset;
            float strictSampleRadius = Mathf.Min(Mathf.Max(0.5f, sampleRadius), allowedOffset);

            if (TrySample(checkpointPosition, strictSampleRadius, out sampledPosition) &&
                (sampledPosition - checkpointPosition).sqrMagnitude <= allowedOffsetSqr)
            {
                return true;
            }

            if (!TrySample(checkpointPosition, sampleRadius, out sampledPosition))
                return false;

            return (sampledPosition - checkpointPosition).sqrMagnitude <= allowedOffsetSqr;
        }

        private bool TryGetPathLength(Vector3 from, Vector3 to, out float pathLength)
        {
            pathLength = 0f;

            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, pathBuffer))
                return false;

            if (pathBuffer.status != NavMeshPathStatus.PathComplete || pathBuffer.corners == null || pathBuffer.corners.Length < 2)
                return false;

            for (int i = 1; i < pathBuffer.corners.Length; i++)
                pathLength += Vector3.Distance(pathBuffer.corners[i - 1], pathBuffer.corners[i]);

            return true;
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
