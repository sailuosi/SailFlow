using saiflow.AI.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saiflow.AI.Service
{
    public interface IAiModelDataService
    {
        AiModelProviderEntity GetModelById(string id);
        List<AiModelProviderEntity> GetModelListAll();
        List<AiModelProviderEntity> GetModelListByType(string modelType);
        string CreateModel(AiModelProviderEntity modelEntity);
        Boolean UpdateModel(AiModelProviderEntity modelEntity);
        void DeleteModel(string modelId);
        AiActivityConfigEntity GetAiActivityConfigByProcessVersionActivity(string processId, string version, string activityId);
        void SaveAiActivityConfigInfo(AiActivityConfigEntity axConfigEntity);
        void DeleteAiActivityConfig(string processId, string version, string activityId);
    }
}
