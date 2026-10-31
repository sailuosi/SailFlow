using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.AI.Entity;

namespace saiflow.AI.Manager
{
    /// <summary>
    /// Agent Manager
    /// 智能体管理类
    /// </summary>
    public class AiModelProviderManager : ManagerBase
    {
        public AiModelProviderManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        private SQLBuilder Kit() => Sail.UseRunSQL();

        #region Obtain model data 获取模型数据
        public AiModelProviderEntity GetById(int id)
        {
            var entity = Repository.GetById<AiModelProviderEntity>(id);
            return entity;
        }

        public AiModelProviderEntity GetById(IDbConnection conn, int id, IDbTransaction trans)
        {
            var entity = Repository.GetById<AiModelProviderEntity>(conn, id, trans);
            return entity;
        }

        public List<AiModelProviderEntity> GetAll()
        {
            var list = Repository.GetAll<AiModelProviderEntity>().ToList<AiModelProviderEntity>();
            return list;
        }

        /// <summary>
        /// Get models by model_type (e.g. vector_model for embedding models)
        /// </summary>
        public List<AiModelProviderEntity> GetByModelType(string modelType)
        {
            if (string.IsNullOrWhiteSpace(modelType))
                return new List<AiModelProviderEntity>();
            var list = Repository.GetAll<AiModelProviderEntity>()
                .Where<AiModelProviderEntity>(x => x.ModelType == modelType && x.IsActive)
                .ToList();
            return list;
        }
        #endregion

        #region Addition, deletion, modification, and search of agent records 新增、更新和删除智能体数据
        public int Insert(AiModelProviderEntity entity)
        {
            using (var kit = Kit())
            {
                kit.beginTransaction();
                try
                {
                    var modelId = Insert(kit, entity);
                    kit.commit();
                    return modelId;
                }
                catch
                {
                    throw;
                }
            }
        }

        public int Insert(SQLBuilder kit, AiModelProviderEntity entity)
            => kit.useRepo<AiModelProviderEntity, int>().InsertReturnId(entity);

        public int Insert(IDbConnection conn, AiModelProviderEntity entity, IDbTransaction trans)
            => Repository.Insert<AiModelProviderEntity>(conn, entity, trans);

        /// <summary>
        /// Update process
        /// 更新流程记录
        /// </summary>
        public void Update(AiModelProviderEntity entity)
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

        public void Update(SQLBuilder kit, AiModelProviderEntity entity)
        {
            entity.UpdatedDateTime = DateTime.UtcNow;
            kit.useRepo<AiModelProviderEntity, int>().Update(entity);
        }

        public void Update(IDbConnection conn, AiModelProviderEntity entity, IDbTransaction trans)
        {
            entity.UpdatedDateTime = DateTime.UtcNow;
            Repository.Update<AiModelProviderEntity>(conn, entity, trans);
        }

        /// <summary>
        /// Delete process
        /// 删除流程记录
        /// </summary>
        public void DeleteAgent(int modelId)
        {
            Repository.Delete<AiModelProviderEntity>(modelId);
        }
        #endregion
    }
}
