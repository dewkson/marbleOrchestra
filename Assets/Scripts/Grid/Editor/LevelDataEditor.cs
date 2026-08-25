using UnityEditor;
using UnityEngine;

namespace MarbleOrchestra.Grid.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Open in Level Grid Editor"))
            {
                LevelGridEditorWindow.OpenFor((LevelData)target);
            }
        }
    }
}
