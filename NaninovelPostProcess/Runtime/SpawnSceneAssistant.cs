using NaninovelPostProcess;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace NaninovelPostProcess
{
    public interface ISceneAssistant
    {
        IReadOnlyDictionary<string, string> ParameterList();

        string SceneAssistantParameters();

        //bool HasCommand();
    }

    public static class SpawnSceneAssistant
    {
        public static string GetCommandString(IReadOnlyDictionary<string, string> parameters)
            => parameters != null ? string.Join(" ", parameters.Where(x => x.Value != null).Select(x => x.Key + ":" + x.Value)) : null;

        public static string GetSpawnString(IReadOnlyDictionary<string, string> parameters)
            => parameters != null ? string.Join(",", parameters.Select(x => x.Value)) : null;

        public static string CommandButton(string command, bool inlined, bool logResult, IReadOnlyDictionary<string, string> parameterList)
        {
            var finalString = String.Empty;
            if (GUILayout.Button(inlined ? "[" + command + "]" : "@" + command, GUILayout.Height(30), GUILayout.MaxWidth(150)))
            {
                var commandString = command + " " + SpawnSceneAssistant.GetCommandString(parameterList);
                finalString = inlined ? "[" + commandString + "]" : "@" + commandString;
                GUIUtility.systemCopyBuffer = finalString;
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }

            return finalString;
        }

        public static string SpawnButton(string name, bool paramsOnly, bool inlined, bool logResult, IReadOnlyDictionary<string, string> parameterList)
        {
            var finalString = String.Empty;
            if (GUILayout.Button(paramsOnly ? "params" : inlined ? "[spawn]" : "@spawn", GUILayout.Height(30), GUILayout.MaxWidth(90)))
            {
                var paramsString = SpawnSceneAssistant.GetSpawnString(parameterList);
                var commandString = paramsOnly ? paramsString : "spawn " + name + " " + paramsString;
                finalString = paramsOnly ? paramsString : inlined ? "[" + commandString + "]" : "@" + commandString;
                GUIUtility.systemCopyBuffer = finalString;
                if (logResult) Debug.Log(GUIUtility.systemCopyBuffer);
            }
            return finalString;
        }

        public static float IntField(string label, int value) => EditorGUILayout.IntField(label, value, GUILayout.Width(413));
        public static float FloatField(string label, float value) => EditorGUILayout.FloatField(label, value, GUILayout.Width(413));
        public static float SliderField(string label, float value, float minValue, float maxValue) => EditorGUILayout.Slider(label, value, minValue, maxValue, GUILayout.Width(413));
        public static Color ColorField(string label, Color value) => EditorGUILayout.ColorField(label, value, GUILayout.Width(413));
        public static bool BooleanField(string label, bool value) => EditorGUILayout.Toggle(label, value, GUILayout.Width(413));
        public static Vector2 Vector2Field(string label, Vector2 value) => EditorGUILayout.Vector2Field(label, value, GUILayout.Width(413));
        public static Vector3 Vector3Field(string label, Vector3 value) => EditorGUILayout.Vector3Field(label, value, GUILayout.Width(413));
        public static Vector4 Vector4Field(string label, Vector3 value) => EditorGUILayout.Vector4Field(label, value, GUILayout.Width(413));

        public static T EnumField<T>(string label, T value) where T : Enum
        {
            var options = Enum.GetNames(typeof(T));
            var valueIndex = Array.IndexOf(options, value.ToString());
            valueIndex = EditorGUILayout.Popup(label, valueIndex, options, GUILayout.Width(413));
            return (T)Enum.Parse(typeof(T), options[valueIndex]);
        }

        public static Texture TextureField(string label, Texture value, List<Texture> textures)
        {
            string[] texturesArray = PostProcessObject.textureIds;
            var textureIndex = Array.IndexOf(texturesArray, value?.name ?? "None");
            textureIndex = EditorGUILayout.Popup(label, textureIndex, texturesArray, GUILayout.Height(20), GUILayout.Width(413));
            value = textures.FirstOrDefault(s => s.name == texturesArray[textureIndex]);
            return value;
        }
    }
}

