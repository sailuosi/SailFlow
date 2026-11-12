using sailflow.engine.Client;

namespace sailflow.hub.Form
{
    public class FormServiceFactory
    {
        public static IFormService Create(SailClient client)
        {
            return new FormService(client);
        }
    }
}