import os


base_dir = "AdiysoCase"
subdirs = {
    "HeartbeatMonitor": {
        "Program.cs": """\
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Heartbeat Monitor Başlatıldı...");

        var configPath = "config.json";
        if (!File.Exists(configPath))
        {
            Console.WriteLine("config.json bulunamadı.");
            return;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<Config>(json);

        if (config == null || config.Urls == null)
        {
            Console.WriteLine("Geçersiz konfigürasyon.");
            return;
        }

        using var httpClient = new HttpClient();

        while (true)
        {
            foreach (var url in config.Urls)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    var status = response.IsSuccessStatusCode ? "SUCCESS" : $"FAILED ({(int)response.StatusCode})";
                    Console.WriteLine($"{DateTime.Now}: {url} => {status}");
                    await File.AppendAllTextAsync("log.txt", $"{DateTime.Now}: {url} => {status}\n");

                    if (!response.IsSuccessStatusCode)
                    {
                        await File.AppendAllTextAsync("error.log", $"{DateTime.Now}: {url} failed with {(int)response.StatusCode}\n");
                        // Hataları buradan sunucuya post edebilirsin.
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now}: {url} => ERROR: {ex.Message}");
                    await File.AppendAllTextAsync("error.log", $"{DateTime.Now}: {url} => EXCEPTION: {ex.Message}\n");
                }
            }

            await Task.Delay(config.IntervalSeconds * 1000);
        }
    }
}

class Config
{
    public List<string>? Urls { get; set; }
    public int IntervalSeconds { get; set; }
}
""",
        "config.json": """\
{
  "Urls": [
    "http://localhost:5000/health",
    "https://example.com"
  ],
  "IntervalSeconds": 10
}
""",
        "HeartbeatMonitor.csproj": """\
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
"""
    },
    "TestServer": {
        "Program.cs": """\
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Server is healthy"));

app.Run("http://localhost:5000");
""",
        "TestServer.csproj": """\
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

</Project>
"""
    }
}

# Dosya oluşturma fonksiyonu
for subdir, files in subdirs.items():
    dir_path = os.path.join(base_dir, subdir)
    os.makedirs(dir_path, exist_ok=True)
    for filename, content in files.items():
        file_path = os.path.join(dir_path, filename)
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(content)

print("Proje sucs.")
