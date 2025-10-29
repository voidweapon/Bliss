using System;
using System.Collections.Generic;
using Bliss.Core;
using Bliss.Debugger.GameDebugStats;
using Bliss.ViewModel.Debugger.GameDebugStats;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bliss.ViewModel.Debugger
{
    public class GameDebugStatesView : Singleton<GameDebugStatesView>, ISingletonInit
    {
        private bool _enableDraw = false;
        private bool _enableAutoRecordSetting = true;
        private GameDebugStateSetting _gameDebugStateSetting;
        private readonly Dictionary<(int, string), float> _highlightRecords = new Dictionary<(int, string), float>();
        private const float HighlightDuration = 1f;
        public void Init()
        {
            var gui = new GameObject("GameDebugStatesGUI");
            gui.AddComponent<GameDebugStatesGUI>();
            GameObject.DontDestroyOnLoad(gui);

            _gameDebugStateSetting =
                (GameDebugStateSetting)SimpleResourcesComponent.Instance.LoadAsset<ScriptableObject>("Assets/Res/Tools/Prefab/GameDebugStateSettings.asset");
            _gameDebugStateSetting?.Init();
            
            hightlightStyle.normal.background = Texture2D.whiteTexture;
            hightlightStyle.normal.textColor = Color.yellow;
            hightlightStyle.alignment = TextAnchor.LowerRight;

            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.fontSize = 20;
        }
        
        protected override void Destroy()
        {
            _debugStatGroupDrawStates.Clear();
        }

        
        public void Toggle()
        {
            _enableDraw =  !_enableDraw;
        }

        
        private Dictionary<int, DebugStatGroupDrawState> _debugStatGroupDrawStates = new Dictionary<int, DebugStatGroupDrawState>();
        public void Draw()
        {
            if (!_enableDraw)
            {
                return;
            }
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f));
            
            GameDebugStates.Instance.Draw(DrawPerWindow);

            void DrawPerWindow(int windowId, DebugStatGroup group)
            {
                string groupName = group.GroupName;
                GameDebugStateGroupSetting setting = null;
                _gameDebugStateSetting?.TryGetSetting(groupName, out setting);
                _debugStatGroupDrawStates.TryGetValue(windowId, out var drawState);
                
                if (drawState == null)
                {
                    drawState = new();
                    drawState.GroupRect = new Rect(Random.Range(0, Screen.width), Random.Range(0, Screen.height), 200, 200);
                    drawState.GroupFoldRect = new Rect(Random.Range(0, Screen.width), Random.Range(0, Screen.height), 200, 25);
                    if (setting != null)
                    {
                        drawState.IsFold =  setting.IsFold;
                        drawState.GroupRect = setting.GroupRect;
                    }
                    _debugStatGroupDrawStates.Add(windowId, drawState);
                }

                Rect rect = drawState.GroupRect;
                if (_enableAutoRecordSetting && _gameDebugStateSetting != null && setting == null)
                {
                    setting = new GameDebugStateGroupSetting();
                    setting.GroupName = groupName;
                    _gameDebugStateSetting.AddSetting(groupName, setting);
                }

                if (drawState.IsFold)
                {
                    rect.height = 45;
                }
                
                rect = GUILayout.Window(windowId, rect, DrawWindow, groupName,   
                    GUILayout.MinWidth(200), GUILayout.MinHeight(45), 
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                
                if (drawState.IsFold)
                {
                    drawState.GroupRect = new Rect(rect.position, drawState.GroupRect.size);
                }
                else
                {
                    drawState.GroupRect = rect;
                }
                
                if (setting != null)
                {
                    setting.GroupRect = drawState.GroupRect;
                }
            }
            
            GUI.matrix = oldMatrix;
        }
        

        GUIStyle hightlightStyle = new GUIStyle();
        GUIStyle labelStyle = new GUIStyle();
        private void DrawWindow(int windowId)
        {
            var groupName = GameDebugStates.Instance.GetGroupNameById(windowId);
            var group =  GameDebugStates.Instance.GetGroupByName(groupName);
            var drawState = _debugStatGroupDrawStates[windowId];
            
            GameDebugStateGroupSetting setting = null;
            _gameDebugStateSetting?.TryGetSetting(groupName, out setting);
            
            // GUIStyle style = GUI.skin.GetStyle("Label");
            // style.alignment = TextAnchor.MiddleLeft;
            GUIStyle style = labelStyle;
            
            GUILayout.BeginArea(new Rect(0, 0, drawState.GroupRect.width, 30));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(drawState.IsFold ? "+" : "-"))
            {
                drawState.IsFold = !drawState.IsFold;
                if (setting != null)
                {
                    setting.IsFold = drawState.IsFold;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            Color oldColor = GUI.color;
            if (!drawState.IsFold)
            {
                GUILayout.BeginVertical(style);
                group.Draw(DrawProperty);
                GUILayout.EndVertical();

                void DrawProperty(DebugStatGroup.DrawInfo propertyDrawInfo)
                {
                    var key = (windowId, propertyDrawInfo.PropertyName);

                    if (propertyDrawInfo.HighlightWhenChange && propertyDrawInfo.HasChange)
                    {
                        _highlightRecords[key] = Time.unscaledTime;
                    }

                    var highlightColor = Color.yellow;
                    bool inHighlight = false;
                    if (_highlightRecords.TryGetValue(key, out var highlightTime))
                    {
                        var dt = Time.unscaledTime - highlightTime;
                        inHighlight = true;
                        if (dt < HighlightDuration)
                        {
                            highlightColor.a = Mathf.Lerp(1f, 0, dt / HighlightDuration);
                        }
                        else
                        {
                            highlightColor.a = 0f;
                            _highlightRecords.Remove(key);
                        }
                    }

                    var oldBgColor = GUI.backgroundColor;
                    GUILayout.BeginHorizontal();
  
                    GUI.color = ColorConvert.Convert(propertyDrawInfo.Color);
                    GUILayout.Label($"{propertyDrawInfo.PropertyName}: {propertyDrawInfo.ValueStr}", labelStyle, GUILayout.ExpandHeight(false));
                    
                    GUI.color = Color.clear;
                    GUI.backgroundColor = Color.clear;
                    
                    if (inHighlight)
                    {
                        GUI.color = highlightColor;
                        GUI.backgroundColor = highlightColor;
                    }
                    
                    GUILayout.Box("", hightlightStyle, GUILayout.ExpandHeight(false));
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = oldBgColor;
                }
            }
            
            if(drawState.IsFold)
            {
                GUILayout.Space(1);
            }
            
            GUI.DragWindow(new Rect(0, 0, 10000, 100));

            GUI.color = oldColor;
        }
        
        private class DebugStatGroupDrawState
        {
            public Rect GroupRect;
            public Rect GroupFoldRect;
            public bool IsFold;
        }
    }
}