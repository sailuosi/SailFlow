using sailflow.engine.Client;
using mooSQL.data;
using sailflow.data;
using sailflow.AI.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace sailflow.AI.Manager
{
    /// <summary>
    /// AI 节点配置管理类
    /// </summary>
    public class AiActivityConfigManager
    {
        public AiActivityConfigManager(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        private SQLBuilder Kit() => Sail.UseRunSQL();
        #region get

        public AiActivityConfigEntity GetById(string id)
            => Kit().findRowById<AiActivityConfigEntity>(id);

        public AiActivityConfigEntity GetByProcessVersionActivity(string processId, string version, string activityId)
            => GetByProcessVersionActivity(Kit(), processId, version, activityId);

        public AiActivityConfigEntity GetByProcessVersionActivity(SQLBuilder kit, string processId, string version, string activityId)
        {
            var clip = kit.useClip();
            clip.from<AiActivityConfigEntity>(out var p)
                .where(() => p.ProcessId, processId)
                .where(() => p.Version, version)
                .where(() => p.ActivityId, activityId);
            var list = clip.select(p).queryList().ToList();
            return list.Count == 1 ? list[0] : null;
        }

        public List<AiActivityConfigEntity> GetAll()
        {
            var clip = Kit().useClip();
            clip.from<AiActivityConfigEntity>(out var p);
            return clip.select(p).queryList().ToList();
        }

        #endregion

        #region insert / update / delete

        public string Insert(AiActivityConfigEntity entity)
        {
            using (var kit = Kit())
            {
                kit.beginTransaction();
                try
                {
                    var agentId = Insert(kit, entity);
                    kit.commit();
                    return agentId;
                }
                catch
                {
                    throw;
                }
            }
        }

        public string Insert(SQLBuilder kit, AiActivityConfigEntity entity)
        {
            var entityExisted = GetByProcessVersionActivity(kit, entity.ProcessId, entity.Version, entity.ActivityId);
            if (entityExisted != null)
            {
                throw new ApplicationException("AxConfigManager.Insert: A service record with the same process_id, version, and activity_id already exists!");
            }

            if (string.IsNullOrWhiteSpace(entity.ModelProviderId))
            {
                throw new ApplicationException("AxConfigManager.Insert: ModelProviderId is required and cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(entity.ModelName))
            {
                throw new ApplicationException("AxConfigManager.Insert: ModelName is required and cannot be null or empty.");
            }

            entity.CreatedDateTime = DateTime.UtcNow;
            return kit.useRepo<AiActivityConfigEntity, string>().InsertReturnId(entity);
        }

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
            if (string.IsNullOrWhiteSpace(entity.ModelProviderId))
            {
                throw new ApplicationException("AxConfigManager.Update: ModelProviderId is required and cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(entity.ModelName))
            {
                throw new ApplicationException("AxConfigManager.Update: ModelName is required and cannot be null or empty.");
            }

            entity.UpdatedDateTime = DateTime.UtcNow;
            kit.useRepo<AiActivityConfigEntity, string>().Update(entity);
        }

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
        /// 升级流程版本时，同步复制 AI 节点配置记录
        /// </summary>
        public void UpgradeProcessActivityConfig(SQLBuilder kit,
            string processId,
            string originVersion,
            string newVersion)
        {
            var clip = kit.useClip();
            clip.from<AiActivityConfigEntity>(out var p)
                .where(() => p.ProcessId, processId)
                .where(() => p.Version, originVersion);
            var list = clip.select(p).queryList().ToList();
            if (list.Count == 0) return;

            foreach (var entity in list)
            {
                entity.Version = newVersion;
                // 新建：清空 Id，由 AutoKey=Guid 发号
                entity.Id = null;
                Insert(kit, entity);
            }
        }

        #endregion
    }
}
