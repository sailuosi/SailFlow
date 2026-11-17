namespace sailflow.engine.Common
{
    /// <summary>
    /// 办理对象类型（<c>sf_task.tk_sign_kind</c>）。
    /// null/0 = 人（现行默认）；其余对齐 ddflow AssigneeKind。
    /// </summary>
    public enum SignKindEnum : int
    {
        /// <summary>用户/人（库内 null 同此）。</summary>
        User = 0,

        /// <summary>部门。</summary>
        Dept = 1,

        /// <summary>角色。</summary>
        Role = 2,

        /// <summary>组。</summary>
        Group = 3,

        /// <summary>自定义。</summary>
        Custom = 4
    }

    /// <summary><see cref="SignKindEnum"/> 辅助。</summary>
    public static class SignKindHelper
    {
        /// <summary>null 或 0 均视为人。</summary>
        public static bool IsPerson(int? kind) =>
            kind == null || kind == (int)SignKindEnum.User;

        public static bool IsPerson(short? kind) =>
            kind == null || kind == (short)SignKindEnum.User;
    }
}
