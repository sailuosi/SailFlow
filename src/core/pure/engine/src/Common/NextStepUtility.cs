using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Client;

namespace saiflow.engine.Common
{
    /// <summary>
    /// Next Step Utility
    /// 下一步工具类
    /// </summary>
    public class NextStepUtility
    {
        /// <summary>
        /// Deserialize the next personnel list based on the text content
        /// 根据文本内容反序列化下一步人员列表
        /// </summary>
        public static IDictionary<string, PerformerList> DeserializeNextStepPerformers(
            string jsonNextStep,
            IJsonSerializer json)
        {
            IDictionary<string, PerformerList> nextSteps = null;
            if (!string.IsNullOrEmpty(jsonNextStep))
            {
                nextSteps = json.Deserialize<IDictionary<string, PerformerList>>(jsonNextStep);
            }
            return nextSteps;
        }

        /// <summary>
        /// List of personnel involved in serialization steps
        /// 序列化步骤人员列表
        /// </summary>
        public static string SerializeNextStepPerformers(
            IDictionary<string, PerformerList> nextSteps,
            IJsonSerializer json)
        {
            var jsonNextSteps = string.Empty;
            if (nextSteps != null
                && nextSteps.Count > 0
                && nextSteps.First().Value != null
                && nextSteps.First().Value.Count > 0)
            {
                jsonNextSteps = json.Serialize(nextSteps);
            }
            return jsonNextSteps;
        }

        /// <summary>
        /// Generate the next step performer list
        /// 生成下一步人员列表
        /// </summary>
        public static IDictionary<string, PerformerList> CreateNextStepPerformerList(string activityId,
            string userId,
            string userName)
        {
            var performerList = PerformerBuilder.CreatePerformerList(userId, userName);
            var nextStep = new Dictionary<string, PerformerList>();
            nextStep.Add(activityId, performerList);
            return nextStep;
        }
    }
}
