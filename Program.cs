using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Net.Sockets;

string ipPrefix = "10.10.17.0/24";
string ipAddress = "10.10.17.1"; 
string networkName = "HNSDockerTestNetwork";
string networkEndpointName = "HNSDockerTestEndpoint";


var builder = WebApplication.CreateBuilder(args);

var ipEndPoint = IPAddress.Parse(ipAddress);
SetupHnsNetwork(ipAddress);

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.Listen(ipEndPoint, 5000);
});

var app = builder.Build();
app.MapGet("/", () => "OK");
app.Run();

void SetupHnsNetwork(string ipAddress)
{
    try
    {
        // PowerShell command to create a new HNS network
        string createNetworkCmd = $@"
            New-HNSNetwork -Name '{networkName}' -Type 'Internal' -AddressPrefix '{ipPrefix}' | Select ID | ForEach-Object {{
            New-HNSEndpoint -Name '{networkEndpointName}' -NetworkId $_.ID -IPAddress '{ipAddress}'
            }}
        ";

        // Run the command
        RunPowerShellCommand(createNetworkCmd);

        Console.WriteLine($"HNS network created and IP {ipAddress} allocated.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to create HNS network: {ex.Message}");
    }
}

void RunPowerShellCommand(string command)
{
    using (Process powerShell = new Process())
    {
        powerShell.StartInfo.FileName = "powershell.exe";
        powerShell.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"";
        powerShell.StartInfo.RedirectStandardOutput = true;
        powerShell.StartInfo.RedirectStandardError = true;
        powerShell.StartInfo.UseShellExecute = false;
        powerShell.StartInfo.CreateNoWindow = true;
        powerShell.Start();

        string output = powerShell.StandardOutput.ReadToEnd();
        string error = powerShell.StandardError.ReadToEnd();

        powerShell.WaitForExit();

        if (powerShell.ExitCode != 0)
        {
            throw new Exception($"PowerShell error: {error}");
        }

        Console.WriteLine(output);
    }
}