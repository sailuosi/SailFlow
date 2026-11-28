namespace sailflow.assignee
{
    /// <summary>
    /// 范围加载（选人预览）元信息。与 Fixed Scope.Resolve 同源注册表；
    /// 空 / <c>AnyMan</c> 表示无约束（勿当「无人可选」）。
    /// </summary>
    public class ScopeLoadMeta
    {
        /// <summary>规范化后的 Scope 注册键（空串表示未配）。</summary>
        public string ScopeType { get; set; } = "";

        /// <summary>空或 AnyMan：选人器不限范围。</summary>
        public bool Unconstrained { get; set; }

        /// <summary>Scope Type 已写但未注册对应 <c>IAssigneeResolver</c>。</summary>
        public bool UnregisteredType { get; set; }

        /// <summary>失败时对齐 <see cref="AssigneeReasonCodes"/>；成功可为空。</summary>
        public string ReasonCode { get; set; }
    }
}
