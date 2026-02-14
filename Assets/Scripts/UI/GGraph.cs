using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using FairyGUI.Utils;
using Framework.CAnimation.Runtime;
using Framework.Runtime;

namespace FairyGUI
{
    /// <summary>
    /// GGraph class.
    /// 对应编辑器里的图形对象。图形有两个用途，一是用来显示简单的图形，例如矩形等；二是作为一个占位的用途，
    /// 可以将本对象替换为其他对象，或者在它的前后添加其他对象，相当于一个位置和深度的占位；还可以直接将内容设置
    /// 为原生对象。
    /// </summary>
    public class GGraph : GObject, IColorGear
    {
        Shape _shape;

        public GGraph()
        {
        }

        override protected void CreateDisplayObject()
        {
            _shape = new Shape();
            _shape.gOwner = this;
            displayObject = _shape;
        }

        /// <summary>
        /// Replace this object to another object in the display list.
        /// 在显示列表中，将指定对象取代这个图形对象。这个图形对象相当于一个占位的用途。
        /// </summary>
        /// <param name="target">Target object.</param>
        public void ReplaceMe(GObject target)
        {
            if (parent == null)
                throw new Exception("parent not set");

            target.name = this.name;
            target.alpha = this.alpha;
            target.rotation = this.rotation;
            target.visible = this.visible;
            target.touchable = this.touchable;
            target.grayed = this.grayed;
            target.SetXY(this.x, this.y);
            target.SetSize(this.width, this.height);

            int index = parent.GetChildIndex(this);
            parent.AddChildAt(target, index);
            target.relations.CopyFrom(this.relations);

            parent.RemoveChild(this, true);
        }

        /// <summary>
        /// Add another object before this object.
        /// 在显示列表中，将另一个对象插入到这个对象的前面。
        /// </summary>
        /// <param name="target">Target object.</param>
        public void AddBeforeMe(GObject target)
        {
            if (parent == null)
                throw new Exception("parent not set");

            int index = parent.GetChildIndex(this);
            parent.AddChildAt(target, index);
        }

        /// <summary>
        /// Add another object after this object.
        /// 在显示列表中，将另一个对象插入到这个对象的后面。
        /// </summary>
        /// <param name="target">Target object.</param>
        public void AddAfterMe(GObject target)
        {
            if (parent == null)
                throw new Exception("parent not set");

            int index = parent.GetChildIndex(this);
            index++;
            parent.AddChildAt(target, index);
        }

        /// <summary>
        /// 设置内容为一个原生对象。这个图形对象相当于一个占位的用途。
        /// </summary>
        /// <param name="obj">原生对象</param>
        public void SetNativeObject(DisplayObject obj)
        {
            if (displayObject == obj)
                return;

            if (_shape != null)
            {
                if (_shape.parent != null)
                    _shape.parent.RemoveChild(displayObject, true);
                else
                    _shape.Dispose();
                _shape.gOwner = null;
                _shape = null;
            }

            displayObject = obj;

            if (displayObject != null)
            {
                displayObject.alpha = this.alpha;
                displayObject.rotation = this.rotation;
                displayObject.visible = this.visible;
                displayObject.scale = this.scale;
                displayObject.touchable = this.touchable;
                displayObject.gOwner = this;
            }

            if (parent != null)
                parent.ChildStateChanged(this);
            HandlePositionChanged();
        }

        /// <summary>
        /// 
        /// </summary>
        public Color color
        {
            get
            {
                if (_shape != null)
                    return _shape.color;
                else
                    return Color.clear;
            }
            set
            {
                if (_shape != null && _shape.color != value)
                {
                    _shape.color = value;
                    UpdateGear(4);
                }
            }
        }

        /// <summary>
        /// Get the shape object. It can be used for drawing.
        /// 获取图形的原生对象，可用于绘制图形。
        /// </summary>
        public Shape shape
        {
            get { return _shape; }
        }

        /// <summary>
        /// Draw a rectangle.
        /// 画矩形。
        /// </summary>
        /// <param name="aWidth">Width</param>
        /// <param name="aHeight">Height</param>
        /// <param name="lineSize">Line size</param>
        /// <param name="lineColor">Line color</param>
        /// <param name="fillColor">Fill color</param>
        public void DrawRect(float aWidth, float aHeight, int lineSize, Color lineColor, Color fillColor)
        {
            this.SetSize(aWidth, aHeight);
            _shape.DrawRect(lineSize, lineColor, fillColor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aWidth"></param>
        /// <param name="aHeight"></param>
        /// <param name="fillColor"></param>
        /// <param name="corner"></param>
        public void DrawRoundRect(float aWidth, float aHeight, Color fillColor, float[] corner)
        {
            this.SetSize(aWidth, aHeight);
            this.shape.DrawRoundRect(0, Color.white, fillColor, corner[0], corner[1], corner[2], corner[3]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aWidth"></param>
        /// <param name="aHeight"></param>
        /// <param name="fillColor"></param>
        public void DrawEllipse(float aWidth, float aHeight, Color fillColor)
        {
            this.SetSize(aWidth, aHeight);
            _shape.DrawEllipse(fillColor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aWidth"></param>
        /// <param name="aHeight"></param>
        /// <param name="points"></param>
        /// <param name="fillColor"></param>
        public void DrawPolygon(float aWidth, float aHeight, IList<Vector2> points, Color fillColor)
        {
            this.SetSize(aWidth, aHeight);
            _shape.DrawPolygon(points, fillColor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aWidth"></param>
        /// <param name="aHeight"></param>
        /// <param name="points"></param>
        /// <param name="fillColor"></param>
        /// <param name="lineSize"></param>
        /// <param name="lineColor"></param>
        public void DrawPolygon(float aWidth, float aHeight, IList<Vector2> points, Color fillColor, float lineSize,
            Color lineColor)
        {
            this.SetSize(aWidth, aHeight);
            _shape.DrawPolygon(points, fillColor, lineSize, lineColor);
        }

        override public void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_BeforeAdd(buffer, beginPos);

            buffer.Seek(beginPos, 5);

            int type = buffer.ReadByte();
            if (type != 0)
            {
                int lineSize = buffer.ReadInt();
                Color lineColor = buffer.ReadColor();
                Color fillColor = buffer.ReadColor();
                bool roundedRect = buffer.ReadBool();
                Vector4 cornerRadius = new Vector4();
                if (roundedRect)
                {
                    for (int i = 0; i < 4; i++)
                        cornerRadius[i] = buffer.ReadFloat();
                }

                if (type == 1)
                {
                    if (roundedRect)
                        _shape.DrawRoundRect(lineSize, lineColor, fillColor, cornerRadius.x, cornerRadius.y,
                            cornerRadius.z, cornerRadius.w);
                    else
                        _shape.DrawRect(lineSize, lineColor, fillColor);
                }
                else if (type == 2)
                    _shape.DrawEllipse(lineSize, fillColor, lineColor, fillColor, 0, 360);
                else if (type == 3)
                {
                    int cnt = buffer.ReadShort() / 2;
                    Vector2[] points = new Vector2[cnt];
                    for (int i = 0; i < cnt; i++)
                        points[i].Set(buffer.ReadFloat(), buffer.ReadFloat());

                    _shape.DrawPolygon(points, fillColor, lineSize, lineColor);
                }
                else if (type == 4)
                {
                    int sides = buffer.ReadShort();
                    float startAngle = buffer.ReadFloat();
                    int cnt = buffer.ReadShort();
                    float[] distances = null;
                    if (cnt > 0)
                    {
                        distances = new float[cnt];
                        for (int i = 0; i < cnt; i++)
                            distances[i] = buffer.ReadFloat();
                    }

                    _shape.DrawRegularPolygon(sides, lineSize, null, lineColor, fillColor, startAngle, distances);
                }
            }
        }

        #region 2022-1-4 15:14:06 chenhr 针对特效挂载的拓展

        public static System.Func<int, int, ResourceHandle> GameObjectCreateAction;
        public static System.Action<ResourceHandle> GameObjectDestroyAction;
        public static System.Action<ResourceHandle> GameObjectDestroyDelayAction;

        /// <summary>
        /// 特效播放完成（非loop特效不会有回调，当特效节点被Disable后，也不会调用这个回调）
        /// </summary>
        public System.Action<GGraph> VFXPlayCompleteAction;

        /// <summary>
        /// 特效加载完成（非loop特效不会有回调，当特效节点被Disable后，也不会调用这个回调）
        /// </summary>
        public System.Action<ResourceHandle> VFXLoadCompleteAction
        {
            get => mVFXLoadCompleteAction;
            set
            {
                if (m_handle != null && m_handle.IsComplete)
                {
                    value?.Invoke(m_handle);
                    return;
                }

                mVFXLoadCompleteAction = value;
            }
        }

        /// <summary>
        /// 特效播放完成并且消散完成（非loop特效不会有回调，当特效节点被Disable后，不会调用这个回调）
        /// </summary>
        public System.Action<GGraph> VFXDissipateCompleteAction;


        private System.Action<ResourceHandle> mVFXLoadCompleteAction;
        private GoWrapper m_effect;
        private ResourceHandle m_handle;
        private bool m_clone_mat;
        private bool m_destroy_ing;
        private bool m_destroy_handle;
        private int m_play_index;
        private float m_play_speed;

        public ResourceHandle VFXHandle => m_handle;

        public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);
            PlayVFX();
        }

        public void PlayVFX()
        {
            if (this.data != null && this.data is string)
            {
                var value = this.data as string;
                if (!string.IsNullOrEmpty(value))
                {
                    var index = value.IndexOf("vfx_id:");
                    if (index >= 0)
                    {
                        var str = value.Substring(index + 7);
                        var i_2 = str.Split(',');
                        bool cloneMat = false;
                        if (i_2.Length > 1)
                        {
                            var c = i_2[1];
                            if (int.TryParse(c, out var va))
                            {
                                cloneMat = va == 1;
                            }
                        }

                        var vfx_id_s = i_2[0];
                        if (int.TryParse(vfx_id_s, out var vfx_id))
                        {
                            PlayVFX(vfx_id, false);
                        }
                    }
                }
            }
        }

        public override void Dispose()
        {
            OnDestroyVfx();
            base.Dispose();
        }

        /// <summary>
        /// 清除特效
        /// </summary>
        public void DestroyVFX()
        {
            OnDestroyVfx();
        }

        /// <summary>
        /// 播放指定动作的特效
        /// </summary>
        public void PlayVFX(int vfx_id, int index /*Table.ModelAnimType*/, float speed, bool copy_mat)
        {
            m_play_index = index;
            m_play_speed = speed;
            m_clone_mat = copy_mat;
            if (GameObjectCreateAction != null)
            {
                if (VFXShowing())
                {
                    if (m_handle.Params == vfx_id)
                    {
                        if (m_handle.IsComplete)
                        {
                            var animation = m_handle.gameObject.GetComponent<CAnimation>();
                            if (animation != null)
                            {
                                if (animation is VFXInstance vfx)
                                {
                                    if (vfx.IsLoop)
                                    {
                                        // 循环特效不处理
                                        // 非循环特效才重新播放
                                        return;
                                    }
                                }

                                animation.Play(m_play_index, m_play_speed);
                            }
                        }

                        return;
                    }
                }

                if (m_effect != null)
                {
                    OnDestroyVfx();
                }

                //Debug.LogError($"{Time.frameCount}, PlayVFX, {vfx_id}");
                m_effect = new GoWrapper();
                SetNativeObject(m_effect);
                m_destroy_handle = true;
                m_handle = GameObjectCreateAction.Invoke(vfx_id, 0);
                m_handle.Completed += OnGameObjectLoaded;
                return;
            }

            if (VFXPlayCompleteAction != null)
            {
                var callback = VFXPlayCompleteAction;
                VFXPlayCompleteAction = null;
                callback.Invoke(this);
            }
        }

        /// <summary>
        /// 载入特效
        /// </summary>
        public void PlayVFX(int vfx_id, bool copy_mat = false)
        {
            PlayVFX(vfx_id, 4, 1, copy_mat);
        }

        /// <summary>
        /// 通过加载创建
        /// </summary>
        public void PlayVFX(ResourceHandle handle, bool copy_mat = false)
        {
            if (m_effect != null)
            {
                OnDestroyVfx();
            }

            m_effect = new GoWrapper();
            SetNativeObject(m_effect);
            m_destroy_handle = false;
            m_clone_mat = copy_mat;
            m_handle = handle;
            m_handle.Completed += OnGameObjectLoaded;
        }

        /// <summary>
        /// 特效是否显示中
        /// </summary>
        public bool VFXShowing()
        {
            return m_handle != null;
        }

        private void OnGameObjectLoaded(ResourceHandle handle)
        {
            //Debug.LogError($"{Time.frameCount}, GGrah OnGameObjectLoaded {handle}");
            m_effect.SetWrapTarget(handle.gameObject, m_clone_mat);
            if (m_effect != null)
            {
                handle.transform.localPosition = Vector3.zero;
                handle.transform.localRotation = Quaternion.identity;
                handle.transform.localScale = Vector3.one;

                // callback
                var animation = handle.gameObject.GetComponent<CAnimation>();
                if (animation != null)
                {
                    animation.RegisterFrameEventListener(OnFrameEventMessage);
                    animation.Play(m_play_index, m_play_speed);
                }
            }
            else
            {
                // 这里有一个很难查的bug
                // SetWrapTarget的时候有可能将对象设置在一个Disable的对象下面
                // 这将触发OnDestroyVfxDelay
                // 而OnDestroyVfxDelay会将m_effect及goWrap清空而导致错误
            }

            if (VFXLoadCompleteAction != null)
            {
                var callback = VFXLoadCompleteAction;
                VFXLoadCompleteAction = null;
                callback.Invoke(handle);
            }
        }

        /// <summary>
        /// 触发事件（播放完成）
        /// </summary>
        private void OnInvokeAction_PlayComplete()
        {
            if (VFXPlayCompleteAction != null)
            {
                var action = VFXPlayCompleteAction;
                VFXPlayCompleteAction = null;
                try
                {
                    action.Invoke(this);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// 触发事件（特效消散完成）
        /// </summary>
        private void OnInvokeAction_DissipateComplete()
        {
            if (VFXDissipateCompleteAction != null)
            {
                var action = VFXDissipateCompleteAction;
                VFXDissipateCompleteAction = null;
                try
                {
                    action.Invoke(this);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // 播放完成回调
        private void OnFrameEventMessage(CAnimation animation, string /*ModelAnimType*/ arg1, FrameEventType arg2)
        {
            var act = animation.CurrentAction;
            if (arg2 == FrameEventType.fire)
            {
                // 特效播放完成
                if (act != -2)
                {
                    // 表示非Disable触发的帧事件
                    OnInvokeAction_PlayComplete();
                }
            }
            else if (arg2 == FrameEventType.end)
            {
                // 特效消散完成
                if (act != -2)
                {
                    // 正常播放完成
                    OnInvokeAction_DissipateComplete();
                    // 需要自动回收
                    OnDestroyVfx();
                }
                else
                {
                    // 表示是有Disable触发的帧结算(非loop特效)
                    OnDestroyVfxDelay();
                }
            }
        }

        private void OnDestroyVfx()
        {
            if (!m_destroy_ing)
            {
                m_destroy_ing = true;
                VFXPlayCompleteAction = null;
                VFXLoadCompleteAction = null;
                SetNativeObject(null);
                if (m_effect != null)
                {
                    m_effect.wrapTarget = null;
                    DisposeEffectDelay(m_effect);
                    m_effect = null;
                    if (m_handle != null)
                    {
#if ENABLE_LOG
                        UnityEngine.Debug.Log($"{Time.frameCount}, GGraph {this.name}, 回收 {m_handle.Params}");
#endif
                        if (m_destroy_handle)
                        {
                            GameObjectDestroyAction.Invoke(m_handle);
                        }
#if ENABLE_LOG
                        else {
                            if (!m_handle.IsRelease) {
                                UnityEngine.Debug.Log($"{Time.frameCount}, GGraph 使用了 PlayVFX (LoadHandle) 的方式需要手动回收LoadHandle对象");
                            }
                        }
#endif
                        m_handle.Completed -= OnGameObjectLoaded;
                        m_handle = null;
                    }
                }

                m_destroy_ing = false;
            }
        }

        private void OnDestroyVfxDelay()
        {
            if (!m_destroy_ing)
            {
                m_destroy_ing = true;
                VFXPlayCompleteAction = null;
                VFXLoadCompleteAction = null;
                SetNativeObject(null);
                if (m_effect != null)
                {
                    DisposeEffectDelay(m_effect);
                    m_effect = null;
#if ENABLE_LOG
                    UnityEngine.Debug.Log($"{Time.frameCount}, {this.name}, 回收特效 {m_handle.Params}");
#endif
                    if (m_destroy_handle)
                    {
                        GameObjectDestroyDelayAction.Invoke(m_handle);
                    }
#if ENABLE_LOG
                    else {
                        if (!m_handle.IsRelease) {
                            UnityEngine.Debug.Log($"{Time.frameCount}, GGraph 使用了 PlayVFX (LoadHandle) 的方式需要手动回收LoadHandle对象");
                        }
                    }
#endif
                    m_handle.Completed -= OnGameObjectLoaded;
                    m_handle = null;
                }

                m_destroy_ing = false;
            }
        }

        private async UniTask DisposeEffectDelay(GoWrapper effect)
        {
            effect.visible = false;
            await UniTask.DelayFrame(1);
            effect.ClearWrapTarget();
            effect.visible = true;
            effect.Dispose();
        }

        #endregion
    }
}