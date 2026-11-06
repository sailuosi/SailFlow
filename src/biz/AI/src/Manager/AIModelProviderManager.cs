using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.AI.Entity;

namespace saiflow.AI.Manager
{
    /// <summary>
    /// AI 模型提供方管理类
    /// </summary>
    public class AiModelProviderManager
    {
        public AiModelProviderManager(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        private SQLBuilder Kit() => Sail.UseRunSQL();
        #region get

        public AiModelProviderEntity GetById(int id)
            => Kit().useRepo<AiModelProviderEntity, int>().GetById(id);

        public List<AiModelProviderEntity> GetAll()
        {
            var clip = Kit().useClip();
            clip.from<AiModelProviderEntity>(out var p);
            return clip.select(p).queryList().ToList();
        }

        /// <summary>
        /// Get models by model_type (e.g. vector_model for embedding models)
        /// </summary>
        public List<AiModelProviderEntity> GetByModelType(string modelType)
        {
            if (string.IsNullOrWhiteSpace(modelType))
                return new List<AiModelProviderEntity>();

            var clip = Kit().useClip();
            clip.from<AiModelProviderEntity>(out var p);
            clip.where(() => p.ModelType, modelType);
            clip.where(() => p.IsActive, true);
            return clip.select(p).queryList().ToList();
        }

        #endregion

        #region insert / update / delete

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
            => kit.useSQL().useRepo<AiModelProviderEntity, int>().InsertReturnId(entity);

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
            kit.useSQL().useRepo<AiModelProviderEntity, int>().Update(entity);
        }

        public void DeleteAgent(int modelId)
            => Kit().useRepo<AiModelProviderEntity, int>().DeleteById(modelId);

        #endregion
    }
}
