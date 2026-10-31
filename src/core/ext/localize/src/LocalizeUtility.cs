using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace saiflow.hub.Localize
{
    /// <summary>
    /// Localization Help Tool
    /// 本地化帮助工具
    /// </summary>
    internal class LocalizeUtility
    {
        static LocalizeUtility()
        {
            ReadProjectJSONResourceFromFile(ProjectTypeEnum.Engine);
            ReadProjectJSONResourceFromFile(ProjectTypeEnum.Designer);
            ReadProjectJSONResourceFromFile(ProjectTypeEnum.Web);
            ReadProjectJSONResourceFromFile(ProjectTypeEnum.Job);
        }

        /// <summary>
        /// Read json resource file
        /// 读取JSON资源文件
        /// </summary>
        /// <param name="project"></param>
        /// <param name="jsonFile"></param>
        static void ReadProjectJSONResourceFromFile(ProjectTypeEnum project)
        {
            var jsonFile = GetJSONFileNameByProject(project);
            try
            {
                var langJsonResource = new Dictionary<LangTypeEnum, Dictionary<string, string>>();
                foreach (var item in jsonFile)
                {
                    var jsonResource = ReadProjectJSONResourceFromFile(item.Value);
                    langJsonResource.Add(item.Key, jsonResource);
                }
                LanguageJsonResourceCachedHelper.SetJsonResource(project, langJsonResource);
            }
            catch (System.Exception ex)
            {
                throw new ApplicationException(string.Format("An error occurred when reading json resource file, detai:{0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// Get json data from resource file
        /// </summary>
        /// <param name="prjType"></param>
        /// <returns></returns>
        private static Dictionary<LangTypeEnum, string> GetJSONFileNameByProject(ProjectTypeEnum prjType)
        {
            var dictLang = new Dictionary<LangTypeEnum, string>();
            dictLang[LangTypeEnum.en] = string.Format("saiflow.hub.Localize.{0}.en.json", prjType.ToString());
            dictLang[LangTypeEnum.zh] = string.Format("saiflow.hub.Localize.{0}.zh.json", prjType.ToString());

            return dictLang;
        }

        /// <summary>
        /// Get json data from resource file
        /// 从资源文件读取JSON数据
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ReadProjectJSONResourceFromFile(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // 兼容未设置 LogicalName 时 SDK 默认名：sailflow.localize.src.Engine.en.json
                var suffix = resourceName.StartsWith("saiflow.hub.Localize.", StringComparison.Ordinal)
                    ? resourceName.Substring("saiflow.hub.Localize.".Length)
                    : resourceName;
                stream = assembly.GetManifestResourceNames()
                    .Where(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                                || n.EndsWith("src." + suffix, StringComparison.OrdinalIgnoreCase))
                    .Select(n => assembly.GetManifestResourceStream(n))
                    .FirstOrDefault(s => s != null);
            }

            if (stream == null)
            {
                var available = string.Join(", ", assembly.GetManifestResourceNames());
                throw new FileNotFoundException(
                    $"Embedded resource '{resourceName}' not found. Available: [{available}]");
            }

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonFile)
                       ?? new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Get language item
        /// 获取语言单条项目
        /// </summary>
        /// <param name="project"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetItem(ProjectTypeEnum project, string key)
        {
            var lang = LanguageKeyCachedHelper.GetLang();
            var lanJsonResource = LanguageJsonResourceCachedHelper.GetJsonResource(project);
            var value = lanJsonResource[lang][key];
            return value;
        }

        /// <summary>
        /// Get local language display information
        /// 获取本地语言显示信息
        /// </summary>
        /// <param name="project"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetMessage(ProjectTypeEnum project, string key)
        {
            var message = GetItem(project, key);
            return message;
        }

        /// <summary>
        /// Get local language display information
        /// 获取本地语言显示信息
        /// </summary>
        /// <param name="project"></param>
        /// <param name="key"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static string GetMessage(ProjectTypeEnum project, string key, string message)
        {
            var fullMessage = string.Format("{0}:{1}", GetItem(project, key), message);
            return fullMessage;
        }
    }
}
