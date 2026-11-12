using System;

namespace sailflow.hub.Localize
{
    /// <summary>
    /// JSON embedded-resource implementation of <see cref="ILocalizeMessages"/>.
    /// </summary>
    public sealed class JsonLocalizeMessages : ILocalizeMessages
    {
        public string GetEngineMessage(string key)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Engine, key);

        public string GetEngineMessage(string key, string message)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Engine, key, message);

        public string GetDesignerMessage(string key)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Designer, key);

        public string GetDesignerMessage(string key, string message)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Designer, key, message);

        public string GetWebMessage(string key)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Web, key);

        public string GetWebMessage(string key, string message)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Web, key, message);

        public string GetJobMessage(string key)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Job, key);

        public string GetJobMessage(string key, string message)
            => LocalizeUtility.GetMessage(ProjectTypeEnum.Job, key, message);

        public void SetLang(LangTypeEnum lang)
            => LanguageKeyCachedHelper.SetLang(lang);

        public LangTypeEnum GetLang()
            => LanguageKeyCachedHelper.GetLang();

        public void SetDefault()
        {
            var lang = LanguageKeyCachedHelper.GetLang();
            if (lang == LangTypeEnum.none)
                LanguageKeyCachedHelper.SetLang(LangTypeEnum.zh);
        }
    }
}
