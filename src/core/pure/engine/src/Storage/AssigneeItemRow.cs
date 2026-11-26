namespace sailflow.storage
{
    /// <summary>
    /// 办理人项集行（Preselect / Picked 共用投影；不含宿主表主键）。
    /// </summary>
    public sealed class AssigneeItemRow
    {
        public string UserId { get; set; }

        public string UserName { get; set; }

        public string GroupId { get; set; }

        public string GroupName { get; set; }

        public string GroupRole { get; set; }

        public string UnitId { get; set; }

        public string UnitCode { get; set; }

        public string UnitName { get; set; }

        public decimal Weight { get; set; } = 1m;

        public int SortNo { get; set; }
    }
}
