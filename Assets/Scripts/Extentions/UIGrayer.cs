using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGUI
{
    public class UIGrayer
    {
        public static readonly Color GrayColor = Color.gray;
        private Color _grayColor;
        private GObject _root;
        private Dictionary<GObject, Color> _colorDict = new Dictionary<GObject, Color>();

        public UIGrayer(GObject root)
            : this(root, GrayColor)
        {
        }

        public UIGrayer(GObject root, Color grayColor)
        {
            _root = root;
            _grayColor = grayColor;
        }

        public void SetGray(bool gray, params GObject[] excludes)
        {
            SetGray(_root, gray, excludes);
        }

        private void SetGray(GObject obj, bool gray, params GObject[] excludes)
        {
            if (obj.name.StartsWith("not_gray") || excludes.Contains(obj))
            {
                return;
            }
            if (obj is GImage)
            {
                GImage image = obj as GImage;
                if (gray)
                {
                    Color color;
                    if (!_colorDict.TryGetValue(image, out color))
                    {
                        color = image.color;
                        _colorDict.Add(image, image.color);
                    }
                    image.color = color * _grayColor;
                }
                else
                {
                    if (_colorDict.TryGetValue(image, out var color))
                    {
                        image.color = color;
                    }
                }
            }
            else if (obj is GTextField)
            {
                GTextField text = obj as GTextField;
                if (gray)
                {
                    Color color;
                    if (!_colorDict.TryGetValue(text, out color))
                    {
                        color = text.color;
                        _colorDict.Add(text, text.color);
                    }
                    text.color = color * _grayColor;
                }
                else
                {
                    if (_colorDict.TryGetValue(text, out var color))
                    {
                        text.color = color;
                    }
                }
            }
            else if (obj is GRichTextField)
            {
                GRichTextField richText = obj as GRichTextField;
                if (gray)
                {
                    Color color;
                    if (!_colorDict.TryGetValue(richText, out color))
                    {
                        color = richText.color;
                        _colorDict.Add(richText, richText.color);
                    }
                    richText.color = color * _grayColor;
                }
                else
                {
                    if (_colorDict.TryGetValue(richText, out var color))
                    {
                        richText.color = color;
                    }
                }
            }
            else if (obj is GLoader)
            {
                GLoader loader = obj as GLoader;
                if (gray)
                {
                    Color color;
                    if (!_colorDict.TryGetValue(loader, out color))
                    {
                        color = loader.color;
                        _colorDict.Add(loader, loader.color);
                    }
                    loader.color = color * _grayColor;
                }
                else
                {
                    if (_colorDict.TryGetValue(loader, out var color))
                    {
                        loader.color = color;
                    }
                }

                if (loader.displayObject is Container)
                {
                    SetGray(loader.displayObject as Container, gray, excludes);
                }
            }
            else if (obj is GComponent)
            {
                GComponent comp = obj as GComponent;
                for (int i = 0; i < comp.numChildren; ++i)
                {
                    SetGray(comp.GetChildAt(i), gray, excludes);
                }
            }
        }

        private void SetGray(Container container, bool gray, params GObject[] excludes)
        {
            for (int i = 0; i < container.numChildren; ++i)
            {
                var child = container.GetChildAt(i);
                if (child.gOwner != null)
                {
                    SetGray(child.gOwner, gray, excludes);
                }
            }
        }
    }
}