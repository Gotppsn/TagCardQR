// Models/FileResponse.cs
namespace CardTagManager.Models
{
    public class FileResponse
    {
        public string ErrorMessage { get; set; }
        public byte[] FileBytes { get; set; }
        public string FilePath { get; set; }
        public string FileThumbnailPath { get; set; }
        public string FileThumbnailUrl { get; set; }
        public string FileUrl { get; set; }
        public bool IsSuccess { get; set; }
    }
}