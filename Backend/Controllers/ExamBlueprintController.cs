using Backend.Common;
using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/exam-blueprints")]
    [ApiController]
    [Authorize(Policy = nameof(Roles.Teacher))]
    public class ExamBlueprintController : ControllerBase
    {
        private readonly IExamBlueprintService _examBlueprintService;

        public ExamBlueprintController(IExamBlueprintService examBlueprintService)
        {
            _examBlueprintService = examBlueprintService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlueprints([FromQuery] BlueprintListQueryDto query)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                var result = await _examBlueprintService.GetBlueprintsAsync(query, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBlueprintDetail(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                var result = await _examBlueprintService.GetBlueprintDetailAsync(id, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            try
            {
                var result = await _examBlueprintService.GetSubjectsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("subjects/{subjectId:int}/chapters")]
        public async Task<IActionResult> GetChaptersBySubject(int subjectId)
        {
            try
            {
                var result = await _examBlueprintService.GetChaptersBySubjectAsync(subjectId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlueprint([FromBody] CreateExamBlueprintRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                var result = await _examBlueprintService.CreateBlueprintAsync(userId, request);
                return CreatedAtAction(nameof(GetBlueprintDetail), new { id = result.ExamBlueprintId }, result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBlueprint(int id, [FromBody] CreateExamBlueprintRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                var result = await _examBlueprintService.UpdateBlueprintAsync(id, userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPatch("status")]
        public async Task<IActionResult> UpdateBlueprintStatus([FromBody] BlueprintStatusUpdateDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                if (request == null || !(request.ExamBlueprintIds?.Any() ?? false))
                {
                    return BadRequest(new { message = "ExamBlueprintIds are required." });
                }

                var count = await _examBlueprintService.UpdateBlueprintStatusAsync(request.ExamBlueprintIds, userId, request.Status);
                return Ok(new { message = $"Đã lưu trữ {count} ma trận đề thành công.", count });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBlueprint(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                await _examBlueprintService.DeleteBlueprintAsync(id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdString, out var userId) ? userId : 0;
        }

        private IActionResult HandleException(Exception ex)
        {
            return ex switch
            {
                ExamBlueprintValidationException vex => BadRequest(new { message = vex.Message, errors = vex.Errors }),
                KeyNotFoundException kex => NotFound(new { message = kex.Message }),
                UnauthorizedAccessException => Forbid(),
                InvalidOperationException ioex => BadRequest(new { message = ioex.Message }),
                _ => StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình xử lý ma trận đề.", details = ex.Message })
            };
        }
    }
}
