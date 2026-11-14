using System;
using System.Threading;
using System.Threading.Tasks;
using sailflow.engine.Client;
using sailflow.engine.Hooks;
using sailflow.hub.Resource;

namespace sailflow.msg.Notify
{
    /// <summary>默认 SMTP 待办邮件（须显式 <see cref="Hosting.TaskMailSailExtensions.UseDefaultTaskMail"/> 注册）。</summary>
    public sealed class DefaultMailTaskNotifier : TaskNotifyChannelBase
    {
        private readonly SailClient _client;
        private readonly TaskMailOptions _options;

        public DefaultMailTaskNotifier(SailClient client, TaskMailOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public override string Name => "default.mail";

        protected override async Task SendCoreAsync(TaskNotifyContext ctx, CancellationToken ct)
        {
            if (ctx.Kind != TaskNotifyKind.Created && ctx.Kind != TaskNotifyKind.Completed)
                return;

            var rm = ResourceServiceLocator.Resolve();
            var user = rm.GetUserById(ctx.AssignedUserId);
            if (user == null || string.IsNullOrEmpty(user.EMail))
                return;

            var title = _options.Subject;
            var body = string.Format(_options.BodyTemplate, _options.AppBaseUrl, ctx.BizChannel, ctx.BizPK);

            var emailSendUtility = new EMailSendUtility(_client, _options, ctx.TaskId);
            await emailSendUtility.SendEMailAsync(title, body, user.EMail).ConfigureAwait(false);
        }
    }
}
