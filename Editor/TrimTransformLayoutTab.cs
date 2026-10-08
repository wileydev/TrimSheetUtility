using UnityEngine;
using UnityEditor;

namespace TrimSheetUtility
{
// Tab 2 UI
    public static class TrimTransformLayoutTab
    {
        public static void Draw(TrimEditorState state, System.Action onRenderCall, System.Action onBakeCall)
        {
            if (state.SelectedTexture)
            {
                onRenderCall?.Invoke();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Target Object: {state.SelectedObject.name}", state.GlobalLabelStyle);

            TrimIslandModifier modifier = state.SelectedObject.GetComponent<TrimIslandModifier>();
            if (modifier == null || state.ActiveIslandIndex == -1 ||
                state.ActiveIslandIndex >= modifier.DetectedIslands.Count)
            {
                EditorGUILayout.HelpBox("Hold [Ctrl + Left Click] over a colored island to adjust its layout sliders.",
                    MessageType.Warning);
                return;
            }

            var activeIsland = modifier.DetectedIslands[state.ActiveIslandIndex];
            string displayLabel = string.IsNullOrEmpty(activeIsland.CustomName)
                ? $"Auto Island #{activeIsland.IslandId}"
                : $"{activeIsland.CustomName}";

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Selected Target UV Island:", displayLabel, state.GlobalTitleStyle);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("UV Offset Vector (Click & Drag X/Y Labels)", state.GlobalLabelStyle);

            state.CurrentIslandOffset =
                EditorGUILayout.Vector2Field("", state.CurrentIslandOffset, GUILayout.Height(24f));

            EditorGUILayout.Space(25f);

            EditorGUILayout.LabelField($"UV Scale Factor: {state.CurrentIslandScale.ToString("F2")}x",
                state.GlobalLabelStyle);
            state.CurrentIslandScale = GUILayout.HorizontalSlider(state.CurrentIslandScale, 0.1f, 5.0f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.Height(20f));

            EditorGUILayout.LabelField($"UV Rotation Angle: {Mathf.RoundToInt(state.CurrentIslandRotation)}°",
                state.GlobalLabelStyle);
            state.CurrentIslandRotation = GUILayout.HorizontalSlider(state.CurrentIslandRotation, 0f, 360f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.Height(20f));

            // Returns true if variables inside the tracked segment changed
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(modifier, "Translate Island Transform Matrix");
                modifier.ApplyIslandTransform(state.ActiveIslandIndex, state.CurrentIslandOffset,
                    state.CurrentIslandRotation, state.CurrentIslandScale);
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset Island", state.GlobalButtonStyle))
            {
                Undo.RecordObject(modifier, "Reset Selected Island Offset");
                modifier.ApplyIslandTransform(state.ActiveIslandIndex, Vector2.zero, 0f, 1f);
                state.CurrentIslandOffset = Vector2.zero;
                state.CurrentIslandRotation = 0f;
                state.CurrentIslandScale = 1f;
            }

            GUI.backgroundColor = new Color(0.95f, 0.65f, 0.25f);
            if (GUILayout.Button("Reset All Transforms", state.GlobalButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Reset Transforms?", "Discard all offset changes?", "Reset Layouts",
                        "Cancel"))
                {
                    Undo.RecordObject(modifier, "Reset All Transforms");
                    modifier.ResetAllTransformsOnly();
                    state.CurrentIslandOffset = Vector2.zero;
                    state.CurrentIslandRotation = 0f;
                }
            }

            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("Nuclear Reset", state.GlobalButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Reset Everything?",
                        "Completely discard all custom islands and trasforms?", "Nuclear Reset", "Cancel"))
                {
                    Undo.RecordObject(modifier, "Nuclear Re-Scan Mesh Islands");
                    modifier.AutoGenerateIslands();
                    state.ActiveIslandIndex = -1;
                    state.CurrentIslandOffset = Vector2.zero;
                    state.CurrentIslandRotation = 0f;
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(25);
            GUILayout.Label("Bake Options", state.GlobalTitleStyle);

            state.BakeMeshAssetOnly = EditorGUILayout.ToggleLeft("Export Raw Mesh Only (No Prefab)",
                state.BakeMeshAssetOnly, GUILayout.Height(24f));

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.2f, 0.65f, 0.85f);
            string buttonLabel =
                state.BakeMeshAssetOnly ? "Bake Raw Mesh Asset File" : "Bake Full Prefab Asset Package";
            if (GUILayout.Button(buttonLabel, state.GlobalButtonStyle, GUILayout.Height(38f)))
            {
                onBakeCall?.Invoke();
            }

            GUI.backgroundColor = Color.white;
        }
    }
}