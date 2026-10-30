using saiflow.engine.Client;

namespace saiflow.hub.Resource
{
    public class ResourceServiceFactory
    {
        public static IResourceService Create(SailClient client)
        {
            return new ResourceService(client);
        }
    }
}
