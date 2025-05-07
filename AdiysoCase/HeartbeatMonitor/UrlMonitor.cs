using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class UrlMonitor
{
    private readonly UrlMonitorConfig _config;
    private readonly HttpClient _httpClient = new();

    public UrlMonitor(UrlMonitorConfig config)
    {
        _config = config;
    }

    public async Task StartAsync()
    {
        while (true)
        {
            try
            {
                var response = await _httpClient.GetAsync(_config.Url);
                var status = response.IsSuccessStatusCode ? "SUCCESS" : $"FAILED ({(int)response.StatusCode})";

                await AddLog($"{(int)response.StatusCode}", status, "heartbeat check success", _config.Url);

                if (!response.IsSuccessStatusCode)
                {
                    string message = $"{DateTime.Now}: {_config.Url} failed with {(int)response.StatusCode}";

                    if (_config.Email != null)
                    {
                        string subject = $"[Heartbeat Error] {_config.Url}";
                        string body = $@"
                                        An error occurred while checking {_config.Url}.

                                        Time: {DateTime.Now}
                                        Error Message: {message}
                                        ";

                        bool mailSent = await SendEmailAsync(subject, body);

                        if (!mailSent)
                            await AddLog("0000", "MAIL ERROR", "Email could not be sent", _config.Url);
                        else
                            await AddLog("0000", "MAIL SUCCESS", "Email was sent", _config.Url);
                    }
                    else
                    {
                        await AddLog("0000", "MAIL SKIPPED", "config.email is null", _config.Url);
                    }
                }
            }
            catch (Exception ex)
            {
                await AddLog("1111", "EXCEPTION", ex.Message, _config.Url);

                if (_config.Email != null)
                {
                    string subject = $"[Heartbeat Error] {_config.Url}";
                    string body = $@"
                    An exception occurred while checking {_config.Url}.

                    Time: {DateTime.Now}
                    Exception: {ex.Message}
                    ";

                    bool mailSent = await SendEmailAsync(subject, body);

                    if (!mailSent)
                        await AddLog("0000", "MAIL ERROR", "Email could not be sent", _config.Url);
                    else
                        await AddLog("0000", "MAIL SUCCESS", "Email was sent", _config.Url);
                }
                else
                {
                    await AddLog("0000", "MAIL SKIPPED", "config.email is null", _config.Url);
                }
            }

            await Task.Delay(_config.IntervalSeconds * 1000);
        }
    }

    private async Task<bool> SendEmailAsync(string subject, string body)
{
    try
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config.Email.From));
        email.To.Add(MailboxAddress.Parse(_config.Email.To));
        email.Subject = subject;
        email.Body = new TextPart("plain") { Text = body };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _config.Email.SmtpServer,
            _config.Email.Port,
            _config.Email.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

        await smtp.AuthenticateAsync(_config.Email.Username, _config.Email.Password); // 💥 burada şifre hatası olabilir

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);

        return true;
    }
    catch (AuthenticationException ex)
    {
        await AddLog("2222", "AUTH ERROR", $"Authentication failed: {ex.Message}", _config.Url);
    }
    catch (SmtpCommandException ex)
    {
        await AddLog("2222", "SMTP CMD ERROR", $"SMTP command error: {ex.Message} | Code: {ex.StatusCode}", _config.Url);
    }
    catch (SmtpProtocolException ex)
    {
        await AddLog("2222", "SMTP PROTOCOL ERROR", $"SMTP protocol error: {ex.Message}", _config.Url);
    }
    catch (Exception ex)
    {
        await AddLog("2222", "SMTP ERROR", $"General error: {ex.Message}", _config.Url);
    }

    return false;
}


    private async Task AddLog(string statusCode, string status, string msg, string url)
{
    string path = "log.txt";
    string statusLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{statusCode}] [{status}] {msg} [{url}]";

    try
    {
        Console.WriteLine(statusLine);
        await File.AppendAllTextAsync(path, statusLine + Environment.NewLine);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGGING ERROR] {ex.Message}");
    }
}

}
