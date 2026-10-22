using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Api.Services;

namespace StudentManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ResourcesController : ControllerBase
    {
        private readonly IContentService _contentService;

        public ResourcesController(IContentService contentService)
        {
            _contentService = contentService;
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadResource(int id)
        {
            var (success, statusCode, error, fileStream, contentType, fileName) = await _contentService.DownloadResourceAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return File(fileStream, contentType, fileName);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var (success, statusCode, error) = await _contentService.DeleteResourceAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return NoContent();
        }
    }
}
