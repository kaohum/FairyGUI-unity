using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    /// 
    /// </summary>
    public class TextField : DisplayObject, IMeshFactory
    {
        VertAlignType _verticalAlign;
        TextFormat _textFormat;
        bool _input;
        string _text;
        AutoSizeType _autoSize;
        bool _wordWrap;
        bool _singleLine;
        bool _html;
        RTLSupport.DirectionType _textDirection;
        int _maxWidth;
        int _maxHeight;

        List<HtmlElement> _elements;
        List<LineInfo> _lines;
        List<CharPosition> _charPositions;

        BaseFont _font;
        float _textWidth;
        float _textHeight;
        bool _textChanged;
        float _yOffset;
        float _fontSizeScale;
        float _renderScale;
        int _fontVersion;
        string _parsedText;
        /// <summary>
        /// 是否一行只有一个单词
        /// </summary>
        bool _lineSingleWord;
        /// <summary>
        /// 忽略单行单个单词缩放
        /// </summary>
        bool _ignoreLineSingleWordScale;
        /// <summary>
        /// 忽略一个单词换行，但是符号不受控制
        /// </summary>
        bool _ignoreWordLineFeed;

        RichTextField _richTextField;

        const int GUTTER_X = 2;
        const int GUTTER_Y = 2;
        const float IMAGE_BASELINE = 0.8f;
        static float[] STROKE_OFFSET = new float[]
        {
             -1, 0, 1, 0,
            0, -1, 0, 1,
            -1, -1, 1, -1,
            -1, 1, 1, 1
        };
        List<LineCharInfo> sLineChars = null;

        public TextField ()
        {
            _flags |= Flags.TouchDisabled;

            _textFormat = new TextFormat();
            _fontSizeScale = 1;
            _renderScale = UIContentScaler.scaleFactor;

            _wordWrap = false;
            _text = string.Empty;
            _parsedText = string.Empty;

            _elements = new List<HtmlElement>(0);
            _lines = new List<LineInfo>(1);

            CreateGameObject<TextField>("TextField");
            graphics = new NGraphics(gameObject);
            
            // 这里的修改是因为:
            // 为了能让image和text通过urp batch节省drawcall
            // 强行修改了img和text使用的shader，合并为一个shader
            // fgui有material的缓存机制，缓存的key是根据keyword做的
            // shader里面是通过if else条件分支执行，并没有做变体
            // 这里假定了一个keyword来做一个唯一key
            // 2023-12-25 16:46:10 by chenhr
            graphics.ApplyTextMode = true;
            graphics.meshFactory = this;
        }

        protected override void OnDestroyGameObject ()
        {
            if (sLineChars != null) FairyGUIListPool<LineCharInfo>.Recycle(ref sLineChars);
            DestroyGameObject<TextField>();
        }

        internal void EnableRichSupport (RichTextField richTextField)
        {
            _richTextField = richTextField;
            if (richTextField is InputTextField)
            {
                _input = true;
                EnableCharPositionSupport();
            }
        }

        public void EnableCharPositionSupport ()
        {
            if (_charPositions == null)
            {
                _charPositions = new List<CharPosition>();
                _textChanged = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TextFormat textFormat
        {
            get { return _textFormat; }
            set
            {
                _textFormat = value;
                ApplyFormat();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ApplyFormat ()
        {
            string fontName = _textFormat.font;
            if (string.IsNullOrEmpty(fontName))
                fontName = UIConfig.defaultFont;
            BaseFont newFont = FontManager.GetFont(fontName);
            if (_font != newFont)
            {
                _font = newFont;
                _fontVersion = _font.version;
                graphics.SetShaderAndTexture(_font.shader, _font.mainTexture);
                
                if (_font is BitmapFont)
                {
	                // 如果是美术字
	                if (graphics.ApplyTextMode)
	                {
		                graphics.ApplyTextMode = false; // 恢复image渲染方式
	                }
                }
            }

            if (!string.IsNullOrEmpty(_text))
                _textChanged = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public AlignType align
        {
            get { return _textFormat.align; }
            set
            {
                if (_textFormat.align != value)
                {
                    _textFormat.align = value;
                    if (!string.IsNullOrEmpty(_text))
                        _textChanged = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public VertAlignType verticalAlign
        {
            get
            {
                return _verticalAlign;
            }
            set
            {
                if (_verticalAlign != value)
                {
                    _verticalAlign = value;
                    if (!_textChanged)
                        ApplyVertAlign();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string text
        {
            get { return _text; }
            set
            {
                if (_text == value && !_html)
                    return;

                _text = value;
                _textChanged = true; // 文字大小是懒重算
                _html = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string htmlText
        {
            get { return _text; }
            set
            {
                if (_text == value && _html)
                    return;

                _text = value;
                _textChanged = true;
                _html = true;
            }
        }

        public string parsedText
        {
            get { return _parsedText; }
        }

        /// <summary>
        /// 
        /// </summary>
        public AutoSizeType autoSize
        {
            get { return _autoSize; }
            set
            {
                if (_autoSize != value)
                {
                    _autoSize = value;
                    _textChanged = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool wordWrap
        {
            get { return _wordWrap; }
            set
            {
                if (_wordWrap != value)
                {
                    _wordWrap = value;
                    _textChanged = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool singleLine
        {
            get { return _singleLine; }
            set
            {
                if (_singleLine != value)
                {
                    _singleLine = value;
                    _textChanged = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public float stroke
        {
            get
            {
                return _textFormat.outline;
            }
            set
            {
                if (_textFormat.outline != value)
                {
                    _textFormat.outline = value;
                    graphics.SetMeshDirty();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Color strokeColor
        {
            get
            {
                return _textFormat.outlineColor;
            }
            set
            {
                if (_textFormat.outlineColor != value)
                {
                    _textFormat.outlineColor = value;
                    graphics.SetMeshDirty();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Vector2 shadowOffset
        {
            get
            {
                return _textFormat.shadowOffset;
            }
            set
            {
                _textFormat.shadowOffset = value;
                graphics.SetMeshDirty();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public float textWidth
        {
            get
            {
                if (_textChanged)
                    BuildLines();

                return _textWidth;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public float textHeight
        {
            get
            {
                if (_textChanged)
                    BuildLines();

                return _textHeight;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int maxWidth
        {
            get { return _maxWidth; }
            set
            {
                if (_maxWidth != value)
                {
                    _maxWidth = value;
                    _textChanged = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int maxHeight
        {
            get { return _maxHeight; }
            set
            {
                if (_maxHeight != value)
                {
                    _maxHeight = value;
                    _textChanged = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<HtmlElement> htmlElements
        {
            get
            {
                if (_textChanged)
                    BuildLines();

                return _elements;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<LineInfo> lines
        {
            get
            {
                if (_textChanged)
                    BuildLines();

                return _lines;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<CharPosition> charPositions
        {
            get
            {
                if (_textChanged)
                    BuildLines();

                graphics.UpdateMesh();

                return _charPositions;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public RichTextField richTextField
        {
            get { return _richTextField; }
        }

        /// <summary>
        /// 忽略单行单个单词缩放
        /// </summary>
        public bool ignoreLineSingleWordScale
        {
            get
            {
                return _ignoreLineSingleWordScale;
            }
            set
            {
                if (_ignoreLineSingleWordScale != value)
                {
                    _textChanged = true; // 文字大小是懒重算
                    _ignoreLineSingleWordScale = value;
                }

            }
        }

        /// <summary>
        /// 忽略单词换行，符号不受影响
        /// </summary>
        public bool ignoreWordLineFeed
        {
            get
            {
                return _ignoreWordLineFeed;
            }
            set
            {
                if (_ignoreWordLineFeed != value)
                {
                    _textChanged = true;
                    _ignoreWordLineFeed = value;
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Redraw ()
        {
            if (_font == null)
            {
                _font = FontManager.GetFont(UIConfig.defaultFont);
                graphics.SetShaderAndTexture(_font.shader, _font.mainTexture);
                _fontVersion = _font.version;
                _textChanged = true;
            }

            if (_font is BitmapFont)
            {
	            // 如果是美术字
	            if (graphics.ApplyTextMode)
	            {
		            graphics.ApplyTextMode = false; // 恢复image渲染方式
	            }
            }
            
            if (_font.keepCrisp && _renderScale != UIContentScaler.scaleFactor)
                _textChanged = true;

            if (_font.version != _fontVersion)
            {
                _fontVersion = _font.version;
                if (_font.mainTexture != graphics.texture)
                {
                    graphics.SetShaderAndTexture(_font.shader, _font.mainTexture);
                    InvalidateBatchingState();
                }

                _textChanged = true;
            }

            if (_textChanged)
                BuildLines();

            return graphics.UpdateMesh();
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasCharacter (char ch)
        {
            return _font.HasCharacter(ch);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="startCharX"></param>
        /// <param name="endLine"></param>
        /// <param name="endCharX"></param>
        /// <param name="clipped"></param>
        /// <param name="resultRects"></param>
        public void GetLinesShape (int startLine, float startCharX, int endLine, float endCharX,
            bool clipped,
            List<Rect> resultRects)
        {
            LineInfo line1 = _lines[startLine];
            LineInfo line2 = _lines[endLine];
            bool leftAlign = _textFormat.align == AlignType.Left;
            if (startLine == endLine)
            {
                Rect r = Rect.MinMaxRect(startCharX, line1.y, endCharX, line1.y + line1.height);
                if (clipped)
                    resultRects.Add(ToolSet.Intersection(ref r, ref _contentRect));
                else
                    resultRects.Add(r);
            }
            else if (startLine == endLine - 1)
            {
                Rect r = Rect.MinMaxRect(startCharX, line1.y, leftAlign ? (GUTTER_X + line1.width) : _contentRect.xMax, line1.y + line1.height);
                if (clipped)
                    resultRects.Add(ToolSet.Intersection(ref r, ref _contentRect));
                else
                    resultRects.Add(r);
                r = Rect.MinMaxRect(GUTTER_X, line1.y + line1.height, endCharX, line2.y + line2.height);
                if (clipped)
                    resultRects.Add(ToolSet.Intersection(ref r, ref _contentRect));
                else
                    resultRects.Add(r);
            }
            else
            {
                Rect r = Rect.MinMaxRect(startCharX, line1.y, leftAlign ? (GUTTER_X + line1.width) : _contentRect.xMax, line1.y + line1.height);
                if (clipped)
                    resultRects.Add(ToolSet.Intersection(ref r, ref _contentRect));
                else
                    resultRects.Add(r);
                for (int i = startLine + 1; i < endLine; i++)
                {
                    LineInfo line = _lines[i];
                    r = Rect.MinMaxRect(GUTTER_X, r.yMax, leftAlign ? (GUTTER_X + line.width) : _contentRect.xMax, line.y + line.height);
                    if (clipped)
                        resultRects.Add(ToolSet.Intersection(ref r, ref _contentRect));
                    else
                        resultRects.Add(r);
                }
                r = Rect.MinMaxRect(GUTTER_X, r.yMax, endCharX, line2.y + line2.height);
                if (clipped)
                    resultRects.Add(ToolSet.Intersection(ref r, ref _contentRect));
                else
                    resultRects.Add(r);
            }
        }

        override protected void OnSizeChanged ()
        {
            if ((_flags & Flags.UpdatingSize) == 0)
            {
                if (_autoSize == AutoSizeType.Shrink || _wordWrap && (_flags & Flags.WidthChanged) != 0)
                    _textChanged = true;
                else if (_autoSize != AutoSizeType.None)
                    graphics.SetMeshDirty();

                if (_verticalAlign != VertAlignType.Top)
                    ApplyVertAlign();
            }

            base.OnSizeChanged();
        }

        public override void EnsureSizeCorrect ()
        {
            if (_textChanged && _autoSize != AutoSizeType.None)
                BuildLines();
        }

        public override void Update (UpdateContext context)
        {
            if (_richTextField == null) //如果是richTextField，会在update前主动调用了Redraw
                Redraw();

            base.Update(context);

            if (graphics.material && _font != null && _font is DynamicFont)
            {
	            graphics.material.SetFloat(ShaderConfig.ShaderID_RenderType, 1);
            }
            else
            {
	            graphics.material.SetFloat(ShaderConfig.ShaderID_RenderType, 0);
            }
		}

        /// <summary>
        /// 准备字体纹理
        /// </summary>
        void RequestText ()
        {
            if (!_html)
            {
                _font.SetFormat(_textFormat, _fontSizeScale);
                _font.PrepareCharacters(_parsedText);
            }
            else
            {
                int count = _elements.Count;
                for (int i = 0; i < count; i++)
                {
                    HtmlElement element = _elements[i];
                    if (element.type == HtmlElementType.Text)
                    {
                        _font.SetFormat(element.format, _fontSizeScale);
                        _font.PrepareCharacters(element.text);
                    }
                }
            }
        }

        void BuildLines ()
        {
            if (_font == null)
            {
                _font = FontManager.GetFont(UIConfig.defaultFont);
                _fontVersion = _font.version;
                graphics.SetShaderAndTexture(_font.shader, _font.mainTexture);
            }

            _textChanged = false;
            graphics.SetMeshDirty();
            _renderScale = UIContentScaler.scaleFactor;
            _fontSizeScale = 1;

            Cleanup();

            if (_text.Length == 0)
            {
                LineInfo emptyLine = LineInfo.Borrow();
                emptyLine.width = 0;
                emptyLine.height = _font.GetLineHeight(_textFormat.size);
                emptyLine.charIndex = emptyLine.charCount = 0;
                emptyLine.y = emptyLine.y2 = GUTTER_Y;
                _lines.Add(emptyLine);

                _textWidth = _textHeight = 0;
            }
            else
            {
                ParseText();
                BuildLines2();

                bool shrink = false;
                if (_autoSize == AutoSizeType.Shrink)
                {
                    shrink = true;
                }

                if (_lineSingleWord && !_ignoreLineSingleWordScale)
                {
                    // shrink = true;
                    HandleWidth();
                }

                if (_autoSize != AutoSizeType.None && _autoSize != AutoSizeType.Shrink)
                {
                    /* 遇到最大宽高，需要自动缩小
	                 * 1、如果有最大宽度没有最大高度，文字达到最大宽度的时候会自动换行，由于高度没有做限制，所以不用做任何变化
	                 * 2、有最大高度没有最大宽度，文字在非手动换行的时候会一直加宽，遇到'\n'换行达到最大高度时候会判定是否需要缩小文字
	                 * 3、没有达到最大宽度和高度，不会自动缩放
	                 *
	                 * 所以这里的逻辑是只判定高度是否达到最大高度即可
	                 */
                    if (maxHeight > 0 && _textHeight > maxHeight)
                    {
                        _contentRect.height = maxHeight; // 修正一次高
                        if (maxWidth > 0)
                            _contentRect.width = maxWidth; // 修正一次宽
                        shrink = true;
                    }
                }

                if (shrink)
                {
                    DoShrink();
                }
            }

            if (_autoSize == AutoSizeType.Both)
            {
                _flags |= Flags.UpdatingSize;
                if (_richTextField != null)
                {
                    if (_input)
                    {
                        float w = Mathf.Max(_textFormat.size, _textWidth);
                        float h = Mathf.Max(_font.GetLineHeight(_textFormat.size) + GUTTER_Y * 2, _textHeight);
                        _richTextField.SetSize(w, h);
                    }
                    else
                        _richTextField.SetSize(_textWidth, _textHeight);
                }
                else
                    SetSize(_textWidth, _textHeight);
                InvalidateBatchingState();
                _flags &= ~Flags.UpdatingSize;
            }
            else if (_autoSize == AutoSizeType.Height)
            {
                _flags |= Flags.UpdatingSize;
                if (_richTextField != null)
                {
                    if (_input)
                        _richTextField.height = Mathf.Max(_font.GetLineHeight(_textFormat.size) + GUTTER_Y * 2, _textHeight);
                    else
                        _richTextField.height = _textHeight;
                }
                else
                    this.height = _textHeight;
                InvalidateBatchingState();
                _flags &= ~Flags.UpdatingSize;
            }

            _yOffset = 0;
            ApplyVertAlign();
        }

        void ParseText ()
        {
#if RTL_TEXT_SUPPORT
            _textDirection = RTLSupport.DetectTextDirection(_text);
#endif
            if (_html)
            {
                HtmlParser.inst.Parse(_text, _textFormat, _elements,
                    _richTextField != null ? _richTextField.htmlParseOptions : null);

                _parsedText = string.Empty;
            }
            else
                _parsedText = _text;

            int elementCount = _elements.Count;
            if (elementCount == 0)
            {
                if (_textDirection != RTLSupport.DirectionType.UNKNOW)
                    _parsedText = RTLSupport.DoMapping(_parsedText);

                bool flag = _input || _richTextField != null && _richTextField.emojies != null;
                if (!flag)
                {
                    //检查文本中是否有需要转换的字符，如果没有，节省一个new StringBuilder的操作。
                    int cnt = _parsedText.Length;
                    for (int i = 0; i < cnt; i++)
                    {
                        char ch = _parsedText[i];
                        if (ch == '\r' || char.IsHighSurrogate(ch))
                        {
                            flag = true;
                            break;
                        }
                    }
                }

                if (flag)
                {
                    StringBuilder buffer = new StringBuilder();
                    ParseText(buffer, _parsedText, -1);
                    elementCount = _elements.Count;
                    _parsedText = buffer.ToString();
                }
            }
            else
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                while (i < elementCount)
                {
                    HtmlElement element = _elements[i];
                    element.charIndex = buffer.Length;
                    if (element.type == HtmlElementType.Text)
                    {
                        if (_textDirection != RTLSupport.DirectionType.UNKNOW)
                            element.text = RTLSupport.DoMapping(element.text);

                        i = ParseText(buffer, element.text, i);
                        elementCount = _elements.Count;
                    }
                    else if (element.isEntity)
                        buffer.Append(' ');
                    i++;
                }
                _parsedText = buffer.ToString();

#if RTL_TEXT_SUPPORT
                // element.text拼接完后再进行一次判断文本主语序，避免html标签存在把文本变成混合文本 [2018/12/12/ 16:47:42 by aq_1000]
                _textDirection = RTLSupport.DetectTextDirection(_parsedText);
#endif
            }
        }
        //char[] mDefaultCh = { '\0' };
        //static byte[] mUnicodes = { 0, 0, 0, 0 };
        /// <summary>
        /// 字符是否是中文
        /// </summary>
        unsafe bool IsChinese (char ch)
        {
            try
            {
                var chineseRange = UIConfig.ChineseUniCodeRange;
                if (chineseRange == null || chineseRange.Length == 0) return false;
                
                // 去GC
                int index = 0;
                //mDefaultCh[0] = ch;
                //var len = mUnicodes.Length;
                //Array.Clear(mUnicodes,  0, len);

                //fixed (char* p = mDefaultCh)
                {
	                //fixed (byte* p1 = mUnicodes)
	                {
		                //var count = Encoding.Unicode.GetBytes(&ch, 1, p1, len);
		                //var unicode = *((int*)p1);
		                var unicode = (int)ch;
		                while (index < chineseRange.Length)
		                {
			                int pre = chineseRange[index];
			                int last = chineseRange[index + 1];
			                if (unicode >= pre && unicode <= last)
			                {
				                return true;
			                }
			                index = index + 2;
		                }
		                return false;
	                }
                }
                
                /*
                byte[] unicodeBytes = Encoding.Unicode.GetBytes(mDefaultCh, 0, 1);
                mDefaultUnicode.CopyTo(mUnicodes, 0);
                unicodeBytes.CopyTo(mUnicodes, 0);
                int unicode = BitConverter.ToInt32(mUnicodes, 0);
                while (index < mChineseRange.Length)
                {
                    int pre = mChineseRange[index];
                    int last = mChineseRange[index + 1];
                    if (unicode >= pre && unicode <= last)
                    {
                        return true;
                    }
                    index = index + 2;
                }
                return false;*/
            }
            catch (Exception e)
            {
                return false;
            }

        }
        void BuildLines2 ()
        {
            _lineSingleWord = false;
            float letterSpacing = _textFormat.letterSpacing * _fontSizeScale;
            float lineSpacing = (_textFormat.lineSpacing - 1) * _fontSizeScale;
            float rectWidth = _contentRect.width - GUTTER_X * 2;
            float glyphWidth = 0, glyphHeight = 0, baseline = 0;
            short wordLen = 0;
            bool wordPossible = false;
            float posx = 0;

            TextFormat format = _textFormat;
            _font.SetFormat(format, _fontSizeScale);
            bool wrap = _wordWrap && !_singleLine;
            if (_maxWidth > 0)
            {
                wrap = true;
                rectWidth = _maxWidth - GUTTER_X * 2;
            }
            _textWidth = _textHeight = 0;

            RequestText();

            int elementCount = _elements.Count;
            int elementIndex = 0;
            HtmlElement element = null;
            if (elementCount > 0)
                element = _elements[elementIndex];
            int textLength = _parsedText.Length;

            LineInfo line = LineInfo.Borrow();
            _lines.Add(line);
            line.y = line.y2 = GUTTER_Y;
            if (sLineChars == null) sLineChars = FairyGUIListPool<LineCharInfo>.Get();
            else sLineChars.Clear();
            bool isSubFont = false;
            float curWordWidth = 0;
            float maxwordWidth = 0;
            int newWordLength = 0;

            for (int charIndex = 0; charIndex < textLength; charIndex++)
            {
                char ch = _parsedText[charIndex];

                glyphWidth = glyphHeight = baseline = 0;

                while (element != null && element.charIndex == charIndex)
                {
                    isSubFont = false;
                    if (element.type == HtmlElementType.Text && !element.format.fontChange)
                    {
                        format = element.format;
                        _font.SetFormat(format, _fontSizeScale);
                    }
                    else
                    {
                        if (element.type == HtmlElementType.Text && element.format.fontChange)
                        {
                            isSubFont = true;
                        }
                        IHtmlObject htmlObject = element.htmlObject;
                        if (_richTextField != null && htmlObject == null)
                        {
                            element.space = (int)(rectWidth - line.width - 4);
                            htmlObject = _richTextField.htmlPageContext.CreateObject(_richTextField, element);
                            element.htmlObject = htmlObject;
                        }
                        if (htmlObject != null)
                        {
                            glyphWidth = htmlObject.width + 2;
                            glyphHeight = htmlObject.height;
                            baseline = glyphHeight * IMAGE_BASELINE;
                        }

                        if (element.isEntity)
                            ch = '\0'; //indicate it is a place holder
                    }

                    elementIndex++;
                    if (elementIndex < elementCount)
                        element = _elements[elementIndex];
                    else
                        element = null;
                }

                if (ch == '\0' || ch == '\n')
                {
                    wordPossible = false;
                    newWordLength = 0;

                }
                else if (!isSubFont && _font.GetGlyph(ch == '\t' ? ' ' : ch, out glyphWidth, out glyphHeight, out baseline))
                {
                    if (ch == '\t')
                    {
                        glyphWidth *= 4;
                        newWordLength = 0;
                    }


                    if (wordPossible)
                    {
                        if (char.IsWhiteSpace(ch))
                        {
                            wordLen = 0;
                            newWordLength = 0;
                        }
                        else if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z'
                            || ch >= '0' && ch <= '9'
                            || IsPostPunctuation(ch)
                            || format.specialStyle == TextFormat.SpecialStyle.Subscript
                            || format.specialStyle == TextFormat.SpecialStyle.Superscript
                            || _textDirection != RTLSupport.DirectionType.UNKNOW && RTLSupport.IsArabicLetter(ch))
                        {
                            wordLen++;
                            newWordLength++;
                        }
                        else if (IsPrePunctuation(ch))
                        {
                            newWordLength = 0;
                            if (charIndex > 0 && IsPrePunctuation(_parsedText[charIndex - 1]))
                            {
                                wordLen++;
                            }
                            else
                            {
                                wordLen = 1;
                            }

                        }
                        else if ((!IsChinese(ch) || _ignoreWordLineFeed) && !IsSymbolChar(ch))
                        {
                            wordLen++;
                            newWordLength++;
                        }
                        else
                        {
                            wordPossible = false;
                            newWordLength = 0;
                        }
                    }
                    else if (char.IsWhiteSpace(ch))
                    {
                        wordLen = 0;
                        newWordLength = 0;
                        wordPossible = true;
                    }
                    else if (format.specialStyle == TextFormat.SpecialStyle.Subscript
                        || format.specialStyle == TextFormat.SpecialStyle.Superscript)
                    {
                        if (sLineChars.Count > 0)
                        {
                            wordLen = 2; //避免上标和下标折到下一行
                            newWordLength = 2;
                            wordPossible = true;
                        }
                    }
                    else if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z'
                          || ch >= '0' && ch <= '9'
                          || IsPrePunctuation(ch)
                          || _textDirection != RTLSupport.DirectionType.UNKNOW && RTLSupport.IsArabicLetter(ch))
                    {
                        if (IsPrePunctuation(ch))
                        {
                            newWordLength = 0;
                        }
                        else
                        {
                            newWordLength++;
                        }
                        wordLen = 1;

                        wordPossible = true;
                    }
                    else if ((!IsChinese(ch) || _ignoreWordLineFeed) && !IsSymbolChar(ch))
                    {
                        wordLen = 1;
                        newWordLength = 1;
                        wordPossible = true;
                    }
                    else
                    {
                        wordPossible = false;
                        newWordLength = 0;
                    }
                }
                else
                {
                    wordPossible = false;
                    newWordLength = 0;
                }


                sLineChars.Add(new LineCharInfo() { width = glyphWidth, height = glyphHeight, baseline = baseline, ch = ch });
                if (glyphWidth != 0)
                {
                    if (posx != 0)
                        posx += letterSpacing;
                    posx += glyphWidth;
                }

                if (ch == '\n' && !_singleLine)
                {
                    UpdateLineInfo(line, letterSpacing, sLineChars.Count);
                    LineInfo newLine = LineInfo.Borrow();
                    _lines.Add(newLine);
                    newLine.y = line.y + (line.height + lineSpacing);
                    if (newLine.y < GUTTER_Y) //lineSpacing maybe negative
                        newLine.y = GUTTER_Y;
                    newLine.y2 = newLine.y;
                    newLine.charIndex = line.charIndex + line.charCount;

                    sLineChars.Clear();
                    wordPossible = false;
                    posx = 0;
                    line = newLine;
                }
                else if (posx > rectWidth)
                {
                    if (wrap)
                    {
                        int lineCharCount = sLineChars.Count;
                        int toMoveChars;
                        if (wordPossible && wordLen > 0 && wordLen < UIConfig.WordMaxLength && lineCharCount > 2 &&
                            wordLen < lineCharCount) //if word had broken, move word to new line
                        {
                            toMoveChars = _ignoreWordLineFeed ? 1 : wordLen;
                            if (char.IsWhiteSpace(_parsedText[lineCharCount - (toMoveChars + 1)]))
                            {
                                //we caculate the line width WITHOUT the tailing space
                                UpdateLineInfo(line, letterSpacing, lineCharCount - (toMoveChars + 1));
                                line.charCount++; //but keep it in this line.
                            }
                            else if (IsPrePunctuation(_parsedText[lineCharCount - (toMoveChars + 1)]))
                            {
                                UpdateLineInfo(line, letterSpacing, lineCharCount - (toMoveChars + 1));
                                toMoveChars++;
                            }
                            else
                            {
                                UpdateLineInfo(line, letterSpacing, lineCharCount - toMoveChars);
                            }
                        }
                        else
                        {
                            if (lineCharCount > 2)
                            {
                                toMoveChars = 1;
                                int index = 0;
                                if (index < lineCharCount && IsPostPunctuation(_parsedText[charIndex - index]))
                                {
                                    toMoveChars++;
                                    index++;
                                }

                                index++;
                                if (index < lineCharCount && IsPrePunctuation(_parsedText[charIndex - index]))
                                {
                                    toMoveChars++;
                                }

                                if (toMoveChars >= lineCharCount)
                                {
                                    toMoveChars = 1; // 乱七八糟的特殊字符串，不管符号行首/尾规则了！
                                }
                            }
                            else
                            {
                                toMoveChars =
                                    lineCharCount > 1 ? 1 : 0; //if only one char here, we cant move it to new line
                            }

                            UpdateLineInfo(line, letterSpacing, lineCharCount - toMoveChars);

                        }

                        LineInfo newLine = LineInfo.Borrow();

                        _lines.Add(newLine);
                        newLine.y = line.y + (line.height + lineSpacing);
                        if (newLine.y < GUTTER_Y)
                            newLine.y = GUTTER_Y;
                        newLine.y2 = newLine.y;
                        newLine.charIndex = line.charIndex + line.charCount;
                        posx = 0;
                        if (toMoveChars != 0)
                        {
                            for (int i = line.charCount; i < lineCharCount; i++)
                            {
                                LineCharInfo ci = sLineChars[i];
                                if (posx != 0)
                                    posx += letterSpacing;
                                posx += ci.width;
                            }

                            sLineChars.RemoveRange(0, line.charCount);
                        }
                        else
                            sLineChars.Clear();

                        wordPossible = false;
                        line = newLine;
                    }
                }
                if (newWordLength > 0)
                {
                    curWordWidth = glyphWidth + curWordWidth;
                    if (curWordWidth > maxwordWidth) maxwordWidth = curWordWidth;
                }
                else
                {
                    curWordWidth = 0;
                }
            }
            UpdateLineInfo(line, letterSpacing, sLineChars.Count);
            if (!_ignoreLineSingleWordScale && _autoSize != AutoSizeType.None)
            {
                float curContentwidth = 0;
                if ((_autoSize == AutoSizeType.Both && _maxWidth != 0) || _autoSize == AutoSizeType.Height || _autoSize == AutoSizeType.Shrink)
                {
                    curContentwidth = _maxWidth == 0 ? _contentRect.width : _maxWidth;
                    curContentwidth = curContentwidth - GUTTER_X * 2;
                }
                if (curContentwidth != 0)
                {
                    if (maxwordWidth > curContentwidth)
                    {
                        if(_autoSize == AutoSizeType.Shrink && maxwordWidth + 25 < _textWidth){
                            
                        }
                        else{
                            _textWidth = maxwordWidth + 25;
                        }
                        _lineSingleWord = true;
                    }
                }


            }
            if (_textWidth > 0)
                _textWidth += GUTTER_X * 2;
            _textHeight = line.y + line.height + GUTTER_Y;
            _textWidth = Mathf.RoundToInt(_textWidth);
            _textHeight = Mathf.RoundToInt(_textHeight);
        }

        bool IsSymbolChar (char ch)
        {
            return char.IsPunctuation(ch) || char.IsSeparator(ch) || char.IsSymbol(ch);
        }

        /// <summary>
        /// 前置标点（不能出现在行尾）
        /// </summary>
        bool IsPrePunctuation (char ch)
        {
            bool exit = UIConfig.TextPrePunctuation.IndexOf(ch) >= 0
                || UIConfig.TextPrePunctuation_CN.IndexOf(ch) >= 0;
            return exit;
        }

        /// <summary>
        /// 后置标点（不能出现在行首）
        /// </summary>
        bool IsPostPunctuation (char ch)
        {
            bool exit = UIConfig.TextPostPunctuation.IndexOf(ch) >= 0
                || UIConfig.TextPostPunctuation_CN.IndexOf(ch) >= 0;
            return exit;
        }

        void UpdateLineInfo (LineInfo line, float letterSpacing, int cnt)
        {
            for (int i = 0; i < cnt; i++)
            {
                LineCharInfo ci = sLineChars[i];
                if (ci.baseline > line.baseline)
                {
                    line.height += (ci.baseline - line.baseline);
                    line.baseline = ci.baseline;
                }

                if (ci.height - ci.baseline > line.height - line.baseline)
                    line.height += (ci.height - ci.baseline - (line.height - line.baseline));

                if (ci.width > 0)
                {
                    if (line.width != 0)
                        line.width += letterSpacing;
                    line.width += ci.width;
                }
            }

            if (line.height == 0)
            {
                if (_lines.Count == 1)
                    line.height = _textFormat.size;
                else
                    line.height = _lines[_lines.Count - 2].height;
            }

            if (line.width > _textWidth)
                _textWidth = line.width;

            line.charCount = (short)cnt;
        }

        void DoShrink ()
        {
            if (_lines.Count > 1 && _textHeight > _contentRect.height)
            {
                //多行的情况，涉及到自动换行，得用二分法查找最合适的比例，会消耗多一点计算资源
                int low = 0;
                int high = _textFormat.size;

                //先尝试猜测一个比例
                _fontSizeScale = /*Mathf.Sqrt*/(_contentRect.height / _textHeight);
                int cur = Mathf.FloorToInt(_fontSizeScale * _textFormat.size);

                while (true)
                {
                    LineInfo.Return(_lines);
                    BuildLines2();
                    float width = _maxWidth != 0 && _autoSize == AutoSizeType.Shrink ? _maxWidth : _contentRect.width; //wzq:这个地方如果设置最大宽度，应该以最大宽度来计算
                    if (_textWidth > width || _textHeight > _contentRect.height)
                        high = cur;
                    else
                        low = cur;
                    if (high - low > 1 || high != low && cur == high)
                    {
                        cur = low + (high - low) / 2;
                        _fontSizeScale = (float)cur / _textFormat.size;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (_textWidth > _contentRect.width)
            {
                HandleWidth();
            }
        }

        void HandleWidth ()
        {
            _fontSizeScale = _contentRect.width / _textWidth;
            if (_lineSingleWord && _maxWidth != 0) _fontSizeScale = _maxWidth / _textWidth;

            LineInfo.Return(_lines);
            BuildLines2();

            if (_textWidth > _contentRect.width) //如果还超出，缩小一点再来一次
            {
                int size = Mathf.FloorToInt(_textFormat.size * _fontSizeScale);
                size--;
                _fontSizeScale = (float)size / _textFormat.size;

                LineInfo.Return(_lines);
                BuildLines2();
            }
            if (_lineSingleWord && !_ignoreLineSingleWordScale)
            {
                int count = 0;
                while (_textWidth > _contentRect.width && count < 5)
                {
                    int size = Mathf.FloorToInt(_textFormat.size * _fontSizeScale);
                    size--;
                    _fontSizeScale = (float)size / _textFormat.size;

                    LineInfo.Return(_lines);
                    BuildLines2();
                    count++;
                }
            }
        }

        int ParseText (StringBuilder buffer, string source, int elementIndex)
        {
            int textLength = source.Length;
            int j = 0;
            int appendPos = 0;
            bool hasEmojies = _richTextField != null && _richTextField.emojies != null;
            while (j < textLength)
            {
                char ch = source[j];
                if (ch == '\r')
                {
                    buffer.Append(source, appendPos, j - appendPos);
                    if (j != textLength - 1 && source[j + 1] == '\n')
                        j++;
                    appendPos = j + 1;
                    buffer.Append('\n');
                }
                else
                {
                    bool highSurrogate = char.IsHighSurrogate(ch);
                    if (hasEmojies)
                    {
                        uint emojiKey = 0;
                        Emoji emoji;
                        if (highSurrogate)
                            emojiKey = ((uint)source[j + 1] & 0x03FF) + ((((uint)ch & 0x03FF) + 0x40) << 10);
                        else
                            emojiKey = ch;
                        if (_richTextField.emojies.TryGetValue(emojiKey, out emoji))
                        {
                            HtmlElement imageElement = HtmlElement.GetElement(HtmlElementType.Image);
                            imageElement.Set("src", emoji.url);
                            if (emoji.width != 0)
                                imageElement.Set("width", emoji.width);
                            if (emoji.height != 0)
                                imageElement.Set("height", emoji.height);
                            if (highSurrogate)
                                imageElement.text = source.Substring(j, 2);
                            else
                                imageElement.text = source.Substring(j, 1);
                            imageElement.format.align = _textFormat.align;
                            _elements.Insert(++elementIndex, imageElement);

                            buffer.Append(source, appendPos, j - appendPos);
                            appendPos = j;
                            imageElement.charIndex = buffer.Length;
                        }
                    }

                    if (highSurrogate)
                    {
                        buffer.Append(source, appendPos, j - appendPos);
                        appendPos = j + 2;
                        j++;//跳过lowSurrogate
                        buffer.Append(' ');
                    }
                }
                j++;
            }
            if (appendPos < textLength)
                buffer.Append(source, appendPos, j - appendPos);

            return elementIndex;
        }

        public void OnPopulateMesh (VertexBuffer vb)
        {
            if (_textWidth == 0 && _lines.Count == 1)
            {
                if (_charPositions != null)
                {
                    _charPositions.Clear();
                    _charPositions.Add(new CharPosition());
                }

                if (_richTextField != null)
                    _richTextField.RefreshObjects();

                return;
            }

            float letterSpacing = _textFormat.letterSpacing * _fontSizeScale;
            TextFormat format = _textFormat;
            _font.SetFormat(format, _fontSizeScale);
            _font.UpdateGraphics(graphics);

            float rectWidth = _contentRect.width > 0 ? (_contentRect.width - GUTTER_X * 2) : 0;
            float rectHeight = _contentRect.height > 0 ? Mathf.Max(_contentRect.height, _font.GetLineHeight(format.size)) : 0;

            if (_charPositions != null)
                _charPositions.Clear();

            List<Vector3> vertList = vb.vertices;
            List<Vector2> uvList = vb.uvs;
            List<Vector2> uv2List = vb.uvs2;
            List<Color32> colList = vb.colors;

            HtmlLink currentLink = null;
            float linkStartX = 0;
            int linkStartLine = 0;

            float posx = 0;
            float indent_x;
            bool clipping = !_input && _autoSize == AutoSizeType.None;
            bool lineClipped;
            AlignType lineAlign;
            float glyphWidth, glyphHeight, baseline;
            short vertCount;
            float underlineStart;
            float strikethroughStart;
            int minFontSize;
            int maxFontSize;
            string rtlLine = null;

            int elementIndex = 0;
            int elementCount = _elements.Count;
            HtmlElement element = null;
            if (elementCount > 0)
                element = _elements[elementIndex];

            int lineCount = _lines.Count;
            bool isSubFont = false;
            for (int i = 0; i < lineCount; ++i)
            {
                LineInfo line = _lines[i];
                if (line.charCount == 0)
                    continue;

                lineClipped = clipping && i != 0 && line.y + line.height > rectHeight;
                lineAlign = format.align;
                if (element != null && element.charIndex == line.charIndex)
                    lineAlign = element.format.align;
                else
                    lineAlign = format.align;

                if (_textDirection == RTLSupport.DirectionType.RTL)
                {
                    if (lineAlign == AlignType.Center)
                        indent_x = (int)((rectWidth + line.width) / 2);
                    else if (lineAlign == AlignType.Right)
                        indent_x = rectWidth;
                    else
                        indent_x = line.width + GUTTER_X * 2;

                    if (indent_x > rectWidth)
                        indent_x = rectWidth;

                    posx = indent_x - GUTTER_X;
                }
                else
                {
                    if (lineAlign == AlignType.Center)
                        indent_x = (int)((rectWidth - line.width) / 2);
                    else if (lineAlign == AlignType.Right)
                        indent_x = rectWidth - line.width;
                    else
                        indent_x = 0;

                    if (indent_x < 0)
                        indent_x = 0;

                    posx = GUTTER_X + indent_x;
                }

                int lineCharCount = line.charCount;
                underlineStart = posx;
                strikethroughStart = posx;
                minFontSize = maxFontSize = format.size;

                if (_textDirection != RTLSupport.DirectionType.UNKNOW)
                {
                    rtlLine = _parsedText.Substring(line.charIndex, lineCharCount);
                    if (_textDirection == RTLSupport.DirectionType.RTL)
                        rtlLine = RTLSupport.ConvertLineR(rtlLine);
                    else
                        rtlLine = RTLSupport.ConvertLineL(rtlLine);
                    lineCharCount = rtlLine.Length;
                }

                for (int j = 0; j < lineCharCount; j++)
                {
                    int charIndex = line.charIndex + j;
                    char ch = rtlLine != null ? rtlLine[j] : _parsedText[charIndex];

                    while (element != null && charIndex == element.charIndex)
                    {
                        isSubFont = false;
                        if (element.type == HtmlElementType.Text && !element.format.fontChange)
                        {
                            vertCount = 0;
                            if (format.underline != element.format.underline)
                            {
                                if (format.underline)
                                {
                                    if (!lineClipped)
                                    {
                                        float lineWidth;
                                        if (_textDirection == RTLSupport.DirectionType.UNKNOW)
                                            lineWidth = (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx) - underlineStart;
                                        else
                                            lineWidth = underlineStart - (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx);
                                        if (lineWidth > 0)
                                            vertCount += (short)_font.DrawLine(underlineStart < posx ? underlineStart : posx, -(line.y + line.baseline), lineWidth,
                                                maxFontSize, 0, vertList, uvList, uv2List, colList);
                                    }
                                    maxFontSize = 0;
                                }
                                else
                                    underlineStart = posx;
                            }

                            if (format.strikethrough != element.format.strikethrough)
                            {
                                if (format.strikethrough)
                                {
                                    if (!lineClipped)
                                    {
                                        float lineWidth;
                                        if (_textDirection == RTLSupport.DirectionType.UNKNOW)
                                            lineWidth = (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx) - strikethroughStart;
                                        else
                                            lineWidth = strikethroughStart - (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx);
                                        if (lineWidth > 0)
                                            vertCount += (short)_font.DrawLine(strikethroughStart < posx ? strikethroughStart : posx, -(line.y + line.baseline), lineWidth,
                                                minFontSize, 1, vertList, uvList, uv2List, colList);
                                    }
                                    minFontSize = int.MaxValue;
                                }
                                else
                                    strikethroughStart = posx;
                            }

                            if (vertCount > 0 && _charPositions != null)
                            {
                                CharPosition cp = _charPositions[_charPositions.Count - 1];
                                cp.vertCount += vertCount;
                                _charPositions[_charPositions.Count - 1] = cp;
                            }

                            format = element.format;
                            minFontSize = Math.Min(minFontSize, format.size);
                            maxFontSize = Math.Max(maxFontSize, format.size);
                            _font.SetFormat(format, _fontSizeScale);
                        }
                        else if (element.type == HtmlElementType.Link)
                        {
                            currentLink = (HtmlLink)element.htmlObject;
                            if (currentLink != null)
                            {
                                element.position = Vector2.zero;
                                currentLink.SetPosition(0, 0);
                                linkStartX = posx;
                                linkStartLine = i;
                            }
                        }
                        else if (element.type == HtmlElementType.LinkEnd)
                        {
                            if (currentLink != null)
                            {
                                currentLink.SetArea(linkStartLine, linkStartX, i, posx);
                                currentLink = null;
                            }
                        }
                        else
                        {
                            if (element.format.fontChange && element.type == HtmlElementType.Text)
                            {
                                isSubFont = true;
                            }
                            IHtmlObject htmlObj = element.htmlObject;
                            if (htmlObj != null)
                            {
                                if (_textDirection == RTLSupport.DirectionType.RTL)
                                    posx -= htmlObj.width - 2;

                                if (_charPositions != null)
                                {
                                    CharPosition cp = new CharPosition();
                                    cp.lineIndex = (short)i;
                                    cp.charIndex = _charPositions.Count;
                                    cp.imgIndex = (short)(elementIndex + 1);
                                    cp.offsetX = posx;
                                    cp.width = (short)htmlObj.width;
                                    _charPositions.Add(cp);
                                }

                                if (lineClipped || clipping && (posx < GUTTER_X || posx > GUTTER_X && posx + htmlObj.width > _contentRect.width - GUTTER_X))
                                    element.status |= 1;
                                else
                                    element.status &= 254;

                                element.position = new Vector2(posx + 1, line.y + line.baseline - htmlObj.height * IMAGE_BASELINE);
                                int offsetx = element.GetInt("offsetx", 0);
                                int offsety = element.GetInt("offsety", 0);
                                htmlObj.SetPosition(element.position.x + offsetx, element.position.y + offsety);

                                if (_textDirection == RTLSupport.DirectionType.RTL)
                                    posx -= letterSpacing;
                                else
                                    posx += htmlObj.width + letterSpacing + 2;
                            }
                        }

                        if (element.isEntity)
                            ch = '\0';

                        elementIndex++;
                        if (elementIndex < elementCount)
                            element = _elements[elementIndex];
                        else
                            element = null;
                    }

                    if (ch == '\0')
                        continue;

                    if (!isSubFont && _font.GetGlyph(ch == '\t' ? ' ' : ch, out glyphWidth, out glyphHeight, out baseline))
                    {
                        if (ch == '\t')
                            glyphWidth *= 4;

                        if (_textDirection == RTLSupport.DirectionType.RTL)
                        {
                            if (lineClipped || clipping && (rectWidth < 7 || posx != (indent_x - GUTTER_X)) && posx < GUTTER_X - 0.5f) //超出区域，剪裁
                            {
                                posx -= (letterSpacing + glyphWidth);
                                continue;
                            }

                            posx -= glyphWidth;
                        }
                        else
                        {
                            if (lineClipped || clipping && (rectWidth < 7 || posx != (GUTTER_X + indent_x)) && posx + glyphWidth > _contentRect.width - GUTTER_X + 0.5f) //超出区域，剪裁
                            {
                                posx += letterSpacing + glyphWidth;
                                continue;
                            }
                        }

                        vertCount = (short)_font.DrawGlyph(posx, -(line.y + line.baseline), vertList, uvList, uv2List, colList);

                        if (_charPositions != null)
                        {
                            CharPosition cp = new CharPosition();
                            cp.lineIndex = (short)i;
                            cp.charIndex = _charPositions.Count;
                            cp.vertCount = vertCount;
                            cp.offsetX = posx;
                            cp.width = (short)glyphWidth;
                            _charPositions.Add(cp);
                        }

                        if (_textDirection == RTLSupport.DirectionType.RTL)
                            posx -= letterSpacing;
                        else
                            posx += letterSpacing + glyphWidth;
                    }
                    else //if GetGlyph failed
                    {
                        if (_charPositions != null)
                        {
                            CharPosition cp = new CharPosition();
                            cp.lineIndex = (short)i;
                            cp.charIndex = _charPositions.Count;
                            cp.offsetX = posx;
                            _charPositions.Add(cp);
                        }

                        if (_textDirection == RTLSupport.DirectionType.RTL)
                            posx -= letterSpacing;
                        else
                            posx += letterSpacing;
                    }
                }//text loop

                if (!lineClipped)
                {
                    vertCount = 0;
                    if (format.underline)
                    {
                        float lineWidth;
                        if (_textDirection == RTLSupport.DirectionType.UNKNOW)
                            lineWidth = (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx) - underlineStart;
                        else
                            lineWidth = underlineStart - (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx);
                        if (lineWidth > 0)
                            vertCount += (short)_font.DrawLine(underlineStart < posx ? underlineStart : posx, -(line.y + line.baseline), lineWidth,
                                maxFontSize, 0, vertList, uvList, uv2List, colList);
                    }

                    if (format.strikethrough)
                    {
                        float lineWidth;
                        if (_textDirection == RTLSupport.DirectionType.UNKNOW)
                            lineWidth = (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx) - strikethroughStart;
                        else
                            lineWidth = strikethroughStart - (clipping ? Mathf.Clamp(posx, GUTTER_X, GUTTER_X + rectWidth) : posx);
                        if (lineWidth > 0)
                            vertCount += (short)_font.DrawLine(strikethroughStart < posx ? strikethroughStart : posx, -(line.y + line.baseline), lineWidth,
                                minFontSize, 1, vertList, uvList, uv2List, colList);
                    }

                    if (vertCount > 0 && _charPositions != null)
                    {
                        CharPosition cp = _charPositions[_charPositions.Count - 1];
                        cp.vertCount += vertCount;
                        _charPositions[_charPositions.Count - 1] = cp;
                    }
                }

            }//line loop

            if (element != null && element.type == HtmlElementType.LinkEnd && currentLink != null)
                currentLink.SetArea(linkStartLine, linkStartX, lineCount - 1, posx);

            if (_charPositions != null)
            {
                CharPosition cp = new CharPosition();
                cp.lineIndex = (short)(lineCount - 1);
                cp.charIndex = _charPositions.Count;
                cp.offsetX = posx;
                _charPositions.Add(cp);
            }

            int count = vertList.Count;
            if (count > 65000)
            {
                Debug.LogWarning("Text is too large. A mesh may not have more than 65000 vertices.");
                vertList.RemoveRange(65000, count - 65000);
                colList.RemoveRange(65000, count - 65000);
                uvList.RemoveRange(65000, count - 65000);
                if (uv2List.Count > 0)
                    uv2List.RemoveRange(65000, count - 65000);
                count = 65000;
            }

            if (_font.customOutline)
            {
                bool hasShadow = _textFormat.shadowOffset.x != 0 || _textFormat.shadowOffset.y != 0;
                int allocCount = count;
                int drawDirs = 0;
                if (_textFormat.outline != 0)
                {
                    drawDirs = UIConfig.enhancedTextOutlineEffect ? 8 : 4;
                    allocCount += count * drawDirs;
                }
                if (hasShadow)
                    allocCount += count;
                if (allocCount > 65000)
                {
                    Debug.LogWarning("Text is too large. Outline/shadow effect cannot be completed.");
                    allocCount = count;
                }

                if (allocCount != count)
                {
                    VertexBuffer vb2 = VertexBuffer.Begin();
                    List<Vector3> vertList2 = vb2.vertices;
                    List<Color32> colList2 = vb2.colors;

                    Color32 col = _textFormat.outlineColor;
                    float outline = _textFormat.outline;
                    if (outline != 0)
                    {
                        for (int j = 0; j < drawDirs; j++)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                Vector3 vert = vertList[i];
                                vertList2.Add(new Vector3(vert.x + STROKE_OFFSET[j * 2] * outline, vert.y + STROKE_OFFSET[j * 2 + 1] * outline, 0));
                                colList2.Add(col);
                            }

                            vb2.uvs.AddRange(uvList);
                            if (uv2List.Count > 0)
                                vb2.uvs2.AddRange(uv2List);
                        }
                    }

                    if (hasShadow)
                    {
                        col = _textFormat.shadowColor;
                        Vector2 offset = _textFormat.shadowOffset;
                        for (int i = 0; i < count; i++)
                        {
                            Vector3 vert = vertList[i];
                            vertList2.Add(new Vector3(vert.x + offset.x, vert.y - offset.y, 0));
                            colList2.Add(col);
                        }

                        vb2.uvs.AddRange(uvList);
                        if (uv2List.Count > 0)
                            vb2.uvs2.AddRange(uv2List);
                    }

                    vb.Insert(vb2);
                    vb2.End();
                }
            }

            vb.AddTriangles();

            if (_richTextField != null)
                _richTextField.RefreshObjects();
        }

        void Cleanup ()
        {
            if (_richTextField != null)
                _richTextField.CleanupObjects();

            HtmlElement.ReturnElements(_elements);
            LineInfo.Return(_lines);
            _textWidth = 0;
            _textHeight = 0;
            _parsedText = string.Empty;
            _textDirection = RTLSupport.DirectionType.UNKNOW;

            if (_charPositions != null)
                _charPositions.Clear();
        }

        void ApplyVertAlign ()
        {
            float oldOffset = _yOffset;
            if (_autoSize == AutoSizeType.Both || _autoSize == AutoSizeType.Height
                || _verticalAlign == VertAlignType.Top)
                _yOffset = 0;
            else
            {
                float dh;
                if (_textHeight == 0 && _lines.Count > 0)
                    dh = _contentRect.height - _lines[0].height;
                else
                    dh = _contentRect.height - _textHeight;
                if (dh < 0)
                    dh = 0;
                if (_verticalAlign == VertAlignType.Middle)
                    _yOffset = (int)(dh / 2);
                else
                    _yOffset = dh;
            }

            if (oldOffset != _yOffset)
            {
                int cnt = _lines.Count;
                for (int i = 0; i < cnt; i++)
                    _lines[i].y = _lines[i].y2 + _yOffset;

                graphics.SetMeshDirty();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class LineInfo
        {
            /// <summary>
            /// 行的宽度
            /// </summary>
            public float width;

            /// <summary>
            /// 行的高度
            /// </summary>
            public float height;

            /// <summary>
            /// 文字渲染基线
            /// </summary>
            public float baseline;

            /// <summary>
            /// 行首的字符索引
            /// </summary>
            public int charIndex;

            /// <summary>
            /// 行包括的字符个数
            /// </summary>
            public short charCount;

            /// <summary>
            /// 行的y轴位置
            /// </summary>
            public float y;

            /// <summary>
            /// 行的y轴位置的备份
            /// </summary>
            internal float y2;

            static Stack<LineInfo> pool = new Stack<LineInfo>();

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public static LineInfo Borrow ()
            {
                if (pool.Count > 0)
                {
                    LineInfo ret = pool.Pop();
                    ret.width = ret.height = ret.baseline = 0;
                    ret.y = ret.y2 = 0;
                    ret.charIndex = ret.charCount = 0;
                    return ret;
                }
                else
                    return new LineInfo();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="value"></param>
            public static void Return (LineInfo value)
            {
                pool.Push(value);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="values"></param>
            public static void Return (List<LineInfo> values)
            {
                int cnt = values.Count;
                for (int i = 0; i < cnt; i++)
                    pool.Push(values[i]);

                values.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public struct LineCharInfo
        {
            public float width;
            public float height;
            public float baseline;
            public char ch;
        }

        /// <summary>
        /// 
        /// </summary>
        public struct CharPosition
        {
            /// <summary>
            /// 字符索引
            /// </summary>
            public int charIndex;

            /// <summary>
            /// 字符所在的行索引
            /// </summary>
            public short lineIndex;

            /// <summary>
            /// 字符的x偏移
            /// </summary>
            public float offsetX;

            /// <summary>
            /// 字符占用的顶点数量。
            /// </summary>
            public short vertCount;

            /// <summary>
            /// 字符的宽度
            /// </summary>
            public short width;

            /// <summary>
            /// 大于0表示图片索引。
            /// </summary>
            public short imgIndex;
        }
    }
}
