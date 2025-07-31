using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IMinioClient _minioClient;
        private const string BucketName = "mybucket";

        public HomeController()
        {
            _minioClient = new MinioClient()
                .WithEndpoint("localhost:9000")
                .WithCredentials("minioadmin", "minioadmin")
                .WithSSL(false)
                .Build();
        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            return Content("✅ API server is running.");
        }

        private async Task EnsureBucketExistsAsync()
        {
            bool found = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(BucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(BucketName));
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("⚠️ No file uploaded.");

            try
            {
                await EnsureBucketExistsAsync();

                using (var stream = file.OpenReadStream())
                {
                    await _minioClient.PutObjectAsync(
                        new PutObjectArgs()
                            .WithBucket(BucketName)
                            .WithObject(file.FileName)
                            .WithStreamData(stream)
                            .WithObjectSize(file.Length)
                            .WithContentType(file.ContentType));
                }

                return Ok($"✅ File '{file.FileName}' uploaded successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Upload failed: {ex.Message}");
            }
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var ms = new MemoryStream();

            try
            {
                await _minioClient.GetObjectAsync(
                    new GetObjectArgs()
                        .WithBucket(BucketName)
                        .WithObject(fileName)
                        .WithCallbackStream(stream => stream.CopyTo(ms)));

                ms.Position = 0;
                return File(ms, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Download failed: {ex.Message}");
            }
        }

        [HttpGet("presigned-upload-url/{fileName}")]
        public async Task<IActionResult> GetPresignedUploadUrl(string fileName)
        {
            try
            {
                await EnsureBucketExistsAsync();

                var url = await _minioClient.PresignedPutObjectAsync(
                    new PresignedPutObjectArgs()
                        .WithBucket(BucketName)
                        .WithObject(fileName)
                        .WithExpiry(3600));

                return Ok(new { UploadUrl = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Presigned upload URL failed: {ex.Message}");
            }
        }

        [HttpGet("presigned-download-url/{fileName}")]
        public async Task<IActionResult> GetPresignedDownloadUrl(string fileName)
        {
            try
            {
                var url = await _minioClient.PresignedGetObjectAsync(
                    new PresignedGetObjectArgs()
                        .WithBucket(BucketName)
                        .WithObject(fileName)
                        .WithExpiry(3600));

                return Ok(new { DownloadUrl = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Presigned download URL failed: {ex.Message}");
            }
        }

        [HttpGet("list-files")]
        public async Task<IActionResult> ListFiles()
        {
            var files = new List<string>();

            try
            {
                var args = new ListObjectsArgs()
                    .WithBucket(BucketName)
                    .WithRecursive(true);

                // استفاده از متد جدید extension برای لیست:
                await foreach (var item in _minioClient.ListObjectsEnumAsync(args))
                {
                    files.Add(item.Key);
                }

                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Listing files failed: {ex.Message}");
            }
        }

        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                await _minioClient.RemoveObjectAsync(
                    new RemoveObjectArgs()
                        .WithBucket(BucketName)
                        .WithObject(fileName));

                return Ok($"🗑️ File '{fileName}' deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Delete failed: {ex.Message}");
            }
        }
    }
}
