#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine;
using Naninovel;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Commands;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering.PostProcessing;
using System;

namespace NaninovelPostProcess
{
    public class PostProcessObject : MonoBehaviour
    {
        private PostProcessingConfiguration postProcessingConfiguration;

        public interface ISceneAssistant
        {
            IReadOnlyDictionary<string, string> ParameterList();
        }

    private void Awake()
        {
            postProcessingConfiguration = Engine.GetConfiguration<PostProcessingConfiguration>();
            if (postProcessingConfiguration.OverrideObjectsLayer) gameObject.layer = postProcessingConfiguration.PostProcessingLayer;
        }

        public string GetCommandString() => (this is ISceneAssistant sceneAssistant) ?  
            string.Join(" ", sceneAssistant.ParameterList().Where(x => x.Value != null).Select(x => x.Key + ":" + x.Value)) : null;
        
        public string GetSpawnString() => (this is ISceneAssistant sceneAssistant) ? 
            string.Join(",", sceneAssistant.ParameterList().Select(x => x.Value)) : null;

        protected virtual float FloatField(string label, float value)
        {
            GUILayout.BeginHorizontal();
            value = EditorGUILayout.FloatField(label, value, GUILayout.Width(413));
            GUILayout.EndHorizontal();
            return value;
        }

        protected virtual float SliderField(string label, float value, float minValue, float maxValue)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(190));
            value = EditorGUILayout.Slider(value, minValue, maxValue, GUILayout.Width(220));
            GUILayout.EndHorizontal();
            return value;
        }

        protected virtual Color ColorField(string label, Color value)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color", GUILayout.Width(190));
            value = EditorGUILayout.ColorField(value, GUILayout.Width(220));
            GUILayout.EndHorizontal();
            return value;
        }

        protected virtual bool BooleanField(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(190));
            value = EditorGUILayout.Toggle(value);
            GUILayout.EndHorizontal();
            return value;
        }

        protected virtual Texture TextureField(string label, Texture value, List<Texture> textures, List<string> textureIds)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Dirt Texture", GUILayout.Width(190));
            string[] texturesArray = textureIds.ToArray();
            var textureIndex = Array.IndexOf(texturesArray, value?.name ?? "None");
            textureIndex = EditorGUILayout.Popup(textureIndex, texturesArray, GUILayout.Height(20), GUILayout.Width(220));
            value = textures.FirstOrDefault(s => s.name == textureIds[textureIndex]) ?? null;
            GUILayout.EndHorizontal();
            return value;
            
        }
    }


#if UNITY_EDITOR
    public class PostProcessObjectEditor : Editor
    {
        private PostProcessObject targetObject;
        protected virtual string label => null;
        public bool LogResult;
        private bool showDefaultValues;

        private void Awake()
        {
            targetObject = (PostProcessObject)target;
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
