#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine;
using Naninovel;
using NaninovelPostProcess;
using UnityEditor;

namespace NaninovelPostProcess
{
    public class PostProcessObjectManager : MonoBehaviour
    {
        private PostProcessingConfiguration postProcessingConfiguration;

        public interface ISceneAssistant
        {
            string GetCommandString();
            string GetSpawnString();
        }

        private void Awake()
        {
            postProcessingConfiguration = Engine.GetConfiguration<PostProcessingConfiguration>();
            if (postProcessingConfiguration.OverrideObjectsLayer) gameObject.layer = postProcessingConfiguration.PostProcessingLayer;
        }

    }

#if UNITY_EDITOR
    public class PostProcessEditor : Editor
    {
        private PostProcessObjectManager.ISceneAssistant targetObject;
        protected virtual string label => null;
        public bool LogResult;
        private bool showDefaultValues;

        private void Awake()
        {
            targetObject = (PostProcessObjectManager.ISceneAssistant)target;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(30f);
            GUILayout.Label("@" + label, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("@spawn", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            if (GUILayout.Button("@", GUILayout.Height(50), GUILayout.Width(50)))
            {
                GUIUtility.systemCopyBuffer = "@" + label + " " + targetObject.GetCommandString();
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(5f);
            if (GUILayout.Button("[]", GUILayout.Height(50), GUILayout.Width(50)))
            {
                GUIUtility.systemCopyBuffer = "["+ label + " " + targetObject.GetCommandString() + "]";
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("@", GUILayout.Height(50), GUILayout.Width(50)))
            {
                GUIUtility.systemCopyBuffer = "@spawn " + targetObject.GetType().Name + " params:" + targetObject.GetSpawnString();
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(5f);
            if (GUILayout.Button("[]", GUILayout.Height(50), GUILayout.Width(50)))
            {
                GUIUtility.systemCopyBuffer = "[spawn " + targetObject.GetType().Name + " params:" + targetObject.GetSpawnString() + "]";
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.Space(5f);
            if (GUILayout.Button("params", GUILayout.Height(50), GUILayout.Width(50)))
            {
                GUIUtility.systemCopyBuffer = targetObject.GetSpawnString();
                if (LogResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);
            if (GUILayout.Toggle(LogResult, "Log Results")) LogResult = true;
            else LogResult = false;
            GUILayout.Space(5f);

            showDefaultValues = EditorGUILayout.Foldout(showDefaultValues, "Default values");
            if (showDefaultValues) base.DrawDefaultInspector();
        }
    #endif
}
#endif

}
