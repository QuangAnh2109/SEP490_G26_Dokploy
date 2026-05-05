using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest.AnalyticsTests
{
    public class GetExamSubmitResultsAsync_UTCID_Tests
    {
        private readonly Mock<IAnalyticsRepository> _analyticsRepoMock;
        private readonly Mock<IStudentExamRepository> _studentExamRepoMock;
        private readonly AnalyticsService _service;

        public GetExamSubmitResultsAsync_UTCID_Tests()
        {
            _analyticsRepoMock = new Mock<IAnalyticsRepository>(MockBehavior.Strict);
            _studentExamRepoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
            _service = new AnalyticsService(_analyticsRepoMock.Object, _studentExamRepoMock.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetExamSubmitResultsAsync - UTCID01 - Có class members và submissions -> build đúng trạng thái/histories")]
        public async Task GetExamSubmitResultsAsync_UTCID01_ClassMembersAndSubmissions_ShouldReturnCorrectResults()
        {
            int examId = 1;
            var exam = BuildExamWithClass(examId, maxAttempts: 2);
            var classMembers = BuildClassMembers();

            _studentExamRepoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                .ReturnsAsync(exam);
            _analyticsRepoMock.Setup(r => r.GetClassMembersWithStudentsAsync(exam.ClassId!.Value))
                .ReturnsAsync(classMembers);

            var result = await _service.GetExamSubmitResultsAsync(examId);

            Assert.NotNull(result);
            Assert.Equal(examId, result.ExamId);
            Assert.Equal("Submit Results Exam", result.ExamTitle);
            Assert.Equal("SEP490", result.ClassName);
            Assert.Equal(2, result.MaxAttempts);
            Assert.Equal(3, result.TotalStudents);
            Assert.Equal(1, result.SubmittedCount);

            var submitted = result.Students.Single(x => x.StudentId == 1);
            Assert.Equal("Đã nộp", submitted.Status);
            Assert.Equal(2, submitted.AttemptCount);
            Assert.NotEmpty(submitted.History);

            var inProgress = result.Students.Single(x => x.StudentId == 2);
            Assert.Equal("Đang làm", inProgress.Status);

            var absent = result.Students.Single(x => x.StudentId == 3);
            Assert.Equal("Vắng thi", absent.Status);

            _studentExamRepoMock.VerifyAll();
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamSubmitResultsAsync - UTCID02 - Không có class members -> lấy học sinh từ submissions")]
        public async Task GetExamSubmitResultsAsync_UTCID02_NoClassMembers_ShouldFallbackToStudentsFromSubmissions()
        {
            int examId = 2;
            var exam = BuildExamWithoutClass(examId, maxAttempts: 1);

            _studentExamRepoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                .ReturnsAsync(exam);

            var result = await _service.GetExamSubmitResultsAsync(examId);

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalStudents);
            Assert.Equal(1, result.SubmittedCount);
            Assert.Null(result.ClassName);

            _studentExamRepoMock.VerifyAll();
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamSubmitResultsAsync - UTCID03 - MaxAttempts <= 0 -> fallback 999")]
        public async Task GetExamSubmitResultsAsync_UTCID03_MaxAttemptsInvalid_ShouldFallbackTo999()
        {
            int examId = 3;
            var exam = BuildExamWithClass(examId, maxAttempts: 0);
            var classMembers = BuildClassMembers();

            _studentExamRepoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                .ReturnsAsync(exam);
            _analyticsRepoMock.Setup(r => r.GetClassMembersWithStudentsAsync(exam.ClassId!.Value))
                .ReturnsAsync(classMembers);

            var result = await _service.GetExamSubmitResultsAsync(examId);

            Assert.Equal(999, result.MaxAttempts);

            _studentExamRepoMock.VerifyAll();
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamSubmitResultsAsync - UTCID04 - Exam không tồn tại -> KeyNotFoundException")]
        public async Task GetExamSubmitResultsAsync_UTCID04_ExamNotFound_ShouldThrowKeyNotFoundException()
        {
            int examId = 999;

            _studentExamRepoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                .ReturnsAsync((Exam?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetExamSubmitResultsAsync(examId));

            Assert.Equal($"Không tìm thấy bài thi với ID {examId}.", ex.Message);

            _studentExamRepoMock.VerifyAll();
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetExamSubmitResultsAsync - UTCID05 - ClassName null và fallback StudentCode/FullName/Duration")]
        public async Task GetExamSubmitResultsAsync_UTCID05_NullClassNameAndStudentFallbacks_ShouldReturnFallbackValues()
        {
            int examId = 5;

            var inProgressStudent = new User
            {
                UserId = 1,
                Email = "fallback@email.com",
                FullName = null,
                StudentId = null,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var submittedStudent = new User
            {
                UserId = 2,
                Email = "s2@x.com",
                FullName = "Student 2",
                StudentId = "HE170002",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var exam = new Exam
            {
                ExamId = examId,
                ClassId = 100,
                Class = null!,
                Title = "Fallback Submit Results",
                Duration = 60,
                MaxAttempts = 1,
                TeacherId = 99,
                SubjectId = 1,
                ShowScore = 1,
                ShowAnswer = 1,
                AnswerTimingMode = 0,
                Status = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question>(),
                        Submissions = new List<Submission>
                        {
                            new Submission
                            {
                                SubmissionId = 1,
                                StudentId = 1,
                                PaperId = 10,
                                CreatedAtUtc = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                                UpdatedAtUtc = new DateTime(2026, 4, 1, 8, 30, 0, DateTimeKind.Utc),
                                TotalPoints = null,
                                Status = SubmissionStatus.InProgress,
                                Student = inProgressStudent,
                                ConcurrencyStamp = Array.Empty<byte>()
                            },
                            new Submission
                            {
                                SubmissionId = 2,
                                StudentId = 2,
                                PaperId = 10,
                                CreatedAtUtc = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                                UpdatedAtUtc = new DateTime(2026, 4, 1, 8, 45, 0, DateTimeKind.Utc),
                                TotalPoints = 8m,
                                Status = SubmissionStatus.Submitted,
                                Student = submittedStudent,
                                ConcurrencyStamp = Array.Empty<byte>()
                            }
                        }
                    }
                }
            };

            var classMembers = new List<ClassMember>
            {
                new ClassMember { ClassId = 100, StudentId = 1, MemberStatus = 1, Student = inProgressStudent, ConcurrencyStamp = Array.Empty<byte>() },
                new ClassMember { ClassId = 100, StudentId = 2, MemberStatus = 1, Student = submittedStudent, ConcurrencyStamp = Array.Empty<byte>() },
                new ClassMember { ClassId = 100, StudentId = 3, MemberStatus = 1, Student = null!, ConcurrencyStamp = Array.Empty<byte>() }
            };

            _studentExamRepoMock.Setup(r => r.ForceSubmitOverdueExamsAsync(examId))
                .Returns(Task.CompletedTask);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(examId))
                .ReturnsAsync(exam);
            _analyticsRepoMock.Setup(r => r.GetClassMembersWithStudentsAsync(100))
                .ReturnsAsync(classMembers);

            var result = await _service.GetExamSubmitResultsAsync(examId);

            Assert.Null(result.ClassName);
            Assert.Equal(3, result.TotalStudents);

            var inProgress = result.Students.Single(x => x.StudentId == 1);
            Assert.Equal("#1", inProgress.StudentCode);
            Assert.Equal("fallback@email.com", inProgress.FullName);
            Assert.Null(inProgress.LastSubmitAt);
            Assert.NotNull(inProgress.DurationFormatted);
            Assert.Null(inProgress.LastScore);

            var absent = result.Students.Single(x => x.StudentId == 3);
            Assert.Equal("#3", absent.StudentCode);
            Assert.Equal("Học sinh #3", absent.FullName);
            Assert.Null(absent.LastSubmitAt);
            Assert.Null(absent.DurationFormatted);
            Assert.Equal("Vắng thi", absent.Status);

            _studentExamRepoMock.VerifyAll();
            _analyticsRepoMock.VerifyAll();
        }

        private static Exam BuildExamWithClass(int examId, int maxAttempts)
        {
            var student1 = new User
            {
                UserId = 1,
                Email = "s1@x.com",
                FullName = "Student 1",
                StudentId = "HE170001",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var student2 = new User
            {
                UserId = 2,
                Email = "s2@x.com",
                FullName = "Student 2",
                StudentId = "HE170002",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var submissionSubmitted1 = new Submission
            {
                SubmissionId = 1,
                StudentId = 1,
                PaperId = 10,
                CreatedAtUtc = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAtUtc = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                TotalPoints = 8m,
                Status = SubmissionStatus.Submitted,
                Student = student1,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var submissionInProgress1 = new Submission
            {
                SubmissionId = 2,
                StudentId = 1,
                PaperId = 10,
                CreatedAtUtc = new DateTime(2026, 4, 1, 7, 0, 0, DateTimeKind.Utc),
                UpdatedAtUtc = new DateTime(2026, 4, 1, 7, 30, 0, DateTimeKind.Utc),
                TotalPoints = null,
                Status = SubmissionStatus.InProgress,
                Student = student1,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var submissionInProgress2 = new Submission
            {
                SubmissionId = 3,
                StudentId = 2,
                PaperId = 10,
                CreatedAtUtc = new DateTime(2026, 4, 1, 8, 15, 0, DateTimeKind.Utc),
                UpdatedAtUtc = new DateTime(2026, 4, 1, 8, 45, 0, DateTimeKind.Utc),
                TotalPoints = null,
                Status = SubmissionStatus.InProgress,
                Student = student2,
                ConcurrencyStamp = Array.Empty<byte>()
            };

            return new Exam
            {
                ExamId = examId,
                ClassId = 100,
                Class = new Class
                {
                    ClassId = 100,
                    Name = "SEP490",
                    TeacherId = 99,
                    SubjectId = 1,
                    Semester = "SP26",
                    InvitationCode = "ABC",
                    InvitationCodeStatus = 1,
                    Status = 1,
                    CreatedAtUtc = DateTime.UtcNow,
                    ConcurrencyStamp = Array.Empty<byte>()
                },
                Title = "Submit Results Exam",
                Duration = 60,
                MaxAttempts = maxAttempts,
                TeacherId = 99,
                SubjectId = 1,
                ShowScore = 1,
                ShowAnswer = 1,
                AnswerTimingMode = 0,
                Status = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question>(),
                        Submissions = new List<Submission>
                        {
                            submissionSubmitted1,
                            submissionInProgress1,
                            submissionInProgress2
                        }
                    }
                }
            };
        }

        private static Exam BuildExamWithoutClass(int examId, int maxAttempts)
        {
            var student1 = new User
            {
                UserId = 1,
                Email = "s1@x.com",
                FullName = "Student 1",
                StudentId = "HE170001",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var student2 = new User
            {
                UserId = 2,
                Email = "s2@x.com",
                FullName = "Student 2",
                StudentId = "HE170002",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            return new Exam
            {
                ExamId = examId,
                ClassId = null,
                Title = "Submit Results No Class",
                Duration = 45,
                MaxAttempts = maxAttempts,
                TeacherId = 99,
                SubjectId = 1,
                ShowScore = 1,
                ShowAnswer = 1,
                AnswerTimingMode = 0,
                Status = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 20,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question>(),
                        Submissions = new List<Submission>
                        {
                            new Submission
                            {
                                SubmissionId = 10,
                                StudentId = 1,
                                PaperId = 20,
                                CreatedAtUtc = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                                UpdatedAtUtc = new DateTime(2026, 4, 1, 8, 50, 0, DateTimeKind.Utc),
                                TotalPoints = 9m,
                                Status = SubmissionStatus.Submitted,
                                Student = student1,
                                ConcurrencyStamp = Array.Empty<byte>()
                            },
                            new Submission
                            {
                                SubmissionId = 11,
                                StudentId = 2,
                                PaperId = 20,
                                CreatedAtUtc = new DateTime(2026, 4, 1, 8, 10, 0, DateTimeKind.Utc),
                                UpdatedAtUtc = new DateTime(2026, 4, 1, 8, 30, 0, DateTimeKind.Utc),
                                TotalPoints = null,
                                Status = SubmissionStatus.InProgress,
                                Student = student2,
                                ConcurrencyStamp = Array.Empty<byte>()
                            }
                        }
                    }
                }
            };
        }

        private static List<ClassMember> BuildClassMembers()
        {
            var s1 = new User
            {
                UserId = 1,
                Email = "s1@x.com",
                FullName = "Student 1",
                StudentId = "HE170001",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var s2 = new User
            {
                UserId = 2,
                Email = "s2@x.com",
                FullName = "Student 2",
                StudentId = "HE170002",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var s3 = new User
            {
                UserId = 3,
                Email = "s3@x.com",
                FullName = "Student 3",
                StudentId = "HE170003",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            return new List<ClassMember>
            {
                new ClassMember { ClassId = 100, StudentId = 1, MemberStatus = 1, Student = s1, ConcurrencyStamp = Array.Empty<byte>() },
                new ClassMember { ClassId = 100, StudentId = 2, MemberStatus = 1, Student = s2, ConcurrencyStamp = Array.Empty<byte>() },
                new ClassMember { ClassId = 100, StudentId = 3, MemberStatus = 1, Student = s3, ConcurrencyStamp = Array.Empty<byte>() }
            };
        }
    }
}