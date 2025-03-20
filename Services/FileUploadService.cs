// Services/FileUploadService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using CardTagManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace CardTagManager.Services
{
    public class FileUploadService
    {
        private readonly IConfiguration _configuration;

        public FileUploadService(IConfiguration configuration)
        {
            _configuration = configuration;
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

        // Create RestClient with options
        var options = new RestClientOptions(GetApiUrl())
        {
            MaxTimeout = -1,
        };
        
        var client = new RestClient(options);
        var request = new RestRequest("api/Service_File/Upload", Method.Post);
        
        request.AddHeader("Token", tokenKey);
        request.AlwaysMultipartFormData = true;
        request.AddFile("fileUpload", fileBytes, HttpUtility.UrlEncode(file.FileName), file.ContentType);
        request.AddParameter("FolderPath", folderPath);
        
        var response = await client.ExecuteAsync(request);
        
        if (response.IsSuccessful)
        {
            fileResponse = JsonConvert.DeserializeObject<FileResponse>(response.Content);
            fileResponse.FileBytes = fileBytes;
            
            if (!fileResponse.IsSuccess)
            {
                throw new Exception(fileResponse.ErrorMessage);
            }
        }
        else
        {
            fileResponse.IsSuccess = false;
            fileResponse.ErrorMessage = $"API Error: {response.ErrorMessage}";
        }
    }
    catch (Exception ex)
    {
        fileResponse.IsSuccess = false;
        fileResponse.ErrorMessage = $"Upload error: {ex.Message}";
        
        // Log the error but don't rethrow to allow for graceful degradation
        Console.WriteLine($"File upload failed: {ex.Message}");
    }

    return fileResponse;
}

        public async Task<FileResponse> DeleteFile(string fileUrl)
        {
            var res = new FileResponse();
            var tokenKey = GetToken();
            fileUrl = HttpUtility.UrlEncode(fileUrl);

            // Create RestClient with the API URL
            var client = new RestClient(GetApiUrl());

            // Create a POST request
            var request = new RestRequest("api/Service_File/Delete_File", Method.Post);

            // Add headers
            request.AddHeader("Token", tokenKey);
            request.AddHeader("FilePath", fileUrl);

            // Execute the request asynchronously
            var response = await client.ExecuteAsync(request);
            
            // Ensure the response was successful
            if (response.IsSuccessful)
            {
                // Deserialize the response content
                res = JsonConvert.DeserializeObject<FileResponse>(response.Content);
                if (!res.IsSuccess)
                {
                    throw new Exception($"Error deleting file: {res.ErrorMessage}");
                }
            }
            else
            {
                res.IsSuccess = false;
                res.ErrorMessage = $"API Error: {response.ErrorMessage}";
            }

            return res;
        }
    }
}