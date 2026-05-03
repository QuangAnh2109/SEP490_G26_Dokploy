namespace Backend.Repositories.Interfaces
{
    public interface IExamRepository
    {
        Task<IReadOnlyList<int>> BulkCloseBySemesterAsync(int semesterId, System.DateTime now);
    }
}
