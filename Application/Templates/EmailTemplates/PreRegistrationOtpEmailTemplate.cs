namespace Application.Templates.EmailTemplates;

public static class PreRegistrationOtpEmailTemplate
{
    public static string GetHtmlContent(string studio, string otp, string fullName)
    {
        var safeStudio = studio ?? string.Empty;
        var safeFullName = fullName ?? string.Empty;
        var code = (otp ?? string.Empty).PadRight(6).Substring(0, 6);
        var d1 = code[0] == ' ' ? '•' : code[0];
        var d2 = code[1] == ' ' ? '•' : code[1];
        var d3 = code[2] == ' ' ? '•' : code[2];
        var d4 = code[3] == ' ' ? '•' : code[3];
        var d5 = code[4] == ' ' ? '•' : code[4];
        var d6 = code[5] == ' ' ? '•' : code[5];

        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <title>FitJourney - Doğrulama Kodunuz</title>
  <meta name='x-apple-disable-message-reformatting'>
  <meta name='format-detection' content='telephone=no,address=no,email=no,date=no,url=no'>
  <style>
    @media only screen and (max-width: 600px) {{
      .contact-table {{ width: 100% !important; }}
      .contact-row {{ display: block !important; width: 100% !important; }}
      .contact-cell {{ display: block !important; width: 100% !important; padding: 12px 16px !important; border-bottom: 1px solid #f0f0f0; text-align: left !important; }}
      .contact-cell:last-child {{ border-bottom: none !important; }}
      .otp-box {{ font-size: 26px !important; padding: 12px 0 !important; }}
    }}
  </style>
</head>
<body style='margin:0; padding:0; font-family: Arial, Helvetica, sans-serif;
             background:url(""https://cdn.fitjourney.com.tr/login-background.png"") no-repeat center top / cover #EAF6F1;
             min-height:100vh; color:#000;'>

  <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'
         style='background-image:url(""https://cdn.fitjourney.com.tr/login-background.png""); background-repeat:no-repeat; background-position:center top; background-size:cover; background-color:#EAF6F1;'>
    <tr>
      <td align='center'
          style='background-image:url(""https://cdn.fitjourney.com.tr/login-background.png""); background-repeat:no-repeat; background-position:center top; background-size:cover; background-color:#EAF6F1;'>

        <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='max-width:600px; width:100%; margin:0 auto;'>

          <!-- LOGO -->
          <tr>
            <td align='center' style='padding:24px 20px 8px;'>
              <img src='https://cdn.fitjourney.com.tr/logo.png' width='160' alt='FitJourney' style='display:block; border:0; margin:0 auto;'>
            </td>
          </tr>

          <!-- HEADLINES -->
          <tr>
            <td align='center' style='padding:0 20px;'>
              <h2 style='margin:0; font-size:30px; line-height:38px; font-weight:700; font-family: Arial, sans-serif; color:#6fba6e;'>
                Doğrulama Kodunuz
              </h2>
              <h1 style='margin:6px 0 14px; font-size:22px; line-height:30px; font-weight:800; color:#000000;'>
                {safeStudio} Ön Kayıt İşlemi
              </h1>
            </td>
          </tr>

          <!-- ILLUSTRATION -->
          <tr>
            <td align='center' style='padding:0 20px 10px;'>
              <img src='https://cdn.fitjourney.com.tr/applause.png' width='140' alt='Hoş geldiniz' style='display:block; border:0; margin:0 auto;'>
            </td>
          </tr>

          <!-- TEXT -->
          <tr>
            <td align='center' style='padding:0 24px 18px;'>
              <p style='margin:0 0 6px; font-size:16px; line-height:26px; color:#000000;'>Merhaba {safeFullName},</p>
              <p style='margin:0; font-size:16px; line-height:26px; color:#000000;'>
                {safeStudio} ailesine katılmak için ön kayıt formunu doldurdunuz. 
                İşleminizi tamamlamak için aşağıdaki <strong>6 haneli doğrulama kodunu</strong> girmeniz gerekiyor.
              </p>
              <p style='margin:10px 0 0; font-size:15px; color:#000000;'>
                Kod <strong>10 dakika</strong> boyunca geçerlidir.
              </p>
            </td>
          </tr>

          <!-- OTP CARD -->
          <tr>
            <td align='center' style='padding:6px 20px 20px;'>
              <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'
                     style='max-width:560px; width:100%; background:#FFFFFF; border-radius:16px; box-shadow:0 8px 26px rgba(17,17,17,0.08);'>
                <tr>
                  <td style='padding:20px 22px;'>
                    <h3 style='margin:0 0 10px; font-size:18px; line-height:24px; color:#000000; text-align:center;'>
                      Onay Kodunuz
                    </h3>

                    <!-- KUTUCULAR -->
                    <table role='presentation' cellspacing='0' cellpadding='0' border='0' align='center' style='margin:8px auto 6px;'>
                      <tr>
                        <td class='otp-box' style='font-family:monospace; font-size:30px; font-weight:700; color:#2E7D32; text-align:center; border:2px solid #2BBB7F; border-radius:10px; width:44px; height:44px;'>{d1}</td>
                        <td width='8'></td>
                        <td class='otp-box' style='font-family:monospace; font-size:30px; font-weight:700; color:#2E7D32; text-align:center; border:2px solid #2BBB7F; border-radius:10px; width:44px; height:44px;'>{d2}</td>
                        <td width='8'></td>
                        <td class='otp-box' style='font-family:monospace; font-size:30px; font-weight:700; color:#2E7D32; text-align:center; border:2px solid #2BBB7F; border-radius:10px; width:44px; height:44px;'>{d3}</td>
                        <td width='8'></td>
                        <td class='otp-box' style='font-family:monospace; font-size:30px; font-weight:700; color:#2E7D32; text-align:center; border:2px solid #2BBB7F; border-radius:10px; width:44px; height:44px;'>{d4}</td>
                        <td width='8'></td>
                        <td class='otp-box' style='font-family:monospace; font-size:30px; font-weight:700; color:#2E7D32; text-align:center; border:2px solid #2BBB7F; border-radius:10px; width:44px; height:44px;'>{d5}</td>
                        <td width='8'></td>
                        <td class='otp-box' style='font-family:monospace; font-size:30px; font-weight:700; color:#2E7D32; text-align:center; border:2px solid #2BBB7F; border-radius:10px; width:44px; height:44px;'>{d6}</td>
                      </tr>
                    </table>

                    <p style='margin:12px 0 0; font-size:13px; color:#000000; text-align:center;'>
                      Güvenliğiniz için bu kodu kimseyle paylaşmayın. Siz talep etmediyseniz bu mesajı dikkate almayın.
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- CONTACT -->
          <tr>
            <td align='center' style='padding:8px 14px 26px;'>
              <table role='presentation' cellspacing='0' cellpadding='0' border='0' align='center'
                     class='contact-table' style='max-width:560px; width:100%; background:#fff; border-radius:16px; box-shadow:0 6px 16px rgba(17,17,17,0.06);'>
                <tr class='contact-row'>
                  <td align='left' class='contact-cell' style='padding:12px 16px; font-size:14px; color:#000000;'>
                    <img src='https://cdn.fitjourney.com.tr/insta.png' alt='Instagram' width='18' style='vertical-align:middle; margin-right:10px;'>
                    <a href='https://www.instagram.com/fitjourney.tr/' style='color:#000000; text-decoration:none;'>@fitjourney.tr</a>
                  </td>
                  <td align='left' class='contact-cell' style='padding:12px 16px; font-size:14px; color:#000000;'>
                    <img src='https://cdn.fitjourney.com.tr/mail.png' alt='E-posta' width='18' style='vertical-align:middle; margin-right:10px;'>
                    <a href='mailto:info@fitjourney.com.tr' style='color:#000000; text-decoration:none;'>info@fitjourney.com.tr</a>
                  </td>
                  <td align='left' class='contact-cell' style='padding:12px 16px; font-size:14px; color:#000000;'>
                    <img src='https://cdn.fitjourney.com.tr/tel.png' alt='Telefon' width='18' style='vertical-align:middle; margin-right:10px;'>
                    <a href='tel:+905433488668' style='color:#000000; text-decoration:none;'>0543 348 86 68</a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
";
    }
}