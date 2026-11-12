namespace sailflow.AI.Abstractions
{
    /// <summary>
    /// AI 节点服务类型（引擎 Xpdl 与 biz.AI 共用契约）。
    /// </summary>
    public enum AiServiceTypeEnum
    {
        None = 0,

        /// <summary>Large Language Model</summary>
        LLM = 1,

        /// <summary>RAG：OCR、自动生成等</summary>
        RAG = 2,

        /// <summary>Agent：多轮 LLM + 可选 tool-calling / ReAct</summary>
        Agent = 3,
    }
}
