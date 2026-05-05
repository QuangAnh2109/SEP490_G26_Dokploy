using Backend.Common;
using Backend.DTOs.ExamBlueprint;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/exam-blueprints")]
    [ApiController]
    public class ExamBlueprintController(IExamBlueprintService examBlueprintService) : ControllerBase
    {
        private readonly IExamBlueprintService _examBlueprintService = examBlueprintService;

        [HttpGet]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> GetBlueprints([FromQuery] BlueprintListQueryDto query)
        {
            var result = await _examBlueprintService.GetBlueprintsAsync(query);
            return result.ToActionResult(this);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> GetBlueprintDetail(int id)
        {
            var result = await _examBlueprintService.GetBlueprintDetailAsync(id);
            return result.ToActionResult(this);
        }

        [HttpGet("subjects")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> GetSubjects()
        {
            var result = await _examBlueprintService.GetSubjectsAsync();
            return result.ToActionResult(this);
        }

        [HttpGet("subjects/{subjectId:int}/chapters")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> GetChaptersBySubject(int subjectId)
        {
            var result = await _examBlueprintService.GetChaptersBySubjectAsync(subjectId);
            return result.ToActionResult(this);
        }

        [HttpPost]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> CreateBlueprint([FromBody] CreateExamBlueprintRequest request)
        {
            var result = await _examBlueprintService.CreateBlueprintAsync(request);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetBlueprintDetail), new { id = result.Value.ExamBlueprintId }, result.Value);
            }
            return result.ToActionResult(this);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> UpdateBlueprint(int id, [FromBody] CreateExamBlueprintRequest request)
        {
            var result = await _examBlueprintService.UpdateBlueprintAsync(id, request);
            return result.ToActionResult(this);
        }

        [HttpPatch("status")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> UpdateBlueprintStatus([FromBody] BlueprintStatusUpdateDto request)
        {
            var result = await _examBlueprintService.UpdateBlueprintStatusAsync(request.ExamBlueprintIds!, request.Status!.Value);
            if (result.IsSuccess)
            {
                return Ok(new { message = $"Đã lưu trữ {result.Value} ma trận đề thành công.", count = result.Value });
            }
            return result.ToActionResult(this);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleIds.Teacher)]
        public async Task<IActionResult> DeleteBlueprint(int id)
        {
            var result = await _examBlueprintService.DeleteBlueprintAsync(id);
            if (result.IsSuccess)
            {
                return NoContent();
            }
            return result.ToActionResult(this);
        }
    }
}
