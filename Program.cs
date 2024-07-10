using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.Core;
using Azure.ResourceManager.Network;
using Azure;




namespace Automation
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            string clientID = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            string tenantID = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            string subscriptionID = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");


            Console.Write("Enter Location for resources: ");
            string location = Console.ReadLine();

            string[] resourceGroups =
            [
                "rg-network-alz",
                "rg-identity-alz"
            ];

            string[] networks = [
                "vnet-alz-deployment-uks"
            ];

            string[] ranges = [
                "192.168.0.0/24"
            ];


            await apiCalls(tenantID, clientID, clientSecret, subscriptionID, location, resourceGroups, networks, ranges);

            Console.ReadLine();

        }


        public static async Task apiCalls(string tenantID, string clientID, string clientSecret, string subscriptionID, string location, string[] resourceGroups, string[] networks, string[] vnet_prefixes)
        {

            // Create a client credential using the environment variables
            ClientSecretCredential credential = new ClientSecretCredential(tenantID, clientID, clientSecret);

            ArmClient client = new ArmClient(credential, subscriptionID);

            string[] rgs = resourceGroups;
            string[] vnets = networks;
            string[] vnet_prefix = vnet_prefixes;

            string loc = location;

            var subscription = await client.GetDefaultSubscriptionAsync();
            

            foreach (string group in rgs)
            {
                Console.WriteLine($"Creating Resource Group {group} in {location} ");

                var rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, group, new ResourceGroupData(loc));
                var resourceGroup = rgLro.Value;
                var resourceGroupId = resourceGroup.Id;

                Console.WriteLine($"created RG {group} in {location} with the Resource ID: {resourceGroupId}");

            }

            int vnet_counter = 0;

            for (int i = 0; i < vnets.Length; i++)
            {
                string network = vnets[i];
                string prefix = vnet_prefix[i];

                Console.WriteLine($"Creating Virtual Network {network} with Range {prefix}");

                var virtualNetworkData = new VirtualNetworkData
                {
                    Location = location,
                    AddressPrefixes =
                    {
                        prefix
                    },
                    Subnets =
                    {
                        new SubnetData
                        {
                            Name = "snet-deployment",
                            AddressPrefix = "192.168.0.0/24"
                        }
                    }
                };

                ResourceGroupResource resourceGroup = subscription.GetResourceGroup(rgs[0]);
                var virtualNetworkContainer = resourceGroup.GetVirtualNetworks();

                var virtualNetwork = (await virtualNetworkContainer.CreateOrUpdateAsync(Azure.WaitUntil.Completed, network, virtualNetworkData)).Value;

                Console.WriteLine($"Created a virtual network called {virtualNetwork.Id}");
            }
        }
    }
}

