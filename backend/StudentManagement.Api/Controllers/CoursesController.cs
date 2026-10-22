using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Services;

namespace StudentManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _courseService.GetCoursesAsync();
            return Ok(courses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseById(int id)
        {
            var (success, statusCode, error, data) = await _courseService.GetCourseByIdAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpPost]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _courseService.CreateCourseAsync(request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return CreatedAtAction(nameof(GetCourseById), new { id = data.Id }, data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _courseService.UpdateCourseAsync(id, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var (success, statusCode, error) = await _courseService.DeleteCourseAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return NoContent();
        }
    }
}
