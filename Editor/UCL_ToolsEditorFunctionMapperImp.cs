using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UCL.ToolsLib
{
    [UnityEditor.InitializeOnLoad]
    public static class UCL_ToolsEditorFunctionMapperImp
    {
        static UCL_ToolsEditorFunctionMapperImp()
        {
            UCL_ToolsEditorFunctionMapper.InitOpenAssetReplaceWindow(UCL_AssetReplaceWindow.ShowWindow);
        }
    }
}

