using System;
using System.IO;
using System.Xml;
using saiflow.engine.Entity;
using saiflow.engine.Manager;

namespace saiflow.engine.Storage
{
    /// <summary>
    /// Process definition file storage (local path from registered <c>SailConfig.WorkflowFileServer</c>).
    /// </summary>
    public class XPDLLocalStorage : IXPDLStorage
    {
        private readonly string _serverPath;

        public XPDLLocalStorage(string workflowFileServer)
        {
            _serverPath = workflowFileServer
                ?? throw new ArgumentNullException(nameof(workflowFileServer));
        }

        public XmlDocument Read(ProcessEntity entity)
        {
            string physicalFileName = string.Format("{0}\\{1}", _serverPath, entity.XmlFilePath);

            if (!File.Exists(physicalFileName))
            {
                throw new ApplicationException(
                    string.Format("Process Xml File Path:{0}", physicalFileName)
                );
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(physicalFileName);
            return xmlDoc;
        }

        public void Save(string filePath, XmlDocument xmlDoc)
        {
            var physicalFileName = string.Format("{0}\\{1}", _serverPath, filePath);
            var path = Path.GetDirectoryName(physicalFileName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            xmlDoc.Save(physicalFileName);
        }
    }
}
