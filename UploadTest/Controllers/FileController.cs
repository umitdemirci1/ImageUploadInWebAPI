using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UploadTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly string _fileDirectory;
        private readonly AppDbContext _context;
        public FileController(AppDbContext context)
        {
            _fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Files");

            if (!Directory.Exists(_fileDirectory))
            {
                Directory.CreateDirectory(_fileDirectory);
            }
            _context = context;
        }

        [HttpPost("save-text")]
        public async Task<IActionResult> UploadFile([FromBody] string content)
        {
            try
            {
                var fileName = $"TextFile_{Guid.NewGuid()}.txt";
                var filePath = Path.Combine(_fileDirectory, fileName);

                await System.IO.File.WriteAllTextAsync(filePath, content);

                var textFile = new Text
                {
                    Purpose = "Bu dosya API üzerinden kaydedildi.",
                    TextUrl = filePath
                };
                await _context.Texts.AddAsync(textFile);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "File saved successfully", FileName = fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllTextFiles()
        {
            try
            {
                var textFiles = await _context.Texts.ToListAsync();

                if (textFiles == null || textFiles.Count == 0)
                {
                    return NotFound(new { Message = "No text files found." });
                }

                var result = new List<TextFileWithContentDto>();

                foreach (var textFile in textFiles)
                {
                    if (System.IO.File.Exists(textFile.TextUrl))
                    {
                        var content = await System.IO.File.ReadAllTextAsync(textFile.TextUrl);

                        result.Add(new TextFileWithContentDto
                        {
                            Id = textFile.Id,
                            Purpose = textFile.Purpose,
                            Content = content
                        });
                    }
                    else
                    {
                        result.Add(new TextFileWithContentDto
                        {
                            Id = textFile.Id,
                            Purpose = textFile.Purpose,
                            Content = "Dosya bulunamadı."
                        });
                    }
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, string alt)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "File is empty." });
            }

            try
            {
                var uploadPath = Path.Combine(_fileDirectory, "Images");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var image = new Image
                {
                    ImageUrl = filePath,
                    Alt = alt
                };
                await _context.Images.AddAsync(image);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "File uploaded successfully", FileName = fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }
        }

        [HttpGet("get-all-images")]
        public async Task<IActionResult> GetAllImages()
        {
            var images = await _context.Images.ToListAsync();
            if (images == null || images.Count == 0)
            {
                return NotFound(new { Message = "No images found." });
            }
            var result = new List<ImageWithDataDto>();
            foreach (var image in images)
            {
                var filePath = image.ImageUrl;
                if (System.IO.File.Exists(filePath))
                {
                    var imageData = await System.IO.File.ReadAllBytesAsync(filePath);
                    var base64Image = Convert.ToBase64String(imageData);
                    result.Add(new ImageWithDataDto
                    {
                        Id = image.Id,
                        Alt = image.Alt,
                        Base64Image = base64Image
                    });
                }
                else
                {
                    result.Add(new ImageWithDataDto
                    {
                        Id = image.Id,
                        Alt = image.Alt,
                        Base64Image = null
                    });
                }
            }
            return Ok(result);
        }
    }

    public class TextFileWithContentDto
    {
        public int Id { get; set; }
        public string Purpose { get; set; }
        public string Content { get; set; }
    }

    public class ImageWithDataDto
    {
        public int Id { get; set; }
        public string Alt { get; set; }
        public string Base64Image { get; set; }
    }
}