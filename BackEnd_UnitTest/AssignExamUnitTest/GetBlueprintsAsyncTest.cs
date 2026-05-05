// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using Backend.DTOs;
// using Backend.Repositories.Interfaces;
// using Backend.Services.Implements;
// using Moq;
// using Xunit;
// 
// namespace Backend_UnitTest.AssignExamTests
// {
//     public class GetBlueprintsAsync_UTCID_Tests
//     {
//         private readonly Mock<IAssignExamRepository> _repoMock;
//         private readonly AssignExamService _service;
//         private readonly CancellationToken _ct = CancellationToken.None;
// 
//         public GetBlueprintsAsync_UTCID_Tests()
//         {
//             _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
//             _service = new AssignExamService(_repoMock.Object);
//         }
// 
//         // UTCID01 – TeacherId=1 active, subjectCode="MAE", keyword="PT1" => có d? li?u
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID01 - Teacher active + exact filters => list has data")]
//         public async Task GetBlueprintsAsync_UTCID01_TeacherActive_ExactFilters_ShouldReturnList()
//         {
//             int teacherId = 1;
//             string subjectCode = "MAE";
//             string keyword = "PT1";
// 
//             var expected = new List<BlueprintListItemDto>
//             {
//                 new BlueprintListItemDto(1, "BP1", "MAE", DateTime.UtcNow, 10)
//             };
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             _repoMock.Setup(r => r.GetBlueprintsAsync(teacherId, subjectCode, keyword, _ct))
//                      .ReturnsAsync(expected);
// 
//             var result = await _service.GetBlueprintsAsync(teacherId, subjectCode, keyword, _ct);
// 
//             Assert.NotNull(result);
//             Assert.NotEmpty(result);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID02 – TeacherId=1 active, subjectCode / keyword có kho?ng tr?ng => Trim v?n có d? li?u
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID02 - Teacher active + filters with spaces => list has data")]
//         public async Task GetBlueprintsAsync_UTCID02_TeacherActive_TrimFilters_ShouldReturnList()
//         {
//             int teacherId = 1;
//             string subjectCode = "  MAE ";
//             string keyword = " PT1 ";
// 
//             var expected = new List<BlueprintListItemDto>
//             {
//                 new BlueprintListItemDto(2, "BP2", "MAE", DateTime.UtcNow, 5)
//             };
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             // Sau khi Trim: "MAE", "PT1"
//             _repoMock.Setup(r => r.GetBlueprintsAsync(teacherId, "MAE", "PT1", _ct))
//                      .ReturnsAsync(expected);
// 
//             var result = await _service.GetBlueprintsAsync(teacherId, subjectCode, keyword, _ct);
// 
//             Assert.NotNull(result);
//             Assert.NotEmpty(result);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID03 – TeacherId=1 active, subjectCode=null, keyword=null => v?n có d? li?u (l?y t?t c? blueprint c?a teacher)
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID03 - Teacher active + null filters => list has data")]
//         public async Task GetBlueprintsAsync_UTCID03_TeacherActive_NullFilters_ShouldReturnList()
//         {
//             int teacherId = 1;
// 
//             var expected = new List<BlueprintListItemDto>
//             {
//                 new BlueprintListItemDto(3, "BP3", "MAE", DateTime.UtcNow, 7)
//             };
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             _repoMock.Setup(r => r.GetBlueprintsAsync(teacherId, null, null, _ct))
//                      .ReturnsAsync(expected);
// 
//             var result = await _service.GetBlueprintsAsync(teacherId, null, null, _ct);
// 
//             Assert.NotNull(result);
//             Assert.NotEmpty(result);
//             _repoMock.VerifyAll();
//         }
// 
//         // UTCID04 – TeacherId không truy?n / null => ArgumentException
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID04 - TeacherId null => ArgumentException")]
//         public async Task GetBlueprintsAsync_UTCID04_TeacherIdNull_ShouldThrowArgumentException()
//         {
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.GetBlueprintsAsync(null, "MAE", "PT1", _ct));
// 
//             Assert.Equal("TeacherId is required.", ex.Message);
//             _repoMock.Verify(r => r.IsUserActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.GetBlueprintsAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID05 – TeacherId = 0 (ví d? 0) => ArgumentException
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID05 - TeacherId non-positive => ArgumentException")]
//         public async Task GetBlueprintsAsync_UTCID05_TeacherIdNonPositive_ShouldThrowArgumentException()
//         {
//             var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//                 _service.GetBlueprintsAsync(0, "MAE", "PT1", _ct));
// 
//             Assert.Equal("TeacherId is required.", ex.Message);
//             _repoMock.Verify(r => r.IsUserActiveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
//             _repoMock.Verify(r => r.GetBlueprintsAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID06 – TeacherId t?n t?i nhung inactive => KeyNotFoundException
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID06 - Teacher inactive => KeyNotFoundException")]
//         public async Task GetBlueprintsAsync_UTCID06_TeacherInactive_ShouldThrowKeyNotFoundException()
//         {
//             int teacherId = 99;
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(false);
// 
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.GetBlueprintsAsync(teacherId, "MAE", "PT1", _ct));
// 
//             Assert.Equal($"User with Id {teacherId} not found or is inactive.", ex.Message);
//             _repoMock.Verify(r => r.IsUserActiveAsync(teacherId, _ct), Times.Once);
//             _repoMock.Verify(r => r.GetBlueprintsAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID07 – TeacherId không t?n t?i (cung b? coi nhu inactive trong IsUserActiveAsync) => KeyNotFoundException
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID07 - Teacher not found => KeyNotFoundException")]
//         public async Task GetBlueprintsAsync_UTCID07_TeacherNotFound_ShouldThrowKeyNotFoundException()
//         {
//             int teacherId = 123; // id không t?n t?i
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(false);
// 
//             var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
//                 _service.GetBlueprintsAsync(teacherId, "MAE", "PT1", _ct));
// 
//             Assert.Equal($"User with Id {teacherId} not found or is inactive.", ex.Message);
//             _repoMock.Verify(r => r.IsUserActiveAsync(teacherId, _ct), Times.Once);
//             _repoMock.Verify(r => r.GetBlueprintsAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
//         }
// 
//         // UTCID08 – TeacherId active, nhung subjectCode / keyword không match => danh sách blueprint r?ng
//         [Fact(DisplayName = "GetBlueprintsAsync - UTCID08 - Teacher active + filters not match => empty list")]
//         public async Task GetBlueprintsAsync_UTCID08_TeacherActive_FiltersNotMatch_ShouldReturnEmptyList()
//         {
//             int teacherId = 1;
//             string subjectCode = "XXX"; // mã môn không t?n t?i
//             string keyword = "YYY";     // keyword không kh?p
// 
//             _repoMock.Setup(r => r.IsUserActiveAsync(teacherId, _ct))
//                      .ReturnsAsync(true);
// 
//             _repoMock.Setup(r => r.GetBlueprintsAsync(teacherId, subjectCode, keyword, _ct))
//                      .ReturnsAsync(new List<BlueprintListItemDto>());
// 
//             var result = await _service.GetBlueprintsAsync(teacherId, subjectCode, keyword, _ct);
// 
//             Assert.NotNull(result);
//             Assert.Empty(result); // Danh sách blueprint r?ng
//             _repoMock.VerifyAll();
//         }
//     }
// }
