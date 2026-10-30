using System;

namespace saiflow.hub.Localize
{
    /// <summary>
    /// Obsolete facade over <see cref="LocalizeMessagesLocator.Current"/>.
    /// Prefer <c>LocalizeMessagesLocator.Current.GetEngineMessage(...)</c> (or inject <see cref="ILocalizeMessages"/>).
    /// </summary>
    [Obsolete("Use LocalizeMessagesLocator.Current (or ILocalizeMessages) instead.")]
    public static class LocalizeHelper
    {
        private static ILocalizeMessages M => LocalizeMessagesLocator.Current;

        public static string GetEngineMessage(string key)
            => M.GetEngineMessage(key);

        public static string GetEngineMessage(string key, string message)
            => M.GetEngineMessage(key, message);

        public static string GetDesignerMessage(string key)
            => M.GetDesignerMessage(key);

        public static string GetDesignerMessage(string key, string message)
            => M.GetDesignerMessage(key, message);

        public static string GetWebMessage(string key)
            => M.GetWebMessage(key);

        public static string GetWebMessage(string key, string message)
            => M.GetWebMessage(key, message);

        public static string GetJobMessage(string key)
            => M.GetJobMessage(key);

        public static string GetJobMessage(string key, string message)
            => M.GetJobMessage(key, message);

        public static void SetLang(LangTypeEnum lang)
            => M.SetLang(lang);

        public static LangTypeEnum GetLang()
            => M.GetLang();

        public static void SetDefault()
            => M.SetDefault();
    }
}
