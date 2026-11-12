using System;
using System.IO;
using System.Reflection;

namespace sailflow.engine.Utility
{
    /// <summary>
    /// 进程路径等与外部配置源无关的工具。
    /// AppSettings / ConnectionStrings 读取已下沉业务侧，勿在此恢复。
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// Get the execution path of the current application
        /// 获取当前应用程序的执行路径
        /// </summary>
        public static string GetExecutingDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
