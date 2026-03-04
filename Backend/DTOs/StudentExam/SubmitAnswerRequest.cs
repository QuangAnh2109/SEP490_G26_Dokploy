namespace Backend.DTOs.StudentExam
{
    public class SubmitAnswerRequest
    {
        public int QuestionAnswerId { get; set; }
        public string Response { get; set; } = string.Empty;
    }
}
