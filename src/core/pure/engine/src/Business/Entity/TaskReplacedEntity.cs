namespace sailflow.engine.Entity
{
    /// <summary>
    /// 任务取代入参（非落库）。
    /// 将待办办理人替换为新用户时使用。
    /// </summary>
    public class TaskReplacedEntity
    {
        /// <summary>
        /// 取代后的新办理人用户 Id。
        /// </summary>
        public string ReplacedByUserId { get; set; }

        /// <summary>
        /// 取代后的新办理人用户姓名。
        /// </summary>
        public string ReplacedByUserName { get; set; }
    }
}
