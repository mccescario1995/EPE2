using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using EPEAPI.Models;

namespace EPEAPI.Services;

public class EmailNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly EpeDbContext _context;

    public EmailNotificationService(HttpClient httpClient, ILogger<EmailNotificationService> logger, EpeDbContext context)
    {
        _httpClient = httpClient;
        _logger = logger;
        _context = context;
    }

    private async Task CreateWebNotificationAsync(int userId, string title, string message, string type = "info", string? relatedUrl = null)
    {
        try
        {
            var notification = new EpeNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = 0,
                RelatedUrl = relatedUrl,
                CreatedDate = DateTime.UtcNow
            };

            _context.EpeNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create web notification for user {UserId}", userId);
            // Don't throw - we don't want to fail email sending if web notification fails
        }
    }

    public async Task SendApprovalRequestEmailAsync(string toEmail, long requestId, string requestNumber, string requestorName, string requestDetails, int userId)
    {
        try
        {
            var trackUrl = "https://apps.fastlogistics.com.ph/epe/exemptions/approve?id=" + requestId;

            toEmail = "mccescario@fastgroup.biz"; // for testing

            var emailBody = $@"
                <html>
                <body style='margin:0; padding:0;'>
                    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f5f5f5;'>
                        <tr>
                            <td align='center'>
                
                                <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0'
                                    style='background-color:#ffffff; margin:20px auto; padding:20px; font-family:Arial, Helvetica, sans-serif; color:#333; width:100%; max-width:600px;'>

                                    <tr>
                                        <td style='padding-bottom:30px;'>Dear Ma'am/Sir,</td>
                                    </tr>

                                    <tr>
                                        <td style='padding-bottom:15px;'>
                                            A new Executive Process Exemption Request requires your approval.
                                        </td>
                                    </tr>

                                    <!-- Details Box -->
                                    <tr>
                                        <td style='padding:15px; background-color:#f4f6f6; border-left:5px solid #2f75b5;'>
                                            <p><strong>Transaction Number:</strong> <span style='color:#2f75b5;'>{{requestNumber}}</span></p>
                                            <p><strong>Requested By:</strong> {{requestorName}}</p>
                                            <p><strong>Transaction Details:</strong> {{requestDetails}}</p>
                                        </td>
                                    </tr>

                                    <tr><td height='20'></td></tr>

                                    <tr>
                                        <td>Please review and take action at your earliest convenience.</td>
                                    </tr>

                                    <tr><td height='20'></td></tr>

                                    <!-- Button -->
                                    <tr>
                                        <td>

                                            <!--[if mso]>
                                            <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' 
                                                href='{{trackUrl}}' 
                                                style='height:45px;v-text-anchor:middle;width:220px;' 
                                                arcsize='10%' stroke='f' fillcolor='#2f75b5'>
                                                <center style='color:#ffffff;font-size:14px;font-weight:bold;'>
                                                    Review Request
                                                </center>
                                            </v:roundrect>
                                            <![endif]-->

                                            <!--[if !mso]><!-- -->
                                            <a href='{{trackUrl}}' 
                                                style='display:inline-block; background-color:#2f75b5; color:#fff;
                                                padding:12px 30px; text-decoration:none; border-radius:5px; font-weight:bold;'>
                                                Review Request
                                            </a>
                                            <!--<![endif]-->

                                        </td>
                                    </tr>

                                    <!-- Fallback -->
                                    <tr>
                                        <td style='padding-top:10px; font-size:12px;'>
                                            If the button doesn't work,
                                            <a href='{{trackUrl}}' style='color:#2f75b5;'>click here</a>.
                                        </td>
                                    </tr>

                                    <tr><td height='30'></td></tr>

                                    <!-- Signature -->
                                    <tr>
                                        <td style='font-family:""Courier New""; font-size:12px; color:#7f8c8d;'>
                                            --<br>
                                            Thank you,<br><br>
                                            M.I.R.A.<br>
                                            <i>FSC - Information Technology</i>
                                        </td>
                                    </tr>

                                </table>
                            </td>
                        </tr>
                    </table>
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
                // Create web notification for the approver
                await CreateWebNotificationAsync(
                    userId,
                    "New Approval Request",
                    $"A new exemption request ({requestNumber}) requires your approval",
                    "info",
                    $"/exemptions/approve?id={requestId}"
                );
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

    private (string Primary, string Border, string LightBg, string Title) GetEmailTheme(string status)
    {
        return status.ToLower() switch
        {
            "Approved" => ("#16A34A", "#16A34A", "#ECFDF5", "Approved"),
            "Disapproved" => ("#DC2626", "#DC2626", "#FEF2F2", "Disapproved"),
            "Pending" => ("#F59E0B", "#F59E0B", "#FFFBEB", "Pending"),
            _ => ("#2563EB", "#2563EB", "#F3F4F6", "Submitted")
        };
    }

    public async Task SendStatusChangedEmailAsync(string toEmail, long requestId, string requestNumber, string oldStatus, string newStatus, string changedBy, int userId)
    {
        try
        {
            var trackUrl = $"https://apps.fastlogistics.com.ph/epe/exemptions/view?id={requestId}";

            toEmail = "mccescario@fastgroup.biz"; // testing

            var theme = GetEmailTheme(newStatus);

            var emailBody = $@"
                <html>
                <body style='margin:0; padding:0; background-color:#F3F4F6;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                <tr>
                <td align='center'>

                <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0'
                    style='background-color:#ffffff; margin:20px auto; padding:20px;
                    font-family:Arial, Helvetica, sans-serif; color:#1F2937; width:100%; max-width:600px;'>

                    <!-- HEADER -->
                    <tr>
                        <td style='padding-bottom:20px; font-size:14px;'>
                            Dear Ma'am/Sir,
                        </td>
                    </tr>

                    <tr>
                        <td style='padding-bottom:20px; font-size:14px;'>
                            Your exemption request status has been updated.
                        </td>
                    </tr>

                    <!-- STATUS BANNER -->
                    <tr>
                        <td style='padding:12px 15px; background-color:{theme.LightBg};
                            border-left:5px solid {theme.Border};
                            font-size:16px; font-weight:bold;'>
                            Status Updated:
                            <span style='color:{theme.Primary};'>{newStatus}</span>
                        </td>
                    </tr>

                    <tr><td height='20'></td></tr>

                    <!-- TRANSACTION BOX -->
                    <tr>
                        <td style='padding:15px; background-color:#F9FAFB;
                            border-left:5px solid #2563EB;
                            font-size:16px; font-weight:bold;'>

                            Transaction Number:
                            <span style='color:#2563EB;'>{requestNumber}</span>

                        </td>
                    </tr>

                    <tr><td height='20'></td></tr>

                    <!-- INFO -->
                    <tr>
                        <td style='font-size:14px; line-height:1.5;'>
                            This request has been changed from
                            <b>{oldStatus}</b> to <b>{newStatus}</b> by {changedBy}.
                            You may track the latest progress using the button below.
                        </td>
                    </tr>

                    <tr><td height='25'></td></tr>

                    <!-- BUTTON -->
                    <tr>
                        <td>

                            <!--[if mso]>
                            <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml'
                                href='{trackUrl}'
                                style='height:45px;v-text-anchor:middle;width:220px;'
                                arcsize='10%' stroke='f' fillcolor='{theme.Primary}'>
                                <center style='color:#ffffff;font-size:14px;font-weight:bold;'>
                                    View Request Status
                                </center>
                            </v:roundrect>
                            <![endif]-->

                            <!--[if !mso]><!-- -->
                            <a href='{trackUrl}'
                                style='display:inline-block;
                                background-color:{theme.Primary};
                                color:#fff;
                                padding:12px 30px;
                                text-decoration:none;
                                border-radius:5px;
                                font-weight:bold;'>
                                View Request Status
                            </a>
                            <!--<![endif]-->

                        </td>
                    </tr>

                    <tr>
                        <td style='padding-top:10px; font-size:12px; color:#6B7280;'>
                            If the button doesn't work,
                            <a href='{trackUrl}' style='color:{theme.Primary};'>click here</a>.
                        </td>
                    </tr>

                    <tr><td height='30'></td></tr>

                    <!-- FOOTER -->
                    <tr>
                        <td style='font-family:""Courier New""; font-size:12px; color:#6B7280;'>
                            --<br>
                            Thank you,<br><br>
                            M.I.R.A.<br>
                            <i>FSC - Information Technology</i>
                        </td>
                    </tr>

                </table>

                </td>
                </tr>
                </table>
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
                // Create web notification for the user
                await CreateWebNotificationAsync(
                    userId,
                    "Request Status Updated",
                    $"Your exemption request ({requestNumber}) status changed from {oldStatus} to {newStatus}",
                    "info",
                    $"/exemptions/view?id={requestId}"
                );
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

    public async Task SendRequestCreatedEmailAsync(string toEmail, long requestId, string requestNumber, string requestDetails, int userId)
    {
        try
        {
            var trackUrl = "https://apps.fastlogistics.com.ph/epe/exemptions/view?id=" + requestId;

            toEmail = "mccescario@fastgroup.biz"; // for testing

            var emailBody = $@"
                <html>
                <body style='margin:0; padding:0;'>
                    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color:#f5f5f5;'>
                        <tr>
                            <td align='center'>

                                <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0'
                                    style='background-color:#ffffff; margin:20px auto; padding:20px; font-family:Arial, Helvetica, sans-serif; color:#333; width:100%; max-width:600px;'>

                                    <tr>
                                        <td style='padding-bottom:30px;'>Dear Ma'am/Sir,</td>
                                    </tr>

                                    <tr>
                                        <td style='padding-bottom:15px;'>
                                            The status of your exemption request has been updated.
                                        </td>
                                    </tr>

                                    <!-- Status Box -->
                                    <tr>
                                        <td style='padding:15px; background-color:#f4f6f6; border-left:5px solid #2f75b5;'>
                                            <p><strong>Transaction Number:</strong> <span style='color:#2f75b5;'>{{requestNumber}}</span></p>
                                            <p><strong>Previous Status:</strong> {{oldStatus}}</p>
                                            <p><strong>New Status:</strong> <strong style='color:#27ae60;'>{{newStatus}}</strong></p>
                                            <p><strong>Updated By:</strong> {{changedBy}}</p>
                                        </td>
                                    </tr>

                                    <tr><td height='20'></td></tr>

                                    <tr>
                                        <td>You can track your request using the button below:</td>
                                    </tr>

                                    <tr><td height='20'></td></tr>

                                    <!-- Button -->
                                    <tr>
                                        <td>

                                            <!--[if mso]>
                                            <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' 
                                                href='{{trackUrl}}' 
                                                style='height:45px;v-text-anchor:middle;width:220px;' 
                                                arcsize='10%' stroke='f' fillcolor='#2f75b5'>
                                                <center style='color:#ffffff;font-size:14px;font-weight:bold;'>
                                                    View Request Status
                                                </center>
                                            </v:roundrect>
                                            <![endif]-->

                                            <!--[if !mso]><!-- -->
                                            <a href='{{trackUrl}}' 
                                                style='display:inline-block; background-color:#2f75b5; color:#fff;
                                                padding:12px 30px; text-decoration:none; border-radius:5px; font-weight:bold;'>
                                                View Request Status
                                            </a>
                                            <!--<![endif]-->

                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='padding-top:10px; font-size:12px;'>
                                            If the button doesn't work,
                                            <a href='{{trackUrl}}' style='color:#2f75b5;'>click here</a>.
                                        </td>
                                    </tr>

                                    <tr><td height='30'></td></tr>

                                    <tr>
                                        <td style='font-family:""Courier New""; font-size:12px; color:#7f8c8d;'>
                                            --<br>
                                            Thank you,<br><br>
                                            M.I.R.A.<br>
                                            <i>FSC - Information Technology</i>
                                        </td>
                                    </tr>

                                </table>

                            </td>
                        </tr>
                    </table>
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
                // Create web notification for the user
                await CreateWebNotificationAsync(
                    userId,
                    "Request Submitted",
                    $"Your exemption request ({requestNumber}) has been successfully submitted",
                    "success",
                    $"/exemptions/view?id={requestId}"
                );
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

    //No Use yet, for future use when messaging feature is implemented
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
                        <h2 style='color: #2c3e50;'>New Message on Your Executive Process Exemption Request</h2>
                        <p>Dear User,</p>

                        <p>A new message has been posted on your exemption request.</p>

                        <div style='margin: 20px 0; padding: 15px; background-color: #f4f6f6; border-left: 5px solid #9b59b6;'>
                            <p style='margin: 5px 0;'><strong>Transaction Number:</strong> <span style='color: #2f6b4a;'>{requestNumber}</span></p>
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
