using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;

class Program
{
    

    static async Task Main(string[] args)
    {
        var json = File.ReadAllText("config.json");
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        List<Task> monitorTasks = new();

        foreach (var urlConfig in config.UrlMonitors)
        {
            var monitor = new UrlMonitor(urlConfig);
            monitorTasks.Add(monitor.StartAsync());
        }

        await Task.WhenAll(monitorTasks);
    }

}
public class EmailConfig
{
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public class UrlMonitorConfig
{
    public string Url { get; set; }
    public int IntervalSeconds { get; set; }
    public EmailConfig Email { get; set; }
}

public class AppConfig
{
    public List<UrlMonitorConfig> UrlMonitors { get; set; }
}