using UnityEngine;

namespace FairyGUI {
    /// <summary>
    /// FGUI的拓展，域外
    /// </summary>
    public static class FairyGUIExtension {
        /// <summary>
        /// 不适应刘海屏的元素
        /// </summary>
        public static void IgnoreLiuHaiScreen (this GObject component, RelationType type) {
            void OnSetScreen () {
                component.relations.ClearAll();
                component.SetXY(-(GRoot.offset_x / UIContentScaler.scaleFactor) - 2, -2);
                component.SetSize(GRoot.inst.sourceWidth + 4, GRoot.inst.sourceHeight + 4);
                //component.RemoveRelation(GRoot.inst, type);
                component.AddRelation(GRoot.inst, type);
            }

            OnSetScreen();
            
            GRoot.inst.onOrientationChanged.Remove(OnSetScreen);
            GRoot.inst.onOrientationChanged.Add(OnSetScreen);
            component.disposeAction += (o) => {
                GRoot.inst.onOrientationChanged.Remove(OnSetScreen);
            };
        }
        
        public static bool IsContainsGlobalPoint (this GObject gObject, Vector2 point) {
            Vector2 pos = gObject.GlobalToLocal(point);
            Vector2 pivot = gObject.pivot;
            Vector2 size = gObject.size;

            float minX = (0 - pivot.x) * size.x;
            float maxX = (1 - pivot.x) * size.x;
            if ((pos.x < minX || pos.x > maxX)) {
                return false;
            }
            float minY = (0 - pivot.y) * size.y;
            float maxY = (1 - pivot.y) * size.y;
            if ((pos.y < minY || pos.y > maxY)) {
                return false;
            }

            return true;
        }

        public static Vector2 ScreenToGlobal (this GObject gObject, Vector2 point) {
            point.y = Screen.height - point.y;
            return point;
        }

        static Color32 Colorone = new Color32(49, 49, 99, 255);
        static Color32 Colortwo = new Color32(216, 98, 3, 255);
        
        /// <summary>
        /// 设置按钮置灰（仅适用于通用按钮组件）
        /// </summary>
        public static void SetNormalBtnGray_Ext (GButton btn, bool grayState, string url) {
            if (grayState) {
                var gray_img = (GLoader) btn.GetChild("__gray_loader");
                if (gray_img != null)
                {
                    gray_img.visible = true;
                    var graytext = (GTextField) btn.GetChild("__gray_text");
                    graytext.visible = true;
                    return; // 已经置灰
                }
                var img =  btn.GetChildAt(0);
                gray_img = new GLoader();
                gray_img.name = "__gray_loader";
                gray_img.autoSize = false;
                gray_img.fill = FillType.ScaleFree;
                gray_img.url = url;
                btn.AddChildAt(gray_img,2);
                gray_img.size = img.size;
                gray_img.xy = img.xy;
                img.visible = false;
                // text
                var text = (GTextField) btn.GetChildAt(1);
                
                var gray_text = (GTextField) btn.GetChild("__gray_text");
                if (gray_text == null) {
                    gray_text = new GTextField();
                    gray_text.name = "__gray_text";
                }
                btn.AddChild(gray_text);
                gray_text.autoSize = text.autoSize;
                gray_text.size = text.size;
                
                gray_text.SetPivot(text.pivotX, text.pivotY, text.pivotAsAnchor);
                gray_text.xy = text.xy;
                gray_text.shadowOffset = text.shadowOffset;
                var format = gray_text.textFormat;
                format.outlineColor = Colorone;
                format.shadowColor = Colorone;
                format.size = text.textFormat.size;
                format.outline = text.textFormat.outline;
                gray_text.textFormat = format;
                gray_text.text = text.text;
                gray_text.color = text.color;
                gray_text.align = text.align;
                gray_text.verticalAlign = text.verticalAlign;
                
                text.visible = false;
                var number = btn.GetChild("number");
                if (number!=null)
                {
                    number.asTextField.textFormat.outlineColor = format.outlineColor;
                }
                //btn.touchable = false;
            } else {
                var gray_img = (GLoader) btn.GetChild("__gray_loader");
                if (gray_img == null) {
                    return;
                }
                //gray_img.RemoveFromParent();
                gray_img.visible = false;
                var gray_text = (GTextField) btn.GetChild("__gray_text");
                if (gray_text != null) {
                    //gray_text.RemoveFromParent();
                    gray_text.visible = false;
                }
                var number = btn.GetChild("number");
                if (number!=null)
                {
                    number.asTextField.textFormat.outlineColor = Colortwo;
                }
                var img = btn.GetChildAt(0);
                if (img != null) img.visible = true;
                var text = btn.GetChildAt(1);
                if (text != null) text.visible = true;
                btn.touchable = true;
            }
        }

        /// <summary>
        /// GProgressBar播放血量衰减
        /// </summary>
        public static void OnProgressRunAnimation (this GProgressBar progressBar, GObject _bar2, float _bar_x, float animationTime) {
            var tweener = GTween.GetTween(progressBar);
            if (tweener != null) {
                tweener.Kill(false);
                tweener = null;
            }
            if (animationTime > 0) {
                if (!progressBar.reverse) {
                    var own_width = progressBar.width - _bar_x * 2;
                    var a = _bar2.width;
                    var b = Mathf.Lerp(0, own_width, (float) (progressBar.value / progressBar.max));
                    GTween.To(a, b, animationTime).SetEase(EaseType.Linear).SetTarget(progressBar)
                        .OnUpdate((t) => { _bar2.width = t.value.x; });
                } else {
                    var own_width = progressBar.width - _bar_x * 2;
                    var a = _bar2.x;
                    var b = Mathf.Lerp(_bar_x, _bar_x + own_width, 1 - (float) (progressBar.value / progressBar.max));
                    GTween.To(a, b, animationTime).SetEase(EaseType.Linear).SetTarget(progressBar)
                        .OnUpdate((t) => {
                            var _x = t.value.x;
                            _bar2.x = _x;
                            _bar2.width = own_width - _x + _bar_x;
                        });
                }
            } else {
                if (!progressBar.reverse) {
                    var own_width = progressBar.width - _bar_x * 2;
                    _bar2.width = Mathf.Lerp(0, own_width, (float) (progressBar.value / progressBar.max));
                } else {
                    var own_width = progressBar.width - _bar_x * 2;
                    var _x = Mathf.Lerp(_bar_x, _bar_x + own_width, 1 - (float) (progressBar.value / progressBar.max));
                    _bar2.x = _x;
                    _bar2.width = own_width - _x + _bar_x;
                }
            }
        }

		public static bool IsVisible (this GObject obj)
		{
			return obj.internalVisible && obj.internalVisible2;
		}

		public static bool IsVisibleInHierarchy (this GObject obj)
		{
			if (obj.IsVisible() == false)
				return false;
			if (obj.parent == GRoot.inst)
				return true;
			if (obj.parent == null)
			{
				if (obj.displayObject != null &&
					obj.displayObject.parent != null &&
					obj.displayObject.parent.gOwner != null)
				{
					return obj.displayObject.parent.gOwner.IsVisibleInHierarchy();
				}
				return false;
			}
			return IsVisibleInHierarchy(obj.parent);
		}

		public static bool IsChildOf (this GObject obj, GComponent root)
		{
			if (obj.parent == null)
			{
				if (obj.displayObject != null &&
					obj.displayObject.parent != null &&
					obj.displayObject.parent.gOwner != null)
				{
					return obj.displayObject.parent.gOwner.IsChildOf(root);
				}
				return false;
			}
			else
			{
				if (obj.parent == root)
					return true;
				else
					return obj.parent.IsChildOf(root);
			}
		}
	}
}