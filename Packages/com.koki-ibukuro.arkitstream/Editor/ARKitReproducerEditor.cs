using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ARKitStream
{
    [CustomEditor(typeof(ARKitReproducer))]
    public class ARKitReproducerEditor : Editor
    {
        ARKitReproducer _target;

        SerializedProperty cameraManager;
        SerializedProperty targetFrameRate;
        SerializedProperty path;

        void OnEnable()
        {
            _target = target as ARKitReproducer;
            path = serializedObject.FindProperty("savePath");
            cameraManager = serializedObject.FindProperty("cameraManager");
            targetFrameRate = serializedObject.FindProperty("targetFramerate");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var result = EditorUtility.OpenFolderPanel("Open", Application.dataPath, string.Empty);
                path.stringValue = string.IsNullOrEmpty(result) ? path.stringValue : result;
            }

            if(!string.IsNullOrEmpty(path.stringValue) &&
                (!Directory.Exists(path.stringValue+"/imgs") || !File.Exists(path.stringValue+"/saved-ardata.bytes")))
            {
                EditorGUILayout.LabelField($"Selected dirctory does not contain seved ar data", new GUIStyle{normal=new GUIStyleState{textColor=Color.red}});
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
