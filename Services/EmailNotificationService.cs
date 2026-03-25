using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace EPEAPI.Services;

public class EmailNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(HttpClient httpClient, ILogger<EmailNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendApprovalRequestEmailAsync(string toEmail, long requestId,string requestNumber, string requestorName, string requestDetails)
    {
        try
        {
            var trackUrl = "https://apps.fastlogistics.com.ph/epe/exemptions/approve?id=" + requestId;

            toEmail = "mccescario@fastgroup.biz"; // for testing

            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>New Exemption Request for Approval</h2>
                        <p>Dear Approver,</p>

                        <p>A new exemption request has been submitted and requires your approval.</p>

                        <div style='margin: 20px 0; padding: 15px; background-color: #f4f6f6; border-left: 5px solid #3498db;'>
                            <p style='margin: 5px 0;'><strong>Request Number:</strong> <span style='color: #2f6b4a;'>{requestNumber}</span></p>
                            <p style='margin: 5px 0;'><strong>Requested By:</strong> {requestorName}</p>
                            <p style='margin: 5px 0;'><strong>Request Details:</strong> {requestDetails}</p>
                        </div>

                        <p>Please review and take action on this request at your earliest convenience.</p>

                        <p style='margin: 20px 0;'>
                            <a href='{trackUrl}' 
                            style='display: inline-block; padding: 12px 30px; background-color: #3498db; color: white; 
                                    text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                Review Request
                            </a>
                        </p>

                        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;' />

                        <p style='color: #7f8c8d; font-size: 12px;'>
                            Best regards,<br>
                            EPE<br>
                            <a href='https://apps.fastlogistics.com.ph/epe' style='color: #7f8c8d;'>https://apps.fastlogistics.com.ph/epe</a>
                        </p>
                    </div>
                </body>
                </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fast.com.ph",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"Exemption Request for Approval - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
            var apiUrl = "https://apps.fastlogistics.com.ph/utilityapi/api/Emailer/send-email";

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Headers =
                {
                    { "Authorization", authHeader }
                },
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Approval request email notification sent successfully to {Email}", toEmail);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send approval request email notification to {Email}. Status: {StatusCode}. Response: {Response}",
                    toEmail, response.StatusCode, errorContent);
                throw new Exception($"Failed to send email. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send approval request email notification to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendStatusChangedEmailAsync(string toEmail, string requestNumber, string oldStatus, string newStatus, string changedBy)
    {
        try
        {
            var trackUrl = "https://apps.fastlogistics.com.ph/epe/exemptions/view?id=" + requestNumber;

            toEmail = "mccescario@fastgroup.biz"; // for testing

            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>Exemption Request Status Updated</h2>
                        <p>Dear User,</p>

                        <p>The status of your exemption request has been updated.</p>

                        <div style='margin: 20px 0; padding: 15px; background-color: #f4f6f6; border-left: 5px solid #e74c3c;'>
                            <p style='margin: 5px 0;'><strong>Request Number:</strong> <span style='color: #2f6b4a;'>{requestNumber}</span></p>
                            <p style='margin: 5px 0;'><strong>Previous Status:</strong> {oldStatus}</p>
                            <p style='margin: 5px 0;'><strong>New Status:</strong> <span style='color: #27ae60; font-weight: bold;'>{newStatus}</span></p>
                            <p style='margin: 5px 0;'><strong>Updated By:</strong> {changedBy}</p>
                        </div>

                        <p>You can track the progress of your request anytime using the button below:</p>

                        <p style='margin: 20px 0;'>
                            <a href='{trackUrl}' 
                            style='display: inline-block; padding: 12px 30px; background-color: #e74c3c; color: white; 
                                    text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                View Request Status
                            </a>
                        </p>

                        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;' />

                        <p style='color: #7f8c8d; font-size: 12px;'>
                            Best regards,<br>
                            EPE System<br>
                            <a href='https://apps.fastlogistics.com.ph/epe' style='color: #7f8c8d;'>https://apps.fastlogistics.com.ph/epe</a>
                        </p>
                    </div>
                </body>
                </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fastlogistic.ph",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"Exemption Request Status Updated - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
            var apiUrl = "https://apps.fastlogistics.com.ph/utilityapi/api/Emailer/send-email";

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Headers =
                {
                    { "Authorization", authHeader }
                },
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Status changed email notification sent successfully to {Email}", toEmail);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send status changed email notification to {Email}. Status: {StatusCode}. Response: {Response}",
                    toEmail, response.StatusCode, errorContent);
                throw new Exception($"Failed to send email. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status changed email notification to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendRequestCreatedEmailAsync(string toEmail, string requestNumber, string requestDetails)
    {
        try
        {
            var trackUrl = "https://apps.fastlogistics.com.ph/epe/exemptions/view?id=" + requestNumber;

            toEmail = "mccescario@fastgroup.biz"; // for testing

            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>Exemption Request Submitted</h2>
                        <p>Dear User,</p>

                        <p>Your exemption request has been successfully submitted.</p>
                        <p>Please keep the request number below for your records:</p>

                        <p style='margin: 20px 0; padding: 15px; background-color: #f4f6f6; border-left: 5px solid #2f6b4a; font-size: 18px; font-weight: bold;'>
                            Request Number: <span style='color: #2f6b4a;'>{requestNumber}</span>
                        </p>

                        <p>Our team will review your request shortly. You can track the progress anytime using the button below:</p>

                        <p style='margin: 20px 0;'>
                            <a href='{trackUrl}' 
                            style='display: inline-block; padding: 12px 30px; background-color: #2f6b4a; color: white; 
                                    text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                View Request Status
                            </a>
                        </p>

                        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;' />

                        <p style='color: #7f8c8d; font-size: 12px;'>
                            Best regards,<br>
                            EPE System<br>
                            <a href='https://apps.fastlogistics.com.ph/epe' style='color: #7f8c8d;'>https://apps.fastlogistics.com.ph/epe</a>
                        </p>
                    </div>
                </body>
                </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fastlogistic.ph",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"Exemption Request Submitted - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
            var apiUrl = "https://apps.fastlogistics.com.ph/utilityapi/api/Emailer/send-email";

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Headers =
                {
                    { "Authorization", authHeader }
                },
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Request created email notification sent successfully to {Email}", toEmail);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send request created email notification to {Email}. Status: {StatusCode}. Response: {Response}",
                    toEmail, response.StatusCode, errorContent);
                throw new Exception($"Failed to send email. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send request created email notification to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendNewMessageEmailAsync(string toEmail, string requestNumber, string messagePreview, string postedBy)
    {
        try
        {
            var trackUrl = "https://apps.fastlogistics.com.ph/epe/exemptions/view?id=" + requestNumber;

            toEmail = "mccescario@fastgroup.biz"; // for testing

            var truncatedMessage = messagePreview.Length > 100 ? messagePreview.Substring(0, 100) + "..." : messagePreview;

            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2c3e50;'>New Message on Your Exemption Request</h2>
                        <p>Dear User,</p>

                        <p>A new message has been posted on your exemption request.</p>

                        <div style='margin: 20px 0; padding: 15px; background-color: #f4f6f6; border-left: 5px solid #9b59b6;'>
                            <p style='margin: 5px 0;'><strong>Request Number:</strong> <span style='color: #2f6b4a;'>{requestNumber}</span></p>
                            <p style='margin: 5px 0;'><strong>Posted By:</strong> {postedBy}</p>
                            <p style='margin: 5px 0;'><strong>Message:</strong> {truncatedMessage}</p>
                        </div>

                        <p>You can view the full message and respond using the button below:</p>

                        <p style='margin: 20px 0;'>
                            <a href='{trackUrl}' 
                            style='display: inline-block; padding: 12px 30px; background-color: #9b59b6; color: white; 
                                    text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                View Request
                            </a>
                        </p>

                        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;' />

                        <p style='color: #7f8c8d; font-size: 12px;'>
                            Best regards,<br>
                            EPE System<br>
                            <a href='https://apps.fastlogistics.com.ph/epe' style='color: #7f8c8d;'>https://apps.fastlogistics.com.ph/epe</a>
                        </p>
                    </div>
                </body>
                </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fastlogistic.ph",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"New Message on Exemption Request - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
            var apiUrl = "https://apps.fastlogistics.com.ph/utilityapi/api/Emailer/send-email";

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Headers =
                {
                    { "Authorization", authHeader }
                },
                Content = JsonContent.Create(requestBody)
            };

            using var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("New message email notification sent successfully to {Email}", toEmail);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send new message email notification to {Email}. Status: {StatusCode}. Response: {Response}",
                    toEmail, response.StatusCode, errorContent);
                throw new Exception($"Failed to send email. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new message email notification to {Email}", toEmail);
            throw;
        }
    }
}
