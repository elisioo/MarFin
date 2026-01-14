using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MarFin_Final.Database.Services
{
    public class SendGridSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "MarFin CRM";
        public string BaseUrl { get; set; } = "https://api.sendgrid.com/v3";
    }

    public class EmailService
    {
        private readonly SendGridSettings _settings;
        private readonly HttpClient? _httpClient;
        private readonly bool _isConfigured;

        public EmailService(IConfiguration configuration)
        {
            _settings = new SendGridSettings();
            configuration.GetSection("SendGrid").Bind(_settings);

            _isConfigured =
                !string.IsNullOrWhiteSpace(_settings.ApiKey) &&
                !string.IsNullOrWhiteSpace(_settings.FromAddress);

            if (_isConfigured)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                _httpClient = client;
            }
        }

        private string GetSendGridUrl()
        {
            var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl) ? "https://api.sendgrid.com/v3" : _settings.BaseUrl;
            return $"{baseUrl.TrimEnd('/')}/mail/send";
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentException("Recipient email is required", nameof(to));
            }

            // Simulation mode: no Mailgun configuration present
            if (!_isConfigured || _httpClient == null)
            {
                // In simulation mode we simply no-op so the app can still be demonstrated
                await Task.CompletedTask;
                return;
            }

            var url = GetSendGridUrl();

            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[]
                        {
                            new { email = to }
                        }
                    }
                },
                from = new
                {
                    email = _settings.FromAddress,
                    name = _settings.FromName
                },
                subject = subject ?? string.Empty,
                content = new[]
                {
                    new
                    {
                        type = "text/html",
                        value = htmlBody ?? string.Empty
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"SendGrid send failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
            }
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var emails = recipients?
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (emails.Count == 0)
            {
                return;
            }

            // Simulation mode: no Mailgun configuration present
            if (!_isConfigured || _httpClient == null)
            {
                // In simulation mode we simply no-op so the app can still be demonstrated
                await Task.CompletedTask;
                return;
            }

            foreach (var email in emails)
            {
                await SendEmailAsync(email, subject, htmlBody, cancellationToken);
            }
        }
    }
}
