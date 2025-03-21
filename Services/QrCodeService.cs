// Path: Services/QrCodeService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using CardTagManager.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace CardTagManager.Services
{
    public class QrCodeService
    {
        // Generate QR code as a base64 encoded image string for inline display
        public async Task<string> GenerateQrCodeImage(Card card)
        {
            if (card == null)
                return string.Empty;
                
            try
            {
                // Create QR code data with structured format
                string qrData = $"PRODUCT:{card.ProductName}:CAT:{card.Category}:ID:{card.Id}";
                
                // Parse colors from hex format
                Color fgColor = ColorTranslator.FromHtml(card.QrFgColor ?? "#000000"); 
                Color bgColor = ColorTranslator.FromHtml(card.QrBgColor ?? "#FFFFFF");
                
                // Generate QR code using QRCoder library
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new QRCode(qrCodeData))
                    {
                        var qrBitmap = qrCode.GetGraphic(20, fgColor, bgColor, true);
                        
                        // Convert to base64 string for direct embedding in HTML
                        using (var ms = new MemoryStream())
                        {
                            qrBitmap.Save(ms, ImageFormat.Png);
                            var imageBytes = ms.ToArray();
                            
                            return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return string.Empty;
            }
        }
        
        // Generate QR code as a byte array for file download
        public async Task<byte[]> GenerateQrCodeBytes(Card card)
        {
            if (card == null)
                return Array.Empty<byte>();
                
            try
            {
                // Create QR code data
                string qrData = $"PRODUCT:{card.ProductName}:CAT:{card.Category}:ID:{card.Id}";
                
                // Parse colors
                Color fgColor = ColorTranslator.FromHtml(card.QrFgColor ?? "#000000"); 
                Color bgColor = ColorTranslator.FromHtml(card.QrBgColor ?? "#FFFFFF");
                
                // Generate QR code
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new QRCode(qrCodeData))
                    {
                        var qrBitmap = qrCode.GetGraphic(20, fgColor, bgColor, true);
                        
                        // Convert to byte array for file downloads
                        using (var ms = new MemoryStream())
                        {
                            qrBitmap.Save(ms, ImageFormat.Png);
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return Array.Empty<byte>();
            }
        }
    }
}