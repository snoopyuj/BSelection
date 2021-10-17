/*
 * @author	Wayne Su
 * @date	2019/12/14
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTools.BSelection
{
    /// <summary>
    /// 已選取物件清單
    /// </summary>
    public class BSelectionData : ScriptableObject
    {
        /// <summary>
        /// 儲存的資料格式
        /// </summary>
        [Serializable]
        public class BSelectionDataInfo
        {
            /// <summary>
            /// 物件資料格式
            /// </summary>
            [Serializable]
            public class ObjectData
            {
                /// <summary>
                /// 物件階層路徑
                /// </summary>
                public List<string> pathList = new List<string>();

                /// <summary>
                /// 物件所處 index 清單
                /// </summary>
                public int siblingIdx = -1;
            }

            /// <summary>
            /// 資料名稱
            /// </summary>
            public string name = string.Empty;

            /// <summary>
            /// 是否在 Prefab Mode
            /// </summary>
            public bool isPrefabMode = false;

            /// <summary>
            /// 各物件資料
            /// </summary>
            public List<ObjectData> objDataList = new List<ObjectData>();
        }

        /// <summary>
        /// 資料清單
        /// </summary>
        public List<BSelectionDataInfo> dataInfoList = new List<BSelectionDataInfo>();
    }
}