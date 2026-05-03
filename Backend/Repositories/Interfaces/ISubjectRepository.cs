using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.DTOs.Curriculum;
using Backend.DTOs.Curriculum.Subject;
using Backend.Models;

namespace Backend.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<(List<SubjectListItem> Items, int Total)> ListAsync(CurriculumListQuery query);
    Task<SubjectDetail?> GetDetailAsync(int subjectId);
    Task<Subject?> GetByCodeAsync(string code);
    Task<Subject?> GetByIdAsync(int subjectId);
    Task AddAsync(Subject subject);
    Task UpdateAsync(Subject subject);
    Task SaveChangesAsync();
}
