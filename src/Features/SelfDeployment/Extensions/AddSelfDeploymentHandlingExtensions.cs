using Conesoft.Server_Host.Helpers;

namespace Conesoft.Server_Host.Features.SelfDeployment.Extensions;

static class AddSelfDeploymentHandlingExtensions
{
    public static WebApplicationBuilder AddSelfDeploymentHandling(this WebApplicationBuilder builder)
    {
        var commands = CommandLineCommand.Parse();
        if(commands.SingleOrDefault() is CommandLineCommand command)
        {
            switch(command.Command)
            {
                case "deploy":
                case "deploy-with-processes":
                    //throw new Exception("lol");

                case "with-processes":
                    break;
            }
        }

        return builder;
    }
}