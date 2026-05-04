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
            // Dedupe theo QuestionAnswerId, ưu tiên giá trị cuối nếu FE gửi trùng.
            var incomingMap = incomingAnswers
                .GroupBy(a => a.QuestionAnswerId)
                .ToDictionary(g => g.Key, g => g.Last().Response);

            var existingByQaId = existingAnswers.ToDictionary(a => a.QuestionAnswerId);

            foreach (var (questionAnswerId, response) in incomingMap)
            {
                if (existingByQaId.TryGetValue(questionAnswerId, out var existing))
                {
                    existing.Response = response;
                }
                else
                {
                    existingAnswers.Add(new StudentAnswer
                    {
                        SubmissionId = submissionId,
                        QuestionAnswerId = questionAnswerId,
                        Response = response
                    });
                }
            }

            var toRemove = existingAnswers
                .Where(a => !incomingMap.ContainsKey(a.QuestionAnswerId))
                .ToList();

            foreach (var sa in toRemove)
            {
                existingAnswers.Remove(sa);
            }
        }
    }
}
