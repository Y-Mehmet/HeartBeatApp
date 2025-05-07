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
                    await File.AppendAllTextAsync("log.txt", $"{DateTime.Now}: {url} => {status}");

                    if (!response.IsSuccessStatusCode)
                    {
                        await File.AppendAllTextAsync("error.log", $"{DateTime.Now}: {url} failed with {(int)response.StatusCode}");
                        var message = $"{DateTime.Now}: {url} failed with {(int)response.StatusCode}";
                        await File.AppendAllTextAsync("error.log", message + "\n");

                        if (config.Email != null)
                             {
                                    await SendEmailAsync(config.Email, "Heartbeat Monitor Hatası", message);
                             }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now}: {url} => ERROR: {ex.Message}");
                    await File.AppendAllTextAsync("error.log", $"{DateTime.Now}: {url} => EXCEPTION: {ex.Message}");
                     var error = $"{DateTime.Now}: {url} => EXCEPTION: {ex.Message}";
                     Console.WriteLine(error);
                     await File.AppendAllTextAsync("error.log", error + "\n");

                     if (config.Email != null)
                     {
                         await SendEmailAsync(config.Email, "Heartbeat Monitor Exception", error);
                     }
                }
            }

            await Task.Delay(config.IntervalSeconds * 1000);
        }
    }

static async Task SendEmailAsync(EmailConfig emailConfig, string subject, string body)
{
    try
    {
        var smtpClient = new SmtpClient(emailConfig.SmtpServer)
        {
            Port = emailConfig.Port,
            Credentials = new NetworkCredential(emailConfig.Username, emailConfig.Password),
            EnableSsl = true,
        };

        var mail = new MailMessage(emailConfig.From, emailConfig.To, subject, body);
        await smtpClient.SendMailAsync(mail);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"E-posta gönderilemedi: {ex.Message}");
    }
}

}

class Config
{
    public List<string>? Urls { get; set; }
    public int IntervalSeconds { get; set; }
    public EmailConfig? Email { get; set; }
}
class EmailConfig
{
    public string? SmtpServer { get; set; }
    public int Port { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}