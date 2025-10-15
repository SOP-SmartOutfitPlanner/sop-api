using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Utils
{
    public class EmailUtils
    {
        [Obsolete("Use IEmailTemplateService.GenerateOtpEmailAsync instead. This method will be removed in a future version.")]
        public static string GenerateOtpEmail(string otp, int expiryMinutes, string displayName)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Verification - Smart Outfit Planner</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f7fa;'>
    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='background-color: #f4f7fa;'>
        <tr>
            <td style='padding: 40px 20px;'>
                <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                    <!-- Header with Logo -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #4A90E2 0%, #357ABD 100%); padding: 40px 30px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <img src='https://storage.wizlab.io.vn/sop/rsz_2logo_web.png' alt='Smart Outfit Planner' style='max-width: 180px; height: auto; display: block; margin: 0 auto;'>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <h1 style='color: #2c3e50; font-size: 28px; margin: 0 0 20px 0; font-weight: 600;'>Email Verification</h1>
                            <p style='color: #5a6c7d; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>
                                Hi <strong>{displayName}</strong>,
                            </p>
                            <p style='color: #5a6c7d; font-size: 16px; line-height: 1.6; margin: 0 0 30px 0;'>
                                Thank you for choosing <strong>Smart Outfit Planner</strong>! To complete your registration and start organizing your wardrobe with AI-powered outfit suggestions, please use the verification code below:
                            </p>
                            
                            <!-- OTP Box -->
                            <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='margin: 30px 0;'>
                                <tr>
                                    <td style='background-color: #f0f7ff; border: 2px dashed #4A90E2; border-radius: 8px; padding: 30px; text-align: center;'>
                                        <div style='color: #4A90E2; font-size: 42px; font-weight: bold; letter-spacing: 8px; font-family: Courier New, monospace;'>{otp}</div>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='color: #e74c3c; font-size: 14px; line-height: 1.6; margin: 20px 0; background-color: #fef5f5; padding: 15px; border-left: 4px solid #e74c3c; border-radius: 4px;'>
                                <strong>⏰ Important:</strong> This code will expire in <strong>{expiryMinutes} minutes</strong>. Please verify your email soon.
                            </p>
                            
                            <p style='color: #5a6c7d; font-size: 14px; line-height: 1.6; margin: 20px 0 0 0;'>
                                If you didn't request this verification code, please ignore this email or contact our support team if you have concerns.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8fafc; padding: 30px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e9f0;'>
                            <p style='color: #8892a6; font-size: 14px; line-height: 1.6; margin: 0 0 10px 0;'>
                                <strong>Smart Outfit Planner</strong><br>
                                Your AI-Powered Wardrobe Assistant
                            </p>
                            <p style='color: #8892a6; font-size: 12px; line-height: 1.5; margin: 0;'>
                                This is an automated message, please do not reply to this email.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        [Obsolete("Use IEmailTemplateService.GenerateWelcomeEmailAsync instead. This method will be removed in a future version.")]
        public static string WelcomeEmail(string displayName)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome to Smart Outfit Planner</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f7fa;'>
    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='background-color: #f4f7fa;'>
        <tr>
            <td style='padding: 40px 20px;'>
                <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                    <!-- Header with Logo -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #4A90E2 0%, #357ABD 100%); padding: 50px 30px; text-align: center; border-radius: 12px 12px 0 0;'>
                            <img src='https://storage.wizlab.io.vn/sop/rsz_2logo_web.png' alt='Smart Outfit Planner' style='max-width: 200px; height: auto; display: block; margin: 0 auto 20px auto;'>
                            <h1 style='color: #ffffff; font-size: 32px; margin: 0; font-weight: 600;'>Welcome Aboard! 🎉</h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <p style='color: #2c3e50; font-size: 18px; line-height: 1.6; margin: 0 0 20px 0;'>
                                Hello <strong style='color: #4A90E2;'>{displayName}</strong>,
                            </p>
                            <p style='color: #5a6c7d; font-size: 16px; line-height: 1.6; margin: 0 0 25px 0;'>
                                Welcome to <strong>Smart Outfit Planner</strong>! We're thrilled to have you join our community of fashion-forward individuals who are revolutionizing the way they manage their wardrobe.
                            </p>
                            
                            <!-- Features Section -->
                            <div style='background: linear-gradient(135deg, #f0f7ff 0%, #e6f2ff 100%); border-radius: 8px; padding: 25px; margin: 30px 0;'>
                                <h2 style='color: #4A90E2; font-size: 22px; margin: 0 0 20px 0; font-weight: 600;'>What You Can Do:</h2>
                                
                                <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%'>
                                    <tr>
                                        <td style='padding: 10px 0;'>
                                            <span style='color: #4A90E2; font-size: 24px; margin-right: 10px;'>👔</span>
                                            <span style='color: #2c3e50; font-size: 15px; font-weight: 500;'>Organize Your Wardrobe</span>
                                            <p style='color: #5a6c7d; font-size: 14px; margin: 5px 0 0 34px; line-height: 1.5;'>
                                                Keep track of all your clothing items in one smart digital closet.
                                            </p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0;'>
                                            <span style='color: #4A90E2; font-size: 24px; margin-right: 10px;'>🤖</span>
                                            <span style='color: #2c3e50; font-size: 15px; font-weight: 500;'>AI-Powered Outfit Suggestions</span>
                                            <p style='color: #5a6c7d; font-size: 14px; margin: 5px 0 0 34px; line-height: 1.5;'>
                                                Get personalized outfit recommendations based on your style and occasion.
                                            </p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 10px 0;'>
                                            <span style='color: #4A90E2; font-size: 24px; margin-right: 10px;'>✨</span>
                                            <span style='color: #2c3e50; font-size: 15px; font-weight: 500;'>Smart Planning</span>
                                            <p style='color: #5a6c7d; font-size: 14px; margin: 5px 0 0 34px; line-height: 1.5;'>
                                                Plan your outfits ahead and never worry about what to wear again.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            
                            <p style='color: #5a6c7d; font-size: 16px; line-height: 1.6; margin: 25px 0 30px 0;'>
                                Start building your digital wardrobe today and let our AI help you look your best every day!
                            </p>
                            
                            <!-- CTA Button -->
                            <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='margin: 30px 0;'>
                                <tr>
                                    <td style='text-align: center;'>
                                        <a href='#' style='background: linear-gradient(135deg, #4A90E2 0%, #357ABD 100%); color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 600; display: inline-block; box-shadow: 0 4px 6px rgba(74, 144, 226, 0.3);'>
                                            Get Started Now
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='color: #5a6c7d; font-size: 14px; line-height: 1.6; margin: 30px 0 0 0;'>
                                If you have any questions or need assistance, our support team is always here to help.
                            </p>
                            
                            <p style='color: #5a6c7d; font-size: 16px; line-height: 1.6; margin: 25px 0 0 0;'>
                                Best regards,<br>
                                <strong style='color: #4A90E2;'>The Smart Outfit Planner Team</strong>
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8fafc; padding: 30px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e9f0;'>
                            <p style='color: #8892a6; font-size: 14px; line-height: 1.6; margin: 0 0 10px 0;'>
                                <strong>Smart Outfit Planner</strong><br>
                                Your AI-Powered Wardrobe Assistant
                            </p>
                            <p style='color: #8892a6; font-size: 12px; line-height: 1.5; margin: 0;'>
                                © 2024 Smart Outfit Planner. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}