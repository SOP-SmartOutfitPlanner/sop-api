using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Utils
{
    public class EmailUtils
    {
        public static string GenerateOtpEmail(string otp, int expiryMinutes)
        {
            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%);
            padding: 40px 20px;
            position: relative;
        }}
        body::before {{
            content: '';
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: 
                radial-gradient(circle at 20% 50%, rgba(120, 119, 198, 0.3), transparent 50%),
                radial-gradient(circle at 80% 80%, rgba(240, 147, 251, 0.3), transparent 50%),
                radial-gradient(circle at 40% 20%, rgba(102, 126, 234, 0.3), transparent 50%);
            pointer-events: none;
        }}
        .email-wrapper {{
            max-width: 600px;
            margin: 0 auto;
            position: relative;
            z-index: 1;
        }}
        .container {{
            background: white;
            border-radius: 20px;
            overflow: hidden;
            box-shadow: 0 25px 70px rgba(0, 0, 0, 0.35);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%);
            color: white;
            padding: 60px 40px;
            text-align: center;
            position: relative;
            overflow: hidden;
        }}
        .header::before {{
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: repeating-linear-gradient(
                45deg,
                transparent,
                transparent 10px,
                rgba(255, 255, 255, 0.05) 10px,
                rgba(255, 255, 255, 0.05) 20px
            );
            animation: slide 20s linear infinite;
        }}
        @keyframes slide {{
            0% {{ transform: translate(0, 0); }}
            100% {{ transform: translate(50px, 50px); }}
        }}
        .header-content {{
            position: relative;
            z-index: 1;
        }}
        .header h1 {{
            font-size: 36px;
            font-weight: 700;
            text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.2);
        }}
        .content {{
            padding: 50px 40px;
            text-align: center;
            background: linear-gradient(180deg, #ffffff 0%, #f8f9ff 100%);
        }}
        .greeting {{
            font-size: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            margin-bottom: 25px;
            font-weight: 700;
        }}
        .message {{
            font-size: 15px;
            color: #555;
            margin-bottom: 15px;
            line-height: 1.8;
        }}
        .otp-container {{
            margin: 45px 0;
            padding: 40px 30px;
            background: linear-gradient(135deg, #e0c3fc 0%, #8ec5fc 100%);
            border-radius: 16px;
            position: relative;
            box-shadow: 0 10px 30px rgba(102, 126, 234, 0.3);
        }}
        .otp-container::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: linear-gradient(45deg, rgba(255,255,255,0.1) 25%, transparent 25%, transparent 75%, rgba(255,255,255,0.1) 75%),
                        linear-gradient(45deg, rgba(255,255,255,0.1) 25%, transparent 25%, transparent 75%, rgba(255,255,255,0.1) 75%);
            background-size: 20px 20px;
            background-position: 0 0, 10px 10px;
            border-radius: 16px;
            opacity: 0.3;
        }}
        .otp-label {{
            font-size: 14px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 2px;
            margin-bottom: 20px;
            position: relative;
            z-index: 1;
        }}
        .otp-box {{
            background: white;
            border-radius: 12px;
            padding: 25px 30px;
            display: inline-block;
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.15);
            position: relative;
            z-index: 1;
            border: 3px solid transparent;
            background-clip: padding-box;
            position: relative;
        }}
        .otp-box::before {{
            content: '';
            position: absolute;
            top: -3px;
            left: -3px;
            right: -3px;
            bottom: -3px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%);
            border-radius: 12px;
            z-index: -1;
        }}
        .otp-code {{
            font-size: 48px;
            font-weight: bold;
            letter-spacing: 14px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            font-family: 'Courier New', monospace;
        }}
        .expiry-info {{
            margin-top: 35px;
            padding: 25px;
            background: linear-gradient(135deg, #fff7e6 0%, #ffe8cc 100%);
            border-radius: 12px;
            text-align: left;
            box-shadow: 0 5px 15px rgba(255, 152, 0, 0.2);
        }}
        .expiry-info p {{
            font-size: 14px;
            color: #e65100;
        }}
        .expiry-info strong {{
            color: #bf360c;
            font-weight: 700;
        }}
        .security-notice {{
            margin-top: 25px;
            padding: 25px;
            background: linear-gradient(135deg, #ffebee 0%, #ffcdd2 100%);
            border-radius: 12px;
            text-align: left;
            box-shadow: 0 5px 15px rgba(244, 67, 54, 0.2);
        }}
        .security-notice p {{
            font-size: 14px;
            color: #c62828;
        }}
        .security-notice strong {{
            color: #b71c1c;
            font-weight: 700;
        }}
        .help-text {{
            margin-top: 40px;
            padding: 20px;
            background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
            border-radius: 12px;
            font-size: 14px;
            color: #2e7d32;
            line-height: 1.8;
            box-shadow: 0 5px 15px rgba(46, 125, 50, 0.15);
        }}
        .divider {{
            height: 2px;
            background: linear-gradient(to right, transparent, #667eea, #764ba2, #f093fb, transparent);
            margin: 40px 0;
        }}
        .footer {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 50%, #f093fb 100%);
            padding: 40px;
            text-align: center;
            position: relative;
            overflow: hidden;
        }}
        .footer::before {{
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: repeating-linear-gradient(
                90deg,
                transparent,
                transparent 10px,
                rgba(255, 255, 255, 0.05) 10px,
                rgba(255, 255, 255, 0.05) 20px
            );
        }}
        .footer-content {{
            position: relative;
            z-index: 1;
        }}
        .footer-brand {{
            font-size: 24px;
            font-weight: 700;
            color: white;
            margin-bottom: 15px;
            text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.2);
        }}
        .footer-text {{
            font-size: 13px;
            color: rgba(255, 255, 255, 0.9);
            margin: 8px 0;
        }}
        .footer-links {{
            margin-top: 25px;
            padding-top: 25px;
            border-top: 1px solid rgba(255, 255, 255, 0.3);
        }}
        .footer-links a {{
            color: white;
            text-decoration: none;
            font-size: 13px;
            margin: 0 15px;
            transition: all 0.3s ease;
            font-weight: 500;
        }}
        .footer-links a:hover {{
            text-shadow: 0 0 8px rgba(255, 255, 255, 0.8);
        }}
        @media only screen and (max-width: 600px) {{
            .header {{
                padding: 40px 25px;
            }}
            .header h1 {{
                font-size: 28px;
            }}
            .content {{
                padding: 35px 25px;
            }}
            .otp-code {{
                font-size: 36px;
                letter-spacing: 10px;
            }}
            .footer {{
                padding: 30px 20px;
            }}
        }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='container'>
            <div class='header'>
                <div class='header-content'>
                    <h1>Verification Code</h1>
                </div>
            </div>
            
            <div class='content'>
                <div class='greeting'>
                    Dear,
                </div>
                
                <p class='message'>
                    We received a request to verify your account. Please use the code below to complete your verification.
                </p>
                
                <div class='otp-container'>
                    <div class='otp-label'>
                        Your OTP Code
                    </div>
                    <div class='otp-box'>
                        <div class='otp-code'>{otp}</div>
                    </div>
                </div>
                
                <div class='expiry-info'>
                    <p><strong>Expiration Notice:</strong> This code will expire in <strong>{expiryMinutes} minutes</strong>.</p>
                </div>
                
                <div class='security-notice'>
                    <p><strong>Security Warning:</strong> Never share this code with anyone.</p>
                </div>
                
                <div class='divider'></div>
                
                <div class='help-text'>
                    <p>If you didn't request this code, please ignore this email or contact our support team.</p>
                </div>
            </div>
            
            <div class='footer'>
                <div class='footer-content'>
                    <div class='footer-brand'>Smart Outfit Planner - SOP</div>
                    <p class='footer-text'>© 2025 Smart Outfit Planner - SOP. All rights reserved.</p>
                    <p class='footer-text'>This is an automated email. Please do not reply.</p>
                    <div class='footer-links'>
                        <a href='#'>Privacy Policy</a>
                        <a href='#'>Terms of Service</a>
                        <a href='#'>Contact</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        public static string WelcomeEmail(string displayName)
        {
            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: white;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background: #667eea;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chào mừng bạn đến với SOPServer!</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{displayName}</strong>,</p>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại SOPServer. Chúng tôi rất vui mừng được đồng hành cùng bạn!</p>
            <p>Bạn đã có thể bắt đầu sử dụng các dịch vụ của chúng tôi ngay bây giờ.</p>
            <p style='text-align: center;'>
                <a href='#' class='button'>Bắt đầu khám phá</a>
            </p>
            <p>Nếu bạn có bất kỳ câu hỏi nào, đừng ngại liên hệ với chúng tôi.</p>
            <p>Trân trọng,<br><strong>Đội ngũ SOPServer</strong></p>
        </div>
    </div>
</body>
</html>";
        }
        }
}
