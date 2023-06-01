using System.IO;
using UnityEngine;
using UnityEditor;

namespace ARKitStream
{
    [CustomEditor(typeof(ARKitReproducer))]
    public class ARKitReproducerEditor : Editor
    {
        SerializedProperty path;

        void OnEnable()
        {
            path = serializedObject.FindProperty("savePath");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string startPath = string.IsNullOrEmpty(path.stringValue)? Application.dataPath:path.stringValue;
                var result = EditorUtility.OpenFolderPanel("Open", startPath+"../", string.Empty);
                path.stringValue = string.IsNullOrEmpty(result) ? path.stringValue : result;
            }

            if(!string.IsNullOrEmpty(path.stringValue) &&
                (!Directory.Exists(path.stringValue+"/imgs") || !File.Exists(path.stringValue+"/saved-ardata.bytes")))
            {
                EditorGUILayout.LabelField($"Selected directory does not contain seved ar datas", new GUIStyle{normal=new GUIStyleState{textColor=Color.red}});
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
