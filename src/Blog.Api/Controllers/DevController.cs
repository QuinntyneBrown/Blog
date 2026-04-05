using Blog.Api.Common.Attributes;
using Blog.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Blog.Api.Controllers;

/// <summary>Development-only utilities. NOT available in production.</summary>
[ApiController]
[Route("dev")]
[RawResponse]
public class DevController(IPasswordHasher passwordHasher, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("hash-password")]
    public IActionResult HashPassword([FromQuery] string password)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(password))
            return BadRequest("password query param is required");

        return Ok(new { hash = passwordHasher.HashPassword(password) });
    }
}
