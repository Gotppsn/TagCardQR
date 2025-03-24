// Path: Services/FileUploadService.cs
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
                
                // Log request information
                Console.WriteLine($"Uploading file to: {GetApiUrl()}api/Service_File/Upload");
                Console.WriteLine($"File: {file.FileName}, Size: {file.Length}, Type: {file.ContentType}");
                
                var response = await client.ExecuteAsync(request);
                response.ThrowIfError();
                // Ensure the response was successful
                if (response.IsSuccessful)
                {
                    fileResponse = JsonConvert.DeserializeObject<FileResponse>(response.Content);
                    fileResponse.FileBytes = fileBytes;
                    
                    if (!fileResponse.IsSuccess)
                    {
                        Console.WriteLine($"API returned success=false: {fileResponse.ErrorMessage}");
                        throw new Exception(fileResponse.ErrorMessage);
                    }
                    
                    Console.WriteLine($"File uploaded successfully: {fileResponse.FileUrl}");
                }
                else
                {
                    Console.WriteLine($"API error: {response.ErrorMessage}, StatusCode: {response.StatusCode}");
                    fileResponse.IsSuccess = false;
                    fileResponse.ErrorMessage = $"API Error: {response.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during upload: {ex.Message}");
                fileResponse.IsSuccess = false;
                fileResponse.ErrorMessage = $"Upload error: {ex.Message}";
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