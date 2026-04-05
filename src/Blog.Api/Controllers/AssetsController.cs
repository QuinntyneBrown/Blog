using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

[Route("assets")]
[ApiController]
public class AssetsController(IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("{fileName}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public IActionResult Serve(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Contains(".."))
            return BadRequest();

        var assetsPath = Path.Combine(env.WebRootPath, "assets");
        var filePath = Path.Combine(assetsPath, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".avif" => "image/avif",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };

        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        Response.Headers["ETag"] = $"\"{fileName}\"";

        if (Request.Headers.IfNoneMatch.Contains($"\"{fileName}\""))
            return StatusCode(304);

        return PhysicalFile(filePath, contentType);
    }
}
