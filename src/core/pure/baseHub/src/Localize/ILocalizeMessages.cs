namespace sailflow.hub.Localize
{
    /// <summary>
    /// Localization message lookup (engine / designer / web / job).
    /// Implementation lives in sailflow.localize; register via <c>UseLocalize()</c>.
    /// </summary>
    public interface ILocalizeMessages
    {
        string GetEngineMessage(string key);
        string GetEngineMessage(string key, string message);

        string GetDesignerMessage(string key);
        string GetDesignerMessage(string key, string message);

        string GetWebMessage(string key);
        string GetWebMessage(string key, string message);

        string GetJobMessage(string key);
        string GetJobMessage(string key, string message);

        void SetLang(LangTypeEnum lang);
        LangTypeEnum GetLang();
        void SetDefault();
    }
}
