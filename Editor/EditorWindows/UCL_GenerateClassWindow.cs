using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UCL.Core;
using UCL.Core.JsonLib;
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
        private UCL_ObjectDictionary m_Dic = new();
        private bool m_Inited = false;
        public class Config : UnityJsonSerializable
        {
            public UCL_ScriptDefinition m_ScriptDefinition = new();

            public string m_Result = "";
            public string m_ExportFolder = "GeneratedScripts";
            public string m_FileName = "Test.cs";
            public bool m_ExportToFile = true;
        }
        private Config m_Config = new Config();
        //public UCL_GenerateClassWindow()
        //{
        //    LoadConfig();
        //}
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
        private void SaveConfig()
        {
            PlayerPrefs.SetString(nameof(UCL_GenerateClassWindow), m_Config.SerializeToJson().ToJson());
        }
        private void LoadConfig()
        {
            //Debug.LogError("LoadConfig");
            string json = PlayerPrefs.GetString(nameof(UCL_GenerateClassWindow));
            if (!string.IsNullOrEmpty(json))
            {
                m_Config.DeserializeFromJson(JsonData.ParseJson(json));
            }
        }
        private void WindowOnGUI()
        {
            if (!m_Inited)
            {
                LoadConfig();
                m_Inited = true;
            }


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Config", UCL_GUIStyle.ButtonStyle))
            {
                SaveConfig();
            }
            if (GUILayout.Button("Load Config", UCL_GUIStyle.ButtonStyle))
            {
                LoadConfig();
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            m_Config.m_ExportToFile = UCL_GUILayout.CheckBox(m_Config.m_ExportToFile);
            GUILayout.Label("Export To File", UCL_GUIStyle.LabelStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (m_Config.m_ExportToFile)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label("ExportFolder", UCL_GUIStyle.LabelStyle, GUILayout.ExpandWidth(false));
                m_Config.m_ExportFolder = GUILayout.TextField(m_Config.m_ExportFolder, UCL_GUIStyle.TextFieldStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("FileName", UCL_GUIStyle.LabelStyle, GUILayout.ExpandWidth(false));
                m_Config.m_FileName = GUILayout.TextField(m_Config.m_FileName, UCL_GUIStyle.TextFieldStyle);
                GUILayout.EndHorizontal();
            }


            UCL_GUILayout.DrawObjectData(m_Config.m_ScriptDefinition, m_Dic, "ScriptDefinition");


            if (GUILayout.Button("Test", UCL_GUIStyle.ButtonStyle))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                m_Config.m_ScriptDefinition.FormScript(sb);
                m_Config.m_Result = sb.ToString();
                if (m_Config.m_ExportToFile)
                {
                    string folder = Path.Combine(Application.dataPath, m_Config.m_ExportFolder);
                    Directory.CreateDirectory(folder);
                    string path = Path.Combine(folder, m_Config.m_FileName);
                    File.WriteAllText(path, m_Config.m_Result);
                }
            }
            GUILayout.Space(UCL_GUIStyle.GetScaledSize(10f));
            GUILayout.Label(m_Config.m_Result, UCL_GUIStyle.LabelStyle);
        }
    }
}