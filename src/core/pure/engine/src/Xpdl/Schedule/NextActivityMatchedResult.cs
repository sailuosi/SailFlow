using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.hub.Localize;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Next Activity Matched Result
    /// 执行活动的可选类型
    /// </summary>
    public class NextActivityMatchedResult
    {
        #region Property and Constructor
        public string Message
        {
            get;
            set;
        }

        public NextActivityMatchedType MatchedType
        {
            get;
            set;
        }

        public NextActivityComponent Root
        {
            get;
            set;
        }

        private NextActivityMatchedResult(NextActivityMatchedType matchedType,
            NextActivityComponent root)
        {
            MatchedType = matchedType;
            Root = root;
        }

        #endregion

        #region Create Method
        /// <summary>
        /// Create Method
        /// 创建方法
        /// </summary>
        /// <param name="matchedType"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        internal static NextActivityMatchedResult CreateNextActivityMatchedResultObject(NextActivityMatchedType matchedType,
            NextActivityComponent root)
        {
            NextActivityMatchedResult result = new NextActivityMatchedResult(matchedType, root);
            switch (matchedType)
            {
                case NextActivityMatchedType.Unknown:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.Unkonwn");
                    break;
                case NextActivityMatchedType.Failed:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.Exceptional"); 
                    break;
                case NextActivityMatchedType.Successed:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.Successed"); 
                    break;
                case NextActivityMatchedType.NoneTransitionFilteredByCondition:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.NoneTransitionFilteredByCondition"); 
                    break;
                case NextActivityMatchedType.WaitingForOtherSplitting:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.WaitingForOtherSplitting"); 
                    break;
                case NextActivityMatchedType.WaitingForAgreedOrRefused:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.WaitingForAgreedOrRefused");
                    break;
                case NextActivityMatchedType.NoneTransitionMatchedToSplit:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.NoneWayMatchedToSplit"); 
                    break;
                case NextActivityMatchedType.FailedPassRateOfMulitipleInstance:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.FailedPassRateOfMulitipleInstance"); 
                    break;
                case NextActivityMatchedType.WaitingForOthersJoin:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.WaitingForOthersJoin"); 
                    break;
                case NextActivityMatchedType.NotMadeItselfToJoin:
                    result.Message = LocalizeMessagesLocator.Current.GetEngineMessage("nextactivitymatchedresult.NotMadeItselfToJoin"); 
                    break;
            }
            return result;
        }
        #endregion
    }
}
