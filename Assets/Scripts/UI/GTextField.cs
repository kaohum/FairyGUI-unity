using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    /// 
    /// </summary>
    public class GTextField : GObject, ITextColorGear
    {
        protected TextField _textField;
        protected string _text;
        protected bool _ubbEnabled;
        protected bool _updatingSize;
        protected bool _ignoreWordSingleScale;
        protected Dictionary<string, string> _templateVars;

        public GTextField()
            : base()
        {
            TextFormat tf = _textField.textFormat;
            tf.font = UIConfig.defaultFont;
            tf.size = 12;
            tf.color = Color.black;
            tf.lineSpacing = 3;
            tf.letterSpacing = 0;
            _textField.textFormat = tf;

            _text = string.Empty;
            _textField.autoSize = AutoSizeType.Both;
            _textField.wordWrap = false;
        }

        //----------------------------------- 渐变色 begin ----------------------------------
        /// <summary>
        /// 渐变色
        /// </summary>
        public Color32[] gradientColor
        {
	        get => _textField.textFormat.gradientColor;
	        set
	        {
		        var tf = _textField.textFormat;
                
		        if (value == null)
		        {
			        if (tf.gradientColor == null)
				        return;
                    
			        tf.gradientColor = null;
		        }
		        else if (tf.gradientColor == value)
		        {
			        // do nothing
		        }
		        else if (tf.gradientColor == null)
		        {
			        tf.gradientColor = new Color32[4];
			        value.CopyTo(this.gradientColor, 0);
		        }
		        else
		        {
			        value.CopyTo(this.gradientColor, 0);
		        }
                
		        _textField.textFormat = tf;
		        UpdateGear(4);
	        }
        }
        
        /// <summary>
        /// 设置四方向渐变色
        /// </summary>
        public void UpdateGradientColor(Color32 leftTop, Color32 leftBottom, Color32 rightTop, Color32 rightBottom)
        {
	        var tf = _textField.textFormat;

	        if (tf.gradientColor == null)
	        {
		        var buffer = new Color32[4];
		        buffer[0] = leftTop;
		        buffer[1] = leftBottom;
		        buffer[2] = rightTop;
		        buffer[3] = rightBottom;
            
		        this.gradientColor = buffer;
	        }
	        else
	        {
		        var buffer = tf.gradientColor;
		        buffer[0] = leftTop;
		        buffer[1] = leftBottom;
		        buffer[2] = rightTop;
		        buffer[3] = rightBottom;
            
		        this.gradientColor = buffer;
	        }
        }
        
        /// <summary>
        /// 设置从上到下的渐变色
        /// </summary>
        public void UpdateVerticalGradientColor(Color32 top, Color32 bottom)
        {
	        UpdateGradientColor(top, bottom, top, bottom);
        }
        
        /// <summary>
        /// 设置从左到右的渐变色
        /// </summary>
        public void UpdateHorizontalGradientColor(Color32 left, Color32 right)
        {
	        UpdateGradientColor(left, left, right, right);
        }
        
        //----------------------------------- 渐变色 end ----------------------------------
        
        override protected void CreateDisplayObject()
        {
            _textField = new TextField();
            _textField.gOwner = this;
            displayObject = _textField;
        }

        /// <summary>
        /// 
        /// </summary>
        override public string text
        {
            get
            {
                if (this is GTextInput)
                    _text = ((GTextInput)this).inputTextField.text;
                return _text;
            }
            set
            {
                if (value == null)
                    value = string.Empty;

                // 开了数字格式转换
                value = _SetText(value);
                
                _text = value;
                SetTextFieldText();
                UpdateSize();
                UpdateGear(6);
            }
        }

        virtual protected void SetTextFieldText()
        {
            string str = _text;
            
            // 开启了模板
            if (_templateVars != null)
                str = ParseTemplate(str);

            _textField.maxWidth = maxWidth;
            _textField.maxHeight = maxHeight;
            if (_ubbEnabled)
                _textField.htmlText = UBBParser.inst.Parse(XMLUtils.EncodeString(str));
            else
                _textField.text = str;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> templateVars
        {
            get { return _templateVars; }
            set
            {
                if (_templateVars == null && value == null)
                    return;

                _templateVars = value;

                FlushVars();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTextField SetVar(string name, string value)
        {
            if (_templateVars == null)
                _templateVars = new Dictionary<string, string>();
            _templateVars[name] = value;

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void FlushVars()
        {
            SetTextFieldText();
            UpdateSize();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasCharacter(char ch)
        {
            return _textField.HasCharacter(ch);
        }

        protected string ParseTemplate(string template)
        {
            int pos1 = 0, pos2 = 0;
            int pos3;
            string tag;
            string value;
            StringBuilder buffer = new StringBuilder();

            while ((pos2 = template.IndexOf('{', pos1)) != -1)
            {
                if (pos2 > 0 && template[pos2 - 1] == '\\')
                {
                    buffer.Append(template, pos1, pos2 - pos1 - 1);
                    buffer.Append('{');
                    pos1 = pos2 + 1;
                    continue;
                }

                buffer.Append(template, pos1, pos2 - pos1);
                pos1 = pos2;
                pos2 = template.IndexOf('}', pos1);
                if (pos2 == -1)
                    break;

                if (pos2 == pos1 + 1)
                {
                    buffer.Append(template, pos1, 2);
                    pos1 = pos2 + 1;
                    continue;
                }

                tag = template.Substring(pos1 + 1, pos2 - pos1 - 1);
                pos3 = tag.IndexOf('=');
                if (pos3 != -1)
                {
                    if (!_templateVars.TryGetValue(tag.Substring(0, pos3), out value))
                        value = tag.Substring(pos3 + 1);
                }
                else
                {
                    if (!_templateVars.TryGetValue(tag, out value))
                        value = "";
                }
                buffer.Append(value);
                pos1 = pos2 + 1;
            }
            if (pos1 < template.Length)
                buffer.Append(template, pos1, template.Length - pos1);

            return buffer.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public TextFormat textFormat
        {
            get
            {
                return _textField.textFormat;
            }
            set
            {
                _textField.textFormat = value;
                if (!underConstruct)
                    UpdateSize();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Color color
        {
            get
            {
                return _textField.textFormat.color;
            }
            set
            {
                if (_textField.textFormat.color != value)
                {
                    TextFormat tf = _textField.textFormat;
                    tf.color = value;
                    _textField.textFormat = tf;
                    UpdateGear(4);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public AlignType align
        {
            get { return _textField.align; }
            set { _textField.align = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public VertAlignType verticalAlign
        {
            get { return _textField.verticalAlign; }
            set { _textField.verticalAlign = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool singleLine
        {
            get { return _textField.singleLine; }
            set { _textField.singleLine = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public float stroke
        {
            get { return _textField.stroke; }
            set { _textField.stroke = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Color strokeColor
        {
            get { return _textField.strokeColor; }
            set
            {
                _textField.strokeColor = value;
                UpdateGear(4);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector2 shadowOffset
        {
            get { return _textField.shadowOffset; }
            set { _textField.shadowOffset = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UBBEnabled
        {
            get { return _ubbEnabled; }
            set { _ubbEnabled = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public AutoSizeType autoSize
        {
            get { return _textField.autoSize; }
            set
            {
                _textField.autoSize = value;
                if (value == AutoSizeType.Both)
                {
                    _textField.wordWrap = false;

                    if (!underConstruct)
                        this.SetSize(_textField.textWidth, _textField.textHeight);
                }
                else
                {
                    _textField.wordWrap = true;

                    if (value == AutoSizeType.Height)
                    {
                        if (!underConstruct)
                        {
                            displayObject.width = this.width;
                            this.height = _textField.textHeight;
                        }
                    }
                    else
                        displayObject.SetSize(this.width, this.height);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public float textWidth
        {
            get { return _textField.textWidth; }
        }

        /// <summary>
        /// 
        /// </summary>
        public float textHeight
        {
            get { return _textField.textHeight; }
        }

        /// <summary>
        /// 值为true时单行单个单词不再受到缩放的影响
        /// </summary>
        public bool ignoreSingleWordScale{
            get { return _textField.ignoreLineSingleWordScale;}
            set { _textField.ignoreLineSingleWordScale = value;}
        }

        /// <summary>
        /// 值为true时单个单词部分超过宽度不再整体换行
        /// </summary>
        public bool ignoreWordLineFeed{
            get { return _textField.ignoreWordLineFeed;}
            set { _textField.ignoreWordLineFeed = value;}
        }

        protected void UpdateSize()
        {
            if (_updatingSize)
                return;

            _updatingSize = true;

            if (_textField.autoSize == AutoSizeType.Both)
            {
                this.size = displayObject.size;
                InvalidateBatchingState();
            }
            else if (_textField.autoSize == AutoSizeType.Height)
            {
                this.height = displayObject.height;
                InvalidateBatchingState();
            }

            _updatingSize = false;
        }

        override protected void HandleSizeChanged()
        {
            if (_updatingSize)
                return;

            if (underConstruct)
                displayObject.SetSize(this.width, this.height);
            else if (_textField.autoSize != AutoSizeType.Both)
            {
                if (_textField.autoSize == AutoSizeType.Height)
                {
                    displayObject.width = this.width;//先调整宽度，让文本重排
                    if (_text != string.Empty) //文本为空时，1是本来就不需要调整， 2是为了防止改掉文本为空时的默认高度，造成关联错误
                        SetSizeDirectly(this.width, displayObject.height);
                }
                else
                    displayObject.SetSize(this.width, this.height);
            }
        }

        override public void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_BeforeAdd(buffer, beginPos);

            buffer.Seek(beginPos, 5);

            TextFormat tf = _textField.textFormat;

            tf.font = buffer.ReadS();
            tf.size = buffer.ReadShort();
            tf.color = buffer.ReadColor();
            this.align = (AlignType)buffer.ReadByte();
            this.verticalAlign = (VertAlignType)buffer.ReadByte();
            tf.lineSpacing = buffer.ReadShort();
            tf.letterSpacing = buffer.ReadShort();
            _ubbEnabled = buffer.ReadBool();
            this.autoSize = (AutoSizeType)buffer.ReadByte();
            tf.underline = buffer.ReadBool();
            tf.italic = buffer.ReadBool();
            tf.bold = buffer.ReadBool();
            this.singleLine = buffer.ReadBool();
            if (buffer.ReadBool())
            {
                tf.outlineColor = buffer.ReadColor();
                tf.outline = buffer.ReadFloat();
            }

            if (buffer.ReadBool())
            {
                tf.shadowColor = buffer.ReadColor();
                float f1 = buffer.ReadFloat();
                float f2 = buffer.ReadFloat();
                tf.shadowOffset = new Vector2(f1, f2);
            }

            if (buffer.ReadBool())
                _templateVars = new Dictionary<string, string>();

            if (buffer.version >= 3)
            {
                tf.strikethrough = buffer.ReadBool();
#if FAIRYGUI_TMPRO
                tf.faceDilate = buffer.ReadFloat();
                tf.outlineSoftness = buffer.ReadFloat();
                tf.underlaySoftness = buffer.ReadFloat();
#else
                buffer.Skip(12);
#endif
            }

            _textField.textFormat = tf;

            //add for locale
            _Setup_BeforeAdd2(buffer, beginPos);
        }

        override public void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);

            buffer.Seek(beginPos, 6);

            string str = buffer.ReadS();
            if (str != null)
                this.text = str;
            
            UIUtils.SetupGradientText(this, dataJsonObject);
        }
        
        
        #region add for Locale
        //是否应用千分隔符
        public bool IsNumberSeparator;

        //是否开启数字缩写
        public bool IsShortNumber;

        public long[] shortParams;

        const string MARK_K = "K";
        const string MARK_M = "M";
        const string MARK_B = "B";
        const string FLAG_SHORTNUM = "shortNumber&";
        const string FLAG_SEPRARATOR = "separatorNumber&";
        const string REX_PARAM_SHORTNUM = @"shortNumber&\[(.+?)\]";
        static Regex regexShortNum = new Regex(REX_PARAM_SHORTNUM); 
        const string REX_SPRARATOR = @"(-?\d+)(\d\d\d)";
        static Regex regexSpraratorNum = new Regex(REX_SPRARATOR);
        
        private string _SetText(string value)
        {
            string t = value;

            if (IsShortNumber)
            {
                if (long.TryParse(value, out var longRet))
                {
                    if (shortParams != null && shortParams.Length == 6)
                    {
                        t = CoverToShortNumber(longRet, shortParams[0], shortParams[1],
                            shortParams[2], shortParams[3], shortParams[4], shortParams[5]);
                    }
                    else
                    {
                        t = CoverToShortNumber(longRet);
                    }
                }
            }

            if (IsNumberSeparator)
            {
                t = NumberToString(t);
            }
            
            return t;
        }

        private void _Setup_BeforeAdd2(ByteBuffer buffer, int beginPos)
        {
            IsNumberSeparator = false;
            IsShortNumber = false;
            string customData = data?.ToString();
            if (!string.IsNullOrEmpty(customData))
            {
                if (customData.Contains(FLAG_SHORTNUM))
                {
                    IsShortNumber = true;
                    
                    var mc = regexShortNum.Match(customData);
                    if (mc.Success)
                    {
                        string[] paramlist = mc.Groups[1].Value.Split(';');
                        if (paramlist.Length == 6)
                        {
                            long.TryParse(paramlist[0], out long beginK);
                            long.TryParse(paramlist[1], out long decimalK);
                            long.TryParse(paramlist[2], out long beginM);
                            long.TryParse(paramlist[3], out long decimalM);
                            long.TryParse(paramlist[4], out long beginB);
                            long.TryParse(paramlist[5], out long decimalB);
                            shortParams = new[] {beginK, decimalK, beginM, decimalM, beginB, decimalB};
                        }
                    }
                }

                if (customData.Contains(FLAG_SEPRARATOR))
                {
                    IsNumberSeparator = true;
                }
            }
        }

        /// <summary>
        /// 传入79963123，返回格式如"79,963,123"
        /// 传入79963.1K，返回格式如"79,963.1K"
        /// </summary>
        public string NumberToString(string number)
        {
            var mc = regexSpraratorNum.Match(number);
            while (mc.Success)
            {
                number = regexSpraratorNum.Replace(number, "$1,$2");
                mc = regexSpraratorNum.Match(number);
            }

            return number;
        }
        
        private string TransformNum(long num, double baseNum, long deciamlNum, string nail)
        {
            double temp = num / baseNum;

            double keepDeciaml = Math.Pow(10, deciamlNum);
            temp = (long) (temp * keepDeciaml) / keepDeciaml;
            return temp + nail;
        }

        public string CoverToShortNumber(long num, 
            long beginK = 1000, long decimalK = 1, 
            long beginM = 1000000, long decimalM = 1,
            long beginB = 1000000000, long decimalB = 1)
        {
            string ret;
            if (num >= beginB)
            {
                ret = TransformNum(num, Math.Pow(10, 9), decimalB, MARK_B);
            }
            else if (num >= beginM)
            {
                ret = TransformNum(num, Math.Pow(10, 6), decimalM, MARK_M);
            }
            else if (num >= beginK)
            {
                ret = TransformNum(num, Math.Pow(10, 3), decimalK, MARK_K);
            }
            else
            {
                ret = num.ToString();
            }

            return ret;
        }
        
        #endregion
    }
}
