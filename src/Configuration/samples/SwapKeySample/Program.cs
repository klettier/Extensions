using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace SwapKeySample
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("MyCompany_Foo_Bar", "server=localhost;database=mydb;integrated security=true");

            IDictionary<string, string> keysToSwap =
                new Dictionary<string, string>
                {
                    { "connectionStrings:MyDb","MyCompany_Foo_Bar" },
                };

            var json = @"{
  ""connectionStrings"": {
    ""MyDb"": """"
  },
}";
            MemoryStream jsonStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonStream(jsonStream)
                .AddKeySwapper(c => c.AddEnvironmentVariables(), keysToSwap)
                .Build();

            var connectionStrings = configuration.GetSection("connectionStrings").Get<ConnectionStrings>();
        }

        class ConnectionStrings
        {
            public string MyDb { get; set; }
        }
    }
}
