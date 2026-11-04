using saiflow.engine.Manager;
using saiflow.engine.Client;

namespace saiflow.engine.Storage
{
    /// <summary>
    /// Factory method for external storage
    /// 外部存储的工厂方法（存储策略取自已注册的 <see cref="SailConfig"/> 形状）。
    /// </summary>
    internal class XPDLStorageFactory
    {
        /// <summary>
        /// Create XPDL storage from <see cref="SailClient.Config"/>.
        /// </summary>
        internal static IXPDLStorage CreateXPDLStorage(SailClient client)
        {
            if (client?.Config == null)
                return null;

            return CreateXPDLStorage(client.Config);
        }

        internal static IXPDLStorage CreateXPDLStorage(SailConfig config)
        {
            if (config == null)
                return null;

            var storageType = config.WorkflowFileStorageType;
            if (!string.IsNullOrEmpty(storageType)
                && string.Equals(storageType, "localfile", System.StringComparison.OrdinalIgnoreCase))
            {
                return new XPDLLocalStorage(config.WorkflowFileServer);
            }

            return null;
        }
    }
}
