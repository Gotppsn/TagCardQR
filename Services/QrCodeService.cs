// Path: Services/QrCodeService.cs
// Implement QR code generation service

using System;
using System.IO;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace CardTagManager.Services
{
    public class QrCodeService
    {
        public string GenerateQrCodeAsBase64(string textData, string foregroundColor = "#000000", string backgroundColor = "#FFFFFF")
        {
            // Create QR code generator
            using (var qrGenerator = new QRCodeGenerator())
            {
                // Create QR code data
                var qrCodeData = qrGenerator.CreateQrCode(textData, QRCodeGenerator.ECCLevel.Q);
                
                // Convert hex colors to RGB
                var fgColor = HexToColor(foregroundColor);
                var bgColor = HexToColor(backgroundColor);
                
                // Use QRCode with custom colors
                using (var qrCode = new QRCode(qrCodeData))
                {
                    // Generate bitmap with custom colors
                    using (var qrBitmap = qrCode.GetGraphic(20, fgColor, bgColor, true))
                    {
                        // Convert bitmap to Base64 string
                        using (var ms = new MemoryStream())
                        {
                            qrBitmap.Save(ms, ImageFormat.Png);
                            byte[] byteImage = ms.ToArray();
                            return $"data:image/png;base64,{Convert.ToBase64String(byteImage)}";
                        }
                    }
                }
            }
        }
        
        private Color HexToColor(string hexColor)
        {
            // Remove # if present
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);
                
            // Convert hex to RGB
            int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            
            return Color.FromArgb(r, g, b);
        }
    }
}