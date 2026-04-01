using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.DTOs.ExamBlueprint;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest
{
    public class ExamBlueprintGetSubjectsUnitTest
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintGetSubjectsUnitTest()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>();
            _service = new ExamBlueprintService(_mockRepo.Object);
        }

        // UTCID01 - Normal: Kết nối tốt + Có dữ liệu trong DB
        //           → Trả về danh sách môn học thành công
        [Fact]
        public async Task GetSubjectsAsync_UTCID01_ConnectionOk_DataExists_ShouldReturnSubjectList()
        {
            // Arrange
            var fakeSubjects = new List<SubjectOptionDto>
            {
                new SubjectOptionDto { SubjectId = 1, Name = "MAD" },
                new SubjectOptionDto { SubjectId = 2, Name = "MAE" },
                new SubjectOptionDto { SubjectId = 3, Name = "MAS" }
            };

            _mockRepo.Setup(r => r.GetSubjectsAsync())
                     .ReturnsAsync(fakeSubjects);

            // Act
            var result = await _service.GetSubjectsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("MAD", result[0].Name);
            Assert.Equal("MAE", result[1].Name);
            Assert.Equal("MAS", result[2].Name);
        }

        // UTCID02 - Boundary: Kết nối tốt nhưng DB trống
        //           → Trả về danh sách rỗng (vẫn thành công)
        [Fact]
        public async Task GetSubjectsAsync_UTCID02_ConnectionOk_EmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetSubjectsAsync())
                     .ReturnsAsync(new List<SubjectOptionDto>());

            // Act
            var result = await _service.GetSubjectsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // UTCID03 - Abnormal: Không thể kết nối tới server
        //           → Throw Exception "Can not connect to server"
        [Fact]
        public async Task GetSubjectsAsync_UTCID03_CannotConnectToServer_ShouldThrowException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetSubjectsAsync())
                     .ThrowsAsync(new Exception("Can not connect to server"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _service.GetSubjectsAsync());

            Assert.Equal("Can not connect to server", exception.Message);
        }
    }
}
