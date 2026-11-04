using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using saiflow.engine.Hooks;
using saiflow.msg.Notify;

namespace saiflow.msg.Samples
{
    /// <summary>
    /// 业务继承样例：仅实现 <see cref="SendCoreAsync"/>，POST JSON 到配置的回调 URL。
    /// 注册：<c>UseTaskNotifier(new SampleHttpTaskNotifier("https://example/callback"))</c>
    /// </summary>
    public sealed class SampleHttpTaskNotifier : TaskNotifyChannelBase
    {
        private static readonly HttpClient SharedClient = new();
        private readonly Uri _callbackUri;

        public SampleHttpTaskNotifier(string callbackUrl)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL is required.", nameof(callbackUrl));
            _callbackUri = new Uri(callbackUrl, UriKind.Absolute);
        }

        public override string Name => "sample.http";

        protected override async Task SendCoreAsync(TaskNotifyContext ctx, CancellationToken ct)
        {
            var payload = JsonConvert.SerializeObject(new
            {
                intent = TaskNotifyMessageIntentMapping.FromKind(ctx.Kind).ToString(),
                ctx.Kind,
                ctx.ProcessCode,
                ctx.ProcessInstanceId,
                ctx.TaskId,
                ctx.AssignedUserId,
                ctx.AppInstanceId,
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await SharedClient.PostAsync(_callbackUri, content, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}
