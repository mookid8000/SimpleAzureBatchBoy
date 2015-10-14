using System;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace SimpleAzureBatchBoy.Task
{
    class Program
    {
        static void Main()
        {
            var appSettings = string.Join(Environment.NewLine, ConfigurationManager.AppSettings.AllKeys
                .Select(key => $"    {key}: {ConfigurationManager.AppSettings[key]}"));

            var environmentVariables = Environment.GetEnvironmentVariables();
            var environment = string.Join(Environment.NewLine, environmentVariables
                .Keys.Cast<string>().Select(key => $"    {key}: {environmentVariables[key]}"));

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

            Console.WriteLine($@"This is {assemblyName} running!

AppSettings:

{appSettings}

Environment:

{environment}

");
        }
    }
}
