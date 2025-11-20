using System;
using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// Helper for drag and drop.
    /// 这是一个提供特殊拖放功能的功能类。与GObject.draggable不同，拖动开始后，他使用一个替代的图标作为拖动对象。
    /// 当玩家释放鼠标/手指，目标组件会发出一个onDrop事件。
    /// </summary>
    public class DragDropManager
    {
        private GComponent _agent;
        private GComponent _tempCom;
        private GLoader _tempIcon;
        private object _sourceData;
        public GObject _source;

        private static DragDropManager _inst;
        public static DragDropManager inst
        {
            get
            {
                if (_inst == null)
                    _inst = new DragDropManager();
                return _inst;
            }
        }

        public DragDropManager()
        {
            _agent = (GComponent)UIObjectFactory.NewObject(ObjectType.Component);
            _tempIcon = (GLoader)UIObjectFactory.NewObject(ObjectType.Loader);
            _agent.gameObjectName = "DragDropAgent";
            _tempIcon.name = "icon";
            _tempIcon.icon = string.Empty;
            _agent.asCom.AddChild(_tempIcon);
            _agent.SetHome(GRoot.inst);
            _agent.touchable = false;//important
            _agent.draggable = true;
            //_agent.SetSize(100, 100);
            //_agent.SetPivot(0.5f, 0.5f, true);
            _tempIcon.align = AlignType.Center;
            _tempIcon.verticalAlign = VertAlignType.Middle;
            _agent.sortingOrder = int.MaxValue;
            _agent.onDragEnd.Add(__dragEnd);
        }

        /// <summary>
        /// Loader object for real dragging.
        /// 用于实际拖动的Loader对象。你可以根据实际情况设置loader的大小，对齐等。
        /// </summary>
        public GComponent dragAgent
        {
            get { return _agent; }
        }
        
        /// <summary>
        /// Loader object for real dragging.
        /// 用于实际拖动的Loader对象。你可以根据实际情况设置loader的大小，对齐等。
        /// </summary>
        public GComponent tempCom
        {
            get { return _tempCom; }
        }
        
        /// <summary>
        /// Loader object for real dragging.
        /// 用于实际拖动的Loader对象。你可以根据实际情况设置loader的大小，对齐等。
        /// </summary>
        public GLoader tempIcon
        {
            get { return _tempIcon; }
        }

        /// <summary>
        /// Is dragging?
        /// 返回当前是否正在拖动。
        /// </summary>
        public bool dragging
        {
            get { return _agent.parent != null; }
        }

        /// <summary>
        /// StartDrag
        /// </summary>
        /// <param name="source"></param>
        /// <param name="url"></param>
        /// <param name="sourceData"></param>
        /// <param name="touchPointID"></param>
        /// <param name="action"></param>
        /// <param name="type">=0 为Icon 1 = GComponent</param>
        public void StartDrag(GObject source, string url, object sourceData, int touchPointID = -1,Action<GComponent> action = null,int type = 0)
        {
            if (_agent.parent != null)
                return;

            _sourceData = sourceData;
            _source = source;
            if (type == 0) {
                _tempIcon.icon = url;
            }else if (type == 1) {
                _tempCom = (GComponent)UIPackage.CreateObjectFromURL(url);
                _agent.size = _tempCom.size;
                _agent.AddChild(_tempCom);
                action?.Invoke(_tempCom);    
            }
            GRoot.inst.AddChild(_agent);
            _agent.xy = GRoot.inst.GlobalToLocal(source.LocalToGlobal(Vector2.zero)/*Stage.inst.GetTouchPosition(touchPointID)*/);
            _agent.StartDrag(touchPointID);
        }

        /// <summary>
        /// Cancel dragging.
        /// 取消拖动。
        /// </summary>
        public void Cancel()
        {
            if (_agent.parent != null)
            {
                _agent.StopDrag();
                GRoot.inst.RemoveChild(_agent);
                if (_tempCom != null) {
                    _agent.RemoveChild(_tempCom);
                    _tempIcon.url = string.Empty;
                    GRoot.inst.RemoveChild(_tempCom);
                }
                _sourceData = null;
            }
        }

        private void __dragEnd(EventContext evt)
        {
            if (_agent.parent == null) //cancelled
                return;
            //GameObject.Destroy(_agent.displayObject.gameObject);
            _agent.dragBounds = null;
            GRoot.inst.RemoveChild(_agent);
            if (_tempCom != null) {
                _agent.RemoveChild(_tempCom);
                _tempIcon.url = string.Empty;
                GRoot.inst.RemoveChild(_tempCom);
                _tempCom.Dispose();
                _tempCom = null;
            }
            object sourceData = _sourceData;
            GObject source = _source;
            _sourceData = null;
            _source = null;

            GObject obj = GRoot.inst.touchTarget;
            while (obj != null)
            {
                if (obj.hasEventListeners("onDrop"))
                {
                    obj.RequestFocus();
                    obj.DispatchEvent("onDrop", sourceData, source);
                    return;
                }
                obj = obj.parent;
            }
        }
    }
}