using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Services;

namespace StudentManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private readonly IContentService _contentService;

        public LessonsController(IContentService contentService)
        {
            _contentService = contentService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLessonDetail(int id)
        {
            var (success, statusCode, error, data) = await _contentService.GetLessonDetailAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> UpdateLesson(int id, [FromBody] UpdateLessonRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _contentService.UpdateLessonAsync(id, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var (success, statusCode, error) = await _contentService.DeleteLessonAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return NoContent();
        }

        [HttpPost("{id}/complete")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> CompleteLesson(int id)
        {
            var (success, statusCode, error, data) = await _contentService.CompleteLessonAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpPost("{id}/resources")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> UploadResource(int id, IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new { message = "Vui lòng chọn tệp tin cần tải lên" });
            }

            var (success, statusCode, error, data) = await _contentService.UploadResourceAsync(id, file);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return StatusCode(statusCode, data);
        }
    }
}
