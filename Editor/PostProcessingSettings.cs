#if UNITY_POST_PROCESSING_STACK_V2

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Naninovel;

namespace NaninovelPostProcess
{
    public class PostProcessingSettings : ConfigurationSettings<PostProcessingConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(PostProcessingConfiguration.PostProcessingLayer)] = l =>
            {
                if (!Configuration.OverrideObjectsLayer) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, l);
                l.intValue = EditorGUILayout.LayerField(label, l.intValue);
            };

            drawers[nameof(PostProcessingConfiguration.PostProcessResources)] = p => {
                DrawWhen(Configuration.AddPostProcessLayerToCamera, p);
            };


            drawers[nameof(PostProcessingConfiguration.AntiAliasing)] = a =>
            {
                if (!Configuration.AddPostProcessLayerToCamera) return;
                EditorGUILayout.Space(20f);
                EditorGUILayout.LabelField("Anti-Aliasing", EditorStyles.boldLabel);
                string[] antialiasSettings = new string[] { "No Anti-aliasing", "Fast Approximate Anti-aliasing (FXAA)", "Subpixel Morghological Anti-aliasing (SMAA)", "Temporal Anti-aliasing (TAA)" };
                var label = EditorGUI.BeginProperty(Rect.zero, null, a);
                a.intValue = EditorGUILayout.Popup(label, a.intValue, antialiasSettings);
            };

            drawers[nameof(PostProcessingConfiguration.LayerMask)] = p => {
                DrawWhen(Configuration.AddPostProcessLayerToCamera, p);
            };

            drawers[nameof(PostProcessingConfiguration.FastMode)] = f => {
                DrawWhen(Configuration.AntiAliasing == 1, f);
            };

            drawers[nameof(PostProcessingConfiguration.KeepAlpha)] = k => {
                DrawWhen(Configuration.AntiAliasing == 1, k);
            };

            drawers[nameof(PostProcessingConfiguration.SMAAQuality)] = s => {
                DrawWhen(Configuration.AntiAliasing == 2, s);
            };

            drawers[nameof(PostProcessingConfiguration.SMAAQuality)] = q =>
            {
                if (Configuration.AntiAliasing != 2) return;
                string[] antialiasSettings = new string[] { "Low", "Medium", "High" };
                var label = EditorGUI.BeginProperty(Rect.zero, null, q);
                q.intValue = EditorGUILayout.Popup(label, q.intValue, antialiasSettings);
            };

            drawers[nameof(PostProcessingConfiguration.JitterSpread)] = j =>
            {
                if (Configuration.AntiAliasing != 3) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, j);
                j.floatValue = EditorGUILayout.Slider(label, j.floatValue, 0f,1f);
            };

            drawers[nameof(PostProcessingConfiguration.StationaryBlending)] = s =>
            {
                if (Configuration.AntiAliasing != 3) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, s);
                s.floatValue = EditorGUILayout.Slider(label, s.floatValue, 0f, 1f);
            };

            drawers[nameof(PostProcessingConfiguration.MotionBlending)] = m =>
            {
                if (Configuration.AntiAliasing != 3) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, m);
                m.floatValue = EditorGUILayout.Slider(label, m.floatValue, 0f, 1f);
            };

            drawers[nameof(PostProcessingConfiguration.Sharpness)] = s =>
            {
                if (Configuration.AntiAliasing != 3) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, s);
                s.floatValue = EditorGUILayout.Slider(label, s.floatValue, 0f, 1f);
            };

            return drawers;


        }
        
    }
}
#endif