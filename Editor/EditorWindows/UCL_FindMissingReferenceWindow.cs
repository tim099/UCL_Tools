using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UCL.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UCL.Core
{
    [System.Serializable]
    public class ClearConfig
    {
        public bool m_ClearMissingPrefab = true;
        public bool m_ClearMissingComponent = true;
        public bool m_ClearMissingField = true;
        /// <summary>
        /// Clear missing references in Prefab
        /// </summary>
        public bool m_ClearMissingInPrefab = false;
    }


    public class MissingReferenceInfo
    {
        /// <summary>
        /// 實際missing的目標
        /// </summary>
        public Object target;
        /// <summary>
        /// 紀錄路徑用來追查位置
        /// </summary>
        public string path;

        public MissingReferenceInfo() { }
        public MissingReferenceInfo(Object target, string path)
        {
            this.target = target;
            this.path = path;
        }
    }
    public class MissingReferenceScene
    {
        public SceneAsset scene;
        public List<MissingReferenceInfo> missingReferences = null;
        public bool showDetail = false;
    }
    public class UCL_FindMissingReferenceWindow : EditorWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 scrollPosition2 = Vector2.zero;
        private List<MissingReferenceScene> m_MissingAssetSceneList = null;
        private ClearConfig m_ClearConfig = new ClearConfig();
        private UCL_ObjectDictionary m_Dic = new UCL_ObjectDictionary();

        [UnityEditor.MenuItem("UCL/Tools/FindMissingReferenceWindow")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<UCL_FindMissingReferenceWindow>("FindMissingReferenceWindow");
        }
        public UCL_FindMissingReferenceWindow() { }
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
        /// <summary>
        /// 判斷目標Object內是否有missing reference
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static bool CheckMissingReference(UnityEngine.Object target, out string name)
        {
            name = string.Empty;
            if (target == null)//missing!!
            {
                return true;
            }
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty itr = serializedObject.GetIterator();
            while (itr.Next(true))
            {

                if (itr.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (itr.objectReferenceValue == null && itr.objectReferenceInstanceIDValue != 0)
                    {
                        name = $"{itr.propertyPath}({itr.objectReferenceInstanceIDValue})";
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CheckGameObjectMissingReference(GameObject target, List<MissingReferenceInfo> missingReferences, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = target.name;
            }
            else
            {
                path = $"{path}.{target.name}";
            }
            if (PrefabUtility.IsPrefabAssetMissing(target))//missing!!
            {

                missingReferences.Add(new MissingReferenceInfo(target, path));
                //Debug.LogError($"Missing Prefab detected in GameObject: {target.name}");
                return true;
            }

            if (target == null)//missing!!
            {
                Debug.LogError($"CheckGameObjectMissingReference target == null, path:{path}");
                return true;
            }

            if (PrefabUtility.IsAnyPrefabInstanceRoot(target))//是Prefab 要特殊處理
            {
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(target) as GameObject;

                if (prefab != null)
                {
                    //Debug.Log($"path: {path}, Prefab:{prefab.name}");
                    //TODO Prefab內部檢查要特殊處理
                    //missingReferences.Add(new MissingReferenceInfo(target, $"(Prefab){path}"));
                    return CheckGameObjectMissingReference(prefab, missingReferences, $"(Prefab){path}");
                }
                else
                {
                    Debug.LogError($"CheckGameObjectMissingReference prefab == null, path:{path}");
                }
                return false;
            }

            var components = target.GetComponents<Component>();
            bool isMissingReference = false;
            foreach (var component in components)
            {
                if (CheckMissingReference(component, out string name))
                {
                    string result = null;
                    if (component != null)
                    {
                        result = $"{path}({component.GetType().Name})";
                    }
                    else
                    {
                        result = $"{path}(Missing Component)";
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        result += $".{name}";
                    }
                    missingReferences.Add(new MissingReferenceInfo(target, result));//標記是哪個Component
                    isMissingReference = true;
                }
            }

            foreach (Transform child in target.transform)
            {
                if (CheckGameObjectMissingReference(child.gameObject, missingReferences, path))
                {
                    isMissingReference = true;
                }
            }
            return isMissingReference;
        }

        private static void ClearMissingReference(SceneAsset target, ClearConfig config, bool restoreCurrentScene = true)
        {
            // 保存當前開啟的場景路徑
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(target);
                var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
                if (scene.IsValid())
                {
                    //bool hasMissingReference = false;
                    // 遍歷場景中的所有 GameObject
                    var rootGameObjects = scene.GetRootGameObjects();
                    var missingReferences = new List<MissingReferenceInfo>();
                    foreach (var root in rootGameObjects)
                    {
                        ClearGameObjectMissingReference(root, config);
                    }
                    //if (hasMissingReference)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene, assetPath);//保存修改
                        AssetDatabase.SaveAssets();//保存修改
                    }
                }
                else
                {
                    Debug.LogError($"Failed to open scene: {assetPath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if(restoreCurrentScene) EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }
        }
        private static void ClearGameObjectMissingReference(GameObject target, ClearConfig config)
        {
            if (PrefabUtility.IsPrefabAssetMissing(target))//missing!!
            {
                if (config.m_ClearMissingPrefab)
                {
                    //Debug.LogError($"ClearGameObjectMissingReference DestroyImmediate:{target.name}");
                    GameObject.DestroyImmediate(target);
                }
                return;
            }

            if (target == null)//理論上不會遇到 防呆
            {
                Debug.LogError($"ClearGameObjectMissingReference target == null");
                if (config.m_ClearMissingPrefab) GameObject.DestroyImmediate(target);
                return;
            }
            if (PrefabUtility.IsAnyPrefabInstanceRoot(target))//是Prefab 要特殊處理
            {
                if (!config.m_ClearMissingInPrefab)//Dont clear missing references in Prefab
                {
                    return;
                }
                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(target) as GameObject;

                if (prefab != null)
                {
                    //Debug.Log($"path: {path}, Prefab:{prefab.name}");
                    //TODO Prefab內部檢查要特殊處理
                    //missingReferences.Add(new MissingReferenceInfo(target, $"(Prefab){path}"));
                    ClearGameObjectMissingReference(prefab, config);
                    AssetDatabase.SaveAssetIfDirty(prefab);
                }
                else
                {
                    Debug.LogError($"CheckGameObjectMissingReference prefab == null");
                }
                return;
            }


            var components = target.GetComponents<Component>();
            bool hasMissingComponents = false;//有missing Component
            bool dirty = false;
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)//missing Component!!
                {
                    hasMissingComponents = true;
                }
                else if (config.m_ClearMissingField)
                {
                    if (ClearMissingReference(component, config))
                    {
                        dirty = true;
                    }
                }
            }
            if (hasMissingComponents && config.m_ClearMissingComponent)//有missing Components
            {
                int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);//目前暫時無效
                dirty = true;
            }
            if (dirty)
            {
                EditorUtility.SetDirty(target);
            }
            foreach (Transform child in target.transform)
            {
                ClearGameObjectMissingReference(child.gameObject, config);
            }
        }
        private static bool ClearMissingReference(UnityEngine.Object target, ClearConfig config)
        {
            if (target == null)//missing!!
            {
                return true;
            }
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty itr = serializedObject.GetIterator();
            bool hasChanges = false;
            while (itr.Next(true))
            {
                if (itr.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (itr.objectReferenceValue == null && itr.objectReferenceInstanceIDValue != 0)
                    {
                        //Debug.LogError($"Clear:{itr.propertyPath}");
                        itr.objectReferenceValue = null; // 清除missing引用
                        hasChanges = true;
                    }
                }
            }
            if (hasChanges)
            {
                serializedObject.ApplyModifiedProperties(); // 保存更改
                EditorUtility.SetDirty(target);
            }
            return false;
        }
        private static async UniTask<List<MissingReferenceScene>> CheckMissingReference()
        {
            List<MissingReferenceScene> missingAssetSceneList = new();
            //m_MissingAssetSceneList.Clear();
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            var scenesPath = allAssets.Where(assetPath => assetPath.EndsWith(".unity"));
            int completedCount = 0;
            int totalCount = scenesPath.Count();
            // 保存當前開啟的場景路徑 改用OpenSceneMode.Additive 不需要還原
            var currentScene = EditorSceneManager.GetActiveScene();
            string currentScenePath = currentScene.path;
            
            try
            {
                foreach (string assetPath in scenesPath)
                {
                    try
                    {
                        //https://discussions.unity.com/t/check-if-asset-inside-package-is-readonly/793326
                        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath) != null)
                        {//If the above is “null”, the scene is not in a package and can be opened.
                            continue;//scene in a package
                        }
                        float progress = ++completedCount / (float)totalCount;
                        bool cancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar("AssetReplace", assetPath, progress);
                        if (cancel) break;

                        var scene = (currentScenePath == assetPath)? currentScene : EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                        if (scene.IsValid())
                        {
                            bool hasMissingReference = false;
                            // 遍歷場景中的所有 GameObject
                            var rootGameObjects = scene.GetRootGameObjects();
                            var missingReferences = new List<MissingReferenceInfo>();
                            foreach (var root in rootGameObjects)
                            {
                                if (CheckGameObjectMissingReference(root, missingReferences, ""))//scene.name
                                {
                                    hasMissingReference = true;
                                    //Debug.LogError($"hasMissingReference: {hasMissingReference}");
                                }
                            }
                            if (hasMissingReference)//此場景有Missing Reference
                            {
                                MissingReferenceScene data = new();
                                data.scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath);
                                data.missingReferences = missingReferences;
                                missingAssetSceneList.Add(data);
                            }
                        }
                        else
                        {
                            Debug.LogError($"Failed to open scene: {assetPath}");
                        }
                        if (currentScenePath != assetPath)//排除目前開啟的場景
                        {
                            EditorSceneManager.CloseScene(scene, true);
                        }
                            
                        await UniTask.Yield();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                    finally
                    {
                        //EditorSceneManager.CloseScene(scene, true);
                    }

                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                //EditorSceneManager.OpenScene(currentScenePath);
                UnityEditor.EditorUtility.ClearProgressBar();
            }
            return missingAssetSceneList;
        }
        private void WindowOnGUI()
        {
            if (GUILayout.Button("Find Missing reference", UCL_GUIStyle.ButtonStyle))
            {
                async UniTask Check()
                {
                    m_MissingAssetSceneList = await CheckMissingReference();
                }
                Check().Forget();
            }

            if (!m_MissingAssetSceneList.IsNullOrEmpty())//顯示找到Missing Reference的場景
            {
                if (GUILayout.Button("Clear All Missing Reference", UCL_GUIStyle.ButtonStyle))
                {
                    //m_MissingAssetSceneList.Clear();
                    for (int i = 0; i < m_MissingAssetSceneList.Count; i++)
                    {
                        // 保存當前開啟的場景路徑
                        string currentScenePath = EditorSceneManager.GetActiveScene().path;
                        ClearMissingReference(m_MissingAssetSceneList[i].scene, m_ClearConfig, restoreCurrentScene: false);//restore after clear all!!
                        EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                    }
                }
                UCL_GUILayout.DrawObjectData(m_ClearConfig, m_Dic.GetSubDic(nameof(m_ClearConfig)), "Clear Config");
                using (var scope = new GUILayout.ScrollViewScope(scrollPosition2))
                {
                    scrollPosition2 = scope.scrollPosition;

                    

                    for (int i = 0; i < m_MissingAssetSceneList.Count; i++)
                    {
                        var target = m_MissingAssetSceneList[i];
                        var scene = target.scene;
                        GUILayout.BeginHorizontal();
                        target.showDetail = UCL_GUILayout.Toggle(target.showDetail);
                        using (new GUILayout.VerticalScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button($"Clear Missing Reference({target.missingReferences.Count})", UCL_GUIStyle.ButtonStyle, GUILayout.ExpandWidth(false)))//清除目標場景的Missing Reference
                                {
                                    ClearMissingReference(scene, m_ClearConfig);
                                }
                                m_MissingAssetSceneList[i].scene = EditorGUILayout.ObjectField(scene, scene.GetType(), true) as SceneAsset;
                            }
                            if (target.showDetail)
                            {
                                foreach (var missingReference in target.missingReferences)
                                {
                                    GUILayout.Label(missingReference.path, UCL_GUIStyle.LabelStyle);
                                    //string path = GetGameObjectPath(obj);
                                    //EditorGUILayout.ObjectField(obj, obj.GetType(), true);
                                }
                            }
                        }


                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

    }
}
