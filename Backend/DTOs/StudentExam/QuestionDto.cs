namespace Backend.DTOs.StudentExam
{
    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public int MainQuestionAnswerId { get; set; } // The primary QuestionAnswerId for single-answer questions
        public string ContentLatex { get; set; } = string.Empty;
        public string? Frame { get; set; } // Answer template with \placeholder{} markers
        public List<int>? FrameAnswerIds { get; set; } // Ordered QuestionAnswerId list, 1:1 with placeholders
        public Dictionary<int, List<string>>? FrameAllowedInputs { get; set; } // Map QuestionAnswerId -> Allowed InputType names
        public string QuestionType { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        
        // This holds the actual typed answers natively for fill-in-the-blank or multiple answers
        public string? Answer { get; set; }

        // Clean structured data properties for the frontend to render safely
        public List<QuestionOptionDto>? Options { get; set; }
        public List<QuestionStepDto>? Steps { get; set; }
    }

    public class QuestionOptionDto
    {
        public string Id { get; set; } = string.Empty; // "A", "B", "C", "D" map down to real original correct answers.
        public string Text { get; set; } = string.Empty; // the text string
    }

    public class QuestionStepDto
    {
        public int Step { get; set; }
        public string? Hint { get; set; } // We intentionally don't expose 'a'/Answer/Answer field to client.
    }

    public class QuestionContentFormat
    {
        public string stem { get; set; } = null!;
        public string frame { get; set; } = null!;
    }
}
