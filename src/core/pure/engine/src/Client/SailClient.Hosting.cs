using Microsoft.Extensions.Configuration;
using saiflow.engine.Client.Hosting;

namespace saiflow.engine.Client;

public partial class SailClient
{
    public static SailClientBuilder CreateBuilder() => new();

    public static SailClientBuilder CreateBuilder(IConfiguration configuration)
        => new SailClientBuilder().UseConfiguration(configuration);
}
