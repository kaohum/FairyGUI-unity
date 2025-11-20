using System;

namespace FairyGUI
{
    public class Language
    {
        public enum LanguageType
        {
            //所有语言
            None = 0,
            //英文
            English = 1,
            //中文_简体
            Chinese_Simplified = 2,
            //中文_繁体
            Chinese_Aditional = 3,
            //印尼语
            Indonesian = 4,
            //德语
            German = 5,
            //法语
            French = 6,
        }

        public static LanguageType languageType = LanguageType.English;

        public static bool IsChinese()
        {
            return languageType == LanguageType.Chinese_Simplified
                || languageType == LanguageType.Chinese_Aditional;
        }
    }
}
