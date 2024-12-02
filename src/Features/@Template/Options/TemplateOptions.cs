namespace Conesoft.Server_Host.Features.@Template.Options;

class TemplateOptions()
{
    [ConfigurationKeyName("value")]
    public string Value { get; init; } = "";
}
