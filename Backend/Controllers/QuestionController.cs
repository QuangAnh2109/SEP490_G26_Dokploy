using Backend.DTOs.Question;
using Backend.Exceptions;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/questions")]
    [ApiController]
    [Authorize(Roles = "Teacher")]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionsAsync([FromQuery] QuestionListQueryDto query)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _questionService.GetQuestionsAsync(query, userId);
            return Ok(result);
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateQuestionBatchAsync([FromBody] CreateQuestionBatchRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                {
                    return Unauthorized(new { message = "Invalid token." });
                }

                var result = await _questionService.CreateQuestionsAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("input-types")]
        public async Task<IActionResult> GetInputTypesAsync()
        {
            var result = await _questionService.GetInputTypesAsync();
            return Ok(result);
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjectsAsync()
        {
            var result = await _questionService.GetSubjectsWithChaptersAsync();
            return Ok(result);
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
                QuestionValidationException vex => BadRequest(new { message = vex.Message, errors = vex.Errors }),
                KeyNotFoundException kex => NotFound(new { message = kex.Message }),
                UnauthorizedAccessException => Forbid(),
                _ => StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình xử lý câu hỏi.", details = ex.Message })
            };
        }
    }
}
