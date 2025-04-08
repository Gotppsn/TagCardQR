// Path: Services/QrCodeService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using CardTagManager.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.Http;

namespace CardTagManager.Services
{
    public class QrCodeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QrCodeService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Generate QR code as a base64 encoded image string for inline display
        public Task<string> GenerateQrCodeImage(Card card, string baseUrl = null)
        {
            if (card == null)
                return Task.FromResult(string.Empty);
                
            try
            {
                // Create QR code data with absolute URL to ScanShow page
                string qrData;
                
                // Get path base from current request
                string pathBase = _httpContextAccessor?.HttpContext?.Request.PathBase.Value ?? "";
                
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    // Use fully qualified URL with path base
                    baseUrl = baseUrl.TrimEnd('/');
                    qrData = $"{baseUrl}{pathBase}/Card/ScanShow/{card.Id}";
                }
                else
                {
                    // Get current request details
                    var httpContext = _httpContextAccessor?.HttpContext;
                    if (httpContext != null)
                    {
                        var scheme = httpContext.Request.Scheme;
                        var host = httpContext.Request.Host.Value;
                        
                        // Generate absolute URL with path base
                        qrData = $"{scheme}://{host}{pathBase}/Card/ScanShow/{card.Id}";
                    }
                    else
                    {
                        // Fallback with path base included
                        qrData = $"{pathBase}/Card/ScanShow/{card.Id}";
                    }
                }
                
                Color fgColor = ColorTranslator.FromHtml(card.QrFgColor ?? "#000000"); 
                Color bgColor = ColorTranslator.FromHtml(card.QrBgColor ?? "#FFFFFF");
                
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                    using (var qrCode = new QRCode(qrCodeData))
                    {
                        var qrBitmap = qrCode.GetGraphic(20, fgColor, bgColor, true);
                        
                        using (var ms = new MemoryStream())
                        {
                            qrBitmap.Save(ms, ImageFormat.Png);
                            var imageBytes = ms.ToArray();
                            
                            return Task.FromResult($"data:image/png;base64,{Convert.ToBase64String(imageBytes)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return Task.FromResult(string.Empty);
            }
        }
        
        // Generate QR code as a byte array for file download
        public Task<byte[]> GenerateQrCodeBytes(Card card, string baseUrl = null)
        {
            if (card == null)
                return Task.FromResult(Array.Empty<byte>());
                
            try
            {
                // Create QR code data with URL to ScanShow page
                string qrData;
                
                // Get path base from current request
                string pathBase = _httpContextAccessor?.HttpContext?.Request.PathBase.Value ?? "";
                
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    // Use fully qualified URL with path base
                    baseUrl = baseUrl.TrimEnd('/');
                    qrData = $"{baseUrl}{pathBase}/Card/ScanShow/{card.Id}";
                }
                else
                {
                    // Get current request details
                    var httpContext = _httpContextAccessor?.HttpContext;
                    if (httpContext != null)
                    {
                        var scheme = httpContext.Request.Scheme;
                        var host = httpContext.Request.Host.Value;
                        
                        // Generate absolute URL with path base
                        qrData = $"{scheme}://{host}{pathBase}/Card/ScanShow/{card.Id}";
                    }
                    else
                    {
                        // Fallback with structured format
                        qrData = $"PRODUCT:{card.ProductName}:CAT:{card.Category}:ID:{card.Id}";
                    }
                }
                
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
                            return Task.FromResult(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating QR code: {ex.Message}");
                return Task.FromResult(Array.Empty<byte>());
            }
        }
    }
}