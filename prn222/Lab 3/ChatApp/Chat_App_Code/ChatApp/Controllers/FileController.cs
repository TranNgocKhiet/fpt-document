using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, FileMetadata> FileMetadataStore = new();
        private static readonly string UploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        private const long MaxFileSize = 2L * 1024 * 1024 * 1024; // 2GB

        public FileController()
        {
            // Ensure upload directory exists
            if (!Directory.Exists(UploadDirectory))
            {
                Directory.CreateDirectory(UploadDirectory);
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(2L * 1024 * 1024 * 1024)] // 2GB
        [RequestFormLimits(MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string username, [FromForm] string fileId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                if (file.Length > MaxFileSize)
                    return BadRequest("File too large. Maximum size is 2GB.");

                // Save file to disk (much faster than memory)
                var filePath = Path.Combine(UploadDirectory, fileId);
                
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                {
                    await file.CopyToAsync(stream);
                }

                // Store metadata only
                var metadata = new FileMetadata
                {
                    FileId = fileId,
                    FileName = file.FileName,
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    Username = username,
                    FilePath = filePath,
                    UploadTime = DateTime.Now,
                    IsLocalFile = false
                };

                FileMetadataStore[fileId] = metadata;

                // Clean up after 30 minutes
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(30));
                    if (FileMetadataStore.TryRemove(fileId, out var meta))
                    {
                        try
                        {
                            if (!meta.IsLocalFile && System.IO.File.Exists(meta.FilePath))
                                System.IO.File.Delete(meta.FilePath);
                        }
                        catch { }
                    }
                });

                return Ok(new { fileId, fileName = file.FileName, fileSize = file.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }

        [HttpPost("share-local")]
        public IActionResult ShareLocalFile([FromBody] LocalFileRequest request)
        {
            try
            {
                // Validate file exists
                if (!System.IO.File.Exists(request.FilePath))
                    return BadRequest("File not found on server");

                var fileInfo = new FileInfo(request.FilePath);
                
                if (fileInfo.Length > MaxFileSize)
                    return BadRequest("File too large. Maximum size is 2GB.");

                // Store metadata pointing to local file
                var metadata = new FileMetadata
                {
                    FileId = request.FileId,
                    FileName = request.FileName,
                    FileType = request.FileType,
                    FileSize = fileInfo.Length,
                    Username = request.Username,
                    FilePath = request.FilePath,
                    UploadTime = DateTime.Now,
                    IsLocalFile = true // Don't delete this file
                };

                FileMetadataStore[request.FileId] = metadata;

                // Clean up metadata after 30 minutes (but don't delete file)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(30));
                    FileMetadataStore.TryRemove(request.FileId, out _);
                });

                return Ok(new { fileId = request.FileId, fileName = request.FileName, fileSize = fileInfo.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Share failed: {ex.Message}");
            }
        }

        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> Download(string fileId)
        {
            try
            {
                if (!FileMetadataStore.TryGetValue(fileId, out var metadata))
                    return NotFound("File not found or expired");

                if (!System.IO.File.Exists(metadata.FilePath))
                    return NotFound("File not found on disk");

                // Stream file directly (no base64, much faster)
                var memory = new MemoryStream();
                using (var stream = new FileStream(metadata.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, metadata.FileType, metadata.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Download failed: {ex.Message}");
            }
        }

        [HttpGet("info/{fileId}")]
        public IActionResult GetFileInfo(string fileId)
        {
            try
            {
                if (!FileMetadataStore.TryGetValue(fileId, out var metadata))
                    return NotFound("File not found");

                return Ok(new
                {
                    fileId = metadata.FileId,
                    fileName = metadata.FileName,
                    fileType = metadata.FileType,
                    fileSize = metadata.FileSize,
                    username = metadata.Username,
                    isLocalFile = metadata.IsLocalFile
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get file info: {ex.Message}");
            }
        }
    }

    public class FileMetadata
    {
        public string FileId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public long FileSize { get; set; }
        public string Username { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime UploadTime { get; set; }
        public bool IsLocalFile { get; set; } = false;
    }

    public class LocalFileRequest
    {
        public string FileId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Username { get; set; } = "";
    }
}
