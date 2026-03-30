using UnityEngine;

namespace TankGame.Tank.AI
{
    /// <summary>
    /// Узел маршрута для ботов. Связи задаются через Transform-пустышки в инспекторе.
    /// </summary>
    [ExecuteAlways]
    public class NavMeshCheckpointNode : MonoBehaviour
    {
        [Header("Links")]
        [Tooltip("Следующие узлы маршрута (можно задавать обычными пустышками Transform).")]
        [SerializeField] private Transform[] nextNodeTransforms;

        [Header("Gizmos")]
        [SerializeField] private float gizmoSphereRadius = 0.75f;
        [SerializeField] private Color nodeColor = Color.white;
        [SerializeField] private Color noLinksColor = Color.black;
        [SerializeField] private Color missingBackLinkColor = new Color(1f, 0.35f, 0.2f, 1f);
        [SerializeField] private Color wireColor = new Color(0.1f, 0.35f, 0.1f, 0.95f);
        [SerializeField] private Color linkColor = new Color(0.15f, 0.75f, 1f, 0.95f);
        [SerializeField] private Color brokenLinkColor = new Color(1f, 0.45f, 0.2f, 0.95f);
        [SerializeField] private float arrowHeadLength = 0.45f;
        [SerializeField] private float arrowHeadAngle = 26f;

        private NavMeshCheckpointNode[] resolvedNextNodes;

        public NavMeshCheckpointNode[] NextNodes
        {
            get
            {
                ResolveNextNodes();
                return resolvedNextNodes;
            }
        }

        private void OnValidate()
        {
            ResolveNextNodes();
        }

        private void ResolveNextNodes()
        {
            if (nextNodeTransforms == null || nextNodeTransforms.Length == 0)
            {
                resolvedNextNodes = System.Array.Empty<NavMeshCheckpointNode>();
                return;
            }

            int count = nextNodeTransforms.Length;
            if (resolvedNextNodes == null || resolvedNextNodes.Length != count)
                resolvedNextNodes = new NavMeshCheckpointNode[count];

            for (int i = 0; i < count; i++)
            {
                Transform t = nextNodeTransforms[i];
                if (t == null)
                {
                    resolvedNextNodes[i] = null;
                    continue;
                }

                NavMeshCheckpointNode node = t.GetComponent<NavMeshCheckpointNode>();
                if (node == this)
                    node = null;

                resolvedNextNodes[i] = node;
            }
        }

        private void OnDrawGizmos()
        {
            ResolveNextNodes();

            float r = Mathf.Max(0.15f, gizmoSphereRadius);
            Vector3 from = transform.position;
            bool hasLinks = HasAnyLinks();
            bool hasMissingBackLinks = HasMissingBackLinks();

            Color effectiveNodeColor = nodeColor;
            if (!hasLinks)
                effectiveNodeColor = noLinksColor;
            else if (hasMissingBackLinks)
                effectiveNodeColor = missingBackLinkColor;

            Gizmos.color = effectiveNodeColor;
            Gizmos.DrawSphere(from, r);
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(from, r * 1.15f);

            if (resolvedNextNodes == null || resolvedNextNodes.Length == 0)
                return;

            for (int i = 0; i < resolvedNextNodes.Length; i++)
            {
                NavMeshCheckpointNode next = resolvedNextNodes[i];
                if (next == null)
                    continue;

                bool hasBackLink = next.HasDirectLinkTo(this);
                Gizmos.color = hasBackLink ? linkColor : brokenLinkColor;

                Vector3 to = next.transform.position;
                Gizmos.DrawLine(from, to);
                DrawArrowHead(from, to);
            }
        }

        private bool HasAnyLinks()
        {
            if (resolvedNextNodes == null || resolvedNextNodes.Length == 0)
                return false;

            for (int i = 0; i < resolvedNextNodes.Length; i++)
            {
                if (resolvedNextNodes[i] != null)
                    return true;
            }

            return false;
        }

        private bool HasMissingBackLinks()
        {
            if (resolvedNextNodes == null || resolvedNextNodes.Length == 0)
                return false;

            for (int i = 0; i < resolvedNextNodes.Length; i++)
            {
                NavMeshCheckpointNode next = resolvedNextNodes[i];
                if (next == null)
                    continue;

                if (!next.HasDirectLinkTo(this))
                    return true;
            }

            return false;
        }

        private bool HasDirectLinkTo(NavMeshCheckpointNode target)
        {
            ResolveNextNodes();
            if (target == null || resolvedNextNodes == null || resolvedNextNodes.Length == 0)
                return false;

            for (int i = 0; i < resolvedNextNodes.Length; i++)
            {
                if (resolvedNextNodes[i] == target)
                    return true;
            }

            return false;
        }

        private void DrawArrowHead(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            if (dir.sqrMagnitude < 0.001f)
                return;

            dir.Normalize();
            float len = Mathf.Max(0.01f, arrowHeadLength);

            Quaternion leftRot = Quaternion.AngleAxis(180f - arrowHeadAngle, Vector3.up);
            Quaternion rightRot = Quaternion.AngleAxis(180f + arrowHeadAngle, Vector3.up);

            Vector3 left = to + (leftRot * dir) * len;
            Vector3 right = to + (rightRot * dir) * len;

            Gizmos.DrawLine(to, left);
            Gizmos.DrawLine(to, right);
        }
    }
}
