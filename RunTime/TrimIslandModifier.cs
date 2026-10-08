using UnityEngine;
using System.Collections.Generic;

namespace TrimSheetUtility
{
    public class TrimIslandModifier : MonoBehaviour
    {
        [System.Serializable]
        public class AutoIsland
        {
            public int IslandId;
            public string CustomName = "";
            public List<int> TriangleIndices = new List<int>();
            public Vector2 TotalOffset = Vector2.zero;
            public float TotalRotation = 0f;
            public float TotalScale = 1f;
            public Color PreviewColor = Color.white;
        }

        public List<AutoIsland> DetectedIslands = new List<AutoIsland>();

        private MeshFilter _meshFilter;
        private Mesh _uniqueMeshInstance;

        [HideInInspector] public Vector2[] OriginalUVs;


        // clone the mesh to allow for modifications
        // detect separate islands
        public void AutoGenerateIslands()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }

            if (_meshFilter == null || _meshFilter.sharedMesh == null)
            {
                return;
            }

            _uniqueMeshInstance = Instantiate(_meshFilter.sharedMesh);
            _uniqueMeshInstance.name = _meshFilter.sharedMesh.name + " (Trim Instance)";
            _meshFilter.mesh = _uniqueMeshInstance;

            OriginalUVs = _uniqueMeshInstance.uv;
            DetectedIslands.Clear();

            int[] triangles = _uniqueMeshInstance.triangles;
            Vector3[] vertices = _uniqueMeshInstance.vertices;
            Vector2[] uvs = OriginalUVs;
            int totalTriangles = triangles.Length / 3;

            HashSet<int> processedTriangles = new HashSet<int>();
            const float epsilon = 0.0001f;

            for (int i = 0; i < totalTriangles; i++)
            {
                if (processedTriangles.Contains(i))
                {
                    continue;
                }

                AutoIsland newIsland = new AutoIsland();
                newIsland.IslandId = i;
                newIsland.PreviewColor = Random.ColorHSV(0f, 1f, 0.7f, 0.9f, 0.6f, 0.9f);

                Queue<int> queue = new Queue<int>();
                queue.Enqueue(i);
                processedTriangles.Add(i);

                // This is O(n^2) but since this is meant to be used on low poly game models, this should be fast enough
                // If you're trying to edit a high poly model, I would consider using Blender, Substance Painter or another tool for UV editing.
                while (queue.Count > 0)
                {
                    int currentTri = queue.Dequeue();
                    newIsland.TriangleIndices.Add(currentTri);

                    Vector3 a0 = vertices[triangles[currentTri * 3 + 0]];
                    Vector3 a1 = vertices[triangles[currentTri * 3 + 1]];
                    Vector3 a2 = vertices[triangles[currentTri * 3 + 2]];

                    Vector2 uvA0 = uvs[triangles[currentTri * 3 + 0]];
                    Vector2 uvA1 = uvs[triangles[currentTri * 3 + 1]];
                    Vector2 uvA2 = uvs[triangles[currentTri * 3 + 2]];

                    for (int targetTri = 0; targetTri < totalTriangles; targetTri++)
                    {
                        if (processedTriangles.Contains(targetTri))
                        {
                            continue;
                        }

                        Vector3 b0 = vertices[triangles[targetTri * 3 + 0]];
                        Vector3 b1 = vertices[triangles[targetTri * 3 + 1]];
                        Vector3 b2 = vertices[triangles[targetTri * 3 + 2]];

                        Vector2 uvB0 = uvs[triangles[targetTri * 3 + 0]];
                        Vector2 uvB1 = uvs[triangles[targetTri * 3 + 1]];
                        Vector2 uvB2 = uvs[triangles[targetTri * 3 + 2]];

                        int sharedPoints = 0;
                        bool uvMatches = true;

                        if (Vector3.SqrMagnitude(a0 - b0) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA0 != uvB0)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a0 - b1) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA0 != uvB1)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a0 - b2) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA0 != uvB2)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a1 - b0) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA1 != uvB0)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a1 - b1) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA1 != uvB1)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a1 - b2) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA1 != uvB2)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a2 - b0) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA2 != uvB0)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a2 - b1) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA2 != uvB1)
                            {
                                uvMatches = false;
                            }
                        }

                        if (Vector3.SqrMagnitude(a2 - b2) < epsilon)
                        {
                            sharedPoints++;
                            if (uvA2 != uvB2)
                            {
                                uvMatches = false;
                            }
                        }

                        if (sharedPoints >= 2 && uvMatches)
                        {
                            // triangle is part of the current island, 
                            processedTriangles.Add(targetTri);
                            queue.Enqueue(targetTri);
                        }
                    }
                }

                DetectedIslands.Add(newIsland);
            }
        }

        // Unity enforces that each vertex only has 1 UV,
        // so if we want to have adjacent faces to have different UVs, we'll need to duplicate the vertices
        public void CreateExplicitIslandFromSelection(List<int> chosenTriangles, string groupName)
        {
            if (chosenTriangles == null || chosenTriangles.Count == 0)
            {
                return;
            }

            if (_uniqueMeshInstance == null || OriginalUVs == null)
            {
                AutoGenerateIslands();
            }

            // remove triangles from all other islands
            foreach (var autoIsland in DetectedIslands)
            {
                autoIsland.TriangleIndices.RemoveAll(t => chosenTriangles.Contains(t));
            }

            // remove islands that are empty
            DetectedIslands.RemoveAll(a => a.TriangleIndices.Count == 0 && string.IsNullOrEmpty(a.CustomName));

            List<Vector3> verts = new List<Vector3>(_uniqueMeshInstance.vertices);
            List<Vector2> uvs = new List<Vector2>(OriginalUVs);
            List<Vector3> normals = new List<Vector3>(_uniqueMeshInstance.normals);
            List<Vector4> tangents = new List<Vector4>(_uniqueMeshInstance.tangents);
            int[] tris = _uniqueMeshInstance.triangles;

            Dictionary<int, int> oldToNewVertexMap = new Dictionary<int, int>();

            foreach (int triIdx in chosenTriangles)
            {
                for (int f = 0; f < 3; f++)
                {
                    int oldVertIdx = tris[triIdx * 3 + f];

                    if (!oldToNewVertexMap.ContainsKey(oldVertIdx))
                    {
                        int newVertIdx = verts.Count;
                        verts.Add(verts[oldVertIdx]);
                        uvs.Add(uvs[oldVertIdx]);
                        if (normals.Count > oldVertIdx)
                        {
                            normals.Add(normals[oldVertIdx]);
                        }

                        if (tangents.Count > oldVertIdx)
                        {
                            tangents.Add(tangents[oldVertIdx]);
                        }

                        oldToNewVertexMap.Add(oldVertIdx, newVertIdx);
                    }

                    tris[triIdx * 3 + f] = oldToNewVertexMap[oldVertIdx];
                }
            }

            _uniqueMeshInstance.vertices = verts.ToArray();
            OriginalUVs = uvs.ToArray();
            if (normals.Count > 0)
            {
                _uniqueMeshInstance.normals = normals.ToArray();
            }

            if (tangents.Count > 0)
            {
                _uniqueMeshInstance.tangents = tangents.ToArray();
            }

            _uniqueMeshInstance.triangles = tris;

            AutoIsland explicitIsland = new AutoIsland();
            explicitIsland.IslandId = DetectedIslands.Count + 1000;
            explicitIsland.CustomName = groupName;
            explicitIsland.TriangleIndices = new List<int>(chosenTriangles);
            explicitIsland.PreviewColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.7f, 1f);

            DetectedIslands.Add(explicitIsland);
            RecomputeAllUVIslands();
        }

        public void ApplyIslandTransform(int islandIndex, Vector2 offset, float rotation, float scale)
        {
            if (islandIndex < 0 || islandIndex >= DetectedIslands.Count)
            {
                return;
            }

            DetectedIslands[islandIndex].TotalOffset = offset;
            DetectedIslands[islandIndex].TotalRotation = rotation;
            DetectedIslands[islandIndex].TotalScale = scale;

            RecomputeAllUVIslands();
        }

        private void RecomputeAllUVIslands()
        {
            if (_uniqueMeshInstance == null || OriginalUVs == null)
            {
                return;
            }

            Vector2[] dynamicUVs = (Vector2[])OriginalUVs.Clone();
            int[] triangles = _uniqueMeshInstance.triangles;

            foreach (var island in DetectedIslands)
            {
                if (island.TotalOffset == Vector2.zero && island.TotalRotation == 0f && island.TotalScale == 1f)
                {
                    continue;
                }

                float rad = -island.TotalRotation * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                Vector2 baseCentroid = Vector2.zero;
                HashSet<int> islandVertexIndices = new HashSet<int>();

                foreach (int triIdx in island.TriangleIndices)
                {
                    islandVertexIndices.Add(triangles[triIdx * 3 + 0]);
                    islandVertexIndices.Add(triangles[triIdx * 3 + 1]);
                    islandVertexIndices.Add(triangles[triIdx * 3 + 2]);
                }

                foreach (int vertIdx in islandVertexIndices)
                {
                    if (vertIdx < OriginalUVs.Length)
                    {
                        baseCentroid += OriginalUVs[vertIdx];
                    }
                }

                if (islandVertexIndices.Count > 0)
                {
                    baseCentroid /= islandVertexIndices.Count;
                }

                // Find where the center of the island would land *after* translation/scale is factored in
                Vector2 theoreticalTargetCentroid = baseCentroid + island.TotalOffset;

                // Calculate an integer offset block vector based on the center point crossing boundaries,
                // rather than checking individual vertices.
                float wrappedCentroidX = theoreticalTargetCentroid.x - Mathf.Floor(theoreticalTargetCentroid.x);
                float wrappedCentroidY = theoreticalTargetCentroid.y - Mathf.Floor(theoreticalTargetCentroid.y);
                Vector2 blockWrapCorrectionVector = new Vector2(wrappedCentroidX - theoreticalTargetCentroid.x,
                    wrappedCentroidY - theoreticalTargetCentroid.y);

                HashSet<int> processedVerticesThisIsland = new HashSet<int>();

                foreach (int triIdx in island.TriangleIndices)
                {
                    for (int f = 0; f < 3; f++)
                    {
                        int vertIdx = triangles[triIdx * 3 + f];
                        if (vertIdx >= dynamicUVs.Length || processedVerticesThisIsland.Contains(vertIdx))
                        {
                            continue;
                        }

                        processedVerticesThisIsland.Add(vertIdx);
                        Vector2 baseUV = OriginalUVs[vertIdx];

                        // Subtract base centroid to pivot operations locally
                        Vector2 originCentered = baseUV - baseCentroid;

                        // Apply local rotation matrix
                        Vector2 rotated = new Vector2(
                            originCentered.x * cos - originCentered.y * sin,
                            originCentered.x * sin + originCentered.y * cos
                        );

                        // Apply local uniform scaling factor scalar
                        Vector2 scaledAndRotated = rotated * island.TotalScale;

                        // Re-add base centroid, add translation offset, and apply block wrap shift
                        // This moves all spanned triangles as a rigid block without dividing vertex coordinates
                        dynamicUVs[vertIdx] = scaledAndRotated + baseCentroid + island.TotalOffset +
                                              blockWrapCorrectionVector;
                    }
                }
            }

            _uniqueMeshInstance.uv = dynamicUVs;
        }

        public int FindIslandByTriangle(int triangleIndex)
        {
            for (int i = 0; i < DetectedIslands.Count; i++)
            {
                if (DetectedIslands[i].TriangleIndices.Contains(triangleIndex))
                {
                    return i;
                }
            }

            return -1;
        }

        public void ResetAllTransformsOnly()
        {
            for (int i = 0; i < DetectedIslands.Count; i++)
            {
                DetectedIslands[i].TotalOffset = Vector2.zero;
                DetectedIslands[i].TotalRotation = 0f;
                DetectedIslands[i].TotalScale = 1f;
            }

            if (_uniqueMeshInstance != null && OriginalUVs != null)
            {
                _uniqueMeshInstance.uv = OriginalUVs;
            }
        }
    }
}