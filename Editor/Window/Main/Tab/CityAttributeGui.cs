﻿using System.Threading.Tasks;
using Newtonsoft.Json;
using PLATEAU.CityInfo;
using PLATEAU.Editor.Window.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static PLATEAU.CityInfo.CityObjectList;
using ProgressBar = PLATEAU.Util.ProgressBar;

namespace PLATEAU.Editor.Window.Main.Tab
{
    /// <summary>
    /// PLATEAU SDK ウィンドウで「属性情報」タブが選択されている時のGUIです。
    /// </summary>
    internal class CityAttributeGui : ITabContent
    {
        private static readonly string GIZMO_GAMEOBJECT_NAME = "PLATEAUCityObjectGroup_GizmoGameObject";
        private readonly EditorWindow parentEditorWindow;
        private CityObject parent;
        private CityObject child;
        private string parentJson;
        private string childJson;
        private string targetObjectName;
        private string errorMessage;
        private Vector2 scrollParent;
        private Vector2 scrollChild;
        private CityObjectGizmoDrawer gizmoDrawer;
        
        private bool isActive = false;

        public CityAttributeGui(EditorWindow parentEditorWindow)
        {
            this.parentEditorWindow = parentEditorWindow;
        }
        
        public VisualElement CreateGui()
        {
            DestroyGizmoDrawer();
            return new IMGUIContainer(Draw);
        }
        
        private void Draw()
        {
            PlateauEditorStyle.SubTitle("クリックした地物の情報を表示します。");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                using (PlateauEditorStyle.VerticalScopeLevel1())
                {
                    PlateauEditorStyle.MultiLineLabelWithBox(errorMessage);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(targetObjectName))
                {
                    PlateauEditorStyle.Heading("対象オブジェクト", null);

                    using (PlateauEditorStyle.VerticalScopeLevel1())
                    {
                        EditorGUILayout.LabelField(targetObjectName);
                    }
                }

                if (!string.IsNullOrEmpty(parentJson) || !string.IsNullOrEmpty(childJson))
                {
                    PlateauEditorStyle.Heading("属性情報", null);
                }

                if (!string.IsNullOrEmpty(parentJson) && parent != null)
                {
                    using (PlateauEditorStyle.VerticalScopeLevel1())
                    {
                        EditorGUILayout.LabelField(parent.GmlID);
                        scrollParent = EditorGUILayout.BeginScrollView(scrollParent, GUILayout.MaxHeight(400));
                        EditorGUILayout.TextArea(parentJson);
                        EditorGUILayout.EndScrollView();
                    }
                }

                if (!string.IsNullOrEmpty(childJson) && child != null)
                {
                    using (PlateauEditorStyle.VerticalScopeLevel1())
                    {
                        EditorGUILayout.LabelField(child.GmlID);
                        scrollChild = EditorGUILayout.BeginScrollView(scrollChild, GUILayout.MaxHeight(400));
                        EditorGUILayout.TextArea(childJson);
                        EditorGUILayout.EndScrollView();
                    }
                }
            }
            
            if(!isActive)
            {
                SceneView.duringSceneGui += OnSceneGUI;
                isActive = true;
            }
        }

        private async void OnSceneGUI(SceneView scene)
        {
            if (!isActive) return;
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (Physics.Raycast(ray, out var hit, 100000.0f))
                {
                    if (hit.transform.TryGetComponent<PLATEAUCityObjectGroup>(out var cog))
                    {
                        using var progressBar = new ProgressBar("クリックした地物の情報を取得中です...");
                        progressBar.Display(0f);

                        parent = child = null;
                        parentJson = childJson = targetObjectName = errorMessage = null;
                        parentEditorWindow.Repaint();

                        var selected = await cog.GetPrimaryAndAtomicCityObjectsAsync(hit);
                        if (selected != null)
                        {
                            parent = selected[0];
                            child = selected[1];
                        }
                        progressBar.Display(0.2f);

                        if (parent == null && child == null) 
                        {
                            errorMessage = $"{hit.transform.gameObject.name}:\r\n地物がクリックされましたが、属性情報が見つかりませんでした。\r\nインポート時に属性情報を含める設定になっているか確認してください。";
                        }
                        else
                        {
                            targetObjectName = hit.transform.root.name;
                            
                            //PrimaryとAtomicが同一であった場合片方だけ表示
                            if (parent?.GmlID == child?.GmlID)
                                child = null;

                            if (parent!=null)
                            {
                                parentJson = await Task.Run(() => JsonConvert.SerializeObject(parent, Formatting.Indented));
                                progressBar.Display(0.4f);

                                //最小値物
                                if (!string.IsNullOrEmpty(cog.CityObjects.outsideParent))
                                {
                                    Transform parentTrans = (hit.transform.parent.gameObject.name == cog.CityObjects.outsideParent) ? hit.transform.parent : GameObject.Find(cog.CityObjects.outsideParent)?.transform;
                                    if (parentTrans != null)
                                    {
                                        await GetGizmoDrawer().ShowParentSelection(parentTrans, parent.IndexInMesh, GetIdAndAttributeString(parent));
                                        progressBar.Display(0.6f);
                                    }
                                }
                                else
                                {
                                    await GetGizmoDrawer().ShowParentSelection(hit.transform, parent.IndexInMesh, GetIdAndAttributeString(parent));
                                    progressBar.Display(0.6f);
                                }
                            }
                            else
                            {
                                parentJson = null;
                                GetGizmoDrawer().ClearParentSelection();
                            }
                            
                            if(child != null)
                            {
                                childJson = await Task.Run(() =>  JsonConvert.SerializeObject(child, Formatting.Indented));
                                progressBar.Display(0.8f);

                                await GetGizmoDrawer().ShowChildSelection(hit.transform, child.IndexInMesh, GetIdAndAttributeString(child));
                                progressBar.Display(0.99f);
                            } 
                            else
                            {
                                childJson = null;
                                GetGizmoDrawer().ClearChildSelection();
                            }
                        }
                    }
                    else
                    {
                        errorMessage = $"{hit.transform.gameObject.name}:\r\n地物がクリックされましたが、属性情報が見つかりませんでした。\r\nインポート時に属性情報を含める設定になっているか確認してください。";
                    }
                }
                else
                {
                    Clear();
                    errorMessage = "クリック箇所のレイキャストがコライダーにヒットしませんでした。\r\nコライダーがセットされているか確認してください。";  
                }

                parentEditorWindow.Repaint();
                scene.Repaint();
            }
        }

        private string GetIdAndAttributeString(CityObject obj)
        {
            var id = obj.GmlID;

            var attr = obj.AttributesMap.DebugString(0);
            if(attr.Length > 50)
            {
                attr = attr.Substring(0, 50);
            }
            return $"{id}\n{attr}";
        }

        private void Clear()
        {
            parent = child = null;
            parentJson = childJson = targetObjectName = errorMessage = null;
            DestroyGizmoDrawer();
            isActive = false;
        }

        private CityObjectGizmoDrawer GetGizmoDrawer()
        {
            if (gizmoDrawer == null)
            {
                var gizmoDrawerObj = GameObject.Find(GIZMO_GAMEOBJECT_NAME);
                if (gizmoDrawerObj == null)
                {
                    gizmoDrawerObj = new GameObject(GIZMO_GAMEOBJECT_NAME);
                }
                gizmoDrawer = gizmoDrawerObj.GetComponent<CityObjectGizmoDrawer>();
                if (gizmoDrawer == null)
                {
                    gizmoDrawer = gizmoDrawerObj.AddComponent<CityObjectGizmoDrawer>();
                }
            }
            return gizmoDrawer;
        }

        private void DestroyGizmoDrawer()
        {
            gizmoDrawer = null;
            var gizmoDrawers = GameObject.FindObjectsOfType<CityObjectGizmoDrawer>();
            foreach (var g in gizmoDrawers)
            {
                Object.DestroyImmediate(g.gameObject);
            }
        }

        public void Dispose() 
        {
            Clear();
            SceneView.duringSceneGui -= OnSceneGUI;
            isActive = false;
        }

        public void OnTabUnselect()
        {
            Clear();
            SceneView.duringSceneGui -= OnSceneGUI;
            isActive = false;
        }
    }
}
