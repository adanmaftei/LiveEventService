using Amazon.CDK;
using System;

namespace LiveEventService.Infrastructure.CDK
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            var skipPrimary = string.Equals(app.Node.TryGetContext("SkipPrimaryStack") as string, "true", StringComparison.OrdinalIgnoreCase);
            var createReplica = string.Equals(app.Node.TryGetContext("CreateReplicaStack") as string, "true", StringComparison.OrdinalIgnoreCase);

            if (!skipPrimary)
            {
                new LiveEventServiceStack(app, "LiveEventServiceStack", new StackProps
                {
                    Env = new Amazon.CDK.Environment
                    {
                        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
                    }
                });
            }

            if (createReplica)
            {
                new LiveEventReplicaStack(app, "LiveEventReplicaStack", new StackProps
                {
                    Env = new Amazon.CDK.Environment
                    {
                        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
                    }
                });
            }

            app.Synth();
        }
    }
}
