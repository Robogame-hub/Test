using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TankGame.Tank.AI
{
    /// <summary>
    /// Builds and maintains a patrol route.
    /// For checkpoint routes, points are ordered strictly by graph links (NextNodes).
    /// </summary>
    public sealed class TankAIPatrolPlanner
    {
        private sealed class PatrolGraphNode
        {
            public NavMeshCheckpointNode Checkpoint;
            public Vector3 Position;
            public readonly List<int> Outgoing = new List<int>(4);
        }

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

            var graphNodes = BuildGraphNodes(checkpoints, selfOnMesh, sampleRadius);
            if (graphNodes.Count <= 1)
            {
                ResetProgressWindow();
                return;
            }

            int startIndex = FindNearestStartNodeIndex(graphNodes, selfOnMesh);
            if (startIndex < 0)
            {
                ResetProgressWindow();
                return;
            }

            var orderedIndices = BuildLinkedTraversal(graphNodes, startIndex);
            if (orderedIndices.Count <= 1)
            {
                ResetProgressWindow();
                return;
            }

            for (int i = 0; i < orderedIndices.Count; i++)
            {
                int graphIndex = orderedIndices[i];
                if (graphIndex < 0 || graphIndex >= graphNodes.Count)
                    continue;

                patrolPoints.Add(graphNodes[graphIndex].Position);
            }

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

        private List<PatrolGraphNode> BuildGraphNodes(
            IReadOnlyList<NavMeshCheckpointNode> checkpoints,
            Vector3 selfOnMesh,
            float sampleRadius)
        {
            var nodes = new List<PatrolGraphNode>(checkpoints.Count);
            var indexByCheckpoint = new Dictionary<NavMeshCheckpointNode, int>(checkpoints.Count);

            for (int i = 0; i < checkpoints.Count; i++)
            {
                NavMeshCheckpointNode checkpoint = checkpoints[i];
                if (checkpoint == null || indexByCheckpoint.ContainsKey(checkpoint))
                    continue;

                if (!TrySample(checkpoint.transform.position, sampleRadius, out Vector3 sampledPoint))
                    continue;

                if (!TryCalculateCompletePath(selfOnMesh, sampledPoint))
                    continue;

                var graphNode = new PatrolGraphNode
                {
                    Checkpoint = checkpoint,
                    Position = sampledPoint
                };

                int index = nodes.Count;
                nodes.Add(graphNode);
                indexByCheckpoint.Add(checkpoint, index);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                NavMeshCheckpointNode currentCheckpoint = nodes[i].Checkpoint;
                NavMeshCheckpointNode[] nextNodes = currentCheckpoint != null ? currentCheckpoint.NextNodes : null;
                if (nextNodes == null || nextNodes.Length == 0)
                    continue;

                for (int j = 0; j < nextNodes.Length; j++)
                {
                    NavMeshCheckpointNode nextCheckpoint = nextNodes[j];
                    if (nextCheckpoint == null)
                        continue;

                    if (!indexByCheckpoint.TryGetValue(nextCheckpoint, out int nextIndex))
                        continue;

                    if (nextIndex == i || nodes[i].Outgoing.Contains(nextIndex))
                        continue;

                    if (!TryCalculateCompletePath(nodes[i].Position, nodes[nextIndex].Position))
                        continue;

                    nodes[i].Outgoing.Add(nextIndex);
                }
            }

            return nodes;
        }

        private int FindNearestStartNodeIndex(List<PatrolGraphNode> graphNodes, Vector3 selfOnMesh)
        {
            int bestIndex = -1;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < graphNodes.Count; i++)
            {
                if (graphNodes[i].Outgoing.Count == 0)
                    continue;

                float pathDistance = GetPathDistance(selfOnMesh, graphNodes[i].Position);
                if (float.IsInfinity(pathDistance))
                    continue;

                if (pathDistance < bestDistance)
                {
                    bestDistance = pathDistance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
                return bestIndex;

            bestDistance = float.PositiveInfinity;
            for (int i = 0; i < graphNodes.Count; i++)
            {
                float pathDistance = GetPathDistance(selfOnMesh, graphNodes[i].Position);
                if (float.IsInfinity(pathDistance))
                    continue;

                if (pathDistance < bestDistance)
                {
                    bestDistance = pathDistance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
                return bestIndex;

            for (int i = 0; i < graphNodes.Count; i++)
            {
                float sqrDistance = (graphNodes[i].Position - selfOnMesh).sqrMagnitude;
                if (sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private List<int> BuildLinkedTraversal(List<PatrolGraphNode> graphNodes, int startIndex)
        {
            var orderedIndices = new List<int>(graphNodes.Count * 2);
            var visited = new bool[graphNodes.Count];
            int visitedCount = 0;

            orderedIndices.Add(startIndex);
            visited[startIndex] = true;
            visitedCount = 1;

            int current = startIndex;
            int guard = graphNodes.Count * Mathf.Max(4, graphNodes.Count);

            while (visitedCount < graphNodes.Count && guard-- > 0)
            {
                if (!TryFindPathToNearestUnvisited(current, graphNodes, visited, out List<int> toUnvisited))
                    break;

                AppendPathIndices(toUnvisited, orderedIndices, visited, ref visitedCount);
                current = orderedIndices[orderedIndices.Count - 1];
            }

            if (orderedIndices.Count > 1 &&
                orderedIndices[orderedIndices.Count - 1] != startIndex &&
                TryFindShortestPath(orderedIndices[orderedIndices.Count - 1], startIndex, graphNodes, out List<int> returnPath))
            {
                int ignoreVisitedCounter = visitedCount;
                AppendPathIndices(returnPath, orderedIndices, null, ref ignoreVisitedCounter);
            }

            return orderedIndices;
        }

        private static bool TryFindPathToNearestUnvisited(
            int startIndex,
            List<PatrolGraphNode> graphNodes,
            bool[] alreadyVisited,
            out List<int> path)
        {
            path = null;
            if (graphNodes == null || graphNodes.Count == 0 || startIndex < 0 || startIndex >= graphNodes.Count)
                return false;

            int count = graphNodes.Count;
            var queue = new Queue<int>(count);
            var visited = new bool[count];
            var previous = new int[count];

            for (int i = 0; i < previous.Length; i++)
                previous[i] = -1;

            visited[startIndex] = true;
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current != startIndex && !alreadyVisited[current])
                {
                    path = ReconstructPath(current, previous);
                    return path != null && path.Count > 1;
                }

                List<int> outgoing = graphNodes[current].Outgoing;
                for (int i = 0; i < outgoing.Count; i++)
                {
                    int next = outgoing[i];
                    if (next < 0 || next >= count || visited[next])
                        continue;

                    visited[next] = true;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static bool TryFindShortestPath(
            int startIndex,
            int goalIndex,
            List<PatrolGraphNode> graphNodes,
            out List<int> path)
        {
            path = null;
            if (graphNodes == null || graphNodes.Count == 0)
                return false;
            if (startIndex < 0 || startIndex >= graphNodes.Count)
                return false;
            if (goalIndex < 0 || goalIndex >= graphNodes.Count)
                return false;

            int count = graphNodes.Count;
            var queue = new Queue<int>(count);
            var visited = new bool[count];
            var previous = new int[count];
            for (int i = 0; i < previous.Length; i++)
                previous[i] = -1;

            visited[startIndex] = true;
            queue.Enqueue(startIndex);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (current == goalIndex)
                {
                    path = ReconstructPath(goalIndex, previous);
                    return path != null && path.Count > 1;
                }

                List<int> outgoing = graphNodes[current].Outgoing;
                for (int i = 0; i < outgoing.Count; i++)
                {
                    int next = outgoing[i];
                    if (next < 0 || next >= count || visited[next])
                        continue;

                    visited[next] = true;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static List<int> ReconstructPath(int endIndex, int[] previous)
        {
            var path = new List<int>(16);
            int current = endIndex;

            while (current >= 0)
            {
                path.Add(current);
                current = previous[current];
            }

            path.Reverse();
            return path;
        }

        private static void AppendPathIndices(
            List<int> path,
            List<int> orderedIndices,
            bool[] visited,
            ref int visitedCount)
        {
            if (path == null || path.Count <= 1)
                return;

            for (int i = 1; i < path.Count; i++)
            {
                int nodeIndex = path[i];
                int lastIndex = orderedIndices[orderedIndices.Count - 1];
                if (nodeIndex == lastIndex)
                    continue;

                orderedIndices.Add(nodeIndex);

                if (visited != null && !visited[nodeIndex])
                {
                    visited[nodeIndex] = true;
                    visitedCount++;
                }
            }
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

        private float GetPathDistance(Vector3 from, Vector3 to)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, pathBuffer))
                return float.PositiveInfinity;

            if (pathBuffer.status != NavMeshPathStatus.PathComplete)
                return float.PositiveInfinity;

            Vector3[] corners = pathBuffer.corners;
            if (corners == null || corners.Length < 2)
                return Vector3.Distance(from, to);

            float distance = 0f;
            for (int i = 1; i < corners.Length; i++)
                distance += Vector3.Distance(corners[i - 1], corners[i]);

            return distance;
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
