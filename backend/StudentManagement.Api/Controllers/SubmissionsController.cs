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
    public class SubmissionsController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;

        public SubmissionsController(IAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpPut("{id}/grade")]
        [Authorize(Roles = "TEACHER,ADMIN")]
        public async Task<IActionResult> GradeSubmission(int id, [FromBody] GradeSubmissionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, statusCode, error, data) = await _assignmentService.GradeSubmissionAsync(id, request);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return Ok(data);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadSubmissionFile(int id)
        {
            var (success, statusCode, error, fileStream, contentType, fileName) = await _assignmentService.DownloadSubmissionFileAsync(id);
            if (!success)
            {
                return StatusCode(statusCode, new { message = error });
            }

            return File(fileStream, contentType, fileName);
        }
    }
}
