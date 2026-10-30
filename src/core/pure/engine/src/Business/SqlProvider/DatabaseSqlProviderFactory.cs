using System;
using System.Collections.Generic;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Common;

namespace saiflow.engine.Business.SqlProvider
{
    /// <summary>
    /// SQL Provider Factory（Phase E：表名/视图/TOP·LIMIT 经 <see cref="SailSqlDialect"/>）。
    /// </summary>
    public class DatabaseSqlProviderFactory
    {
        public static string GetProcessListSimple_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT 
                        Id, 
                        ProcessId, 
                        ProcessName,
                        ProcessCode,
                        Version,
                        Status,
                        AppType,
                        PackageType,
                        PackageId,
                        PageUrl,
                        Icon,
                        StartType,
                        StartExpression,
                        EndType,
                        EndExpression,
                        CreatedDateTime,
                        UpdatedDateTime
                    FROM {SailSqlDialect.ProcessTable}
                    ORDER BY UpdatedDateTime DESC";
            }

            return $@"SELECT 
                        id AS Id, 
                        process_id AS ProcessId, 
                        process_name AS ProcessName,
                        process_code AS ProcessCode,
                        version AS Version,
                        status AS Status,
                        app_type AS AppType,
                        package_type AS PackageType,
                        package_id AS PackageId,
                        page_url AS PageUrl,
                        icon AS Icon,
                        start_type AS StartType,
                        start_expression AS StartExpression,
                        end_type AS EndType,
                        end_expression AS EndExpression,
                        created_datetime AS CreatedDateTime,
                        updated_datetime As UpdatedDateTime
                    FROM {SailSqlDialect.ProcessTable}
                    ORDER BY COALESCE(updated_datetime, '1970-01-01') DESC";
        }

        public static string GetTaskListEMailUnSent_SQL()
        {
            // processState:2 running; activityType:4 task; activityState:1 ready; isEMailSent:0
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT * 
                             FROM {SailSqlDialect.TaskView} 
                             WHERE ProcessState=2 
                                AND (ActivityType=4 OR WorkItemType=1)
                                AND ActivityState=1
                                AND IsEMailSent=0
							    AND TaskState<>32
                            ";
            }

            return $@"SELECT * 
                             FROM {SailSqlDialect.TaskView} 
                             WHERE process_state=2 
                                AND (activity_type=4 OR work_item_type=1)
                                AND activity_state=1
                                AND is_email_sent=0
							    AND task_state<>32
                            ";
        }

        public static string GetReadyTaskOfApp_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT 
                                * 
                           FROM {SailSqlDialect.TaskView} 
                           WHERE AppInstanceId=@appInstanceId 
                                AND ProcessId=@processId 
                                AND ProcessState=2 
                                AND (ActivityType=4 OR WorkItemType=1)
                                AND ActivityState=1
                           ORDER BY TaskId DESC";
            }

            return $@"SELECT 
                                * 
                           FROM {SailSqlDialect.TaskView} 
                           WHERE app_instance_id=@appInstanceId 
                                AND process_id=@processId 
                                AND process_state=2 
                                AND (activity_type=4 OR work_item_type=1)
                                AND activity_state=1
                           ORDER BY id DESC";
        }

        public static string GetTaskOfMine_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT 
                            {SailSqlDialect.LimitPrefix(1)}* 
                       FROM {SailSqlDialect.TaskView} 
                       WHERE AppInstanceId=@appInstanceId 
                            AND ProcessId=@processId 
                            AND AssignedUserId=@userId 
                            AND ProcessState=2 
                            AND (ActivityType=4 OR ActivityType=5 OR ActivityType=6 OR WorkItemType=1)
                            AND (ActivityState=1 OR ActivityState=2) 
                       ORDER BY TaskId DESC{SailSqlDialect.LimitSuffix(1)}";
            }

            return $@"SELECT 
                            * 
                       FROM {SailSqlDialect.TaskView} 
                       WHERE app_instance_id=@appInstanceId 
                            AND process_id=@processId 
                            AND assigned_user_id=@userId 
                            AND process_state=2 
                            AND (activity_type=4 OR activity_type=5 OR activity_type=6 OR work_item_type=1)
                            AND (activity_state=1 OR activity_state=2) 
                       ORDER BY id DESC{SailSqlDialect.LimitSuffix(1)}";
        }

        public static string GetTaskOfMineByActivityInstance_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT 
                                {SailSqlDialect.LimitPrefix(1)}*
                            FROM {SailSqlDialect.TaskView} 
                            WHERE ActivityInstanceId=@activityInstanceId 
                                AND AssignedUserId=@userId 
                                AND ProcessState=2 
                                AND (ActivityType=4 OR ActivityType=5 OR ActivityType=6) 
                                AND (ActivityState=1 OR ActivityState=2) 
                            ORDER BY TaskId DESC{SailSqlDialect.LimitSuffix(1)}";
            }

            return $@"SELECT 
                            * 
                       FROM {SailSqlDialect.TaskView} 
                       WHERE activity_instance_id=@activityInstanceId 
                            AND assigned_user_id=@userId 
                            AND process_state=2 
                            AND (activity_type=4 OR activity_type=5 OR activity_type=6)
                            AND (activity_state=1 OR activity_state=2) 
                       ORDER BY id DESC{SailSqlDialect.LimitSuffix(1)}";
        }

        public static string GetTaskDoneListTop_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT
                                {SailSqlDialect.LimitPrefix(10)}* 
                            FROM {SailSqlDialect.TaskView} 
                            WHERE ProcessState=2 
                                AND (ActivityType=4 OR WorkItemType=1)
                                AND ActivityState=4
                                AND TaskState<>32
                            ORDER BY TaskId DESC{SailSqlDialect.LimitSuffix(10)}";
            }

            return $@"SELECT
                                * 
                            FROM {SailSqlDialect.TaskView} 
                            WHERE process_state=2 
                                AND (activity_type=4 OR work_item_type=1)
                                AND activity_state=4
                                AND task_state<>32
                            ORDER BY id DESC{SailSqlDialect.LimitSuffix(10)}";
        }

        public static string GetTaskToDoListTop_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT
                                {SailSqlDialect.LimitPrefix(10)}* 
                            FROM {SailSqlDialect.TaskView} 
                            WHERE ProcessState=2 
                                AND (ActivityType=4 OR WorkItemType=1)
                                AND ActivityState=1
                                AND TaskState<>32
                            ORDER BY TaskId DESC{SailSqlDialect.LimitSuffix(10)}";
            }

            return $@"SELECT
                                * 
                            FROM {SailSqlDialect.TaskView} 
                            WHERE process_state=2 
                                AND (activity_type=4 OR work_item_type=1)
                                AND activity_state=1
                                AND task_state<>32
                            ORDER BY id DESC{SailSqlDialect.LimitSuffix(10)}";
        }

        public static string GetFirstRunningTask_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT
                            * 
                         FROM {SailSqlDialect.TaskView} 
                         WHERE ProcessState=2 
                            AND ActivityInstanceId=@activityInstanceId 
                            AND (ActivityType=4 OR WorkItemType=1)
                            AND (ActivityState=1 OR ActivityState=2)
                            AND (TaskState=1 OR TaskState=2)
                            ORDER BY TaskState DESC
                        ";
            }

            return $@"SELECT
                            * 
                         FROM {SailSqlDialect.TaskView} 
                         WHERE process_state=2 
                            AND activity_instance_id=@activityInstanceId 
                            AND (activity_type=4 OR work_item_type=1)
                            AND (activity_state=1 OR activity_state=2)
                            AND (task_state=1 OR task_state=2)
                            ORDER BY task_state DESC
                        ";
        }

        public static string GetTaskViewByActivity_SQL()
        {
            if (SailSqlDialect.IsSqlServer)
            {
                return $@"SELECT
                            * 
                         FROM {SailSqlDialect.TaskView} 
                         WHERE ActivityInstanceId=@activityInstanceId
                            AND ProcessInstanceId=@processInstanceId
                        ";
            }

            return $@"SELECT
                            * 
                         FROM {SailSqlDialect.TaskView} 
                         WHERE activity_instance_id=@activityInstanceId
                            AND process_instance_id=@processInstanceId
                        ";
        }

        public static string GetTasksPaged_SQL(TaskQuery query, int activityState, IDictionary<string, object> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            string sql;
            if (SailSqlDialect.IsSqlServer)
            {
                sql = $@"SELECT
                                * 
                            FROM {SailSqlDialect.TaskView} 
                            WHERE ProcessState=2 
                                AND (ActivityType=4 OR WorkItemType=1)";
            }
            else
            {
                sql = $@"SELECT
                                * 
                            FROM {SailSqlDialect.TaskView} 
                            WHERE process_state=2 
                                AND (activity_type=4 OR work_item_type=1)";
            }

            var sqlBuilder = new StringSQLBuilder(sql);

            if (activityState == (short)ActivityStateEnum.Ready
                || activityState == (short)ActivityStateEnum.Running)
            {
                sqlBuilder.And(SailSqlDialect.MapField("ActivityState"), Operator.Eq, "@activityState");
                sqlBuilder.And(SailSqlDialect.MapField("TaskState"), Operator.Nq, 32);
            }
            else if (activityState == (short)ActivityStateEnum.Completed)
            {
                sqlBuilder.And(SailSqlDialect.MapField("TaskState"), Operator.Eq, 4);
            }

            parameters["@activityState"] = activityState;

            if (activityState == 1)
            {
                sqlBuilder.And(SailSqlDialect.MapField("MainActivityState"), Operator.Nq, 4);
            }

            if (!string.IsNullOrEmpty(query.AppInstanceId))
            {
                sqlBuilder.And(SailSqlDialect.MapField("AppInstanceId"), Operator.Eq, "@appInstanceId");
                parameters["@appInstanceId"] = query.AppInstanceId;
            }

            if (!string.IsNullOrEmpty(query.ProcessId))
            {
                sqlBuilder.And(SailSqlDialect.MapField("ProcessId"), Operator.Eq, "@processId");
                parameters["@processId"] = query.ProcessId;
            }

            if (!string.IsNullOrEmpty(query.Version))
            {
                sqlBuilder.And(SailSqlDialect.MapField("Version"), Operator.Eq, "@version");
                parameters["@version"] = query.Version;
            }

            if (!string.IsNullOrEmpty(query.UserId))
            {
                sqlBuilder.And(SailSqlDialect.MapField("AssignedUserId"), Operator.Eq, "@assignedUserId");
                parameters["@assignedUserId"] = query.UserId;
            }

            if (!string.IsNullOrEmpty(query.EndedUserId))
            {
                sqlBuilder.And(SailSqlDialect.MapField("EndedUserId"), Operator.Eq, "@endedUserId");
                parameters["@endedUserId"] = query.EndedUserId;
            }

            if (!string.IsNullOrEmpty(query.AppName))
            {
                sqlBuilder.And(SailSqlDialect.MapField("AppName"), Operator.Like, "@appName");
                parameters["@appName"] = string.Format("%{0}%", query.AppName);
            }
            sqlBuilder.OrderBy(SailSqlDialect.MapField("TaskId"), true);

            return sqlBuilder.GetSQL();
        }
    }
}
