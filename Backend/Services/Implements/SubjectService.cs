using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Common.Models;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs;
using Backend.DTOs.Curriculum;
using Backend.DTOs.Curriculum.Subject;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using FluentValidation;

namespace Backend.Services.Implements;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _repo;
    private readonly TimeProvider _timeProvider;
    public SubjectService(
        ISubjectRepository repo,
        TimeProvider timeProvider)
    {
        _repo = repo;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PagedResultDto<SubjectListItem>>> ListAsync(CurriculumListQuery query)
    {
        var (items, total) = await _repo.ListAsync(query);
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);
        return new PagedResultDto<SubjectListItem>(page, size, total, items);
    }

    public async Task<Result<SubjectDetail>> GetAsync(int id)
    {
        var detail = await _repo.GetDetailAsync(id);
        if (detail == null)
            return SubjectErrors.NotFound;

        return Result<SubjectDetail>.Success(detail);
    }

    public async Task<Result<SubjectDetail>> CreateAsync(CreateSubjectRequest request, int adminUserId)
    {
        var existing = await _repo.GetByCodeAsync(request.Code);
        if (existing != null)
            return SubjectErrors.CodeDuplicate;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var subject = new Subject
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Status = SubjectStatus.Active,
            CreatedByUserId = adminUserId,
            CreatedAtUtc = now,
            UpdatedByUserId = adminUserId,
            UpdatedAtUtc = now
        };

        await _repo.AddAsync(subject);
        await _repo.SaveChangesAsync();

        var detail = await _repo.GetDetailAsync(subject.SubjectId);
        return Result<SubjectDetail>.Success(detail!);
    }

    public async Task<Result<SubjectDetail>> UpdateAsync(int id, UpdateSubjectRequest request, int adminUserId)
    {
        var subject = await _repo.GetByIdAsync(id);
        if (subject == null)
            return SubjectErrors.NotFound;

        var currentStamp = Convert.ToBase64String(subject.ConcurrencyStamp);
        if (currentStamp != request.ConcurrencyStamp)
            return SubjectErrors.ConcurrentUpdate;

        subject.Name = request.Name;
        subject.Description = request.Description;
        subject.UpdatedByUserId = adminUserId;
        subject.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _repo.UpdateAsync(subject);
        await _repo.SaveChangesAsync();

        var detail = await _repo.GetDetailAsync(subject.SubjectId);
        return Result<SubjectDetail>.Success(detail!);
    }

    public async Task<Result> CloseAsync(int id, int adminUserId)
    {
        var subject = await _repo.GetByIdAsync(id);
        if (subject == null)
            return SubjectErrors.NotFound;

        if (subject.Status == SubjectStatus.Closed)
            return SubjectErrors.AlreadyClosed;

        subject.Status = SubjectStatus.Closed;
        subject.UpdatedByUserId = adminUserId;
        subject.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _repo.UpdateAsync(subject);
        await _repo.SaveChangesAsync();

        return Result.Success();
    }
}
