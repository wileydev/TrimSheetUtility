using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TrimSheetUtility
{
    public static class TrimPreviewRenderer
    {
        public static void DrawUVAtlasPreview(Rect textureBoxRect, TrimEditorState state, MeshFilter filter,
            TrimIslandModifier modifier)
        {
            // Draw main texture
            GUI.DrawTexture(textureBoxRect, state.SelectedTexture, ScaleMode.StretchToFill);

            Mesh mesh = filter.sharedMesh;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;

            if (uvs == null || uvs.Length == 0)
            {
                return;
            }

            GUI.BeginGroup(textureBoxRect);
            Handles.BeginGUI();

            float panelSize = textureBoxRect.width;

            // Tab 1 Selected Faces Fill
            if (state.CurrentMode == TrimEditorState.ToolMode.GroupSelection && state.SelectedFaceIndices.Count > 0)
            {
                Color fillCol = new Color(0.1f, 1f, 0.2f, 0.35f); // Vibrant neon green selection tint
                DrawSolidIslandOverlay(triangles, uvs, state.SelectedFaceIndices, panelSize, fillCol);
            }
            // Tab 2 Selected Island Fill
            else if (state.CurrentMode == TrimEditorState.ToolMode.TransformLayout && state.ActiveIslandIndex != -1 &&
                     state.ActiveIslandIndex < modifier.DetectedIslands.Count)
            {
                float alpha = 0.4f;
                Color islandBaseColor = modifier.DetectedIslands[state.ActiveIslandIndex].PreviewColor;
                Color fillCol = new Color(islandBaseColor.r, islandBaseColor.g, islandBaseColor.b, alpha);

                var activeTriangleIndices = modifier.DetectedIslands[state.ActiveIslandIndex].TriangleIndices;
                DrawSolidIslandOverlay(triangles, uvs, activeTriangleIndices, panelSize, fillCol);
            }

            // Wireframe UV Islands
            Color selectedColor = new Color(0.1f, 1f, 0.2f, 1f);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int triIndex = i / 3;
                int idx0 = triangles[i + 0];
                int idx1 = triangles[i + 1];
                int idx2 = triangles[i + 2];

                Vector2 p0 = uvs[idx0];
                Vector2 p1 = uvs[idx1];
                Vector2 p2 = uvs[idx2];

                float lineWidth = 2f;

                if (state.CurrentMode == TrimEditorState.ToolMode.GroupSelection)
                {
                    // Tab 1
                    if (state.SelectedFaceIndices.Contains(triIndex))
                    {
                        // selected triangle
                        Handles.color = selectedColor;
                        lineWidth = 3f;
                    }
                    else
                    {
                        // unselected triangle
                        int parentIslandId = modifier.FindIslandByTriangle(triIndex);
                        if (parentIslandId != -1)
                        {
                            Color c = modifier.DetectedIslands[parentIslandId].PreviewColor;
                            Handles.color = new Color(c.r, c.g, c.b, 0.25f);
                        }
                        else
                        {
                            // this shouldn't happen but found a triangle that isn't part of an island
                            Handles.color = Color.red;
                        }
                    }
                }
                else // Tab 2 
                {
                    int parentIslandId = modifier.FindIslandByTriangle(triIndex);
                    if (parentIslandId != -1)
                    {
                        Color baseColor = modifier.DetectedIslands[parentIslandId].PreviewColor;

                        if (parentIslandId == state.ActiveIslandIndex)
                        {
                            Handles.color =
                                new Color(baseColor.r, baseColor.g, baseColor.b, 1f); // Solid wire focus glow
                            lineWidth = 2.5f;
                        }
                        else
                        {
                            Handles.color =
                                new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f); // Dim passive lines
                        }
                    }
                    else
                    {
                        // this shouldn't happen but found a triangle that isn't part of an island
                        Handles.color = Color.red;
                    }
                }

                int minTileX = Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)));
                int maxTileX = Mathf.FloorToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)));
                int minTileY = Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)));
                int maxTileY = Mathf.FloorToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)));

                // We assume that the UVs are wrapped normalized to (0,1), so we draw the edges piecemeal (-1, 0, 1, ...)
                // so while this is O(n^2), n ends up being small (unless you go crazy and move the offset really big)
                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    for (int ty = minTileY; ty <= maxTileY; ty++)
                    {
                        Vector2 wrappedP0 = p0 - new Vector2(tx, ty);
                        Vector2 wrappedP1 = p1 - new Vector2(tx, ty);
                        Vector2 wrappedP2 = p2 - new Vector2(tx, ty);

                        Vector2 uv0 = new Vector2(wrappedP0.x * panelSize, (1f - wrappedP0.y) * panelSize);
                        Vector2 uv1 = new Vector2(wrappedP1.x * panelSize, (1f - wrappedP1.y) * panelSize);
                        Vector2 uv2 = new Vector2(wrappedP2.x * panelSize, (1f - wrappedP2.y) * panelSize);

                        Vector3[] points = new Vector3[] { uv0, uv1, uv2, uv0 };
                        Handles.DrawAAPolyLine(lineWidth, points);
                    }
                }
            }

            Handles.EndGUI();
            GUI.EndGroup();
        }

        // Extracted helper method handles multi-tile wrapper triangle fills safely using DrawAAConvexPolygon
        private static void DrawSolidIslandOverlay(int[] triangles, Vector2[] uvs, List<int> triangleIndicesList,
            float panelSize, Color targetFillColor)
        {
            foreach (int triIndex in triangleIndicesList)
            {
                if (triIndex * 3 + 2 >= triangles.Length)
                {
                    continue;
                }

                Vector2 p0 = uvs[triangles[triIndex * 3 + 0]];
                Vector2 p1 = uvs[triangles[triIndex * 3 + 1]];
                Vector2 p2 = uvs[triangles[triIndex * 3 + 2]];

                int minTileX = Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)));
                int maxTileX = Mathf.FloorToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)));
                int minTileY = Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)));
                int maxTileY = Mathf.FloorToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)));

                // Same O(n^2) comment as above, n should be small
                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    for (int ty = minTileY; ty <= maxTileY; ty++)
                    {
                        Vector3[] triPoints = new Vector3[]
                        {
                            new Vector2((p0.x - tx) * panelSize, (1f - (p0.y - ty)) * panelSize),
                            new Vector2((p1.x - tx) * panelSize, (1f - (p1.y - ty)) * panelSize),
                            new Vector2((p2.x - tx) * panelSize, (1f - (p2.y - ty)) * panelSize)
                        };
                        Handles.color = targetFillColor;
                        Handles.DrawAAConvexPolygon(triPoints);
                    }
                }
            }
        }
    }
}