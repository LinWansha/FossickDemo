using Fossick.Core.Definition.Serialization;
using Fossick.MapStudio.Validation;
using Fossick.Core.Definition.Config;
using Fossick.MapStudio.Controllers;
using Fossick.MapStudio.Views;
using UnityEditor;
using UnityEngine;

namespace Fossick.Editor.DebugWindows
{
    public sealed class FossickMapEditorWindow : EditorWindow
    {
        private TextAsset mapJson;
        private FossickValidationResult validation;
        private Vector2 scroll;

        [MenuItem("Fossick/Map Debug Window")]
        public static void Open()
        {
            GetWindow<FossickMapEditorWindow>("Fossick Map Debug");
        }

        private void OnGUI()
        {
            mapJson = (TextAsset)EditorGUILayout.ObjectField("Map JSON", mapJson, typeof(TextAsset), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Selected JSON"))
                {
                    ValidateSelectedJson();
                }

                if (GUILayout.Button("Create MapStudio Object"))
                {
                    CreateMapStudioObject();
                }
            }

            DrawValidation();
        }

        private void ValidateSelectedJson()
        {
            if (mapJson == null)
            {
                validation = FossickMapValidator.Validate(FossickSampleMapFactory.CreateDefaultConfig());
                return;
            }

            var config = FossickMapJsonUtility.FromJson(mapJson.text);
            validation = FossickMapValidator.Validate(config);
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
