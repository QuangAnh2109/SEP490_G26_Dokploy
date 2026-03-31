using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest
{
    public class ExamBlueprintGetChaptersUnitTest
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintGetChaptersUnitTest()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>();
            _service = new ExamBlueprintService(_mockRepo.Object);
        }

        // UTCID01 - Normal: SubjectId hợp lệ + Subject tồn tại trong DB
        //           → Trả về danh sách chương thành công
        [Fact]
        public async Task GetChaptersBySubjectAsync_UTCID01_ValidSubjectId_SubjectExists_ShouldReturnChapterList()
        {
            // Arrange
            var fakeChapters = new List<ChapterOptionDto>
            {
                new ChapterOptionDto { ChapterId = 1, Name = "Chương 1" },
                new ChapterOptionDto { ChapterId = 2, Name = "Chương 2" }
            };

            _mockRepo.Setup(r => r.SubjectExistsAsync(8))
                     .ReturnsAsync(true);

            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(8))
                     .ReturnsAsync(fakeChapters);

            // Act
            var result = await _service.GetChaptersBySubjectAsync(8);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Chương 1", result[0].Name);
            Assert.Equal("Chương 2", result[1].Name);
        }

        // UTCID02 - Normal: SubjectId đúng định dạng nhưng không có trong DB
        //           → KeyNotFoundException
        [Fact]
        public async Task GetChaptersBySubjectAsync_UTCID02_ValidSubjectId_SubjectNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _mockRepo.Setup(r => r.SubjectExistsAsync(8))
                     .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetChaptersBySubjectAsync(8));

            Assert.Equal("Không tìm thấy môn học.", exception.Message);

            // GetChaptersBySubjectAsync không được gọi
            _mockRepo.Verify(r => r.GetChaptersBySubjectAsync(It.IsAny<int>()), Times.Never);
        }

        // UTCID03 - Normal: SubjectId = 0 (không hợp lệ)
        //           → ExamBlueprintValidationException
        [Fact]
        public async Task GetChaptersBySubjectAsync_UTCID03_SubjectIdIsZero_ShouldThrowExamBlueprintValidationException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.GetChaptersBySubjectAsync(0));

            Assert.Contains("Môn học không hợp lệ.", exception.Errors);

            // Không gọi xuống repository
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.GetChaptersBySubjectAsync(It.IsAny<int>()), Times.Never);
        }

        // UTCID04 - Normal: SubjectId = -10 (âm, không hợp lệ)
        //           → ExamBlueprintValidationException
        [Fact]
        public async Task GetChaptersBySubjectAsync_UTCID04_SubjectIdIsNegative_ShouldThrowExamBlueprintValidationException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ExamBlueprintValidationException>(() =>
                _service.GetChaptersBySubjectAsync(-10));

            Assert.Contains("Môn học không hợp lệ.", exception.Errors);

            // Không gọi xuống repository
            _mockRepo.Verify(r => r.SubjectExistsAsync(It.IsAny<int>()), Times.Never);
            _mockRepo.Verify(r => r.GetChaptersBySubjectAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
