using System;
using System.Collections.Generic;
using System.Text;

namespace FairyGUI.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public class HtmlText : IHtmlObject
    {
        public GTextField textField { get; private set; }

        RichTextField _owner;
        HtmlElement _element;
        bool _externalTexture;

        public HtmlText ()
        {
            textField = (GTextField)UIObjectFactory.NewObject(ObjectType.Text);
            textField.gameObjectName = "HtmlText";
        }

        public DisplayObject displayObject
        {
            get { return textField.displayObject; }
        }

        public HtmlElement element
        {
            get { return _element; }
        }

        public float width
        {
            get { return textField.width; }
        }

        public float height
        {
            get { return textField.height; }
        }

        public void Create (RichTextField owner, HtmlElement element)
        {
            _owner = owner;
            _element = element;

            // int sourceWidth = 0;
            // int sourceHeight = 0;

            // int width = element.GetInt("width", sourceWidth);
            // int height = element.GetInt("height", sourceHeight);
            // textField.SetSize(width, height);
            textField.textFormat = element.format;
            textField.text = element.text;
        }

        public void SetPosition (float x, float y)
        {
            textField.SetXY(x, y);
        }

        public void Add ()
        {
            _owner.AddChild(textField.displayObject);
        }

        public void Remove ()
        {
            if (textField.displayObject.parent != null)
                _owner.RemoveChild(textField.displayObject);
        }

        public void Release ()
        {
            textField.RemoveEventListeners();
            _owner = null;
            _element = null;
        }

        public void Dispose ()
        {
            textField.Dispose();
        }
    }
}
