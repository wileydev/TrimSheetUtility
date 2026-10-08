using UnityEngine;
using UnityEditor;

namespace TrimSheetUtility
{
// Tab 1 UI
    public static class TrimGroupSelectionTab
    {
        public static void Draw(TrimEditorState state, System.Action onRenderCall)
        {
            if (state.SelectedTexture)
            {
                onRenderCall?.Invoke();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Island Group Extractor", state.GlobalTitleStyle);
            EditorGUILayout.LabelField(
                "Instructions: Hold [Shift + Left Click] on geometry faces in your scene view to select faces for a new island.",
                state.GlobalLabelStyle);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Custom Island Identifier Name:", state.GlobalLabelStyle);
            state.ExplicitGroupName = EditorGUILayout.TextField(state.ExplicitGroupName, state.GlobalTextFieldStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Selected Faces Count: {state.SelectedFaceIndices.Count}",
                state.GlobalLabelStyle);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Extract Faces & Split to New Island", state.GlobalButtonStyle))
            {
                TrimIslandModifier modifier = state.SelectedObject.GetComponent<TrimIslandModifier>();
                if (modifier != null && state.SelectedFaceIndices.Count > 0)
                {
                    Undo.RecordObject(modifier, "Create Split Explicit Island");
                    modifier.CreateExplicitIslandFromSelection(state.SelectedFaceIndices, state.ExplicitGroupName);
                    state.SelectedFaceIndices.Clear();
                    state.SelectionVerticesCache.Clear();
                    EditorUtility.DisplayDialog("Success!", "Faces successfully split into their own unique island!",
                        "Excellent");
                }
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("Clear Selection", state.GlobalButtonStyle))
            {
                state.SelectedFaceIndices.Clear();
                state.SelectionVerticesCache.Clear();
            }

            EditorGUILayout.EndVertical();
        }
    }
}