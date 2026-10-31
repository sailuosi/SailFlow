using System.Collections.Generic;
using System.Text;

namespace saiflow.engine.Event
{
    /// <summary>
    /// Event Utility — 参数拼装（Action / Script / Service / 扩展 handler 共用）。
    /// </summary>
    public static class EventUtil
    {
        public static string CompositeJsonValue(string arguments, IEventService eventService)
        {
            var jsonValue = string.Empty;
            var arguValue = string.Empty;
            var arguList = arguments.Split(',');

            var strBuilder = new StringBuilder(256);
            foreach (var name in arguList)
            {
                if (strBuilder.ToString() != string.Empty) strBuilder.Append(",");

                arguValue = eventService.GetVariableByScopePriority(name);
                arguValue = FormatJsonStringIfSimple(arguValue);
                strBuilder.Append(string.Format("{0}:{1}", name, arguValue));
            }

            if (strBuilder.ToString() != string.Empty)
            {
                jsonValue = string.Format("{{{0}}}", strBuilder.ToString());
            }
            return jsonValue;
        }

        public static string FormatJsonStringIfSimple(string jsonValue)
        {
            jsonValue = jsonValue.TrimStart().TrimEnd();
            if ((jsonValue.StartsWith('{') && jsonValue.EndsWith('}'))
                || (jsonValue.StartsWith('"') && jsonValue.EndsWith('"')))
            {
                return jsonValue;
            }
            else
            {
                return string.Format("\"{0}\"", jsonValue);
            }
        }

        public static IDictionary<string, object> CompositeSqlParametersValue(string arguments, IEventService eventService)
        {
            var parameters = new Dictionary<string, object>();

            var arguValue = string.Empty;
            var arguList = arguments.Split(',');
            foreach (var name in arguList)
            {
                arguValue = eventService.GetVariableByScopePriority(name);
                parameters[string.Format("@{0}", name)] = arguValue;
            }
            return parameters;
        }

        public static IDictionary<string, string> CompositeKeyValue(string arguments, IEventService eventService)
        {
            var dictionary = new Dictionary<string, string>();
            var arguValue = string.Empty;
            var arguList = arguments.Split(',');
            foreach (var name in arguList)
            {
                arguValue = eventService.GetVariableByScopePriority(name);
                dictionary.Add(name, arguValue);
            }
            return dictionary;
        }

        public static object[] CompositeParameterValues(string arguments, IEventService eventService)
        {
            var arguList = arguments.Split(',');
            object[] valueArray = new object[arguList.Length];
            for (var i = 0; i < arguList.Length; i++)
            {
                valueArray[i] = eventService.GetVariableByScopePriority(arguList[i]);
            }
            return valueArray;
        }
    }
}
