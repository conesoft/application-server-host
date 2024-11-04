namespace Conesoft.Server_Host.Features.@Template.Extensions;

static class AddTemplateExtensions
{
    public static WebApplicationBuilder AddTemplate(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static IApplicationBuilder MapTemplate(this IApplicationBuilder app)
    {
        return app;
    }
}