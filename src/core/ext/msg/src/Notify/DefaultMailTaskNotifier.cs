using System.Threading;
using System.Threading.Tasks;
using saiflow.engine.Client;
using saiflow.engine.Hooks;
using saiflow.hub.Resource;

namespace saiflow.msg.Notify
{
    /// <summary>默认 SMTP 待办邮件（须显式 <see cref="Hosting.TaskMailSailExtensions.UseDefaultTaskMail"/> 注册）。</summary>
    public sealed class DefaultMailTaskNotifier : TaskNotifyChannelBase
    {
        private readonly SailClient _client;

        public DefaultMailTaskNotifier(SailClient client)
        {
            _client = client ?? throw new System.ArgumentNullException(nameof(client));
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

            var title = _client.Messages.GetEngineMessage("taskmanager.emailtemplate.title");
            var body = _client.Messages.GetEngineMessage("taskmanager.emailtemplate.body");
            body = string.Format(body, _client.Config.LocalHostWebApp, ctx.AppName, ctx.AppInstanceId);

            var emailSendUtility = new EMailSendUtility(_client, ctx.TaskId);
            await emailSendUtility.SendEMailAsync(title, body, user.EMail).ConfigureAwait(false);
        }
    }
}
