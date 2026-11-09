using saiflow.engine.Client;
using System;
using System.Collections.Generic;
using mooSQL.data;
using saiflow.AI.Configuration;
using saiflow.AI.Entity;
using saiflow.AI.Manager;
using saiflow.data;

namespace saiflow.AI.Service
{
    public class AiModelDataService : IAiModelDataService
    {
        public AiModelDataService(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        private SQLBuilder Kit() => Sail.UseRunSQL();

        #region AIModel Data Service
        public AiModelProviderEntity GetModelById(string id)
        {
            var modelManager = new AiModelProviderManager(Sail);
            var entity = modelManager.GetById(id);

            return entity;
        }

        public List<AiModelProviderEntity> GetModelListAll()
        {
            var modelManager = new AiModelProviderManager(Sail);
            var list = modelManager.GetAll();

            return list;
        }

        public List<AiModelProviderEntity> GetModelListByType(string modelType)
        {
            var modelManager = new AiModelProviderManager(Sail);
            return modelManager.GetByModelType(modelType ?? string.Empty);
        }

        public string CreateModel(AiModelProviderEntity modelEntity)
        {
            var modelManager = new AiModelProviderManager(Sail);
            modelEntity.ApiUUID = Guid.NewGuid().ToString();
            var secrentKey = ApiKeyCryptoHelper.Encrypt(modelEntity.ApiKey, modelEntity.ApiUUID);
            modelEntity.ApiKey = secrentKey;
            modelEntity.CreatedDateTime = DateTime.UtcNow;
            return modelManager.Insert(modelEntity);
        }

        public Boolean UpdateModel(AiModelProviderEntity modelEntity)
        {
            var modelManager = new AiModelProviderManager(Sail);
            using (var kit = Kit())
            {
                kit.beginTransaction();
                try
                {
                    var existedModelEntity = kit.useRepo<AiModelProviderEntity, string>().GetById(modelEntity.Id);
                    if (modelEntity.ApiKey != existedModelEntity.ApiKey)
                    {
                        modelEntity.ApiKey = ApiKeyCryptoHelper.Encrypt(modelEntity.ApiKey, modelEntity.ApiUUID);
                    }
                    modelEntity.UpdatedDateTime = DateTime.UtcNow;
                    modelManager.Update(kit, modelEntity);
                    kit.commit();
                    return true;
                }
                catch
                {
                    throw;
                }
            }
        }

        public void DeleteModel(string modelId)
        {
            var modelManager = new AiModelProviderManager(Sail);
            modelManager.DeleteAgent(modelId);
        }
        #endregion

        #region AxConfig Data Service
        public AiActivityConfigEntity GetAiActivityConfigByProcessVersionActivity(string processId, string version, string activityId)
        {
            var axConfigManager = new AiActivityConfigManager(Sail);
            var axConfigEntity = axConfigManager.GetByProcessVersionActivity(processId, version, activityId);
            return axConfigEntity;
        }

        /// <summary>
        /// Save AxConfig and Variables
        /// 保存AI节点配置和变量信息
        /// </summary>
        public void SaveAiActivityConfigInfo(AiActivityConfigEntity axConfigEntity)
        {
            var axConfigManager = new AiActivityConfigManager(Sail);
            using (var kit = Kit())
            {
                kit.beginTransaction();
                try
                {
                    var existingAxConfigEntity = axConfigManager.GetByProcessVersionActivity(
                        axConfigEntity.ProcessId, axConfigEntity.Version, axConfigEntity.ActivityId);

                    if (existingAxConfigEntity == null)
                    {
                        axConfigEntity.CreatedDateTime = DateTime.UtcNow;
                        axConfigManager.Insert(kit, axConfigEntity);
                    }
                    else
                    {
                        axConfigEntity.Id = existingAxConfigEntity.Id;
                        axConfigEntity.UpdatedDateTime = DateTime.UtcNow;
                        axConfigManager.Update(kit, axConfigEntity);
                    }

                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        public void DeleteAiActivityConfig(string processId, string version, string activityId)
        {
            var axConfigManager = new AiActivityConfigManager(Sail);
            axConfigManager.Delete(processId, version, activityId);
        }
        #endregion
    }
}
