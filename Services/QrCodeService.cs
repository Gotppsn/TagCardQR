using System;
using System.Drawing;
using System.IO;
using QRCoder;

namespace CardTagManager.Services
{
    public class QrCodeService
    {
        public string GenerateQrCodeAsBase64(string textData)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(textData, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                {
                    using (var bitmap = qrCode.GetGraphic(20))
                    {
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                        }
                    }
                }
            }
        }
    }
}