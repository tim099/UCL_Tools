using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UCL.Core;
using UCL.Core.UI;
using UnityEditor;
using UnityEngine;


namespace UCL.ToolsLib
{
    public class UCL_GenerateClassWindow : EditorWindow
    {
        [UnityEditor.MenuItem("UCL/Tools/GenerateClassWindow")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<UCL_GenerateClassWindow>("GenerateClassWindow");
        }
        private Vector2 scrollPosition = Vector2.zero;
        private List<UCLI_Scope> m_Scopes = new();
        private UCL_ObjectDictionary m_Dic = new();
        private string m_Result = "";
        private string m_ExportFolder = "GeneratedScripts";
        private string m_FileName = "Test.cs";
        private void OnGUI()
        {
            UCL_GUIStyle.IsInEditorWindow = true;
            using (var scope = new GUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scope.scrollPosition;
                WindowOnGUI();
            }

            if (Event.current.type == EventType.Repaint)
            {
                Repaint();
            }

            UCL_GUIStyle.IsInEditorWindow = false;
        }

        private void WindowOnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("ExportFolder", UCL_GUIStyle.LabelStyle, GUILayout.ExpandWidth(false));
            m_ExportFolder = GUILayout.TextField(m_ExportFolder, UCL_GUIStyle.TextFieldStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("FileName", UCL_GUIStyle.LabelStyle, GUILayout.ExpandWidth(false));
            m_FileName = GUILayout.TextField(m_FileName, UCL_GUIStyle.TextFieldStyle);
            GUILayout.EndHorizontal();

            UCL_GUILayout.DrawObjectData(m_Scopes, m_Dic, "Scopes");


            if (GUILayout.Button("Test", UCL_GUIStyle.ButtonStyle))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (var scope in m_Scopes)
                {
                    scope.FormScript(sb);
                }
                m_Result = sb.ToString();
                string folder = Path.Combine(Application.dataPath, m_ExportFolder);
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, m_FileName);
                File.WriteAllText(path, m_Result);
            }
            GUILayout.Space(UCL_GUIStyle.GetScaledSize(10f));
            GUILayout.Label(m_Result, UCL_GUIStyle.LabelStyle);
        }
    }
}