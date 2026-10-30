using saiflow.engine.Client;
using System;
using saiflow.hub.Localize;
using saiflow.engine.Common;

namespace saiflow.engine.Xpdl.Schedule
{
    /// <summary>
    /// Next Activity Schedule Factory
    /// </summary>
    internal class NextActivityScheduleFactory
    {
        /// <summary>
        /// Create ActivitySchedule
        /// </summary>
        internal static NextActivityScheduleBase CreateActivityScheduleGateway(SailClient client, IProcessModel processModel,
            GatewaySplitJoinTypeEnum splitJoinType,
            Nullable<int> activityInstanceId = null)
        {
            NextActivityScheduleBase activitySchedule = null;
            if (splitJoinType == GatewaySplitJoinTypeEnum.Split)
            {
                activitySchedule = new NextActivityScheduleSplit(processModel, activityInstanceId, client);
            }
            else if (splitJoinType == GatewaySplitJoinTypeEnum.Join)
            {
                activitySchedule = new NextActivityScheduleJoin(processModel);
            }
            else
            {
                throw new Exception(client.Messages.GetEngineMessage("nextactivityschedulefactory.unknownnodetype"));
            }
            return activitySchedule;
        }

        /// <summary>
        /// Create ActivitySchedule
        /// </summary>
        /// <param name="processModel"></param>
        /// <returns></returns>
        internal static NextActivityScheduleBase CreateActivityScheduleIntermediate(IProcessModel processModel)
        {
            NextActivityScheduleBase activitySchedule = new NextActivityScheduleIntermediate(processModel);
            return activitySchedule;
        }
    }
}
