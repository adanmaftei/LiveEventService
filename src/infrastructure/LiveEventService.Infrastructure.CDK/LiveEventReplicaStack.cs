using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;

namespace LiveEventService.Infrastructure.CDK;

public class LiveEventReplicaStack : Stack
{
    internal LiveEventReplicaStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // Minimal VPC for the replica DB cluster
        var vpc = new Vpc(this, "ReplicaVPC", new VpcProps
        {
            MaxAzs = 2,
            NatGateways = 0,
            SubnetConfiguration = new[]
            {
                new SubnetConfiguration
                {
                    Name = "Public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24
                },
                new SubnetConfiguration
                {
                    Name = "Private",
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    CidrMask = 24
                }
            }
        });

        // Parameters
        var auroraGlobalId = new CfnParameter(this, "AuroraGlobalClusterId", new CfnParameterProps
        {
            Type = "String",
            Default = "liveevent-global-cluster",
            Description = "Identifier of the existing Aurora Global Database to which this regional replica cluster should attach"
        });

        // DB master credentials secret (regional)
        var databaseCredentialsSecret = new Secret(this, "ReplicaDBCredentials", new SecretProps
        {
            SecretName = "liveevent-db-credentials",
            GenerateSecretString = new SecretStringGenerator
            {
                ExcludeCharacters = "%@/\"",
                ExcludePunctuation = true,
                IncludeSpace = false,
                GenerateStringKey = "password",
                SecretStringTemplate = System.Text.Json.JsonSerializer.Serialize(new { username = "liveeventadmin" })
            }
        });

        // Regional replica cluster and attach to Global
        var replicaCluster = new DatabaseCluster(this, "AuroraReplicaCluster", new DatabaseClusterProps
        {
            Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
            {
                Version = AuroraPostgresEngineVersion.VER_15_3
            }),
            Writer = ClusterInstance.Provisioned("Writer", new ProvisionedClusterInstanceProps
            {
                InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.T4G, InstanceSize.MEDIUM),
                PubliclyAccessible = false
            }),
            Readers = new[]
            {
                ClusterInstance.Provisioned("Reader", new ProvisionedClusterInstanceProps
                {
                    InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.T4G, InstanceSize.MEDIUM),
                    PubliclyAccessible = false
                })
            },
            Credentials = Credentials.FromSecret(databaseCredentialsSecret),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
        });

        var cfnDbCluster = replicaCluster.Node.DefaultChild as CfnDBCluster;
        if (cfnDbCluster != null)
        {
            cfnDbCluster.GlobalClusterIdentifier = auroraGlobalId.ValueAsString;
        }

        _ = new CfnOutput(this, "ReplicaWriterEndpoint", new CfnOutputProps { Value = replicaCluster.ClusterEndpoint.Hostname });
        _ = new CfnOutput(this, "ReplicaReaderEndpoint", new CfnOutputProps { Value = replicaCluster.ClusterReadEndpoint.Hostname });
    }
}


