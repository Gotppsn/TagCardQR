// Path: Services/FileUploadService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using CardTagManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace CardTagManager.Services
{
    public class FileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string GetApiUrl()
        {
            return _configuration["FileUpload:ApiUrl"] ?? "https://devsever.thaiparker.co.th/tp_service/";
        }

        private string GetToken()
        {
            return _configuration["FileUpload:Token"] ?? "3e17dfc9-6225-4183-a610-cef1129c17bb";
        }

public async Task<FileResponse> UploadFile(IFormFile file, string folderPath = "CardImages")
{
    if (file == null || file.Length == 0)
    {
        return new FileResponse
        {
            IsSuccess = false,
            ErrorMessage = "No file was uploaded."
        };
    }

    var fileResponse = new FileResponse();
    var tokenKey = GetToken();

    try
    {
        // Read file bytes
        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        // Sanitize filename to remove illegal characters
        string originalFilename = file.FileName;
        string safeFilename = SanitizeFileName(originalFilename);
        
        _logger.LogInformation($"Original filename: {originalFilename}, Sanitized filename: {safeFilename}");

        // Create RestClient with options
        var options = new RestClientOptions(GetApiUrl())
        {
            MaxTimeout = 300000, // 5 minutes timeout
        };
        
        var client = new RestClient(options);
        var request = new RestRequest("api/Service_File/Upload", Method.Post);
        
        // Set headers
        request.AddHeader("Token", tokenKey);
        request.AlwaysMultipartFormData = true;
        
        // Add file with sanitized filename
        request.AddFile("fileUpload", fileBytes, safeFilename, file.ContentType);
        request.AddParameter("FolderPath", folderPath);
        
        _logger.LogInformation($"Uploading file to: {GetApiUrl()}api/Service_File/Upload");
        _logger.LogInformation($"File: {safeFilename}, Size: {file.Length}, Type: {file.ContentType}");
        
        var response = await client.ExecuteAsync(request);
        
        _logger.LogInformation($"Response status: {response.StatusCode}, Content: {response.Content}");
        
        if (response.IsSuccessful)
        {
            fileResponse = JsonConvert.DeserializeObject<FileResponse>(response.Content);
            fileResponse.FileBytes = fileBytes;
            
            if (!fileResponse.IsSuccess)
            {
                _logger.LogWarning($"API returned success=false: {fileResponse.ErrorMessage}");
                throw new Exception(fileResponse.ErrorMessage);
            }
            
            _logger.LogInformation($"File uploaded successfully: {fileResponse.FileUrl}");
        }
        else
        {
            _logger.LogError($"API error: {response.ErrorMessage}, StatusCode: {response.StatusCode}, Response: {response.Content}");
            fileResponse.IsSuccess = false;
            fileResponse.ErrorMessage = $"API Error: {response.ErrorMessage ?? response.Content}";
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Exception during upload: {ex.Message}");
        fileResponse.IsSuccess = false;
        fileResponse.ErrorMessage = $"Upload error: {ex.Message}";
    }

    return fileResponse;
}

// Add this new method to sanitize filenames
private string SanitizeFileName(string fileName)
{
    if (string.IsNullOrEmpty(fileName))
        return "file";

    // Replace problematic characters with underscores
    char[] invalidChars = Path.GetInvalidFileNameChars();
    string safeName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    
    // Also replace other potentially problematic characters
    safeName = safeName
        .Replace(" ", "_")
        .Replace(".", "_")
        .Replace(",", "_")
        .Replace(";", "_")
        .Replace(":", "_")
        .Replace("(", "_")
        .Replace(")", "_")
        .Replace("[", "_")
        .Replace("]", "_")
        .Replace("{", "_")
        .Replace("}", "_")
        .Replace("+", "_")
        .Replace("=", "_")
        .Replace("*", "_")
        .Replace("&", "_")
        .Replace("%", "_")
        .Replace("$", "_")
        .Replace("#", "_")
        .Replace("@", "_")
        .Replace("!", "_")
        .Replace("?", "_");

    // Ensure the filename includes the original extension
    string originalExtension = Path.GetExtension(fileName);
    if (!string.IsNullOrEmpty(originalExtension))
    {
        safeName = safeName + originalExtension;
    }
    
    // If the filename is now empty, provide a default
    if (string.IsNullOrEmpty(safeName))
    {
        safeName = "file_" + DateTime.Now.Ticks;
        if (!string.IsNullOrEmpty(originalExtension))
        {
            safeName += originalExtension;
        }
    }
    
    return safeName;
}

        public async Task<FileResponse> DeleteFile(string fileUrl)
        {
            var fileResponse = new FileResponse();
            var tokenKey = GetToken();

            try
            {
                // Create RestClient with the API URL
                var client = new RestClient(GetApiUrl());

                // Create a POST request
                var request = new RestRequest("api/Service_File/Delete_File", Method.Post);

                // Add headers
                request.AddHeader("Token", tokenKey);
                request.AddHeader("FilePath", fileUrl);

                _logger.LogInformation($"Deleting file: {fileUrl}");

                // Execute the request asynchronously
                var response = await client.ExecuteAsync(request);
                
                _logger.LogInformation($"Delete response status: {response.StatusCode}, Content: {response.Content}");
                
                // Ensure the response was successful
                if (response.IsSuccessful)
                {
                    // Deserialize the response content
                    fileResponse = JsonConvert.DeserializeObject<FileResponse>(response.Content);
                    if (!fileResponse.IsSuccess)
                    {
                        _logger.LogWarning($"API returned success=false for delete: {fileResponse.ErrorMessage}");
                        throw new Exception($"Error deleting file: {fileResponse.ErrorMessage}");
                    }
                }
                else
                {
                    _logger.LogError($"API error during delete: {response.ErrorMessage}, StatusCode: {response.StatusCode}");
                    fileResponse.IsSuccess = false;
                    fileResponse.ErrorMessage = $"API Error: {response.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during file deletion: {ex.Message}");
                fileResponse.IsSuccess = false;
                fileResponse.ErrorMessage = $"Delete error: {ex.Message}";
            }

            return fileResponse;
        }

        public async Task<List<FileResponse>> UploadFiles(IEnumerable<IFormFile> files, string folderPath = "CardDocuments")
        {
            var responses = new List<FileResponse>();
            
            foreach (var file in files)
            {
                var response = await UploadFile(file, folderPath);
                responses.Add(response);
            }
            
            return responses;
        }
    }
}