using Backend.Models;

namespace Backend.Helpers
{
    public static class StudentAnswerSyncHelper
    {
        /// <summary>
        /// Đồng bộ danh sách câu trả lời của học sinh vào database.
        /// Thêm câu trả lời mới, cập nhật câu trả lời đã có, và xóa các câu trả lời không còn được chọn.
        /// </summary>
        public static void SyncAnswers(
            ICollection<StudentAnswer> existingAnswers,
            IEnumerable<(int QuestionAnswerId, string? Response)> incomingAnswers,
            int submissionId)
        {
            var incomingMap = incomingAnswers.ToDictionary(a => a.QuestionAnswerId);
            
            // 1. Cập nhật câu trả lời cũ hoặc thêm mới
            foreach (var dto in incomingAnswers)
            {
                var existing = existingAnswers.FirstOrDefault(a => a.QuestionAnswerId == dto.QuestionAnswerId);
                if (existing != null)
                {
                    existing.Response = dto.Response;
                }
                else
                {
                    existingAnswers.Add(new StudentAnswer
                    {
                        SubmissionId = submissionId,
                        QuestionAnswerId = dto.QuestionAnswerId,
                        Response = dto.Response
                    });
                }
            }

            // 2. Xóa các câu trả lời không còn trong request
            var incomingIds = incomingMap.Keys.ToHashSet();
            var toRemove = existingAnswers.Where(a => !incomingIds.Contains(a.QuestionAnswerId)).ToList();
            
            foreach (var sa in toRemove)
            {
                existingAnswers.Remove(sa);
            }
        }
    }
}
