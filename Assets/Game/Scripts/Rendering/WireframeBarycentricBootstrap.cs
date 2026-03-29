using System;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class WireframeBarycentricBootstrap
{
    private const string TargetShaderName = "Universal Render Pipeline/VR/SpatialMapping/Wireframe Single Color";
    private static readonly Dictionary<int, Mesh> ProcessedBySourceMeshId = new Dictionary<int, Mesh>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyToLoadedScenes()
    {
        try
        {
            MeshRenderer[] renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (MeshRenderer meshRenderer in renderers)
            {
                if (!UsesTargetShader(meshRenderer.sharedMaterials))
                {
                    continue;
                }

                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Mesh source = meshFilter.sharedMesh;
                Mesh processed = GetOrBuildProcessedMesh(source);
                if (processed != null)
                {
                    meshFilter.sharedMesh = processed;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Wireframe barycentric bootstrap failed: {ex.Message}");
        }
    }

    private static bool UsesTargetShader(Material[] materials)
    {
        if (materials == null)
        {
            return false;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            Material m = materials[i];
            if (m == null || m.shader == null)
            {
                continue;
            }

            if (string.Equals(m.shader.name, TargetShaderName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static Mesh GetOrBuildProcessedMesh(Mesh source)
    {
        int sourceId = source.GetInstanceID();
        if (ProcessedBySourceMeshId.TryGetValue(sourceId, out Mesh cached) && cached != null)
        {
            return cached;
        }

        Mesh processed = BuildBarycentricMesh(source);
        if (processed != null)
        {
            ProcessedBySourceMeshId[sourceId] = processed;
        }

        return processed;
    }

    private static Mesh BuildBarycentricMesh(Mesh source)
    {
        try
        {
            int subMeshCount = source.subMeshCount;
            if (subMeshCount == 0)
            {
                return source;
            }

            Vector3[] srcVertices = source.vertices;
            Vector3[] srcNormals = source.normals;
            Vector4[] srcTangents = source.tangents;
            Vector2[] srcUV = source.uv;
            Color[] srcColors = source.colors;
            BoneWeight[] srcBoneWeights = source.boneWeights;

            bool hasNormals = srcNormals != null && srcNormals.Length == srcVertices.Length;
            bool hasTangents = srcTangents != null && srcTangents.Length == srcVertices.Length;
            bool hasUV = srcUV != null && srcUV.Length == srcVertices.Length;
            bool hasColors = srcColors != null && srcColors.Length == srcVertices.Length;
            bool hasBoneWeights = srcBoneWeights != null && srcBoneWeights.Length == srcVertices.Length;

            List<Vector3> dstVertices = new List<Vector3>(srcVertices.Length);
            List<Vector3> dstNormals = hasNormals ? new List<Vector3>(srcVertices.Length) : null;
            List<Vector4> dstTangents = hasTangents ? new List<Vector4>(srcVertices.Length) : null;
            List<Vector2> dstUV = hasUV ? new List<Vector2>(srcVertices.Length) : null;
            List<Color> dstColors = hasColors ? new List<Color>(srcVertices.Length) : null;
            List<BoneWeight> dstBoneWeights = hasBoneWeights ? new List<BoneWeight>(srcVertices.Length) : null;
            List<Vector3> dstBary = new List<Vector3>(srcVertices.Length);

            List<int>[] dstSubmeshIndices = new List<int>[subMeshCount];
            for (int s = 0; s < subMeshCount; s++)
            {
                dstSubmeshIndices[s] = new List<int>();
                List<int> subIndices = dstSubmeshIndices[s];
                int[] indices = source.GetTriangles(s);

                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    int i0 = indices[i];
                    int i1 = indices[i + 1];
                    int i2 = indices[i + 2];

                    int n0 = AddVertex(i0, new Vector3(1f, 0f, 0f));
                    int n1 = AddVertex(i1, new Vector3(0f, 1f, 0f));
                    int n2 = AddVertex(i2, new Vector3(0f, 0f, 1f));

                    subIndices.Add(n0);
                    subIndices.Add(n1);
                    subIndices.Add(n2);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = source.name + "__WireBary";
            mesh.indexFormat = dstVertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.SetVertices(dstVertices);
            if (hasNormals) mesh.SetNormals(dstNormals);
            if (hasTangents) mesh.SetTangents(dstTangents);
            if (hasUV) mesh.SetUVs(0, dstUV);
            if (hasColors) mesh.SetColors(dstColors);
            if (hasBoneWeights) mesh.boneWeights = dstBoneWeights.ToArray();

            // Store barycentric coordinates in UV3 (TEXCOORD3 in shader).
            mesh.SetUVs(3, dstBary);
            mesh.subMeshCount = subMeshCount;
            for (int s = 0; s < subMeshCount; s++)
            {
                mesh.SetTriangles(dstSubmeshIndices[s], s, true);
            }

            mesh.bindposes = source.bindposes;
            mesh.bounds = source.bounds;
            mesh.RecalculateBounds();
            return mesh;

            int AddVertex(int srcIndex, Vector3 barycentric)
            {
                int newIndex = dstVertices.Count;
                dstVertices.Add(srcVertices[srcIndex]);
                if (hasNormals) dstNormals.Add(srcNormals[srcIndex]);
                if (hasTangents) dstTangents.Add(srcTangents[srcIndex]);
                if (hasUV) dstUV.Add(srcUV[srcIndex]);
                if (hasColors) dstColors.Add(srcColors[srcIndex]);
                if (hasBoneWeights) dstBoneWeights.Add(srcBoneWeights[srcIndex]);
                dstBary.Add(barycentric);
                return newIndex;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to build wireframe barycentric mesh for '{source.name}': {ex.Message}");
            return source;
        }
    }
}
