using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Api.DTOs;
using StudentManagement.Api.Services;

namespace StudentManagement.Api.Controllers
{
    [ApiController]
    [Authorize]
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;

        public AssignmentsController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpGet("api/courses/{courseId}/assignments")]
        public async Task<IActionResult> GetCourseAssignments(int courseId)
        {
            var (success, statusCode, error, data) = await _assignmentService.GetCourseAssignmentsAsync(courseId);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpPost("api/courses/{courseId}/assignments")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> CreateAssignment(int courseId, [FromBody] CreateAssignmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _assignmentService.CreateAssignmentAsync(courseId, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return StatusCode(201, data);
        }

        [HttpGet("api/assignments/{id}")]
        public async Task<IActionResult> GetAssignmentById(int id)
        {
            var (success, statusCode, error, data) = await _assignmentService.GetAssignmentByIdAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpPut("api/assignments/{id}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateAssignmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _assignmentService.UpdateAssignmentAsync(id, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpDelete("api/assignments/{id}")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var (success, statusCode, error) = await _assignmentService.DeleteAssignmentAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return NoContent();
        }

        [HttpPost("api/assignments/{id}/submissions")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> SubmitAssignment(int id)
        {
            string textContent = null;
            IFormFile file = null;

            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                if (form.ContainsKey("textContent"))
                {
                    textContent = form["textContent"];
                }
                if (form.Files.Count > 0)
                {
                    file = form.Files[0];
                }
            }
            else if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
            {
                using var reader = new System.IO.StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("textContent", out var textProp))
                        {
                            textContent = textProp.GetString();
                        }
                    }
                    catch { }
                }
            }

            var (success, statusCode, error, data) = await _assignmentService.SubmitAssignmentAsync(id, textContent, file);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpGet("api/assignments/{id}/submissions/me")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetMySubmission(int id)
        {
            var (success, statusCode, error, data) = await _assignmentService.GetMySubmissionAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpGet("api/assignments/{id}/submissions")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> GetAssignmentSubmissions(int id)
        {
            var (success, statusCode, error, data) = await _assignmentService.GetAssignmentSubmissionsAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }
    }
}
