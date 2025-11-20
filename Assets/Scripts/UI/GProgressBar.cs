using System;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    /// GProgressBar class.
    /// </summary>
    public class GProgressBar : GComponent
    {
        double _min;
        double _max;
        double _value;
        ProgressTitleType _titleType;
        bool _reverse;

        GObject _titleObject;
        GMovieClip _aniObject;
        GObject _barObjectH;
        GObject _barObjectV;
        float _barMaxWidth;
        float _barMaxHeight;
        float _barMaxWidthDelta;
        float _barMaxHeightDelta;
        float _barStartX;
        float _barStartY;
        int _displayMinx = 0; // 原因是进度条通常有圆角，为了避免圆角被压缩不美观，这里设置一个最低值
        EventListener _onValueChanged;

        public GProgressBar()
        {
            _value = 50;
            _max = 100;
        }

        /// <summary>
        /// Dispatched when the object or its child was clicked.
        /// </summary>
        public EventListener onValueChanged
        {
            get { return _onValueChanged ?? (_onValueChanged = new EventListener(this, "onValueChanged")); }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public ProgressTitleType titleType
        {

            get
            {
                return _titleType;
            }
            set
            {
                if (_titleType != value)
                {
                    _titleType = value;
                    Update(_value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double min
        {
            get
            {
                return _min;
            }
            set
            {
                if (_min != value)
                {
                    _min = value;
                    Update(_value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double max
        {
            get
            {
                return _max;
            }
            set
            {
                if (_max != value)
                {
                    _max = value;
                    Update(_value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value != value)
                {
                    GTween.Kill(this, TweenPropType.Progress, false);

                    _value = value;
                    Update(_value);

                    // event
                    BubbleEvent("onValueChanged", null);
                }
            }
        }

        public bool reverse
        {
            get { return _reverse; }
            set { _reverse = value; }
        }

        public void SetDisplayMinSize (int x) {
            _displayMinx = x;
            if (_barObjectH != null) 
            {
                Update(_value);
            }
            if (_barObjectV != null)
            {
                Update(_value);
            }
       
        } 
        
        /// <summary>
        /// 动态改变进度值。
        /// </summary>
        public GTweener TweenValue(double value, float duration)
        {
            double oldValule;

            GTweener twener = GTween.GetTween(this, TweenPropType.Progress);
            if (twener != null)
            {
                oldValule = twener.value.d;
                twener.Kill(false);
            }
            else
                oldValule = _value;

            _value = value;
            return GTween.ToDouble(oldValule, _value, duration)
                .SetEase(EaseType.Linear)
                .SetTarget(this, TweenPropType.Progress);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        public void Update(double newValue)
        {
            float percent = Mathf.Clamp01((float)((newValue - _min) / (_max - _min)));
            if (_titleObject != null)
            {   
                switch (_titleType)
                {
                    case ProgressTitleType.Percent:
                        if (RTLSupport.BaseDirection == RTLSupport.DirectionType.RTL)
                            _titleObject.text = "%" + Mathf.FloorToInt(percent * 100);
                        else
                            _titleObject.text = Mathf.FloorToInt(percent * 100) + "%";
                        break;

                    case ProgressTitleType.ValueAndMax:
                        if (RTLSupport.BaseDirection == RTLSupport.DirectionType.RTL)
                            _titleObject.text = Math.Round(max) + "/" + Math.Round(newValue);
                        else
                            _titleObject.text = Math.Round(newValue) + "/" + Math.Round(max);
                        break;

                    case ProgressTitleType.Value:
                        _titleObject.text = "" + Math.Round(newValue);
                        break;

                    case ProgressTitleType.Max:
                        _titleObject.text = "" + Math.Round(_max);
                        break;
                }
            }
            
            if (_aniObject != null)
                _aniObject.frame = Mathf.RoundToInt(percent * 100);

            float fullWidth = this.width - _barMaxWidthDelta;
            float fullHeight = this.height - _barMaxHeightDelta;
            
            if (!_reverse)
            {
                if (_barObjectH != null)
                {
                    var percent2 = _displayMinx / fullWidth;
                    if (percent > 0 && percent < percent2) {
                        percent = percent2;
                    }
                    if (!SetFillAmount(_barObjectH, percent))
                        _barObjectH.width = Mathf.RoundToInt(fullWidth * percent);
                }
                if (_barObjectV != null)
                {
                    var percent2 = _displayMinx / fullHeight;
                    if (percent > 0 && percent < percent2) {
                        percent = percent2;
                    }
                    if (!SetFillAmount(_barObjectV, percent))
                        _barObjectV.height = Mathf.RoundToInt(fullHeight * percent);
                }
            }
            else
            {
                if (_barObjectH != null)
                {
                    var percent2 = _displayMinx / fullWidth;
                    if (percent > 0 && percent < percent2) {
                        percent = percent2;
                    }
                    if (!SetFillAmount(_barObjectH, 1 - percent))
                    {
                        _barObjectH.width = Mathf.RoundToInt(fullWidth * percent);
                        _barObjectH.x = _barStartX + (fullWidth - _barObjectH.width);
                    }
                }
                if (_barObjectV != null)
                {
                    var percent2 = _displayMinx / fullHeight;
                    if (percent > 0 && percent < percent2) {
                        percent = percent2;
                    }
                    if (!SetFillAmount(_barObjectV, 1 - percent))
                    {
                        _barObjectV.height = Mathf.RoundToInt(fullHeight * percent);
                        _barObjectV.y = _barStartY + (fullHeight - _barObjectV.height);
                    }
                }
            }

            InvalidateBatchingState(true);
        }

        public void Update (double sureValue, double backValue, params GObject[] objs) {
            float surePercent = Mathf.Clamp01((float)((sureValue - _min) / (_max - _min)));
            float backPercent = Mathf.Clamp01((float)((backValue - _min) / (_max - _min)));
            if (_titleObject != null)
            {
                switch (_titleType)
                {
                    case ProgressTitleType.Percent:
                        if (RTLSupport.BaseDirection == RTLSupport.DirectionType.RTL)
                            _titleObject.text = "%" + Mathf.FloorToInt(backPercent * 100);
                        else
                            _titleObject.text = Mathf.FloorToInt(backPercent * 100) + "%";
                        break;
                    case ProgressTitleType.ValueAndMax:
                        if (RTLSupport.BaseDirection == RTLSupport.DirectionType.RTL)
                            _titleObject.text = Math.Round(max) + "/" + Math.Round(backValue);
                        else
                            _titleObject.text = Math.Round(backValue) + "/" + Math.Round(max);
                        break;
                    case ProgressTitleType.Value:
                        _titleObject.text = "" + Math.Round(backValue);
                        break;
                    case ProgressTitleType.Max:
                        _titleObject.text = "" + Math.Round(_max);
                        break;
                }
            }
            
            float fullWidth = this.width - _barMaxWidthDelta;
            float fullHeight = this.height - _barMaxHeightDelta;
            if (!_reverse)
            {
                foreach (var gObject in objs) {
                    if (!SetFillAmount(gObject, backPercent))
                        gObject.width = Mathf.RoundToInt(fullWidth * backPercent);
                }
            }
            else
            {
                foreach (var gObject in objs) {
                    if (!SetFillAmount(gObject, 1 - backPercent))
                    {
                        gObject.width = Mathf.RoundToInt(fullWidth * backPercent);
                        gObject.x = _barStartX + (fullWidth - gObject.width);
                    }
                }
            }
            
            if (!_reverse)
            {
                if (_barObjectH != null)
                {
                    if (!SetFillAmount(_barObjectH, surePercent))
                        _barObjectH.width = Mathf.RoundToInt(fullWidth * surePercent);
                }
                if (_barObjectV != null)
                {
                    if (!SetFillAmount(_barObjectV, surePercent))
                        _barObjectV.height = Mathf.RoundToInt(fullHeight * surePercent);
                }
            }
            else
            {
                if (_barObjectH != null)
                {
                    if (!SetFillAmount(_barObjectH, 1 - surePercent))
                    {
                        _barObjectH.width = Mathf.RoundToInt(fullWidth * surePercent);
                        _barObjectH.x = _barStartX + (fullWidth - _barObjectH.width);
                    }
                }
                if (_barObjectV != null)
                {
                    if (!SetFillAmount(_barObjectV, 1 - surePercent))
                    {
                        _barObjectV.height = Mathf.RoundToInt(fullHeight * surePercent);
                        _barObjectV.y = _barStartY + (fullHeight - _barObjectV.height);
                    }
                }
            }
            if (_aniObject != null)
                _aniObject.frame = Mathf.RoundToInt(surePercent * 100);

            InvalidateBatchingState(true);
        }
        
        bool SetFillAmount(GObject bar, float amount)
        {
            if ((bar is GImage) && ((GImage)bar).fillMethod != FillMethod.None)
                ((GImage)bar).fillAmount = amount;
            else if ((bar is GLoader) && ((GLoader)bar).fillMethod != FillMethod.None)
                ((GLoader)bar).fillAmount = amount;
            else
                return false;

            return true;
        }

        override protected void ConstructExtension(ByteBuffer buffer)
        {
            buffer.Seek(0, 6);

            _titleType = (ProgressTitleType)buffer.ReadByte();
            _reverse = buffer.ReadBool();

            _titleObject = GetChild("title");
            _barObjectH = GetChild("bar");
            _barObjectV = GetChild("bar_v");
            _aniObject = GetChild("ani") as GMovieClip;

            if (_barObjectH != null)
            {
                _barMaxWidth = _barObjectH.width;
                _barMaxWidthDelta = this.width - _barMaxWidth;
                _barStartX = _barObjectH.x;
            }
            if (_barObjectV != null)
            {
                _barMaxHeight = _barObjectV.height;
                _barMaxHeightDelta = this.height - _barMaxHeight;
                _barStartY = _barObjectV.y;
            }
        }

        override public void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);

            if (!buffer.Seek(beginPos, 6))
            {
                Update(_value);
                return;
            }

            if ((ObjectType)buffer.ReadByte() != packageItem.objectType)
            {
                Update(_value);
                return;
            }

            _value = buffer.ReadInt();
            _max = buffer.ReadInt();
            if (buffer.version >= 2)
                _min = buffer.ReadInt();

            if (buffer.version >= 5)
            {
                string sound = buffer.ReadS();
                if (!string.IsNullOrEmpty(sound))
                {
                    float volumeScale = buffer.ReadFloat();
                    displayObject.onClick.Add(() =>
                    {
                        NAudioClip audioClip = UIPackage.GetItemAssetByURL(sound) as NAudioClip;
                        if (audioClip != null && audioClip.nativeClip != null)
                            Stage.inst.PlayOneShotSound(audioClip.nativeClip, volumeScale);
                    });
                }
                else
                    buffer.Skip(4);
            }
            
            Update(_value);
        }

        override protected void HandleSizeChanged()
        {
            base.HandleSizeChanged();

            if (_barObjectH != null)
                _barMaxWidth = this.width - _barMaxWidthDelta;
            if (_barObjectV != null)
                _barMaxHeight = this.height - _barMaxHeightDelta;

            if (!this.underConstruct)
                Update(_value);
        }
    }
}
