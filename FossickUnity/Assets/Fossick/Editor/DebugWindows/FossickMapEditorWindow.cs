using Fossick.Core.Definition.Config;
using Fossick.Core.Definition.Serialization;
using Fossick.MapStudio.Validation;
using Fossick.MapStudio.Controllers;
using Fossick.MapStudio.Views;
using UnityEditor;
using UnityEngine;

namespace Fossick.Editor.DebugWindows
{
    public sealed class FossickMapEditorWindow : EditorWindow
    {
        private FossickValidationResult validation;
        private Vector2 scroll;

        [MenuItem("Fossick/Map Debug Window")]
        public static void Open()
        {
            GetWindow<FossickMapEditorWindow>("Fossick Map Debug");
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate MapStudio Project"))
                {
                    ValidateEditableProject();
                }

                if (GUILayout.Button("Create MapStudio Object"))
                {
                    CreateMapStudioObject();
                }
            }

            DrawValidation();
        }

        private void ValidateEditableProject()
        {
            var project = FossickMapProjectFileService.LoadEditableProject();
            if (project == null)
            {
                validation = FossickMapValidator.Validate(FossickSampleMapFactory.CreateDefaultConfig());
                return;
            }

            validation = FossickMapValidator.Validate(project.ToRuntimeConfig());
        }

        private static void CreateMapStudioObject()
        {
            var go = new GameObject("Fossick MapStudio");
            go.AddComponent<FossickMapStudioController>();
            go.AddComponent<FossickMapStudioView>();
            Selection.activeGameObject = go;
        }

        private void DrawValidation()
        {
            if (validation == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(validation.HasErrors ? "Validation: Errors" : "Validation: OK", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (var i = 0; i < validation.issues.Count; i++)
            {
                var issue = validation.issues[i];
                EditorGUILayout.LabelField($"[{issue.severity}] {issue.message}");
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
