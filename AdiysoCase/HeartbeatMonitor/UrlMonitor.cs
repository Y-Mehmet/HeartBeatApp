using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;

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
                
                
              
                await AddLog( ""+response.StatusCode,status,"", _config.Url);


                if (!response.IsSuccessStatusCode)
                {
                    var message = $"{DateTime.Now}: {_config.Url} failed with {(int)response.StatusCode}";

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
                            {
                                
                                await AddLog("0000", "MAIL ERROR","Email could not be sent", _config.Url );
                            }else
                            {
                                 await AddLog("0000", "MAIL ERROR","Email could  be sent", _config.Url );
                            }
                        }
                        else
                           {
                            
                               await AddLog("0000", "MAIL ERROR","config.email is null", _config.Url );
                           }
                }
            }
            catch (Exception ex)
            {
               
                 await AddLog("1111", "EXCEPTION",ex.Message, _config.Url );

                if (_config.Email != null)
                {
                    

                    string subject = $"[Heartbeat Error] {_config.Url}";
                    string body = $@"
                    An error occurred while checking {_config.Url}.

                    Time: {DateTime.Now}
                    Error Message: {ex.Message}
                    ";
                  
                     
                            bool mailSent = await SendEmailAsync(subject, body);
                            if (!mailSent)
                            {
                               
                                  await AddLog("0000", "MAIL ERROR","Email could not be sent", _config.Url );
                            }
                     

                }
                else
                {
                      await AddLog("0000", "MAIL ERROR","config.email is null", _config.Url );
                }
            }

            await Task.Delay(_config.IntervalSeconds * 1000);
        }
    }

    private async Task<bool> SendEmailAsync(string subject, string body)
{
    try
    {
        var smtp = new SmtpClient(_config.Email.SmtpServer)
        {
            Port = _config.Email.Port,
            EnableSsl = _config.Email.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_config.Email.Username, _config.Email.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var mail = new MailMessage(_config.Email.From, _config.Email.To, subject, body);
        await smtp.SendMailAsync(mail);

        return true; // başarılı
    }
    catch (Exception ex)
    {
        
          await  AddLog("2222", "SMTP ERROR",$"Failed to send email for {_config.Url}- {ex.Message}",(string) _config.Url );
        return false;
    }
}
private async Task AddLog(string statusCode, string status, string msg, string url)
{
    string path = "log.txt";
    string statusLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{statusCode}] [{status}] [{msg}] [{url}]";
    Console.WriteLine(statusLine);
    await File.AppendAllTextAsync(path, statusLine + "\n");
}

}
