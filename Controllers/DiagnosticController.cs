// Path: Controllers/DiagnosticController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CardTagManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticController : ControllerBase
    {
        private readonly ILogger<DiagnosticController> _logger;

        public DiagnosticController(ILogger<DiagnosticController> logger)
        {
            _logger = logger;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            var config = new
            {
                PathBase = Request.PathBase.Value,
                Path = Request.Path.Value,
                RequestScheme = Request.Scheme,
                Host = Request.Host.Value,
                IsHttps = Request.IsHttps,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                ContentType = Request.ContentType,
                ContentLength = Request.ContentLength,
                Method = Request.Method,
                Query = Request.QueryString.Value
            };

            return Ok(config);
        }

        [HttpPost("echo")]
        public async Task<IActionResult> Echo()
        {
            var formContent = new Dictionary<string, string>();
            
            // First check for form data
            if (Request.HasFormContentType)
            {
                foreach (var key in Request.Form.Keys)
                {
                    formContent[key] = Request.Form[key].ToString();
                }
            }
            
            // If there's a body, read it too
            string body = "";
            try
            {
                using (var reader = new System.IO.StreamReader(Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    body = await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading request body");
                body = $"Error reading body: {ex.Message}";
            }

            var result = new
            {
                Timestamp = DateTime.Now,
                RequestPath = Request.Path.Value,
                PathBase = Request.PathBase.Value,
                Method = Request.Method,
                ContentType = Request.ContentType,
                ContentLength = Request.ContentLength,
                FormData = formContent,
                Body = body,
                HasFormContentType = Request.HasFormContentType,
                Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };

            return Ok(result);
        }
    }
}