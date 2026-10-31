
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Common;
using saiflow.hub.Localize;
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// Process Manager
    /// 流程定义管理类
    /// </summary>
    public class ProcessManager
    {
        public ProcessManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }
        private static SQLBuilder Sibling(SQLBuilder kit)
        {
            var next = kit.DBLive.useSQL();
            if (kit.Executor != null)
                next.useTransaction(kit.Executor);
            return next;
        }

        #region Obtain process data 获取流程数据
        /// <summary>
        /// Obtain process entity based on Id
        /// 根据ID获取流程实体
        /// </summary>
        public ProcessEntity GetById(int id)
        {
            return Sail.UseRunSQL().useRepo<ProcessEntity, int>().GetById(id);
        }

        /// <summary>
        /// Obtain process entity based on Id
        /// 根据ID获取流程实体
        /// </summary>
        public ProcessEntity GetById(SQLBuilder kit, int id)
        {
            return Sibling(kit).useRepo<ProcessEntity, int>().GetById(id);
        }

        /// <summary>
        /// Obtain the process based on the processId and version number
        /// Notes: throwException means whether to throw an exception if the query cannot be found
        /// 根据流程GUID和版本标识获取流程
        /// 说明:throwException 为如果查询不到，是否抛出异常
        /// </summary>
        public ProcessEntity GetByVersion(string processId, string version, bool throwException = true)
        {
            var kit = Sail.UseRunSQL();
            return GetByVersion(kit, processId, version, throwException);
        }

        /// <summary>
        /// Obtain the process based on the processId and version number
        /// Notes: throwException means whether to throw an exception if the query cannot be found
        /// 根据流程GUID和版本标识获取流程
        /// 说明:throwException 为如果查询不到，是否抛出异常
        /// </summary>
        public ProcessEntity GetByVersion(SQLBuilder kit,
            string processId,
            string version,
            bool throwException = true)
        {
            ProcessEntity entity = null;
            var clip = Sibling(kit).useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.ProcessId, processId);
            clip.where(() => p.Version, version);
            var list = clip.select(p).queryList().ToList();

            if (list.Count() == 1)
            {
                entity = list[0];
            }
            else
            {
                if (throwException == true)
                {
                    throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.getbyversion.error",
                        string.Format("ProcessId: {0}, Version: {1}", processId, version)
                    ));
                }
            }
            return entity;
        }

        /// <summary>
        /// Get the current version of the process being used
        /// 获取当前使用的流程版本
        /// </summary>
        public ProcessEntity GetVersionUsing(string processId, bool isNotThrownException = true)
        {
            var kit = Sail.UseRunSQL();
            return GetVersionUsing(kit, processId, isNotThrownException);
        }

        /// <summary>
        /// Get the current version of the process being used
        /// 获取当前使用的流程版本
        /// </summary>
        public ProcessEntity GetVersionUsing(SQLBuilder kit,
            string processId,
            bool isNotThrownException)
        {
            ProcessEntity entity = null;
            var clip = Sibling(kit).useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.ProcessId, processId);
            clip.where(() => p.Status, (byte)1);
            var list = clip.select(p).queryList().ToList();

            if (list.Count() == 1)
            {
                entity = list[0];
            }
            else
            {
                if (isNotThrownException == false)
                {
                    throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.getbyversion.error",
                        string.Format("ProcessId: {0}", processId)
));
                }
            }
            return entity;
        }

        /// <summary>
        /// Obtain the process based on the process name and version identifier
        /// 根据流程名称和版本标识获取流程
        /// </summary>
        public ProcessEntity GetByName(string processName, string version = null)
        {
            if (string.IsNullOrEmpty(version)) version = "1";

            ProcessEntity entity = null;
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.ProcessName, processName);
            clip.where(() => p.Version, version);
            var list = clip.select(p).queryList().ToList();

            if (list.Count() == 1)
            {
                entity = list[0];
            }
            else if (list.Count() == 0)
            {
                ;
            }
            else
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.getbyname.error"));
            }
            return entity;
        }

        /// <summary>
        /// Obtain the process based on the process code and version identifier
        /// 根据流程代码和版本标识获取流程
        /// </summary>
        public ProcessEntity GetByCode(string processCode, string version = null)
        {
            if (string.IsNullOrEmpty(version)) version = "1";

            ProcessEntity entity = null;
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.ProcessCode, processCode);
            clip.where(() => p.Version, version);
            var list = clip.select(p).queryList().ToList();

            if (list.Count() == 1)
            {
                entity = list[0];
            }
            else if (list.Count() == 0)
            {
                ;
            }
            else
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.getbycode.error"));
            }
            return entity;
        }

        /// <summary>
        /// Obtain process records according to version
        /// 按照版本获取流程记录
        /// </summary>
        internal ProcessEntity GetByVersionInternal(SQLBuilder kit,
            string processId,
            string version)
        {
            ProcessEntity entity = null;
            var clip = Sibling(kit).useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.ProcessId, processId);
            clip.where(() => p.Version, version);
            var list = clip.select(p).queryList().ToList();

            if (list.Count() > 1)
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.getbyversioninternal.error"));
            }
            else if (list.Count() == 1)
            {
                entity = list[0];
            }
            return entity;
        }

        /// <summary>
        /// Obtain process based on message subject
        /// 根据消息主题获取流程
        /// </summary>
        public ProcessEntity GetByMessage(string topic)
        {
            ProcessEntity entity = null;
            //StartType:2  --- message trigger
            //StartExpression -- message topic
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.StartType, (byte)ProcessStartTypeEnum.Message);
            clip.where(() => p.StartExpression, topic);
            var list = clip.select(p).queryList().ToList();

            if (list.Count() > 1)
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.getbymessage.error"));
            }
            else if (list.Count() == 1)
            {
                entity = list[0];
            }
            return entity;
        }

        /// <summary>
        /// Obtain process based on App type
        /// 根据AppType获取流程记录
        /// </summary>
        public ProcessEntity GetSingleByAppType(string appType)
        {
            var kit = Sail.UseRunSQL();
            return GetSingleByAppType(kit, appType);
        }

        /// <summary>
        /// Obtain process based on App type
        /// 根据AppType获取流程记录
        /// </summary>
        public ProcessEntity GetSingleByAppType(SQLBuilder kit, string appType)
        {
            ProcessEntity entity = null;
            var clip = Sibling(kit).useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.AppType, appType);
            var list = clip.select(p).queryList().ToList();

            if (list.Count() == 1)
            {
                entity = list[0];
            }
            else
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.getsinglebyapptype.error"));
            }
            return entity;
        }

        /// <summary>
        /// Get all process
        /// 获取所有流程记录
        /// </summary>
        public List<ProcessEntity> GetAll()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<ProcessEntity>(out var p);
            return clip.select(p).queryList().ToList();
        }

        #endregion

        #region Addition, deletion, modification, and search of process records 新增、更新和删除流程数据
        /// <summary>
        /// Insert process
        /// 新增流程记录
        /// </summary>
        public int Insert(ProcessEntity entity)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    int processId = Insert(kit, entity);
                    kit.commit();
                    return processId;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Insert process
        /// 新增流程记录
        /// </summary>
        public int Insert(SQLBuilder kit, ProcessEntity entity)
        {
            var entityExisted = GetByVersionInternal(kit, entity.ProcessId, entity.Version);
            if (entityExisted != null)
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.insert.error"));
            }

            return Sibling(kit).useRepo<ProcessEntity, int>().InsertReturnId(entity);
        }

        /// <summary>
        /// Update process
        /// 更新流程记录
        /// </summary>
        public void Update(ProcessEntity entity)
        {
            var entityDB = GetByVersion(entity.ProcessId, entity.Version);

            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    if (entityDB.PackageType == (short)PackageTypeEnum.Package)
                    {
                        //更新主流程泳道流程的使用状态信息
                        //Update the usage status information of the main flow lane process
                        var clip = Sibling(kit).useClip();
                        clip.setTable<ProcessEntity>(out var p);
                        clip.set(() => p.Status, entity.Status);
                        clip.where(() => p.PackageId, entity.Id);
                        clip.doUpdate();
                    }
                    Sibling(kit).useRepo<ProcessEntity, int>().Update(entity);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Update process
        /// 流程定义记录更新
        /// </summary>
        public void Update(SQLBuilder kit, ProcessEntity entity)
        {
            Sibling(kit).useRepo<ProcessEntity, int>().Update(entity);
        }

        /// <summary>
        /// Update process version Status=0,1,2
        /// 更新流程版本
        /// </summary>
        internal void UpdateUsingState(string processId,
            string version,
            byte usingState)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var entity = GetByVersion(kit, processId, version, true);
                    entity.Status = usingState;
                    Sibling(kit).useRepo<ProcessEntity, int>().Update(entity);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Upgrade process version
        /// 升级流程记录
        /// </summary>
        public int Upgrade(string processId, string version, string newVersion)
        {
            int newProcessId = 0;
            if (string.IsNullOrEmpty(newVersion))
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("processmanager.upgrade.error"));
            }

            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var entity = GetByVersion(kit, processId, version, true);
                    var originProcessId = entity.Id;

                    entity.Version = newVersion;
                    entity.CreatedDateTime = System.DateTime.UtcNow;
                    //升级主流程版本
                    //Upgrade package process version
                    newProcessId = Sibling(kit).useRepo<ProcessEntity, int>().InsertReturnId(entity);

                    if (entity.PackageType == (short)PackageTypeEnum.Package)
                    {
                        //升级泳道流程版本
                        //Upgrade pool process version
                        UpgradePoolProcess(kit, originProcessId.Value, newProcessId, newVersion);
                    }
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
            return newProcessId;
        }

        /// <summary>
        /// Upgrade pool process
        /// 升级泳道流程记录
        /// </summary>
        public void UpgradePoolProcess(SQLBuilder kit,
            int originProcessId,
            int newProcessId,
            string newVersion)
        {
            //升级泳道流程的使用状态信息
            //Upgrade the usage status information of the lane process
            var clip = Sibling(kit).useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.PackageType, (byte)PackageTypeEnum.Pool);
            clip.where(() => p.PackageId, originProcessId);
            var list = clip.select(p).queryList().ToList();
        }

        /// <summary>
        /// Delete process
        /// 删除流程记录
        /// </summary>
        public void Delete(int id)
        {
            Sail.UseRunSQL().useRepo<ProcessEntity, int>().DeleteById(id);
        }

        /// <summary>
        /// Delete process
        /// 删除流程记录
        /// </summary>
        public void Delete(string processId, string version)
        {
            var entity = GetByVersion(processId, version);

            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    Sibling(kit).useRepo<ProcessEntity, int>().Delete(entity);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

		/// <summary>
        /// Delete process
        /// 删除流程记录
        /// </summary>
        public void Delete(SQLBuilder kit, string processId)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<ProcessEntity>(out var p);
            clip.where(() => p.ProcessId, processId);
            clip.doDelete();
        }

        /// <summary>
        /// Delete process
        /// 删除流程记录
        /// </summary>
        public void DeleteProcess(string processId, string version)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var entity = GetByVersion(kit, processId, version);
                    if (entity.PackageType == (short)PackageTypeEnum.Package)
                    {
                        //删除泳道流程
                        //Delete pool process
                        var poolClip = Sibling(kit).useClip();
                        poolClip.setTable<ProcessEntity>(out var pool);
                        poolClip.where(() => pool.PackageId, entity.Id);
                        poolClip.doDelete();
                    }

                    //删除主流程
                    //Delete package process
                    var clip = Sibling(kit).useClip();
                    clip.setTable<ProcessEntity>(out var p);
                    clip.where(() => p.ProcessId, processId);
                    clip.where(() => p.Version, version);
                    clip.doDelete();

                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }
        #endregion

        #region Process XML file operation 流程xml文件操作
        /// <summary>
        /// Process XML file operation
        /// 流程定义的创建方法
        /// </summary>
        internal int CreateProcess(ProcessEntity entity)
        {
            int processId = 0;

            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    if (string.IsNullOrEmpty(entity.ProcessId))
                    {
                        entity.ProcessId = Guid.NewGuid().ToString();
                    }

                    if (String.IsNullOrEmpty(entity.Version))
                    {
                        entity.Version = "1";     //default version value;
                    }
                    entity.Status = 1;
                    entity.CreatedDateTime = DateTime.UtcNow;
                    processId = Insert(kit, entity);
                    kit.commit();

                    return processId;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Create process by xml content
        /// 根据XML创建新流程
        /// </summary>
        internal ProcessFileEntity CreateProcessByXML(string xmlContent)
        {
            var processFileEntity = new ProcessFileEntity();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            var root = xmlDoc.DocumentElement;

            //saving single process
            var xmlNodeProcess = root.SelectSingleNode(XPDLDefinition.BPMN_StrXmlPath_Process,
                XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));
            if (xmlNodeProcess != null)
            {
                var process = ProcessModelHelper.ConvertFromXmlNodeProcess(xmlNodeProcess);
                process.XmlContent = xmlContent;
                process.Version = "1";

                var processManager = new ProcessManager(Sail);
                var processDB = processManager.GetByVersion(process.ProcessId, process.Version, false);
                if (processDB != null)
                {
                    throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.import.sameprocessexist.error"));
                }
                else
                {
                    //saving single process file
                    var processEntity = new ProcessEntity();
                    processEntity.ProcessId = !string.IsNullOrEmpty(process.ProcessId) ? process.ProcessId : Guid.NewGuid().ToString();
                    processEntity.ProcessName = process.ProcessName;
                    processEntity.ProcessCode = process.ProcessCode;
                    processEntity.Version = process.Version;
                    processEntity.Status = process.Status;
                    processEntity.XmlContent = process.XmlContent;
                    processEntity.CreatedDateTime = DateTime.UtcNow;
                    processEntity.UpdatedDateTime = DateTime.UtcNow;

                    //数据库存储
                    //Insert into database
                    processManager.Insert(processEntity);

                    //Set process file entity
                    processFileEntity.ProcessId = processEntity.ProcessId;
                    processFileEntity.ProcessCode = processEntity.ProcessCode;
                    processFileEntity.ProcessName = processEntity.ProcessName;
                    processFileEntity.Version = processEntity.Version;
                    processFileEntity.XmlContent = processEntity.XmlContent;
                    processFileEntity.Status = processEntity.Status;
                }
            }
            else
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.import.emptyprocessnode.error"));
            }
            return processFileEntity;
        }



        /// <summary>
        /// Save process file
        /// 保存XML文件
        /// </summary>
        internal void SaveProcessFile(ProcessFileEntity entity)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(entity.XmlContent);
            var root = xmlDoc.DocumentElement;

            var xmlNodeCollaboration = root.SelectSingleNode(XPDLDefinition.BPMN_StrXmlPath_Collaboration,
                XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));
            if (xmlNodeCollaboration == null)
            {
                //saving single process
                SaveProcessFileSingle(entity);
            }
            else
            {
                //saving process by pool/swimlane
                SaveProcessFileWithPool(entity, xmlDoc, xmlNodeCollaboration);
            }
        }

        /// <summary>
        /// Save process file single
        /// 保存XML文件
        /// </summary>
        internal void SaveProcessFileSingle(ProcessFileEntity entity)
        {
            //检查流程名称是否为空
            //Check if the process name is empty
            if (string.IsNullOrEmpty(entity.ProcessName))
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.saveprocessfile.noprocessname.error"));
            }

            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var processEntity = GetByVersion(kit, entity.ProcessId, entity.Version, false);
                    if (processEntity != null)
                    {
                        //删除泳道流程
                        //Delete pool process
                        DeletePoolProcess(kit, processEntity.Id.Value);

                        //更新主流程信息
                        //Update process info
                        processEntity.ProcessName = entity.ProcessName;
                        processEntity.ProcessCode = entity.ProcessCode;
                        processEntity.XmlContent = entity.XmlContent;
                        processEntity.PackageType = null;

                        //数据库存储
                        //Update to database
                        processEntity.UpdatedDateTime = DateTime.UtcNow;
                        Sibling(kit).useRepo<ProcessEntity, int>().Update(processEntity);
                    }
                    else
                    {
                        processEntity = new ProcessEntity();
                        processEntity.ProcessId = !string.IsNullOrEmpty(entity.ProcessId) ? entity.ProcessId : Guid.NewGuid().ToString();
                        processEntity.ProcessName = entity.ProcessName;
                        processEntity.ProcessCode = entity.ProcessCode;
                        processEntity.Version = entity.Version;
                        processEntity.Status = entity.Status;
                        processEntity.XmlContent = entity.XmlContent;
                        processEntity.CreatedDateTime = DateTime.UtcNow;
                        processEntity.UpdatedDateTime = DateTime.UtcNow;

                        //数据库存储
                        //Insert into database
                        Sibling(kit).useRepo<ProcessEntity, int>().Insert(processEntity);
                    }
                    kit.commit();
                }
                catch (System.Exception ex)
                {
                    LogManager.RecordLog(Sail, WfDefine.WF_XPDL_ERROR, LogEventType.Error, LogPriority.High, null, ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Save process file with pool
        /// 保存XML文件
        /// </summary>
        internal void SaveProcessFileWithPool(ProcessFileEntity entity,
            XmlDocument xmlDoc,
            XmlNode xmlNodeCollaboration)
        {
            var packageClient = CollaborationModelHelper.ConvertCollaborationFromXml(xmlDoc, xmlNodeCollaboration);
            //检查流程名称是否为空
            //Check if the process name is empty
            if (string.IsNullOrEmpty(packageClient.Name))
            {
                throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.saveprocessfile.noprocessname.error"));
            }

            var poolProcessListClient = packageClient.ParticipantList;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var packageDB = GetByVersion(kit, packageClient.CollaborationId, entity.Version, false);
                    if (packageDB == null)
                    {
                        var processCollaboration = new ProcessEntity();
                        processCollaboration.ProcessName = packageClient.Name;
                        processCollaboration.ProcessId = packageClient.CollaborationId;
                        processCollaboration.ProcessCode = packageClient.Code;
                        processCollaboration.PackageType = (byte)PackageTypeEnum.Package;
                        processCollaboration.Version = "1";         //default version
                        processCollaboration.Status = 1;
                        processCollaboration.XmlContent = entity.XmlContent;
                        processCollaboration.CreatedDateTime = DateTime.UtcNow;

                        //插入Package流程
                        //Insert package process
                        var newPackageId = Insert(kit, processCollaboration);

                        //插入泳道流程
                        //Insert pool process
                        foreach (var pool in poolProcessListClient)
                        {
                            var child = GetByVersion(kit, pool.Process.ProcessId, entity.Version, false);
                            if (child == null)
                            {
                                var processParticipant = new ProcessEntity();
                                processParticipant.PackageType = (byte)PackageTypeEnum.Pool;
                                processParticipant.PackageId = newPackageId;
                                processParticipant.ProcessName = pool.Name;
                                processParticipant.ProcessCode = pool.Code;
                                processParticipant.ProcessId = pool.Process.ProcessId;
                                processParticipant.ParticipantGUID = pool.ParticipantId;
                                processParticipant.Version = "1";
                                processParticipant.Status = 1;
                                processParticipant.XmlContent = entity.XmlContent;
                                processParticipant.CreatedDateTime = DateTime.UtcNow;
                                Insert(kit, processParticipant);
                            }
                            else
                            {
                                child.PackageId = newPackageId;
                                child.PackageType = (byte)PackageTypeEnum.Pool;
                                child.ProcessId = pool.Process.ProcessId;
                                child.ParticipantGUID = pool.ParticipantId;
                                var childName = String.IsNullOrEmpty(pool.Name) ? pool.Process.Name : pool.Name;
                                if (string.IsNullOrEmpty(childName))
                                {
                                    childName = pool.Process.ProcessId;
                                }
                                child.ProcessName = childName;
                                child.ProcessCode = pool.Code;
                                child.XmlContent = entity.XmlContent;
                                child.UpdatedDateTime = DateTime.UtcNow;
                                Update(kit, child);
                            }
                        }
                    }
                    else
                    {
                        var poolProcessListDatabase = GetPoolProcessList(kit, packageDB.Id.Value);
                        List<int> poolProcessIDs = new List<int>();
                        foreach (var pool in poolProcessListDatabase)
                        {
                            if (poolProcessListClient.Count() == 0)
                            {
                                poolProcessIDs.Add(pool.Id.Value);
                            }
                            else
                            {
                                var item = poolProcessListClient.Find(a=>a.ParticipantId == pool.ParticipantGUID);
                                if (item == null)
                                {
                                    poolProcessIDs.Add(pool.Id.Value);
                                }
                            }
                        }

                        //删除流程图中不含有的泳道流程
                        //Delete lane processes that are not included in the flowchart
                        if (poolProcessIDs.Count() > 0)
                        {
                            DeletePoolProcess(kit, packageDB.Id.Value, poolProcessIDs);
                        }

                        //如果是泳道流程变为单一流程，更新PackageType和PackageId
                        //If the lane process becomes a single process, update the PackageType and PackageId
                        if (poolProcessListClient.Count() == 0)
                        {
                            packageDB.PackageType = null;
                            packageDB.PackageId = null;
                        }

                        //更新主流程，更新或插入泳道流
                        //Update the main process, update or insert lane flow
                        packageDB.PackageType = (byte)PackageTypeEnum.Package;
                        packageDB.ProcessName = entity.ProcessName;
                        packageDB.XmlContent = entity.XmlContent;
                        packageDB.UpdatedDateTime = DateTime.UtcNow;
                        Update(kit, packageDB);

                        foreach (var pool in poolProcessListClient)
                        {
                            var child = GetByVersion(kit, pool.Process.ProcessId, entity.Version, false);
                            if (child == null)
                            {
                                //插入泳道流程
                                //Insert pool process
                                var processChild = new ProcessEntity();
                                processChild.ProcessId = pool.Process.ProcessId;
                                processChild.ParticipantGUID = pool.ParticipantId;
                                processChild.PackageType = (byte)PackageTypeEnum.Pool;
                                processChild.ProcessName = pool.Name;
                                processChild.ProcessCode = pool.Code;
                                processChild.PackageId = packageDB.Id;
                                processChild.Version = packageDB.Version;
                                processChild.XmlContent = entity.XmlContent;
                                processChild.CreatedDateTime = DateTime.UtcNow;
                                Insert(kit, processChild);
                            }
                            else
                            {
                                child.ProcessId = pool.Process.ProcessId;
                                child.ParticipantGUID = pool.ParticipantId;
                                child.PackageType = (byte)PackageTypeEnum.Pool;
                                child.ProcessName = pool.Name;
                                child.ProcessCode = pool.Code;
                                child.XmlContent = entity.XmlContent;
                                child.UpdatedDateTime = DateTime.UtcNow;
                                Update(kit, child);
                            }
                        }
                    }
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

       /// <summary>
       /// Set process timer 
       /// 更新流程定时器信息
       /// </summary>
        internal void SetProcessTimerType(string processId, string version)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var entity = GetByVersion(kit, processId, version, true);
                    var processModel = ProcessModelFactory.CreateByProcess(Sail, entity);

                    //StartNode
                    var startNode = processModel.GetStartActivity();
                    if (startNode != null)
                    {
                        if (startNode.TriggerDetail != null)
                        {
                            if (startNode.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                            {
                                entity.StartType = (byte)ProcessStartTypeEnum.Timer;
                                entity.StartExpression = startNode.TriggerDetail.Expression;
                            }
                            else if (startNode.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                            {
                                entity.StartType = (byte)ProcessStartTypeEnum.Message;
                                entity.StartExpression = startNode.TriggerDetail.Expression;
                            }
                        }
                    }

                    //EndNode
                    var endNode = processModel.GetEndActivity();
                    if (endNode != null)
                    {
                        if (endNode.TriggerDetail != null)
                        {
                            if (endNode.TriggerDetail.TriggerType == TriggerTypeEnum.Timer)
                            {
                                entity.EndType = (byte)ProcessEndTypeEnum.Timer;
                                entity.EndExpression = endNode.TriggerDetail.Expression;
                            }
                            else if (endNode.TriggerDetail.TriggerType == TriggerTypeEnum.Message)
                            {
                                entity.EndType = (byte)ProcessEndTypeEnum.Message;
                                entity.EndExpression = endNode.TriggerDetail.Expression;
                            }
                        }
                    }

                    //更新流程定义实体
                    //Update process entity
                    Update(kit, entity);
                    kit.commit();
                }
                catch (System.Exception ex)
                {
                    throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.setprocesstimertype.error", ex.Message));
                }
            }
        }

        /// <summary>
        /// Obtain the list of lane processes under the main flow
        /// 获取主流程下的泳道流程列表
        /// </summary>
        private List<ProcessEntity> GetPoolProcessList(SQLBuilder kit, int packageId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ProcessEntity>(out var p);
            clip.where(() => p.PackageId, packageId);
            return clip.select(p).queryList().ToList();
        }

        /// <summary>
        /// Import XML document to generate process records
        /// 导入XML文档生成流程记录
        /// </summary>
        internal void ImportProcess(string xmlContent)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            var root = xmlDoc.DocumentElement;

            var xmlNodeCollaboration = root.SelectSingleNode(XPDLDefinition.BPMN_StrXmlPath_Collaboration,
                XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));
            if (xmlNodeCollaboration != null)
            {
                var package = new ProcessFileEntity();
                package.XmlContent = xmlContent;
                package.Version = "1";
                
                package.ProcessId = XMLHelper.GetXmlAttribute(xmlNodeCollaboration, "id");
                var packageDB = GetByVersion(package.ProcessId, package.Version);
                if (packageDB != null)
                {
                    throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.import.sameprocessexist.error"));
                }
                else
                {
                    //saving process by pool/swimlane
                    SaveProcessFileWithPool(package, xmlDoc, xmlNodeCollaboration);
                }
            }
            else
            {
                //saving single process
                var xmlNodeProcess = root.SelectSingleNode(XPDLDefinition.BPMN_StrXmlPath_Process,
                    XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));
                if (xmlNodeProcess != null)
                {
                    var process = ProcessModelHelper.ConvertFromXmlNodeProcess(xmlNodeProcess);
                    process.XmlContent = xmlContent;
                    process.Version = "1";
                    var processDB = GetByVersion(process.ProcessId, process.Version, false);
                    if (processDB != null)
                    {
                        throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.import.sameprocessexist.error"));
                    }
                    else
                    {
                        //saving single process file
                        SaveProcessFileSingle(process);
                    }
                }
                else
                {
                    throw new ApplicationException(Sail.Messages.GetEngineMessage("processmanager.import.emptyprocessnode.error"));
                }
            }
        }

        /// <summary>
        /// Delete pool process
        /// 删除泳道流程记录
        /// </summary>
        private void DeletePoolProcess(SQLBuilder kit,
            int packageId,
            List<int> poolProcessIds)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<ProcessEntity>(out var p);
            clip.where(() => p.PackageId, packageId);
            clip.whereIn(() => p.Id, poolProcessIds.Cast<int?>().ToList());
            clip.doDelete();
        }

        /// <summary>
        /// Delete pool process by package id
        /// 删除泳道流程记录
        /// </summary>
        private void DeletePoolProcess(SQLBuilder kit, int packageId)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<ProcessEntity>(out var p);
            clip.where(() => p.PackageId, packageId);
            clip.where(() => p.PackageType, (byte)PackageTypeEnum.Pool);
            clip.doDelete();
        }

        /// <summary>
        /// Read the content of the process XML file
        /// 读取流程XML文件内容
        /// </summary>
        internal ProcessFileEntity GetProcessFile(string processId, string version, IXPDLStorage extStorage = null)
        {
            var processEntity = GetByVersion(processId, version);
            var processFileEntity = FillProcessFileEntity(processEntity);
            return processFileEntity;
        }

        /// <summary>
        /// Obtain process file entity based on Id
        /// 根据ID获取流程文件实体
        /// </summary>
        internal ProcessFileEntity GetProcessFileById(int id, IXPDLStorage extStorage = null)
        {
            var processEntity = GetById(id);
            var processFileEntity = FillProcessFileEntity(processEntity, extStorage);
            return processFileEntity;
        }

        /// <summary>
        /// Convert process file entity
        /// 转换流程文件实体
        /// </summary>
        private ProcessFileEntity FillProcessFileEntity(ProcessEntity processEntity, IXPDLStorage extStorage = null)
        {
            //流程文件实体
            //Process file entity
            var processFileEntity = new ProcessFileEntity();
            processFileEntity.ProcessId = processEntity.ProcessId;
            processFileEntity.ProcessName = processEntity.ProcessName;
            processFileEntity.ProcessCode = processEntity.ProcessCode;
            processFileEntity.Version = processEntity.Version;
            processFileEntity.StartType = processEntity.StartType.Value;
            processFileEntity.StartExpression = processEntity.StartExpression;
            processFileEntity.Description = processEntity.Description;

            if (extStorage != null)
            {
                //扩展方式读取xml文件内容
                //Extension method for reading XML file content
                var xmlDoc = extStorage.Read(processEntity);
                processFileEntity.XmlContent = xmlDoc.OuterXml;
            }
            else
            {
                processFileEntity.XmlContent = processEntity.XmlContent;
            }
            return processFileEntity;
        }


        /// <summary>
        /// Read xml content
        /// 读取Xml文档
        /// </summary>
        internal XmlDocument GetProcessXmlDocument(string processId, string version, IXPDLStorage extStorage = null)
        {
            var processEntity = GetByVersion(processId, version);

            var xmlDoc = new XmlDocument();
            if (extStorage != null)
            {
                xmlDoc = extStorage.Read(processEntity);
            }
            else
            {
                xmlDoc.LoadXml(processEntity.XmlContent);
            }

            return xmlDoc;
        }
        #endregion
    }
}
