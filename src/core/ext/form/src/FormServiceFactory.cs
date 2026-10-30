using saiflow.engine.Client;

namespace saiflow.hub.Form
{
    public class FormServiceFactory
    {
        public static IFormService Create(SailClient client)
        {
            return new FormService(client);
        }
    }
}