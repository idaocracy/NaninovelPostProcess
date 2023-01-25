#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;


namespace NaninovelPostProcess
{
    public class SpawnObject : MonoBehaviour
    {

    }


#if UNITY_EDITOR

    [CustomEditor(typeof(SpawnObject))]
    public class SpawnObjectEditor : Editor
    {
        protected SpawnObject spawnObject;
        protected virtual string Label => null;
        private bool logResult;
        private bool showDefaultValues;
        protected ISceneAssistant sceneAssistant;

        protected virtual void Awake()
        {
            spawnObject = (SpawnObject)target;
            sceneAssistant = spawnObject.gameObject.GetComponent<ISceneAssistant>() ?? null;
        }

        public override void OnInspectorGUI()
        {
            if (sceneAssistant != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Scene Assistant Options:", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(5f);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                SpawnSceneAssistant.CommandButton(Label, false, logResult, sceneAssistant.ParameterList());
                GUILayout.Space(5f);
                SpawnSceneAssistant.CommandButton(Label, true, logResult, sceneAssistant.ParameterList());
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                SpawnSceneAssistant.SpawnButton(target.name, false, false, logResult, sceneAssistant.ParameterList());
                GUILayout.Space(5f);
                SpawnSceneAssistant.SpawnButton(target.name, false, true, logResult, sceneAssistant.ParameterList());
                GUILayout.Space(5f);
                SpawnSceneAssistant.SpawnButton(target.name, true, true, logResult, sceneAssistant.ParameterList());
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(5f);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Toggle(logResult, "Log Results")) logResult = true;
                else logResult = false;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                showDefaultValues = EditorGUILayout.Foldout(showDefaultValues, "Default values");
                serializedObject.Update();
                if (showDefaultValues) DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
                serializedObject.ApplyModifiedProperties();
            }
        }

    }

#endif
}

#endif