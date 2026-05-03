using Backend.Repositories.Interfaces;

namespace Backend.Repositories.Implements
{
    public class ExamRepository : IExamRepository
    {
        private readonly Models.MtcaSep490G26Context _context;

        public ExamRepository(Models.MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<int>> BulkCloseBySemesterAsync(int semesterId, System.DateTime now)
        {
            var examIds = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                System.Linq.Queryable.Select(
                    System.Linq.Queryable.Where(_context.Exams, e => e.Class!.SemesterId == semesterId
                             && (e.Status == Backend.Constants.ExamStatus.Ready
                              || e.Status == Backend.Constants.ExamStatus.Published
                              || e.Status == Backend.Constants.ExamStatus.InProgress)),
                    e => e.ExamId)
            );

            if (examIds.Count > 0)
            {
                await Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync(
                    System.Linq.Queryable.Where(_context.Exams, e => examIds.Contains(e.ExamId)),
                    s => s.SetProperty(e => e.Status, Backend.Constants.ExamStatus.Closed)
                          .SetProperty(e => e.UpdatedAtUtc, now)
                );
            }
            return examIds;
        }
    }
}
