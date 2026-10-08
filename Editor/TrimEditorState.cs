using UnityEngine;
using System.Collections.Generic;

namespace TrimSheetUtility
{
    public class TrimEditorState
    {
        public enum ToolMode
        {
            GroupSelection,
            TransformLayout
        }

        public ToolMode CurrentMode = ToolMode.GroupSelection;

        public bool BakeMeshAssetOnly = false;

        private GameObject _selectedObject;

        public GameObject SelectedObject
        {
            get => _selectedObject;
            set
            {
                _selectedObject = value;
                SelectedTexture = GetColorTexture(_selectedObject);
            }
        }

        public Texture2D SelectedTexture;

        public int ActiveIslandIndex = -1;

        public Vector2 CurrentIslandOffset;
        public float CurrentIslandRotation;
        public float CurrentIslandScale = 1f;

        public List<int> SelectedFaceIndices = new List<int>();
        public HashSet<int> SelectionVerticesCache = new HashSet<int>();
        public string ExplicitGroupName = "Custom Trim Segment";

        public string SelectionWarningMessage = "";

        public GUIStyle GlobalTitleStyle;
        public GUIStyle GlobalLabelStyle;
        public GUIStyle GlobalTextFieldStyle;
        public GUIStyle GlobalButtonStyle;

        public void BuildGlobalFontStyles()
        {
            const int unifiedFontSize = 14;
            const int titleFontSize = 18;
            const int buttonHeight = 32;
            const int textFieldHeight = 24;

            if (GlobalTitleStyle == null)
            {
                GlobalTitleStyle = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
                GlobalTitleStyle.fontSize = titleFontSize;
                GlobalTitleStyle.margin = new RectOffset(0, 0, 10, 10);
                GlobalTitleStyle.wordWrap = true;
            }

            if (GlobalLabelStyle == null)
            {
                GlobalLabelStyle = new GUIStyle(UnityEditor.EditorStyles.label);
                GlobalLabelStyle.fontSize = unifiedFontSize;
                GlobalLabelStyle.margin = new RectOffset(0, 0, 5, 5);
                GlobalLabelStyle.richText = true;
                GlobalLabelStyle.wordWrap = true;
            }

            if (GlobalTextFieldStyle == null)
            {
                GlobalTextFieldStyle = new GUIStyle(UnityEditor.EditorStyles.textField);
                GlobalTextFieldStyle.fontSize = unifiedFontSize;
                GlobalTextFieldStyle.fixedHeight = textFieldHeight;
            }

            if (GlobalButtonStyle == null)
            {
                GlobalButtonStyle = new GUIStyle(GUI.skin.button);
                GlobalButtonStyle.fontSize = unifiedFontSize;
                GlobalButtonStyle.fixedHeight = buttonHeight;
            }
        }

        public static Texture2D GetColorTexture(GameObject go)
        {
            if (!go)
            {
                return null;
            }

            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();

            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                return TrimEditorState.GetColorTexture(meshRenderer.sharedMaterial);
            }

            return null;
        }

        public static Texture2D GetColorTexture(Material mat)
        {
            if (mat == null)
            {
                return null;
            }

            // Fallback 1: Query native Unity mainTexture reference directly
            if (mat.mainTexture is Texture2D nativeMain)
            {
                return nativeMain;
            }

            // Fallback 2: Modern Universal Render Pipeline (URP Lit) Standard
            if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") is Texture2D urpAlbedo)
            {
                return urpAlbedo;
            }

            // Fallback 3: Legacy Unity Built-in Standard / Mobile Diffuse Standard
            if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") is Texture2D legacyAlbedo)
            {
                return legacyAlbedo;
            }

            // Fallback 4: High Definition Render Pipeline (HDRP Lit) Standard
            if (mat.HasProperty("_BaseColorMap") && mat.GetTexture("_BaseColorMap") is Texture2D hdrpAlbedo)
            {
                return hdrpAlbedo;
            }

            return null; // Return null if no valid 2D image asset could be matched
        }
    }
}