namespace sailflow.hub.Localize
{
    /// <summary>
    /// Fallback when <see cref="LocalizeMessagesLocator"/> is not registered:
    /// returns the key (or key:message). Sufficient for unit tests without UseLocalize.
    /// </summary>
    public sealed class PassthroughLocalizeMessages : ILocalizeMessages
    {
        public static PassthroughLocalizeMessages Instance { get; } = new();

        private LangTypeEnum _lang = LangTypeEnum.en;

        private PassthroughLocalizeMessages()
        {
        }

        public string GetEngineMessage(string key) => key ?? string.Empty;

        public string GetEngineMessage(string key, string message)
            => string.Format("{0}:{1}", key ?? string.Empty, message);

        public string GetDesignerMessage(string key) => key ?? string.Empty;

        public string GetDesignerMessage(string key, string message)
            => string.Format("{0}:{1}", key ?? string.Empty, message);

        public string GetWebMessage(string key) => key ?? string.Empty;

        public string GetWebMessage(string key, string message)
            => string.Format("{0}:{1}", key ?? string.Empty, message);

        public string GetJobMessage(string key) => key ?? string.Empty;

        public string GetJobMessage(string key, string message)
            => string.Format("{0}:{1}", key ?? string.Empty, message);

        public void SetLang(LangTypeEnum lang) => _lang = lang;

        public LangTypeEnum GetLang()
            => _lang == LangTypeEnum.none ? LangTypeEnum.en : _lang;

        public void SetDefault()
        {
            if (_lang == LangTypeEnum.none)
                _lang = LangTypeEnum.zh;
        }
    }
}
