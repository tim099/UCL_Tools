
using System.Collections.Generic;
using UCL.Core;
using UCL.Core.UI;
using UnityEngine;

namespace UCL.ToolsLib
{
    public enum TermType
    {
        Const,
        Static,

        Private,
        Protected,
        Public,

        Virtual,
    }
    public interface UCLI_FormScript
    {
        void FormScript(System.Text.StringBuilder sb, string indent = "");
    }
    public interface UCLI_Scope : UCLI_TypeListable, UCLI_FormScript
    {

    }
    public interface UCLI_ClassScope : UCLI_TypeListable, UCLI_FormScript
    {

    }
    public class UCL_NameSpaceDefinition : UCLI_Scope
    {
        public string m_Name = "NameSpace";
        

        public List<UCLI_Scope> m_Scopes = new();


        public void FormScript(System.Text.StringBuilder sb, string indent)
        {
            sb.AppendLine(indent + $"namespace {m_Name}");
            sb.AppendLine(indent + "{");
            foreach (var scope in m_Scopes)
            {
                scope.FormScript(sb, indent + "\t");
            }
            sb.AppendLine(indent + "}");
        }
    }
    public class UCL_ClassDefinition : UCLI_Scope, UCLI_ClassScope, UCLI_NameOnGUI
    {
        public string m_Name = "ClassName";

        public List<TermType> m_Terms = new();

        public List<UCLI_ClassScope> m_Fields = new();

        public void NameOnGUI(UCL_ObjectDictionary iDic, string iDisplayName, UCL_GUILayout.DrawObjectParams iParams)
        {
            GUILayout.Label($"{m_Terms.GetTermName()} class {m_Name}", UCL_GUIStyle.LabelStyle);
        }

        public void FormScript(System.Text.StringBuilder sb, string indent)
        {
            sb.Append(indent);
            if (!m_Terms.IsNullOrEmpty())
            {
                sb.Append(m_Terms.GetTermName());
                sb.Append(' ');
            }
            sb.Append("class ");
            sb.AppendLine(m_Name);

            sb.AppendLine(indent + "{");
            foreach (var subClass in m_Fields)
            {
                subClass.FormScript(sb, indent + "\t");
            }
            sb.AppendLine(indent + "}");
        }
    }
    public class UCL_FieldDefinition : UCLI_ClassScope
    {
        public string m_Name;
        public List<TermType> m_Terms = new();

        public string m_FieldType;
        public string m_DefaultValue;
        

        public void FormScript(System.Text.StringBuilder sb, string indent)
        {
            sb.Append(indent);
            sb.Append($"{m_Terms.GetTermName()} {m_FieldType} {m_Name}");
            if (!string.IsNullOrEmpty(m_DefaultValue))
            {
                sb.Append(" = ");
                sb.Append(m_DefaultValue);
            }
            sb.AppendLine(";");
        }
    }
    public static partial class TermTypeExtensions
    {
        public static string GetTermName(this TermType term)
        {
            return term.ToString().ToLower();
        }
        public static string GetTermName(this List<TermType> terms)
        {
            return terms.ConcatToString(term => term.GetTermName(), " ");
        }
    }
}