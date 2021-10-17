/*
 * @author	Wayne Su
 * @date	2019/12/14
 */

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BTools.BSelection
{
    /// <summary>
    /// 記錄已選取的物件
    /// </summary>
    public class BSelectionEditor : EditorWindow
    {
        /// <summary>
        /// 此 Editor Window 之 Reference
        /// </summary>
        private static EditorWindow window = null;

        /// <summary>
        /// 設定檔名稱
        /// </summary>
        private readonly string BSelectionDataFileName = "BSelectionData.asset";

        /// <summary>
        /// 添加物件的欄位顏色
        /// </summary>
        private readonly Color AddFieldColor = new Color(0.25f, 0f, 0f, 0.2f);

        /// <summary>
        /// 清單欄位顏色
        /// </summary>
        private readonly Color ListFieldColor = new Color(0f, 0f, 0f, 0.2f);

        private BSelectionData selectionData = null;
        private Vector2 scrollPos = Vector2.zero;

        /// <summary>
        /// 設定檔
        /// </summary>
        private BSelectionData SelectionData
        {
            get
            {
                if (selectionData == null)
                {
                    string configFilePath = GetEditorScriptFilePath() + BSelectionDataFileName;
                    selectionData = (BSelectionData)(AssetDatabase.LoadAssetAtPath(configFilePath, typeof(BSelectionData)));

                    if (selectionData == null)
                    {
                        AssetDatabase.CreateAsset(CreateInstance<BSelectionData>(), configFilePath);
                        AssetDatabase.SaveAssets();

                        selectionData = (BSelectionData)(AssetDatabase.LoadAssetAtPath(configFilePath, typeof(BSelectionData)));
                    }
                }

                return selectionData;
            }
        }

        /// <summary>
        /// Unity Editor 可呼叫此編輯器之 Method
        /// </summary>
        [MenuItem("Window/bTools/bSelection", false, 2)]
        private static void ShowWindow()
        {
            window = GetWindow(typeof(BSelectionEditor));
            window.titleContent.text = "bSelection";
        }

        /// <summary>
        /// 取得某物件的 Hierarchy 絕對路徑與 index
        /// </summary>
        /// <param name="_trans"> 要找的物件 </param>
        /// <returns> 路徑與 index </returns>
        private static (List<string>, int) GetTransformPath(Transform _trans)
        {
            if (_trans == null)
            {
                return (null, -1);
            }

            List<string> pathObjList = new List<string> { _trans.name };

            var curTrans = _trans;
            while (curTrans.parent != null)
            {
                curTrans = curTrans.parent;
                pathObjList.Add(curTrans.name);
            }

            pathObjList.Reverse();

            return (pathObjList, _trans.GetSiblingIndex());
        }

        private void OnGUI()
        {
            var objs = Selection.gameObjects;
            var objsCount = objs.Length;

            #region 添加物件

            Rect addObjFieldRect = new Rect(1f, 0f, (position.width - 2f), 20f);
            DrawBackgroundBox(addObjFieldRect, AddFieldColor);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUIUtility.labelWidth = 100f;
                EditorGUILayout.LabelField(string.Format("已選 {0} 個項目", objsCount), GUILayout.Width(152f));

                GUI.enabled = (objsCount > 0);

                if (GUILayout.Button("記錄", GUILayout.Width(50f)))
                {
                    SaveSelectionsIntoFiles(objs);
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            #endregion 添加物件

            #region 群組清單

            var dataInfoList = SelectionData.dataInfoList;

            Rect listFieldRect = new Rect(1f, 21f, (position.width - 2f), position.height);
            DrawBackgroundBox(listFieldRect, ListFieldColor);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("[No.]", GUILayout.Width(40f));
                EditorGUILayout.LabelField("[群組名稱]", GUILayout.Width(100f));
                EditorGUILayout.LabelField("", GUILayout.Width(5f));

                GUI.enabled = (dataInfoList.Count > 0);

                if (GUILayout.Button("全部清除", GUILayout.Width(104f)))
                {
                    if (EditorUtility.DisplayDialog("確認清除?", "確定清除所有紀錄?", "OK", "Cancel"))
                    {
                        dataInfoList.Clear();
                        EditorUtility.SetDirty(SelectionData);
                    }
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height - 40f));
            {
                int showIdx = 0;
                StringBuilder labelSb = new StringBuilder();
                int deleteIdx = -1;

                for (var i = 0; i < dataInfoList.Count; ++i)
                {
                    var curInfo = dataInfoList[i];

                    EditorGUILayout.BeginHorizontal();
                    {
                        labelSb.Length = 0;
                        labelSb.Append("[");
                        labelSb.Append(showIdx);
                        labelSb.Append("]");

                        EditorGUILayout.LabelField(labelSb.ToString(), GUILayout.Width(40f));

                        EditorGUI.BeginChangeCheck();
                        {
                            curInfo.name = EditorGUILayout.TextField(curInfo.name, GUILayout.Width(100f));
                            EditorGUILayout.LabelField("", GUILayout.Width(5f));
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(SelectionData);
                        }
                        else if (GUILayout.Button("選取", GUILayout.Width(50f)))
                        {
                            SelectObjects(curInfo);
                        }
                        else if (GUILayout.Button("清除", GUILayout.Width(50f)))
                        {
                            if (EditorUtility.DisplayDialog("確認清除?", "確定清除此紀錄?", "OK", "Cancel"))
                            {
                                deleteIdx = i;
                            }
                        }

                        ++showIdx;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (deleteIdx >= 0)
                {
                    dataInfoList.RemoveAt(deleteIdx);
                    EditorUtility.SetDirty(SelectionData);
                }
            }
            EditorGUILayout.EndScrollView();

            #endregion 群組清單
        }

        private string GetEditorScriptFilePath()
        {
            MonoScript ms = MonoScript.FromScriptableObject(this);
            string m_ScriptFilePath = AssetDatabase.GetAssetPath(ms);

            return m_ScriptFilePath.Split(new[] { ms.name + ".cs" }, System.StringSplitOptions.None)[0];
        }

        private void DrawBackgroundBox(Rect _rect, Color _color)
        {
            EditorGUI.HelpBox(_rect, null, MessageType.None);
            EditorGUI.DrawRect(_rect, _color);
        }

        private void SaveSelectionsIntoFiles(GameObject[] _objs)
        {
            if (_objs == null)
            {
                return;
            }

            BSelectionData.BSelectionDataInfo dataInfo = new BSelectionData.BSelectionDataInfo()
            {
                name = _objs[0].name,
                isPrefabMode = (PrefabStageUtility.GetCurrentPrefabStage() != null),
            };

            foreach (var obj in _objs)
            {
                (List<string> path, int idx) transPath = GetTransformPath(obj.transform);
                var objData = new BSelectionData.BSelectionDataInfo.ObjectData()
                {
                    pathList = transPath.path,
                    siblingIdx = transPath.idx,
                };

                dataInfo.objDataList.Add(objData);
            }

            SelectionData.dataInfoList.Add(dataInfo);

            EditorUtility.SetDirty(SelectionData);
        }

        private void SelectObjects(BSelectionData.BSelectionDataInfo _dataInfo)
        {
            if (_dataInfo == null)
            {
                Selection.activeObject = null;
                return;
            }

            var curPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            bool isCurInPrefabMode = (curPrefabStage != null);

            if (_dataInfo.isPrefabMode != isCurInPrefabMode)
            {
                string saveModeName = (_dataInfo.isPrefabMode) ? "Prefab" : "Scene";
                string curModeName = (isCurInPrefabMode) ? "Prefab" : "Scene";

                if (EditorUtility.DisplayDialog("模式錯誤", string.Format("模式不符! \n\n紀錄為 {0} 模式 \n目前在 {1} 模式", saveModeName, curModeName), "取消選取"))
                {
                    return;
                }
            }

            var rootObjs = (isCurInPrefabMode) ? (new GameObject[] { curPrefabStage.prefabContentsRoot }) : (SceneManager.GetActiveScene().GetRootGameObjects());
            List<Object> selectObjList = new List<Object>();

            foreach (var data in _dataInfo.objDataList)
            {
                Transform curTrans = null;
                foreach (var p in data.pathList)
                {
                    bool isRoot = (curTrans == null);
                    if (isRoot)
                    {
                        foreach (var o in rootObjs)
                        {
                            if (o.name == p)
                            {
                                curTrans = o.transform;
                                break;
                            }
                        }
                    }
                    else
                    {
                        curTrans = curTrans.transform.Find(p);
                    }

                    if (curTrans == null)
                    {
                        if (EditorUtility.DisplayDialog("是否繼續?", string.Format("查無 {0} 物件, 繼續選取剩下物件?", data.pathList[data.pathList.Count - 1]), "OK", "Cancel"))
                        {
                            break;
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                if (curTrans != null)
                {
                    if (curTrans.GetSiblingIndex() != data.siblingIdx)
                    {
                        var parent = curTrans.parent;
                        var childAtIdx = (parent == null) ? rootObjs[data.siblingIdx].transform : curTrans.parent.GetChild(data.siblingIdx);

                        if (childAtIdx.name == curTrans.name)
                        {
                            curTrans = childAtIdx;
                        }
                        else if (!EditorUtility.DisplayDialog("物件階層位置已變更", string.Format("{0} 已不在原本階層順序上, 目前選擇的也許不是原本記錄的物件, 是否選取該物件?", curTrans.name), "OK", "Cancel"))
                        {
                            break;
                        }
                    }

                    selectObjList.Add(curTrans.gameObject);
                }
            }

            if (selectObjList.Count > 0)
            {
                Selection.objects = selectObjList.ToArray();
            }
        }
    }
}