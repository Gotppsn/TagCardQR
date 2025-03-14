using System;
using System.IO;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace ProductTagManager.Services
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
                
                // Use PngByteQRCode for better compatibility
                using (var pngGenerator = new PngByteQRCode(qrCodeData))
                {
                    byte[] pngBytes = pngGenerator.GetGraphic(20);
                    return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
                }
            }
        }
    }
}