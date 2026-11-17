using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Entity;

namespace sailflow.engine.Manager
{
    /// <summary>
    /// Process definition database storage interface
    /// 流程定义数据库存储接口
    /// </summary>
    public interface IXPDLStorage
    {
        XmlDocument Read(ProcessEntity entity);
        void Save(string filePath, XmlDocument xmlDoc);
    }
}
