using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UCL.ToolsLib
{
    public static class UCL_ToolsEditorFunctionMapper
    {
        #region OpenAssetReplaceWindow
        static System.Action<UCL_AssetReplace> m_OpenAssetReplaceWindowAct = null;
        public static void OpenAssetReplaceWindow(UCL_AssetReplace iAssetReplace)
        {
            if(m_OpenAssetReplaceWindowAct == null)
            {
                return;
            }
            m_OpenAssetReplaceWindowAct.Invoke(iAssetReplace);
        }
        public static void InitOpenAssetReplaceWindow(System.Action<UCL_AssetReplace> iOpenAssetReplaceWindowAct)
        {
            m_OpenAssetReplaceWindowAct = iOpenAssetReplaceWindowAct;
        }
        #endregion
    }
}