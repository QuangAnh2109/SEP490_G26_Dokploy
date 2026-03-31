using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest.AssignExamTests
{
    public class GetBlueprintDetailAsyncTests
    {
        private readonly Mock<IAssignExamRepository> _repoMock;
        private readonly AssignExamService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public GetBlueprintDetailAsyncTests()
        {
            _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
            _service = new AssignExamService(_repoMock.Object);
        }

        // UTCID01 – BlueprintId hợp lệ, có dữ liệu chi tiết
        [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID01 - BlueprintId hợp lệ, có dữ liệu chi tiết")]
        public async Task GetBlueprintDetailAsync_UTCID01_ValidId_ShouldReturnDetailList()
        {
            // Arrange
            int blueprintId = 1;
            var expected = new List<BlueprintDetailRowDto>
            {
                new BlueprintDetailRowDto(
                    ChapterId: 10,
                    ChapterName: "Chương 1",
                    Difficulty: 1,
                    TotalOfQuestions: 5
                ),
                new BlueprintDetailRowDto(
                    ChapterId: 11,
                    ChapterName: "Chương 2",
                    Difficulty: 2,
                    TotalOfQuestions: 3
                )
            };

            _repoMock.Setup(r => r.GetBlueprintDetailAsync(blueprintId, _ct))
                     .ReturnsAsync(expected);

            // Act
            var result = await _service.GetBlueprintDetailAsync(blueprintId, _ct);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.Count, result.Count);
            Assert.Equal(expected[0].ChapterId, result[0].ChapterId);
            Assert.Equal(expected[0].TotalOfQuestions, result[0].TotalOfQuestions);
            _repoMock.VerifyAll();
        }

        // UTCID02 – BlueprintId hợp lệ nhưng không có dữ liệu (trả list rỗng)
        [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID02 - BlueprintId hợp lệ nhưng không có dữ liệu")]
        public async Task GetBlueprintDetailAsync_UTCID02_ValidId_NoData_ShouldReturnEmptyList()
        {
            // Arrange
            int blueprintId = 2;

            _repoMock.Setup(r => r.GetBlueprintDetailAsync(blueprintId, _ct))
                     .ReturnsAsync(new List<BlueprintDetailRowDto>());

            // Act
            var result = await _service.GetBlueprintDetailAsync(blueprintId, _ct);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _repoMock.VerifyAll();
        }

        // UTCID03 – Repository ném exception (ví dụ blueprintId không tồn tại hoặc lỗi DB)
        [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID03 - Repository throw exception, service propagate")]
        public async Task GetBlueprintDetailAsync_UTCID03_RepoThrows_ShouldPropagateException()
        {
            // Arrange
            int blueprintId = 999;

            _repoMock.Setup(r => r.GetBlueprintDetailAsync(blueprintId, _ct))
                     .ThrowsAsync(new Exception("Database error"));

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.GetBlueprintDetailAsync(blueprintId, _ct));

            // Assert
            Assert.Equal("Database error", ex.Message);
            _repoMock.VerifyAll();
        }
    }
}