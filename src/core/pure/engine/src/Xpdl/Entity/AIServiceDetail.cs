using saiflow.AI.Abstractions;

namespace saiflow.engine.Xpdl
{
    public class AiServiceDetail
    {
        public AiServiceTypeEnum AIServiceType { get; set; }
        public string ConfigUUID { get; set; }

        /// <summary>
        /// Logical name of the tool set to use when this node executes as an Agent.
        /// Matches a key registered in AgentToolSetRegistry.
        /// Set by the process designer in the BPMN properties panel.
        /// </summary>
        public string ToolSetName { get; set; }
    }
}
