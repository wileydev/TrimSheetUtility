using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TrimSheetUtility
{
    public class TrimSheetPainterWindow : EditorWindow
    {
        private TrimEditorState _state = new TrimEditorState();

        [MenuItem("Tools/Trim Sheet Editor")]
        public static void ShowWindow()
        {
            TrimSheetPainterWindow window = GetWindow<TrimSheetPainterWindow>("Trim Sheet Editor");
            window.minSize = new Vector2(380f, 900f);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (_state == null)
            {
                _state = new TrimEditorState();
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    GameObject clickedObj = hit.collider.gameObject;
                    MeshFilter filter = clickedObj.GetComponent<MeshFilter>();
                    // TODO make it so that we can support other types of colliders by adding a meshcollider temporarily
                    MeshCollider collider = clickedObj.GetComponent<MeshCollider>();

                    if (filter != null && filter.sharedMesh != null && collider != null)
                    {
                        _state.SelectionWarningMessage = "";

                        if (_state.SelectedObject != null && _state.SelectedObject != clickedObj)
                        {
                            _state.SelectedFaceIndices.Clear();
                            _state.SelectionVerticesCache.Clear();
                            _state.ActiveIslandIndex = -1;
                            _state.CurrentIslandOffset = Vector2.zero;
                            _state.CurrentIslandRotation = 0f;
                        }

                        _state.SelectedObject = clickedObj;

                        if (Selection.activeGameObject != clickedObj)
                        {
                            Selection.activeGameObject = clickedObj;
                        }

                        int clickedFace = hit.triangleIndex;
                        TrimIslandModifier modifier = _state.SelectedObject.GetComponent<TrimIslandModifier>();
                        if (modifier == null)
                        {
                            modifier = _state.SelectedObject.AddComponent<TrimIslandModifier>();
                            modifier.AutoGenerateIslands();
                        }

                        // tab 1: shift + click
                        if (_state.CurrentMode == TrimEditorState.ToolMode.GroupSelection && e.shift)
                        {
                            int[] tris = filter.sharedMesh.triangles;
                            if (clickedFace * 3 + 2 < tris.Length)
                            {
                                if (_state.SelectedFaceIndices.Contains(clickedFace))
                                {
                                    _state.SelectedFaceIndices.Remove(clickedFace);
                                    _state.SelectionVerticesCache.Remove(tris[clickedFace * 3 + 0]);
                                    _state.SelectionVerticesCache.Remove(tris[clickedFace * 3 + 1]);
                                    _state.SelectionVerticesCache.Remove(tris[clickedFace * 3 + 2]);
                                }
                                else
                                {
                                    _state.SelectedFaceIndices.Add(clickedFace);
                                    _state.SelectionVerticesCache.Add(tris[clickedFace * 3 + 0]);
                                    _state.SelectionVerticesCache.Add(tris[clickedFace * 3 + 1]);
                                    _state.SelectionVerticesCache.Add(tris[clickedFace * 3 + 2]);
                                }

                                Repaint();
                                e.Use();
                            }
                        }

                        // tab 2: ctrl + click
                        if (_state.CurrentMode == TrimEditorState.ToolMode.TransformLayout && e.control)
                        {
                            int foundIslandIdx = modifier.FindIslandByTriangle(clickedFace);
                            if (foundIslandIdx != -1)
                            {
                                _state.ActiveIslandIndex = foundIslandIdx;
                                var island = modifier.DetectedIslands[_state.ActiveIslandIndex];
                                _state.CurrentIslandOffset = island.TotalOffset;
                                _state.CurrentIslandRotation = island.TotalRotation;
                                _state.CurrentIslandScale = island.TotalScale;
                            }

                            Repaint();
                            e.Use();
                        }
                    }
                }
                else
                {
                    if (e.shift || e.control)
                    {
                        _state.SelectionWarningMessage =
                            "No object with a MeshCollider was found under the cursor. Please verify that the target object has an active MeshCollider component attached.";
                        Repaint();
                    }
                }
            }
        }

        private void OnGUI()
        {
            _state.BuildGlobalFontStyles();

            GUILayout.Label("Trim Sheet Editor Workspace", _state.GlobalTitleStyle);

            if (!string.IsNullOrEmpty(_state.SelectionWarningMessage))
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                EditorGUILayout.HelpBox(_state.SelectionWarningMessage, MessageType.Error, true);
                GUI.color = Color.white;
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space(5);
            _state.CurrentMode = (TrimEditorState.ToolMode)GUILayout.Toolbar((int)_state.CurrentMode,
                new string[] { "1.  Group Islands", "2. Transform Islands" }, GUILayout.Height(30));
            EditorGUILayout.Space(5);

            if (_state.SelectedObject == null)
            {
                EditorGUILayout.HelpBox(
                    "Hold [Shift + Left Click] (Tab 1) or [Ctrl + Left Click] (Tab 2) over an object inside your Scene View viewport to start editing.",
                    MessageType.Info);
                return;
            }

            if (_state.CurrentMode == TrimEditorState.ToolMode.GroupSelection)
            {
                TrimGroupSelectionTab.Draw(_state, HandleRenderCall);
            }
            else if (_state.CurrentMode == TrimEditorState.ToolMode.TransformLayout)
            {
                // Removed the old boolean parameter completely from the delegate signature
                TrimTransformLayoutTab.Draw(_state, HandleRenderCall, ExecuteBakeCall);
            }
        }

        private void HandleRenderCall()
        {
            float panelSize = Mathf.Clamp(position.width - 30f, 128f, 1024f);
            Rect boxRect = GUILayoutUtility.GetRect(panelSize, panelSize, GUILayout.ExpandWidth(false));
            boxRect.x = (position.width - panelSize) * 0.5f;

            MeshFilter filter = _state.SelectedObject.GetComponent<MeshFilter>();
            TrimIslandModifier mod = _state.SelectedObject.GetComponent<TrimIslandModifier>();

            if (filter != null && mod != null && _state.SelectedTexture)
            {
                TrimPreviewRenderer.DrawUVAtlasPreview(boxRect, _state, filter, mod);
            }
        }

        private void ExecuteBakeCall()
        {
            MeshFilter filter = _state.SelectedObject.GetComponent<MeshFilter>();
            MeshRenderer renderer = _state.SelectedObject.GetComponent<MeshRenderer>();
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            if (_state.BakeMeshAssetOnly)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Baked Trim Mesh",
                    _state.SelectedObject.name + "_Baked", "asset", "Choose save path.");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                BakeMesh(path);

                _state.ActiveIslandIndex = -1;
                _state.SelectedFaceIndices.Clear();
                _state.SelectionVerticesCache.Clear();

                EditorUtility.DisplayDialog("Success!", "New Mesh Asset Created.", "OK");
                return;
            }

            string prefabPath = EditorUtility.SaveFilePanelInProject("Save Baked Trim Prefab",
                _state.SelectedObject.name + "_Baked", "prefab", "Choose save path.");
            if (string.IsNullOrEmpty(prefabPath))
            {
                return;
            }

            string geometryMeshPath = prefabPath.Replace(".prefab", "_Mesh.asset");

            Mesh bakedMesh = BakeMesh(geometryMeshPath);

            Material originalMaterialLink = (renderer != null) ? renderer.sharedMaterial : null;

            // Assemble Reusable Prefab Capsule Component Structure
            GameObject prefabRootClone = Instantiate(_state.SelectedObject);
            prefabRootClone.name = _state.SelectedObject.name;

            var cloneFilter = prefabRootClone.GetComponent<MeshFilter>();
            var cloneRenderer = prefabRootClone.GetComponent<MeshRenderer>();
            if (cloneFilter != null)
            {
                cloneFilter.sharedMesh = bakedMesh;
            }

            if (cloneRenderer != null)
            {
                cloneRenderer.sharedMaterial = originalMaterialLink;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRootClone, prefabPath);
            DestroyImmediate(prefabRootClone);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _state.ActiveIslandIndex = -1;
            _state.SelectedFaceIndices.Clear();
            _state.SelectionVerticesCache.Clear();

            EditorUtility.DisplayDialog("Success!",
                "Prefab with Mesh with baked UVs linked to original material created.", "OK");
        }

        private Mesh BakeMesh(string path)
        {
            MeshFilter filter = _state.SelectedObject.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return null;
            }

            Mesh meshToSave = filter.sharedMesh;
            Mesh savedMeshInstance = Instantiate(meshToSave);

            savedMeshInstance.uv =
                UniqueUVCompilationLoop(savedMeshInstance, filter.GetComponent<TrimIslandModifier>());
            PreserveSupplementalMeshChannels(meshToSave, savedMeshInstance);

            AssetDatabase.CreateAsset(savedMeshInstance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.RecordObject(filter, "Assign Baked Trim Mesh");
            filter.sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(path, typeof(Mesh));

            TrimIslandModifier modifier = _state.SelectedObject.GetComponent<TrimIslandModifier>();
            if (modifier != null)
            {
                DestroyImmediate(modifier);
            }

            return savedMeshInstance;
        }

        private void PreserveSupplementalMeshChannels(Mesh referenceSource, Mesh outputMeshTarget)
        {
            // Copy over vertex colors
            if (referenceSource.colors != null && referenceSource.colors.Length > 0)
            {
                outputMeshTarget.colors = (Color[])referenceSource.colors.Clone();
            }
            else if (referenceSource.colors32 != null && referenceSource.colors32.Length > 0)
            {
                outputMeshTarget.colors32 = (Color32[])referenceSource.colors32.Clone();
            }

            // Copy over any data in channels: UV1, UV2, UV3, UV4)
            List<Vector4> currentChannelElements = new List<Vector4>();

            for (int channelIndex = 1; channelIndex <= 4; channelIndex++)
            {
                currentChannelElements.Clear();
                referenceSource.GetUVs(channelIndex, currentChannelElements);

                if (currentChannelElements.Count > 0)
                {
                    outputMeshTarget.SetUVs(channelIndex, currentChannelElements);
                }
            }
        }

        private Vector2[] UniqueUVCompilationLoop(Mesh targetMesh, TrimIslandModifier modifier)
        {
            Vector2[] finalUVs = targetMesh.uv;
            int[] triangles = targetMesh.triangles;

            if (modifier == null || modifier.DetectedIslands.Count == 0)
            {
                return finalUVs;
            }

            foreach (var island in modifier.DetectedIslands)
            {
                Vector2 islandCentroid = Vector2.zero;
                HashSet<int> islandVertices = new HashSet<int>();

                foreach (int triIdx in island.TriangleIndices)
                {
                    if (triIdx * 3 + 2 >= triangles.Length)
                    {
                        continue;
                    }

                    islandVertices.Add(triangles[triIdx * 3 + 0]);
                    islandVertices.Add(triangles[triIdx * 3 + 1]);
                    islandVertices.Add(triangles[triIdx * 3 + 2]);
                }

                foreach (int vIdx in islandVertices)
                {
                    if (vIdx < finalUVs.Length) islandCentroid += finalUVs[vIdx];
                }

                if (islandVertices.Count > 0) islandCentroid /= islandVertices.Count;

                float shiftX = Mathf.Floor(islandCentroid.x);
                float shiftY = Mathf.Floor(islandCentroid.y);
                Vector2 uniformBlockBakeShift = new Vector2(-shiftX, -shiftY);

                foreach (int vIdx in islandVertices)
                {
                    if (vIdx < finalUVs.Length)
                    {
                        finalUVs[vIdx] += uniformBlockBakeShift;
                    }
                }
            }

            return finalUVs;
        }
    }
}