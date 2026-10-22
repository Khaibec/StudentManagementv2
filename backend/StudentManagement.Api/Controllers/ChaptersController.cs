using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Services;

namespace StudentManagement.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class ChaptersController : ControllerBase
    {
        private readonly IContentService _contentService;

        public ChaptersController(IContentService contentService)
        {
            _contentService = contentService;
        }

        [HttpGet("api/courses/{courseId}/chapters")]
        public async Task<IActionResult> GetCourseChapters(int courseId)
        {
            var (success, statusCode, error, data) = await _contentService.GetCourseChaptersAsync(courseId);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpPost("api/courses/{courseId}/chapters")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> CreateChapter(int courseId, [FromBody] CreateChapterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _contentService.CreateChapterAsync(courseId, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return StatusCode(statusCode, data);
        }

        [HttpPut("api/chapters/{chapterId}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> UpdateChapter(int chapterId, [FromBody] UpdateChapterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _contentService.UpdateChapterAsync(chapterId, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpDelete("api/chapters/{chapterId}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> DeleteChapter(int chapterId)
        {
            var (success, statusCode, error) = await _contentService.DeleteChapterAsync(chapterId);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return NoContent();
        }

        [HttpPost("api/chapters/{chapterId}/lessons")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> CreateLesson(int chapterId, [FromBody] CreateLessonRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _contentService.CreateLessonAsync(chapterId, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return StatusCode(statusCode, data);
        }
    }
}
