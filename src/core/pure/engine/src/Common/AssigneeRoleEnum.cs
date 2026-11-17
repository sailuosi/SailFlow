namespace sailflow.engine.Common
{
    /// <summary>
    /// 办理人角色（<c>sf_task.tk_assignee_role</c>）。
    /// null/0 = 常规审批办理人；1 = 沟通参与者（仅阅办）。
    /// </summary>
    public enum AssigneeRoleEnum : int
    {
        /// <summary>常规办理人（含库内 null）。</summary>
        Approver = 0,

        /// <summary>沟通参与者：可打开待办、发表意见、关闭阅办；无审批推进权。</summary>
        Participant = 1
    }

    /// <summary><see cref="AssigneeRoleEnum"/> 辅助。</summary>
    public static class AssigneeRoleHelper
    {
        /// <summary>null 或 0 均视为常规办理人。</summary>
        public static bool IsApprover(short? role) =>
            role == null || role == (short)AssigneeRoleEnum.Approver;

        public static bool IsApprover(int? role) =>
            role == null || role == (int)AssigneeRoleEnum.Approver;

        public static bool IsParticipant(short? role) =>
            role == (short)AssigneeRoleEnum.Participant;

        public static bool IsParticipant(int? role) =>
            role == (int)AssigneeRoleEnum.Participant;
    }
}
