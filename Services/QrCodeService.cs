using System;
using System.IO;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace CardTagManager.Services
{
    public class QrCodeService
    {
        public string GenerateQrCodeAsBase64(string textData)
        {
            // Create QR code generator
            using (var qrGenerator = new QRCodeGenerator())
            {
                // Create QR code data
                var qrCodeData = qrGenerator.CreateQrCode(textData, QRCodeGenerator.ECCLevel.Q);
                
                // Use QRCodeHelper instead of direct QRCode class
                using (var qrCode = new QRCoder.QRCode(qrCodeData))
                {
                    using (var bitmap = qrCode.GetGraphic(20))
                    {
                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, ImageFormat.Png);
                            return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                        }
                    }
                }
            }
        }
    }
}