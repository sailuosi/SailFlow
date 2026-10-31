using saiflow.engine.Client;
using mooSQL.data;
using saiflow.data;
using saiflow.AI.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace saiflow.AI.Manager
{
    /// <summary>
    /// AxConfig Manager
    /// AI节点配置管理类
    /// </summary>
    public class AiActivityConfigManager : ManagerBase
    {
        public AiActivityConfigManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        private SQLBuilder Kit() => Sail.UseRunSQL();

        #region  axconfig manager getdata 获取AI节点配置数据
        public AiActivityConfigEntity GetById(int id)
        {
            var entity = Repository.GetById<AiActivityConfigEntity>(id);
            return entity;
        }

        public AiActivityConfigEntity GetById(IDbConnection conn, int id, IDbTransaction trans)
        {
            var entity = Repository.GetById<AiActivityConfigEntity>(conn, id, trans);
            return entity;
        }

        public AiActivityConfigEntity GetByProcessVersionActivity(string processId, string version, string activityId)
        {
            AiActivityConfigEntity entity = null;
            var list = Repository.GetAll<AiActivityConfigEntity>()
                .Where<AiActivityConfigEntity>(
                    p => p.ProcessId == processId && p.Version == version && p.ActivityId == activityId)
                .ToList();

            if (list.Count() == 1)
            {
                entity = list[0];
            }
            return entity;
        }

        public AiActivityConfigEntity GetByProcessVersionActivity(IDbConnection conn, string processId, string version, string activityId, IDbTransaction trans)
        {
            AiActivityConfigEntity entity = null;
            var list = Repository.GetAll<AiActivityConfigEntity>(conn, trans)
                .Where<AiActivityConfigEntity>(
                    p => p.ProcessId == processId && p.Version == version && p.ActivityId == activityId)
                .ToList();

            if (list.Count() == 1)
            {
                entity = list[0];
            }
            return entity;
        }

        public List<AiActivityConfigEntity> GetAll()
        {
            var list = Repository.GetAll<AiActivityConfigEntity>().ToList<AiActivityConfigEntity>();
            return list;
        }
        #endregion

        #region Addition, deletion, modification, and search of axconfig records 新增、更新和删除AI节点配置数据
        public int Insert(AiActivityConfigEntity entity)
        {
            using (var kit = Kit())
            {
                kit.beginTransaction();
                try
                {
                    int agentId = Insert(kit, entity);
                    kit.commit();
                    return agentId;
                }
                catch
                {
                    throw;
                }
            }
        }

        public int Insert(SQLBuilder kit, AiActivityConfigEntity entity)
        {
            var entityExisted = GetByProcessVersionActivity(entity.ProcessId, entity.Version, entity.ActivityId);
            if (entityExisted != null)
            {
                throw new ApplicationException("AxConfigManager.Insert: A service record with the same process_id, version, and activity_id already exists!");
            }

            if (!entity.ModelProviderId.HasValue)
            {
                throw new ApplicationException("AxConfigManager.Insert: ModelProviderId is required and cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(entity.ModelName))
            {
                throw new ApplicationException("AxConfigManager.Insert: ModelName is required and cannot be null or empty.");
            }

            entity.CreatedDateTime = DateTime.UtcNow;
            return kit.useRepo<AiActivityConfigEntity, int>().InsertReturnId(entity);
        }

        public int Insert(IDbConnection conn, AiActivityConfigEntity entity, IDbTransaction trans)
        {
            var entityExisted = GetByProcessVersionActivity(conn, entity.ProcessId, entity.Version, entity.ActivityId, trans);
            if (entityExisted != null)
            {
                throw new ApplicationException("AxConfigManager.Insert: A service record with the same process_id, version, and activity_id already exists!");
            }

            if (!entity.ModelProviderId.HasValue)
            {
                throw new ApplicationException("AxConfigManager.Insert: ModelProviderId is required and cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(entity.ModelName))
            {
                throw new ApplicationException("AxConfigManager.Insert: ModelName is required and cannot be null or empty.");
            }

            entity.CreatedDateTime = DateTime.UtcNow;
            return Repository.Insert<AiActivityConfigEntity>(conn, entity, trans);
        }

        /// <summary>
        /// Update AxConfig
        /// 更新AI节点配置记录
        /// </summary>
        public void Update(AiActivityConfigEntity entity)
        {
            using (var kit = Kit())
            {
                kit.beginTransaction();
                try
                {
                    Update(kit, entity);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        public void Update(SQLBuilder kit, AiActivityConfigEntity entity)
        {
            if (!entity.ModelProviderId.HasValue)
            {
                throw new ApplicationException("AxConfigManager.Update: ModelProviderId is required and cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(entity.ModelName))
            {
                throw new ApplicationException("AxConfigManager.Update: ModelName is required and cannot be null or empty.");
            }

            entity.UpdatedDateTime = DateTime.UtcNow;
            kit.useRepo<AiActivityConfigEntity, int>().Update(entity);
        }

        public void Update(IDbConnection conn, AiActivityConfigEntity entity, IDbTransaction trans)
        {
            if (!entity.ModelProviderId.HasValue)
            {
                throw new ApplicationException("AxConfigManager.Update: ModelProviderId is required and cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(entity.ModelName))
            {
                throw new ApplicationException("AxConfigManager.Update: ModelName is required and cannot be null or empty.");
            }

            Repository.Update<AiActivityConfigEntity>(conn, entity, trans);
        }

        /// <summary>
        /// Delete AxConfig
        /// 删除AI节点配置记录
        /// </summary>
        public void Delete(string processId, string version, string activityId)
        {
            using (var kit = Kit())
            {
                kit.beginTransaction();
                try
                {
                    kit.setTable("ai_activity_config")
                        .where("process_id", processId)
                        .where("version", version)
                        .where("activity_id", activityId)
                        .doDelete();
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Upgrade AI activity config when process version is upgraded
        /// 升级流程版本时，同步复制 AI 节点配置记录
        /// </summary>
        public void UpgradeProcessActivityConfig(IDbConnection conn,
            string processId,
            string originVersion,
            string newVersion,
            IDbTransaction trans)
        {
            var list = Repository.GetAll<AiActivityConfigEntity>(conn, trans)
                .Where<AiActivityConfigEntity>(
                    p => p.ProcessId == processId
                        && p.Version == originVersion)
                .ToList();

            if (list.Count == 0) return;

            foreach (var entity in list)
            {
                entity.Version = newVersion;
                entity.Id = 0;
                Insert(conn, entity, trans);
            }
        }
        #endregion
    }
}
