using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using EPEAPI.Models;
using Microsoft.Extensions.Options;

namespace EPEAPI.Services;

public class EmailNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly EpeDbContext _context;
    private readonly EmailApprovalTokenService _tokenService;
    private readonly UrlBaseSettings _urlBaseSettings;

    public EmailNotificationService(HttpClient httpClient, ILogger<EmailNotificationService> logger, EpeDbContext context, EmailApprovalTokenService tokenService, IOptions<UrlBaseSettings> urlBaseSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _urlBaseSettings = urlBaseSettings.Value;
        _context = context;
        _tokenService = tokenService;

    }

    public async Task CreateWebNotificationAsync(int userId, string title, string message, string type = "info", string? relatedUrl = null)
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
                CreatedDate = DateTime.UtcNow.AddHours(8)
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

    public async Task SendApprovalRequestEmailAsync(string toEmail, long requestId, string requestNumber, string requestorName, string requestDetails, int userId, long transApproverId)
    {
        
        try
        {
            var (approveToken, disapproveToken) = await _tokenService.GenerateTokensAsync(requestId, transApproverId);

            var apiBase = _urlBaseSettings.Api;
            var viewBase = _urlBaseSettings.View;
            var respondUrl = $"{viewBase}/#/main/pending-for-approval/approve?id={requestId}&token={approveToken}";
            var viewUrl = $"{viewBase}/#/main/pending-for-approval/view?id={requestId}&token={approveToken}";

            
            var emailBody = $@"
                    <html>
                    <body style='margin:0;padding:0;font-family:Arial,Helvetica,sans-serif;'>
                      <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5f5;'>
                        <tr><td align='center'>
                          <table width='600' cellpadding='0' cellspacing='0'
                            style='background:#fff;margin:20px auto;padding:30px;max-width:600px;'>

                            <tr><td style='padding-bottom:20px;'>Dear Ma'am/Sir,</td></tr>
                            <tr><td style='padding-bottom:15px;'>
                              A new Executive Process Exemption Request requires your approval.
                            </td></tr>

                            <!-- Details box -->
                              <tr><td style='padding:15px;background:#f4f6f6;border-left:5px solid #2f75b5;margin-bottom:20px;'>
                                <p><strong>Transaction #:</strong> <span style='color:#2f75b5;'>{requestNumber}</span></p>
                                <p><strong>Requested By:</strong> {requestorName}</p>
                                <p><strong>Details:</strong> {requestDetails}</p>
                              </td></tr>

                            <tr><td height='25'></td></tr>

                            <!-- ONE-CLICK ACTION BUTTONS -->
                            <tr><td>
                              <p style='font-weight:bold;margin-bottom:12px;'>Take action directly from this email:</p>
                              <table cellpadding='0' cellspacing='0'>
                                <tr>
                                  <td style='padding-right:10px;'>
                                    <!--[if mso]>
                                    <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' href='{respondUrl}'
                                      style='height:44px;v-text-anchor:middle;width:140px;' arcsize='10%' fillcolor='#16a34a' stroke='f'>
                                      <center style='color:#fff;font-size:14px;font-weight:bold;'>Respond</center>
                                    </v:roundrect>
                                    <![endif]-->
                                    <!--[if !mso]><!-->
                                    <a href='{respondUrl}'
                                      style='display:inline-block;background:#16a34a;color:#fff;padding:12px 28px;
                                             text-decoration:none;border-radius:6px;font-weight:bold;font-size:14px;'>
                                      Respond
                                    </a>
                                    <!--<![endif]-->
                                  </td>
                                  <td>
                                    <a href='{viewUrl}'
                                      style='display:inline-block;background:#2f75b5;color:#fff;padding:12px 28px;
                                             text-decoration:none;border-radius:6px;font-weight:bold;font-size:14px;'>
                                      View Details
                                    </a>
                                  </td>
                                </tr>
                              </table>
                            </td></tr>

                            <tr><td height='15'></td></tr>
                            <tr><td style='font-size:12px;color:#6b7280;'>
                              These links will expire in 72 hours. After expiration,
                            </td></tr>

                            <tr><td height='30'></td></tr>
                            <tr><td style='font-family:Courier New;font-size:12px;color:#7f8c8d;'>
                              --<br>Thank you,<br><br>M.I.R.A.<br><i>FSC - Information Technology</i>
                            </td></tr>
                          </table>
                        </td></tr>
                      </table>
                    </body>
                    </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fastgroup.biz",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"Exemption Request for Approval - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
            var emailUrl = _urlBaseSettings.Emailer;

            var request = new HttpRequestMessage(HttpMethod.Post, emailUrl)
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
                    $"/main/pending-for-approval/approve?id={requestId}"
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
            "approved" => ("#16A34A", "#16A34A", "#ECFDF5", "Approved"),
            "disapproved" => ("#DC2626", "#DC2626", "#FEF2F2", "Disapproved"),
            "pending" => ("#F59E0B", "#F59E0B", "#FFFBEB", "Pending"),
            _ => ("#2563EB", "#2563EB", "#F3F4F6", "Submitted")
        };
    }

    public async Task SendStatusChangedEmailAsync(string toEmail, long requestId, string requestNumber, string oldStatus, string newStatus, string changedBy, int userId)
    {
        try
        {
            var trackUrl = $"https://apps.fastlogistics.com.ph/epe2dev/#/main/pending-for-approval/view?id={requestId}";
            //  var trackUrl = $"http://localhost:3000/epe2dev/main/pending-for-approval/view?id={requestId}";

            // toEmail = "mccescario@fastgroup.biz"; // for testing

            var theme = GetEmailTheme(newStatus);

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
                                        <td style='padding:15px; background-color:{theme.LightBg}; border-left:5px solid {theme.Border};'>
                                            <p><strong>Transaction Number:</strong> <span style='color:#2f75b5;'>{requestNumber}</span></p>
                                            <p><strong>Previous Status:</strong> {oldStatus}</p>
                                            <p><strong>New Status:</strong> <strong style='color:{theme.Primary};'>{newStatus}</strong></p>
                                            <p><strong>Updated By:</strong> {changedBy}</p>
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
                                                href='{trackUrl}' 
                                                style='height:45px;v-text-anchor:middle;width:220px;' 
                                                arcsize='10%' stroke='f' fillcolor='#2f75b5'>
                                                <center style='color:#ffffff;font-size:14px;font-weight:bold;'>
                                                    View Request Status
                                                </center>
                                            </v:roundrect>
                                            <![endif]-->

                                            <!--[if !mso]><!-- -->
                                            <a href='{trackUrl}' 
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
                                            <a href='{trackUrl}' style='color:#2f75b5;'>click here</a>.
                                        </td>
                                    </tr>

                                    <tr><td height='30'></td></tr>

                                    <tr>
                                        <td style='font-family:Courier New; font-size:12px; color:#7f8c8d;'>
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
            var emailUrl = _urlBaseSettings.Emailer;

            var request = new HttpRequestMessage(HttpMethod.Post, emailUrl)
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
                    $"/main/pending-for-approval/view?id={requestId}"
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
            var trackUrl = "https://apps.fastlogistics.com.ph/epe2dev/#/main/pending-for-approval/view?id=" + requestId;
            // var trackUrl = "http://localhost:3000/epe2dev/main/pending-for-approval/view?id=" + requestId;
        

            // toEmail = "mcccescario@fastgroup.biz"; // for testing

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
                                        <td>Your exemption request has been successfully submitted.</td>
                                    </tr>

                                    <tr>
                                        <td style='padding-bottom:25px;'>
                                            Please keep the transaction number below for your records:
                                        </td>
                                    </tr>

                                    <!-- Transaction Box -->
                                    <tr>
                                        <td style='padding:15px; background-color:#f4f6f6; border-left:5px solid #2f75b5; font-size:18px; font-weight:bold;'>
                                            Transaction Number: 
                                            <span style='color:#2f75b5;'>{requestNumber}</span>
                                        </td>
                                    </tr>

                                    <tr><td height='20'></td></tr>

                                    <tr>
                                        <td>
                                            Our team will review your request shortly. You may track the progress using the button below:
                                        </td>
                                    </tr>

                                    <tr><td height='20'></td></tr>

                                    <!-- Button -->
                                    <tr>
                                        <td>

                                            <!--[if mso]>
                                            <v:roundrect xmlns:v='urn:schemas-microsoft-com:vml' 
                                                href='{trackUrl}' 
                                                style='height:45px;v-text-anchor:middle;width:220px;' 
                                                arcsize='10%' stroke='f' fillcolor='#2f75b5'>
                                                <center style='color:#ffffff;font-size:14px;font-weight:bold;'>
                                                    View Request Status
                                                </center>
                                            </v:roundrect>
                                            <![endif]-->

                                            <!--[if !mso]><!-- -->
                                            <a href='{trackUrl}' 
                                                style='display:inline-block; background-color:#2f75b5; color:#fff;
                                                padding:12px 30px; text-decoration:none; border-radius:5px; font-weight:bold;'>
                                                View Request Status
                                            </a>
                                            <!--<![endif]-->

                                        </td>
                                    </tr>

                                    <tr>cd..
                                        <td style='padding-top:10px; font-size:12px;'>
                                            If the button doesn't work,
                                            <a href='{trackUrl}' style='color:#2f75b5;'>click here</a>.
                                        </td>
                                    </tr>

                                    <tr><td height='30'></td></tr>

                                    <tr>
                                        <td style='font-family:Courier New; font-size:12px; color:#7f8c8d;'>
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
                senderDisplayName = "epe.noreply@fastgroup.biz",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"Exemption Request Submitted - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
            var emailUrl = _urlBaseSettings.Emailer;

            var request = new HttpRequestMessage(HttpMethod.Post, emailUrl)
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
                    $"/main/pending-for-approval/view?id={requestId}"
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
            var trackUrl = "https://apps.fastlogistics.com.ph/epe2dev/#/main/pending-for-approval/view?id=" + requestNumber;
            //  var trackUrl = "http://localhost:3000/epe2dev/main/pending-for-approval/view?id=" + requestNumber;

            // toEmail = "mccescario@fastgroup.biz"; // for testing

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
                            <a href='https://apps.fastlogistics.com.ph/epe2dev' style='color: #7f8c8d;'>https://apps.fastlogistics.com.ph/epe2dev</a>
                        </p>
                    </div>
                </body>
                </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fastgroup.biz",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"New Message on Exemption Request - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
            var emailUrl = _urlBaseSettings.Emailer;

            var request = new HttpRequestMessage(HttpMethod.Post, emailUrl)
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

    public async Task SendMainApproverReminderEmailAsync(
    string toEmail,
    string approverName,
    long requestId,
    string requestNumber,
    string requestorName,
    string requestDetails,
    int userId,
    long transApproverId)
    {
        try
        {
            var (approveToken, _) = await _tokenService.GenerateTokensAsync(requestId, transApproverId);

            var viewBase = _urlBaseSettings.View;
            var viewUrl = $"{viewBase}/#/main/pending-for-approval/view?id={requestId}&token={approveToken}";
            var respondUrl = $"{viewBase}/#/main/pending-for-approval/approve?id={requestId}&token={approveToken}";

            var emailBody = $@"
            <html>
            <body style='margin:0;padding:0;font-family:Arial,Helvetica,sans-serif;'>
              <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5f5;'>
                <tr><td align='center'>
                  <table width='600' cellpadding='0' cellspacing='0'
                    style='background:#fff;margin:20px auto;padding:30px;max-width:600px;'>

                    <tr><td style='padding-bottom:20px;'>Dear {approverName},</td></tr>

                    <tr><td style='padding-bottom:15px;'>
                      This is a reminder that the following exemption request is awaiting your approval.
                    </td></tr>

                    <tr><td style='padding:15px;background:#f4f6f6;border-left:5px solid #2f75b5;margin-bottom:20px;'>
                      <p><strong>Transaction #:</strong> <span style='color:#2f75b5;'>{requestNumber}</span></p>
                      <p><strong>Requested By:</strong> {requestorName}</p>
                      <p><strong>Details:</strong> {requestDetails}</p>
                    </td></tr>

                    <tr><td height='20'></td></tr>

                    <tr><td>
                      <p style='font-weight:bold;margin-bottom:12px;'>Action required � please respond:</p>
                      <table cellpadding='0' cellspacing='0'>
                        <tr>
                          <td style='padding-right:10px;'>
                            <a href='{respondUrl}'
                              style='display:inline-block;background:#16a34a;color:#fff;padding:12px 28px;
                                     text-decoration:none;border-radius:6px;font-weight:bold;font-size:14px;'>
                              Respond Now
                            </a>
                          </td>
                          <td>
                            <a href='{viewUrl}'
                              style='display:inline-block;background:#2f75b5;color:#fff;padding:12px 28px;
                                     text-decoration:none;border-radius:6px;font-weight:bold;font-size:14px;'>
                              View Details
                            </a>
                          </td>
                        </tr>
                      </table>
                    </td></tr>

                    <tr><td height='20'></td></tr>
                    <tr><td style='font-size:12px;color:#6b7280;'>
                      You are receiving this because you are the designated approver for this request.
                    </td></tr>

                    <tr><td height='30'></td></tr>
                    <tr><td style='font-family:Courier New;font-size:12px;color:#7f8c8d;'>
                      --<br>Thank you,<br><br>M.I.R.A.<br><i>FSC - Information Technology</i>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fastgroup.biz",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"[Reminder] Approval Required - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            await SendEmailAsync(requestBody);

            await CreateWebNotificationAsync(
                userId,
                "Approval Reminder",
                $"Reminder: Exemption request ({requestNumber}) is awaiting your approval",
                "warning",
                $"/main/pending-for-approval/approve?id={requestId}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send main approver reminder to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendProxyApproverReminderEmailAsync(
        string toEmail,
        string proxyName,
        string mainApproverName,
        long requestId,
        string requestNumber,
        string requestorName,
        string requestDetails,
        int userId,
        long transApproverId)
    {
        try
        {
            var (approveToken, _) = await _tokenService.GenerateTokensAsync(requestId, transApproverId);

            var viewBase = _urlBaseSettings.View;
            var viewUrl = $"{viewBase}/#/main/pending-for-approval/view?id={requestId}&token={approveToken}";
            var respondUrl = $"{viewBase}/#/main/pending-for-approval/approve?id={requestId}&token={approveToken}";

            var emailBody = $@"
            <html>
            <body style='margin:0;padding:0;font-family:Arial,Helvetica,sans-serif;'>
              <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5f5;'>
                <tr><td align='center'>
                  <table width='600' cellpadding='0' cellspacing='0'
                    style='background:#fff;margin:20px auto;padding:30px;max-width:600px;'>

                    <tr><td style='padding-bottom:20px;'>Dear {proxyName},</td></tr>

                    <tr><td style='padding-bottom:15px;'>
                      You have been designated as a <strong>proxy approver</strong> for the following exemption request.
                      The primary approver <strong>{mainApproverName}</strong> is also being notified.
                      Either of you may respond on behalf of this approval group.
                    </td></tr>

                    <tr><td style='padding:15px;background:#fff8e1;border-left:5px solid #f59e0b;margin-bottom:20px;'>
                      <p><strong>Transaction #:</strong> <span style='color:#2f75b5;'>{requestNumber}</span></p>
                      <p><strong>Requested By:</strong> {requestorName}</p>
                      <p><strong>Details:</strong> {requestDetails}</p>
                      <p><strong>Primary Approver:</strong> {mainApproverName}</p>
                    </td></tr>

                    <tr><td height='20'></td></tr>

                    <tr><td>
                      <p style='font-weight:bold;margin-bottom:12px;'>You may respond as proxy:</p>
                      <table cellpadding='0' cellspacing='0'>
                        <tr>
                          <td style='padding-right:10px;'>
                            <a href='{respondUrl}'
                              style='display:inline-block;background:#f59e0b;color:#fff;padding:12px 28px;
                                     text-decoration:none;border-radius:6px;font-weight:bold;font-size:14px;'>
                              Respond as Proxy
                            </a>
                          </td>
                          <td>
                            <a href='{viewUrl}'
                              style='display:inline-block;background:#2f75b5;color:#fff;padding:12px 28px;
                                     text-decoration:none;border-radius:6px;font-weight:bold;font-size:14px;'>
                              View Details
                            </a>
                          </td>
                        </tr>
                      </table>
                    </td></tr>

                    <tr><td height='20'></td></tr>
                    <tr><td style='font-size:12px;color:#6b7280;'>
                      You are receiving this because you are a designated proxy approver.
                      If <strong>{mainApproverName}</strong> has already responded, no action is needed.
                    </td></tr>

                    <tr><td height='30'></td></tr>
                    <tr><td style='font-family:Courier New;font-size:12px;color:#7f8c8d;'>
                      --<br>Thank you,<br><br>M.I.R.A.<br><i>FSC - Information Technology</i>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fast.com.ph",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"[Proxy Reminder] Approval Required - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            await SendEmailAsync(requestBody);

            await CreateWebNotificationAsync(
                userId,
                "Proxy Approval Reminder",
                $"Reminder: You are a proxy approver for request ({requestNumber})",
                "warning",
                $"/main/pending-for-approval/approve?id={requestId}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send proxy approver reminder to {Email}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Sends an escalation notification to the configured escalation contact
    /// </summary>
    public async Task SendEscalationEmailAsync(
        string toEmail,
        string requestNumber,
        string requestorName,
        string requestDetails,
        string approverName,
        string approverEmail,
        int hoursPending)
    {
        try
        {
            var emailBody = $@"
            <html>
            <body style='margin:0;padding:0;font-family:Arial,Helvetica,sans-serif;'>
              <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5f5;'>
                <tr><td align='center'>
                  <table width='600' cellpadding='0' cellspacing='0'
                    style='background:#fff;margin:20px auto;padding:30px;max-width:600px;'>

                    <tr><td style='padding-bottom:20px;'>Dear Ma'am/Sir,</td></tr>

                    <tr><td style='padding-bottom:15px;'>
                      This exemption request has exceeded the escalation threshold and requires your attention.
                    </td></tr>

                    <tr><td style='padding:15px;background:#fef2f2;border-left:5px solid #dc2626;margin-bottom:20px;'>
                      <p><strong>Transaction #:</strong> <span style='color:#dc2626;'>{requestNumber}</span></p>
                      <p><strong>Requested By:</strong> {requestorName}</p>
                      <p><strong>Details:</strong> {requestDetails}</p>
                      <p><strong>Pending Approver:</strong> {approverName}</p>
                      <p><strong>Approver Email:</strong> {approverEmail}</p>
                      <p><strong>Hours Pending:</strong> {hoursPending}</p>
                    </td></tr>

                    <tr><td height='20'></td></tr>
                    <tr><td style='font-size:12px;color:#6b7280;'>
                      The approver has not responded within the configured escalation window.
                    </td></tr>

                    <tr><td height='30'></td></tr>
                    <tr><td style='font-family:Courier New;font-size:12px;color:#7f8c8d;'>
                      --<br>Thank you,<br><br>M.I.R.A.<br><i>FSC - Information Technology</i>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>";

            var requestBody = new
            {
                senderDisplayName = "epe.noreply@fastgroup.biz",
                recipient = toEmail,
                bccrecipient = "mcccescario@fast.com.ph",
                mailSubject = $"[Escalation] Approval Required - {requestNumber}",
                mailBody = emailBody,
                createdBy = "EPE System"
            };

            await SendEmailAsync(requestBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send escalation email to {Email}", toEmail);
            throw;
        }
    }

    // Extract shared HTTP sending logic to avoid repetition
    private async Task SendEmailAsync(object requestBody)
    {
        var authHeader = "65d9ec1b-316f-4294-a294-d8d54f-utility";
        var emailUrl = _urlBaseSettings.Emailer;

        var request = new HttpRequestMessage(HttpMethod.Post, emailUrl)
        {
            Headers = { { "Authorization", authHeader } },
            Content = JsonContent.Create(requestBody)
        };

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send email. Status: {response.StatusCode}. Response: {errorContent}");
        }
    }

    /// <summary>
    /// Sends notification to the next approval group after current group responds
    /// </summary>
    public async Task SendNextApprovalGroupNotificationAsync(
        List<(string Email, string Name, int UserId, long TransApproverId, bool IsProxy)> nextApprovers,
        long requestId,
        string requestNumber,
        string requestorName,
        string requestDetails)
    {
        foreach (var (email, name, userId, transApproverId, isProxy) in nextApprovers)
        {
            try
            {
                if (isProxy)
                {
                    // For proxy, we need the main approver's name
                    await SendProxyApproverReminderEmailAsync(
                        email, name, requestorName, // mainApproverName would be passed differently
                        requestId, requestNumber, requestorName, requestDetails,
                        userId, transApproverId);
                }
                else
                {
                    await SendMainApproverReminderEmailAsync(
                        email, name,
                        requestId, requestNumber, requestorName, requestDetails,
                        userId, transApproverId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send next approval group notification to {Email}", email);
            }
        }
    }
}
