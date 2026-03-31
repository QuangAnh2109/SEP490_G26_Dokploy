using Backend.DTOs.ExamBlueprint;
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
    public class ExamBlueprintGetBlueprintsUnitTest
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintGetBlueprintsUnitTest()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>();
            _service = new ExamBlueprintService(_mockRepo.Object);
        }

        // UTCID01 - Normal: User ID hợp lệ, PageSize = 10
        //           → Trả về danh sách thành công
        [Fact]
        public async Task GetBlueprintsAsync_UTCID01_ValidUserId_PageSize10_ShouldReturnBlueprintList()
        {
            // Arrange
            var fakeItems = new List<BlueprintListItemDto>
            {
                new BlueprintListItemDto { ExamBlueprintId = 1, Name = "Blueprint 1" },
                new BlueprintListItemDto { ExamBlueprintId = 2, Name = "Blueprint 2" }
            };

            var query = new BlueprintListQueryDto { Page = 1, PageSize = 10 };

            _mockRepo.Setup(r => r.GetBlueprintsAsync(query, 1))
                     .ReturnsAsync((fakeItems, 2));

            // Act
            var result = await _service.GetBlueprintsAsync(query, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(2, result.TotalItems);
            Assert.Equal(1, result.TotalPages);
        }

        // UTCID02 - Normal: User ID = 0 (không hợp lệ)
        //           → UnauthorizedAccessException
        [Fact]
        public async Task GetBlueprintsAsync_UTCID02_InvalidUserId_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var query = new BlueprintListQueryDto { Page = 1, PageSize = 10 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetBlueprintsAsync(query, 0));

            Assert.Equal("Invalid user.", exception.Message);

            // Repository không được gọi
            _mockRepo.Verify(r => r.GetBlueprintsAsync(
                It.IsAny<BlueprintListQueryDto>(),
                It.IsAny<int>()), Times.Never);
        }

        // UTCID03 - Normal: Page = 0, User hợp lệ
        //           → Hệ thống tự điều chỉnh Page = 1
        [Fact]
        public async Task GetBlueprintsAsync_UTCID03_PageIsZero_ShouldDefaultToPageOne()
        {
            // Arrange
            var fakeItems = new List<BlueprintListItemDto>
            {
                new BlueprintListItemDto { ExamBlueprintId = 1, Name = "Blueprint 1"}
            };

            var query = new BlueprintListQueryDto { Page = 0, PageSize = 10 };

            _mockRepo.Setup(r => r.GetBlueprintsAsync(It.IsAny<BlueprintListQueryDto>(), 1))
                     .ReturnsAsync((fakeItems, 1));

            // Act
            var result = await _service.GetBlueprintsAsync(query, 1);

            // Assert - Page tự động điều chỉnh về 1
            Assert.NotNull(result);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(1, result.TotalItems);
        }

        // UTCID04 - Normal: PageSize = 0
        //           → Hệ thống tự gán PageSize mặc định = 10
        [Fact]
        public async Task GetBlueprintsAsync_UTCID04_PageSizeIsZero_ShouldDefaultToPageSizeTen()
        {
            // Arrange
            var fakeItems = new List<BlueprintListItemDto>
            {
                new BlueprintListItemDto { ExamBlueprintId = 1, Name = "Blueprint 1" }
            };

            var query = new BlueprintListQueryDto { Page = 1, PageSize = 0 };

            _mockRepo.Setup(r => r.GetBlueprintsAsync(It.IsAny<BlueprintListQueryDto>(), 1))
                     .ReturnsAsync((fakeItems, 1));

            // Act
            var result = await _service.GetBlueprintsAsync(query, 1);

            // Assert - PageSize tự động điều chỉnh về 10
            Assert.NotNull(result);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(1, result.Page);
        }

        // UTCID05 - Normal: PageSize = 101 (vượt ngưỡng tối đa 100)
        //           → Hệ thống tự giới hạn PageSize = 100
        [Fact]
        public async Task GetBlueprintsAsync_UTCID05_PageSizeExceedsMax_ShouldCapPageSizeAtHundred()
        {
            // Arrange
            var fakeItems = new List<BlueprintListItemDto>
            {
                new BlueprintListItemDto { ExamBlueprintId = 1, Name = "Blueprint 1" }
            };

            var query = new BlueprintListQueryDto { Page = 1, PageSize = 101 };

            _mockRepo.Setup(r => r.GetBlueprintsAsync(It.IsAny<BlueprintListQueryDto>(), 1))
                     .ReturnsAsync((fakeItems, 1));

            // Act
            var result = await _service.GetBlueprintsAsync(query, 1);

            // Assert - PageSize bị giới hạn về 100
            Assert.NotNull(result);
            Assert.Equal(100, result.PageSize);
            Assert.Equal(1, result.Page);
        }

        // UTCID06 - Normal: PageSize = 10, totalCount = 0
        //           → Trả về danh sách trống, TotalPages = 0
        [Fact]
        public async Task GetBlueprintsAsync_UTCID06_TotalCountIsZero_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new BlueprintListQueryDto { Page = 1, PageSize = 10 };

            _mockRepo.Setup(r => r.GetBlueprintsAsync(It.IsAny<BlueprintListQueryDto>(), 1))
                     .ReturnsAsync((new List<BlueprintListItemDto>(), 0));

            // Act
            var result = await _service.GetBlueprintsAsync(query, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalItems);
            Assert.Equal(0, result.TotalPages); // totalCount = 0 → TotalPages = 0
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }
    }
}
