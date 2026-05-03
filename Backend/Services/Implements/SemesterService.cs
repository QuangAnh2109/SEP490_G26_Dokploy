using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs;
using Backend.DTOs.Curriculum;
using Backend.DTOs.Curriculum.Semester;
using Backend.Jobs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Services.Implements;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepo;
    private readonly IClassRepository _classRepo;
    private readonly IExamRepository _examRepo;
    private readonly IExamStatusScheduler _examScheduleService;
    private readonly TimeProvider _timeProvider;

    public SemesterService(
        ISemesterRepository semesterRepo,
        IClassRepository classRepo,
        IExamRepository examRepo,
        IExamStatusScheduler examScheduleService,
        TimeProvider timeProvider)
    {
        _semesterRepo = semesterRepo;
        _classRepo = classRepo;
        _examRepo = examRepo;
        _examScheduleService = examScheduleService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PagedResultDto<SemesterListItem>>> ListAsync(CurriculumListQuery query)
    {
        var (semesters, total) = await _semesterRepo.GetAllAsync(query);
        var items = semesters.Select(s => new SemesterListItem
        {
            SemesterId = s.SemesterId,
            Code = s.Code,
            Name = s.Name,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status,
            ClassCount = s.Classes.Count,
            ActiveClassCount = s.Classes.Count(c => c.Status == ClassStatus.Active),
            ActiveExamCount = s.Classes.SelectMany(c => c.Exams).Count(e => e.Status == ExamStatus.Published || e.Status == ExamStatus.InProgress),
            ConcurrencyStamp = Convert.ToBase64String(s.ConcurrencyStamp)
        }).ToList();

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);
        return new PagedResultDto<SemesterListItem>(page, size, total, items);
    }

    public async Task<Result<SemesterDetail>> GetByIdAsync(int id)
    {
        var semester = await _semesterRepo.GetByIdAsync(id);
        if (semester == null) return SemesterErrors.NotFound;

        return MapToDetail(semester);
    }

    public async Task<Result<SemesterDetail>> CreateAsync(CreateSemesterRequest request, int userId)
    {
        var existing = await _semesterRepo.GetByCodeAsync(request.Code);
        if (existing != null) return SemesterErrors.CodeDuplicate;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var semester = new Semester
        {
            Code = request.Code,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = SemesterStatus.Active,
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedByUserId = userId,
            UpdatedAtUtc = now
        };

        await _semesterRepo.AddAsync(semester);
        await _semesterRepo.SaveChangesAsync();

        var created = await _semesterRepo.GetByIdAsync(semester.SemesterId);
        return MapToDetail(created!);
    }

    public async Task<Result<SemesterDetail>> UpdateAsync(int id, UpdateSemesterRequest request, int userId)
    {
        var semester = await _semesterRepo.GetByIdAsync(id);
        if (semester == null) return SemesterErrors.NotFound;

        var incomingStamp = Convert.FromBase64String(request.ConcurrencyStamp);
        if (!semester.ConcurrencyStamp.SequenceEqual(incomingStamp))
        {
            return SemesterErrors.ConcurrentUpdate;
        }

        semester.Name = request.Name;
        semester.StartDate = request.StartDate;
        semester.EndDate = request.EndDate;
        semester.UpdatedByUserId = userId;
        semester.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _semesterRepo.SaveChangesAsync();

        var updated = await _semesterRepo.GetByIdAsync(semester.SemesterId);
        return MapToDetail(updated!);
    }

    public async Task<Result<CloseSemesterResult>> CloseAsync(int id, int adminUserId)
    {
        var semester = await _semesterRepo.GetByIdAsync(id);
        if (semester == null) return SemesterErrors.NotFound;
        if (semester.Status == SemesterStatus.Closed) return SemesterErrors.AlreadyClosed;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        semester.Status = SemesterStatus.Closed;
        semester.UpdatedByUserId = adminUserId;
        semester.UpdatedAtUtc = now;

        var cascadedClassCount = await _classRepo.BulkCloseBySemesterAsync(id);
        var cascadedExamIds = await _examRepo.BulkCloseBySemesterAsync(id, now);

        foreach (var examId in cascadedExamIds)
        {
            await _examScheduleService.CancelExamJobsAsync(examId);
        }

        await _semesterRepo.SaveChangesAsync();

        return new CloseSemesterResult
        {
            CascadedClassCount = cascadedClassCount,
            CascadedExamCount = cascadedExamIds.Count
        };
    }

    private static SemesterDetail MapToDetail(Semester s)
    {
        return new SemesterDetail
        {
            SemesterId = s.SemesterId,
            Code = s.Code,
            Name = s.Name,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status,
            ClassCount = s.Classes.Count,
            ActiveClassCount = s.Classes.Count(c => c.Status == ClassStatus.Active),
            ActiveExamCount = s.Classes.SelectMany(c => c.Exams).Count(e => e.Status == ExamStatus.Published || e.Status == ExamStatus.InProgress),
            CreatedByName = s.CreatedByUser?.FullName,
            CreatedAtUtc = s.CreatedAtUtc,
            UpdatedByName = s.UpdatedByUser?.FullName,
            UpdatedAtUtc = s.UpdatedAtUtc,
            ConcurrencyStamp = Convert.ToBase64String(s.ConcurrencyStamp)
        };
    }
}
