# Báo Cáo Phân Tích & Kế Hoạch Triển Khai
# Loại Bỏ SignalR, Chuyển Đổi Sang Redis, Thiết Lập Hàng Đợi Xử Lý

**Dự án:** MTCA_SEP490_G26 — Math Test Creator Application  
**Ngày:** 29/03/2026  
**Mục tiêu:** Tối ưu hiệu suất cho hệ thống triển khai trên máy chủ 2 vCPU  

---

## MỤC LỤC

1. [Tổng Quan Kiến Trúc Hiện Tại](#1-tổng-quan-kiến-trúc-hiện-tại)
2. [Phân Tích SignalR — Đánh Giá & Loại Bỏ](#2-phân-tích-signalr--đánh-giá--loại-bỏ)
3. [So Sánh IMemoryCache vs Redis](#3-so-sánh-imemorycache-vs-redis)
4. [Phân Tích Hàng Đợi: RabbitMQ vs Redis vs Tự Code](#4-phân-tích-hàng-đợi-rabbitmq-vs-redis-vs-tự-code)
5. [Phân Tích Các Điểm Nghẽn Hiệu Suất (Tử Huyệt)](#5-phân-tích-các-điểm-nghẽn-hiệu-suất)
   - 5.1 [BCrypt — Đánh Giá Lại](#51-bcrypt--đánh-giá-lại)
   - 5.2 [Tạo Đề Thi — Triển Khai Queue Với Redis](#52-tạo-đề-thi--triển-khai-queue-với-redis)
   - 5.3 [OTP + Email — Thiết Kế Hoàn Chỉnh Với Redis](#53-otp--email--thiết-kế-hoàn-chỉnh-với-redis)
6. [Hướng Dẫn Cài Đặt Redis Trên Máy Chủ](#6-hướng-dẫn-cài-đặt-redis-trên-máy-chủ)
7. [Bảng Tổng Hợp Các Thay Đổi Cần Thực Hiện](#7-bảng-tổng-hợp-các-thay-đổi-cần-thực-hiện)

---

## 1. Tổng Quan Kiến Trúc Hiện Tại

| Thành phần      | Công nghệ                          | File chính                    |
|:-----------------|:------------------------------------|:------------------------------|
| Framework        | .NET 8 (ASP.NET Core Web API)       | Backend/Backend.csproj        |
| Database         | SQL Server                          | Backend/appsettings.json      |
| Cache            | IMemoryCache (In-Process)           | Backend/Program.cs (dòng 91)  |
| ORM              | Entity Framework Core 8             | —                             |
| Realtime         | SignalR (ExamHub)                    | Backend/Hubs/ExamHub.cs       |
| Hạ tầng dự kiến  | **2 vCPU**, ước tính 200 req/s cao điểm | —                         |

### Cấu trúc dự án Backend

```
Backend/
├── Constants/          # ExamStatus, MemberStatus, SubmissionStatus...
├── Controllers/        # AuthController, AssignExamController, CourseController...
├── DTOs/               # Data Transfer Objects
├── Exceptions/         # Custom exceptions
├── Helper/             # AnalyticsHelper, ExceptionMiddleware...
├── Hubs/               # ExamHub.cs (SignalR)
├── Migrations/         # EF Core migrations
├── Models/             # Entity models + DbContext
├── Repositories/       # Data access layer
├── Services/           # Business logic layer
└── Program.cs          # Entry point + DI configuration
```

---

## 2. Phân Tích SignalR — Đánh Giá & Loại Bỏ

### 2.1 Chức năng SignalR đang sử dụng

SignalR được cấu hình tại `Backend/Program.cs`:
```csharp
builder.Services.AddSignalR();          // Dòng 90
app.MapHub<Backend.Hubs.ExamHub>("/examHub");  // Dòng 161
```

File `Backend/Hubs/ExamHub.cs` (67 dòng) thực hiện chức năng:
- **Giám sát trạng thái trực tuyến (Presence Tracking)** của học sinh khi thi
- Khi học sinh vào trang thi → gọi `JoinExamGroup(examId, studentId, studentName)` → ghi nhận vào ConcurrentDictionary
- Khi rớt mạng/thoát → `OnDisconnectedAsync` → broadcast `StudentStatusChanged` với trạng thái `Offline` cho nhóm giám thị

### 2.2 Mức độ tích hợp thực tế

**Kết quả quét toàn bộ Frontend:** Không có BẤT KỲ dòng JavaScript nào kết nối đến `/examHub`. Không có file nào import thư viện `@microsoft/signalr`. **Chức năng này chưa được tích hợp phía client.**

### 2.3 Tại sao nên loại bỏ SignalR

| Tiêu chí | Phân tích |
|:--|:--|
| **Tính năng chưa hoàn thiện** | Backend có code nhưng Frontend chưa dùng → Dead code |
| **Tốn tài nguyên** | SignalR duy trì WebSocket persistent connection, ăn RAM + CPU cho Ping/Pong liên tục |
| **Overkill** | Chỉ để biết "học sinh có đang online không" → không cần kết nối duy trì liên tục |
| **Máy chủ 2 vCPU** | WebSocket cho hàng trăm học sinh sẽ tranh giành tài nguyên với các API quan trọng hơn |

### 2.4 Cơ chế thay thế: Heartbeat qua REST HTTP + Cache

Nếu trong tương lai cần tính năng giám sát online:

1. **API Heartbeat nhẹ:** `POST /api/examination/heartbeat` (payload: `{ examId }`)
   - Backend chỉ ghi `DateTime.UtcNow` vào Redis/Cache (key: `Exam_{examId}_Student_{studentId}`)
   - Không truy vấn Database

2. **Frontend (phía học sinh):** `setInterval()` mỗi 30-60 giây gọi Heartbeat

3. **Trang giám thị:** Gọi API kiểm tra trạng thái mỗi 30 giây
   - Backend quét Cache: Nếu `(Thời_gian_hiện_tại - Heartbeat_gần_nhất) > 2 phút` → Offline

4. **Phát hiện chuyển tab (chống gian lận):** Dùng `document.addEventListener("visibilitychange")` + `navigator.sendBeacon()` → gửi cảnh báo 1 request nhẹ, không cần socket.

### 2.5 Các file cần xóa/sửa để loại bỏ SignalR

| File | Thay đổi |
|:--|:--|
| `Backend/Hubs/ExamHub.cs` | **XÓA FILE** |
| `Backend/Program.cs` dòng 90 | Xóa `builder.Services.AddSignalR();` |
| `Backend/Program.cs` dòng 161 | Xóa `app.MapHub<Backend.Hubs.ExamHub>("/examHub");` |

---

## 3. So Sánh IMemoryCache vs Redis

### 3.1 Bảng so sánh chi tiết

| Tiêu chí | IMemoryCache | Redis |
|:--|:--|:--|
| **Vị trí hoạt động** | RAM của process .NET (In-Process) | Process độc lập, tách biệt (Out-Of-Process) |
| **Hiệu suất** | ~0ms (không có network latency) | ~0.5-2ms (có TCP round-trip nhỏ) |
| **Tiêu tụ tài nguyên .NET** | Ăn chung RAM + tăng gánh nặng Garbage Collection | Giải phóng RAM cho .NET, GC nhẹ hơn |
| **Khả năng mở rộng** | Bó gọn 1 server. Deploy 2 server → cache bị sai | Distributed. Nhiều server đọc chung Redis |
| **Bảo toàn dữ liệu** | Server .NET restart → mất trắng 100% | Server .NET restart → dữ liệu Redis vẫn còn |
| **Khả năng làm Queue** | Không có | Có sẵn List, Streams cho message queue |

### 3.2 Kết luận: NÊN chuyển sang Redis

Lý do chính:
- Máy chủ chỉ có 2 vCPU → IMemoryCache lớn sẽ kích hoạt GC thường xuyên → chiếm CPU
- Redis tiêu thụ ~5-10MB RAM khi idle, CPU ~0% → cực kỳ nhẹ
- Có thể tận dụng Redis làm luôn Message Queue → không cần cài thêm RabbitMQ/Kafka
- Nếu tương lai scale lên 2 server → Redis sẵn sàng, IMemoryCache thì không

---

## 4. Phân Tích Hàng Đợi: RabbitMQ vs Redis vs Tự Code

### 4.1 Bối cảnh: 200 req/s trên 2 vCPU

Dựa trên thông số:
- 150 request/giây × 500ms = ~75 kết nối đồng thời
- 50 request/giây × 100ms = ~5 kết nối đồng thời
- **Tổng:** ~80 request chạy cùng lúc tại một thời điểm

Nếu 500ms là thời gian chờ I/O (Database, API ngoài) → async/await nhả luồng, 2 vCPU đủ.
Nếu 500ms là CPU tính toán thuần → 2 vCPU chỉ chạy vật lý 2 tác vụ → nghẽn.

### 4.2 So sánh 3 phương án

| Phương án | Ưu điểm | Nhược điểm | Đánh giá |
|:--|:--|:--|:--|
| **RabbitMQ** (Service độc lập) | Mạnh mẽ, routing phức tạp, retry tự động | Ăn RAM/CPU riêng, setup phức tạp, overkill cho monolith | ❌ Không phù hợp |
| **Tự code** (System.Threading.Channels) | Siêu nhẹ, không phụ thuộc | Server restart → mất hết job trong queue | ⚠️ Chỉ cho task không quan trọng |
| **Redis List/Streams** | Nhẹ, đã cài sẵn Redis, job không mất khi .NET restart | Cần code consumer (BackgroundService) | ✅ **Khuyến nghị** |

### 4.3 Kết luận

Sử dụng **Redis** cho cả 3 vai trò:
1. **Cache** (thay IMemoryCache): OTP, trạng thái tạo đề, heartbeat
2. **Message Queue** (thay RabbitMQ): Hàng đợi tạo đề thi
3. **Rate Limiter**: Giới hạn gửi OTP 60s/email

---

## 5. Phân Tích Các Điểm Nghẽn Hiệu Suất

### Phương pháp quét mã nguồn

Đã quét toàn bộ Backend tìm các pattern gây nghẽn:
- `.Result`, `.Wait()`, `.GetAwaiter()` → Sync-over-Async blocking: **Không tìm thấy** ✅
- `.ToList()`, `.FirstOrDefault()` (đồng bộ, không Async): **Không tìm thấy trong Service layer** ✅
- `Thread.Sleep`, `Task.Delay` (chặn luồng cố tình): **Không tìm thấy** ✅
- `BCrypt`, `Hash`, `Cryptography` (CPU-bound): **Tìm thấy** → Phân tích bên dưới
- `Excel`, `FileStream`, `System.IO` (I/O nặng): **Không tìm thấy** ✅

**Nhận xét chung:** Code viết chuẩn async/await. Các lệnh EF Core đều dùng `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`. Không có lỗi blocking I/O.

---

### 5.1 BCrypt — Đánh Giá Lại

#### Các vị trí sử dụng BCrypt trong `Backend/Services/Implements/AuthService.cs`:

| Dòng | Hàm | Tác vụ |
|:--|:--|:--|
| 43 | `LoginAsync` | `BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)` |
| 205 | `VerifyOtpAndRegisterAsync` | `BCrypt.Net.BCrypt.HashPassword(regRequest.Password)` |
| 311 | `ResetPasswordAsync` | `BCrypt.Net.BCrypt.HashPassword(request.NewPassword)` |
| 330 | `ChangePasswordAsync` | `BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash)` |
| 335 | `ChangePasswordAsync` | `BCrypt.Net.BCrypt.HashPassword(request.NewPassword)` |

#### Tại sao `Task.Run()` không giải quyết vấn đề

`Task.Run()` chỉ đẩy công việc từ request thread sang ThreadPool thread. Trên máy 2 vCPU, dù di chuyển sang thread nào vẫn chỉ có 2 nhân vật lý để chạy. Thậm chí tệ hơn vì thêm overhead context-switching.

#### Kết luận: BCrypt KHÔNG phải tử huyệt thực sự

Các endpoint Login/Register/ChangePassword **không nằm trong nhóm 200 req/s cao điểm**. Đỉnh điểm 200 req/s xảy ra khi học sinh đồng loạt làm bài thi (Submit/TakeExam), không phải đăng nhập. Trong ngữ cảnh thi cử, học sinh đăng nhập trước khi thi bắt đầu, rải đều trong 10-15 phút.

BCrypt mất ~100-300ms/lần. Với tối đa 5-10 người đăng nhập đồng thời (thực tế), 2 vCPU dư sức xử lý. **Không cần thay đổi.**

> **Gợi ý tương lai:** Nếu muốn tối ưu thêm, giảm BCrypt work factor (cost) từ mặc định 11 xuống 10 để giảm ~50% thời gian tính toán mà vẫn đủ bảo mật.

---

### 5.2 Tạo Đề Thi — Triển Khai Queue Với Redis

#### 5.2.1 Phân tích luồng hiện tại

```
[Frontend] assign_exam.js dòng 257
    POST /api/assign-exam (payload)
        ↓
[Controller] AssignExamController.cs dòng 88-110
    await _assignExamService.CreateAssignExamAsync(request, ct)
        ↓ (ĐỒNG BỘ — Client chờ toàn bộ quá trình hoàn thành)
    return Ok(result)  →  HTTP 200
        ↓
[Frontend] assign_exam.js dòng 259-261
    redirect → /Exam/ExamReview?examId=${res.examId}
```

Chi tiết bên trong `CreateAssignExamAsync` (AssignExamService.cs dòng 99-298):

```
Bước 1: Validate input                                    (~1ms)
Bước 2: Load Blueprint + ChapterData từ DB                (~20-50ms)
Bước 3: Vòng lặp: Query ngân hàng câu hỏi từ DB          (~50-200ms, tùy số chapter)
Bước 4: Vòng lặp: Shuffle + phân phối câu hỏi vào papers (~10-500ms, CPU-bound)
Bước 5: BEGIN TRANSACTION
   5a. INSERT Exam                                         (~5ms)
   5b. Vòng lặp PaperCount lần:
       - INSERT Paper                                      (~5ms × N)
       - INSERT PaperQuestion (batch SQL)                  (~5ms × N)
   5c. COMMIT
Bước 6: return response
```

**Bước 4** là CPU-bound thuần túy (shuffle bằng `Guid.NewGuid()`), **Bước 5** giữ Transaction lâu. Với PaperCount = 50, toàn bộ hàm mất **~500ms - 2s**.

#### 5.2.2 Thiết kế mới: 2 Phase (Enqueue + Background Worker)

**Luồng mới:**

```
[Frontend] POST /api/assign-exam (payload)
    ↓
[Phase 1 — Controller] (~50ms, trả NGAY)
    1. Validate tất cả input
    2. INSERT Exam với Status = Generating (-1)
    3. Serialize request → đẩy vào Redis List "exam_creation_queue"
    4. return HTTP 202 Accepted { examId, status: "generating" }
    ↓
[Frontend] redirect → /Exam/ExamReview?examId=X
    ↓
[API Review] GET /api/assign-exam/review/X → query DB
    → Trả về thông tin kỳ thi (title, subject, duration...)
    → status = -1 (Generating) → papers = [] (rỗng)
    → Frontend hiển thị thông tin cơ bản + banner "Đang tạo đề..."
    ↓
[Frontend Polling mỗi 10s] GET /api/assign-exam/status/X
    → Đọc Redis cache (KHÔNG query DB)
    → Trả về { status: "generating" | "ready" | "failed" }
    ↓
[Background Worker - Phase 2] (chạy nền)
    1. BLPOP từ Redis queue "exam_creation_queue"
    2. Chạy thuật toán shuffle + phân phối câu hỏi
    3. BEGIN TRANSACTION → INSERT Papers + PaperQuestions → COMMIT
    4. UPDATE Exam SET Status = Ready (0)
    5. SET Redis cache exam_status:{examId} = "ready"
    ↓
[Frontend Polling] status = "ready" → clearInterval
    → Gọi lại GET /api/assign-exam/review/X → lấy đầy đủ papers/questions
    → Hiển thị đề thi hoàn chỉnh
```

#### 5.2.3 Thay đổi cụ thể

**A. Bổ sung trạng thái Generating vào ExamStatus:**

```csharp
// Backend/Constants/ExamStatus.cs
public static class ExamStatus
{
    public const int Generating = -1;  // ← MỚI: Đang tạo đề trong hàng đợi
    public const int Ready = 0;
    public const int Published = 1;
    public const int InProgress = 2;
    public const int Deleted = 3;
    public const int Cancelled = 4;
    public const int Closed = 5;
}
```

**B. Controller — Sửa CreateAssignExam + Thêm endpoint Status:**

```csharp
// AssignExamController.cs

[HttpPost]
public async Task<ActionResult> CreateAssignExam(
    [FromBody] CreateAssignExamRequest request,
    CancellationToken ct)
{
    try
    {
        var result = await _assignExamService.EnqueueExamCreationAsync(request, ct);
        return Accepted(result); // HTTP 202
    }
    catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
}

[HttpGet("status/{examId:int}")]
public async Task<ActionResult> GetExamCreationStatus(int examId)
{
    var status = await _assignExamService.GetExamCreationStatusAsync(examId);
    return Ok(status);
}
```

**C. Service — Phase 1 (Enqueue nhanh):**

```csharp
// AssignExamService.cs — Hàm mới

public async Task<EnqueueExamResponse> EnqueueExamCreationAsync(
    CreateAssignExamRequest r, CancellationToken ct)
{
    // Validate tất cả input TRƯỚC khi đưa vào queue
    ValidateTimeWindow(r.VisibleFrom, r.OpenAt, r.CloseAt);
    await EnsureUserActiveAsync(r.TeacherId, ct);
    ThrowIf(string.IsNullOrWhiteSpace(r.Title), "Title is required.");
    ThrowIf(r.Duration <= 0, "Duration must be > 0.");
    ThrowIf(r.MaxAttempts <= 0, "MaxAttempts must be > 0.");
    ThrowIf(r.PaperCount <= 0, "PaperCount must be > 0.");
    // ... giữ nguyên các validation khác

    // Resolve subjectId
    int subjectId;
    string mode = (r.GenerationMode ?? "").Trim().ToLower();
    if (mode == "blueprint")
    {
        var bp = await _repo.GetBlueprintWithChaptersAsync(r.ExamBlueprintId ?? 0, ct);
        if (bp == null) throw new KeyNotFoundException("Blueprint not found.");
        subjectId = bp.SubjectId;
    }
    else
    {
        // Manual mode: resolve subject from questions
        var res = await BuildFromManualAsync(r.SubjectId, r.QuestionIds, ct);
        subjectId = res.SubjId;
    }

    // Tạo Exam "vỏ" với status Generating
    var exam = new Exam
    {
        TeacherId = r.TeacherId,
        ClassId = r.ClassId,
        Title = r.Title,
        SubjectId = subjectId,
        ExamBlueprintId = mode == "blueprint" ? r.ExamBlueprintId : null,
        Description = r.Description,
        Duration = r.Duration,
        ShowScore = r.ShowScore,
        ShowAnswer = r.ShowAnswer,
        AnswerTimingMode = r.AnswerTimingMode,
        MaxAttempts = r.MaxAttempts,
        VisibleFrom = r.VisibleFrom,
        OpenAt = r.OpenAt,
        CloseAt = r.CloseAt,
        ShuffleQuestion = r.ShuffleQuestion,
        Status = ExamStatus.Generating,  // ← TRẠNG THÁI MỚI
        UpdatedAtUtc = DateTime.UtcNow
    };
    await _repo.SaveExamAsync(exam, ct);

    // Serialize request → đẩy vào Redis List (Queue)
    var redis = _connectionMultiplexer.GetDatabase();
    var jobPayload = JsonSerializer.Serialize(new ExamCreationJob
    {
        ExamId = exam.ExamId,
        Request = r
    });
    await redis.ListRightPushAsync("exam_creation_queue", jobPayload);

    return new EnqueueExamResponse(exam.ExamId, "generating");
}
```

**D. Service — API Status (đọc Redis, không query DB):**

```csharp
public async Task<ExamCreationStatusDto> GetExamCreationStatusAsync(
    int examId, CancellationToken ct)
{
    var redis = _connectionMultiplexer.GetDatabase();
    var cachedStatus = await redis.StringGetAsync($"exam_status:{examId}");

    if (cachedStatus.HasValue)
    {
        return JsonSerializer.Deserialize<ExamCreationStatusDto>(cachedStatus!);
    }

    // Lần đầu kiểm tra, hoặc cache hết hạn → query DB 1 lần
    var exam = await _repo.GetExamBasicInfoAsync(examId, ct);
    if (exam == null) throw new KeyNotFoundException("Exam not found.");

    var dto = new ExamCreationStatusDto
    {
        ExamId = examId,
        Status = exam.Status == ExamStatus.Generating ? "generating" : "ready",
        Title = exam.Title
    };

    // Cache lại 30s để polling tiếp theo không query DB
    await redis.StringSetAsync(
        $"exam_status:{examId}",
        JsonSerializer.Serialize(dto),
        TimeSpan.FromSeconds(30)
    );

    return dto;
}
```

**E. Background Worker — Phase 2 (File mới):**

```csharp
// Backend/Services/Implements/ExamCreationWorker.cs (MỚI)

public class ExamCreationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ExamCreationWorker> _logger;

    public ExamCreationWorker(
        IServiceProvider sp, IConnectionMultiplexer redis,
        ILogger<ExamCreationWorker> logger)
    {
        _serviceProvider = sp;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await db.ListLeftPopAsync("exam_creation_queue");

                if (!job.HasValue)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                var creationJob = JsonSerializer.Deserialize<ExamCreationJob>(job!);

                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider
                    .GetRequiredService<IAssignExamRepository>();

                try
                {
                    // Chạy thuật toán tạo đề
                    await GeneratePapersAsync(creationJob, repo, stoppingToken);

                    // Cập nhật status → Ready
                    await repo.UpdateExamStatusAsync(
                        creationJob.ExamId, ExamStatus.Ready, stoppingToken);

                    // Cập nhật Redis cache
                    await db.StringSetAsync(
                        $"exam_status:{creationJob.ExamId}",
                        JsonSerializer.Serialize(new ExamCreationStatusDto
                        {
                            ExamId = creationJob.ExamId,
                            Status = "ready"
                        }),
                        TimeSpan.FromMinutes(5)
                    );

                    _logger.LogInformation(
                        "Exam {ExamId} created successfully", creationJob.ExamId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to create exam {ExamId}", creationJob.ExamId);

                    await repo.UpdateExamStatusAsync(
                        creationJob.ExamId, ExamStatus.Cancelled, stoppingToken);

                    await db.StringSetAsync(
                        $"exam_status:{creationJob.ExamId}",
                        JsonSerializer.Serialize(new ExamCreationStatusDto
                        {
                            ExamId = creationJob.ExamId,
                            Status = "failed",
                            ErrorMessage = ex.Message
                        }),
                        TimeSpan.FromMinutes(10)
                    );
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Worker encountered unexpected error");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task GeneratePapersAsync(
        ExamCreationJob job, IAssignExamRepository repo, CancellationToken ct)
    {
        // Di chuyển logic shuffle + insert từ CreateAssignExamAsync
        // (Các bước 3-5 trong luồng hiện tại) vào đây
    }
}
```

Đăng ký Worker trong `Program.cs`:
```csharp
builder.Services.AddHostedService<ExamCreationWorker>();
```

**F. Sửa API Review để xử lý status Generating:**

Tại `GetExamReviewAsync` (AssignExamService.cs dòng 301): Nếu `exam.Status == ExamStatus.Generating`, trả DTO với `papers = []` thay vì throw lỗi khi không có papers.

**G. Frontend — assign_exam.js (sửa saveAssignExam):**

```javascript
// Thay đổi: Không chờ result đầy đủ, redirect ngay
const res = await apiPostJson('/api/assign-exam', payload);
showToast('Hệ thống đang tạo đề thi...', 'info');
window.location.href = `/Exam/ExamReview?examId=${res.examId}&classId=${state.selClassId}`;
```

**H. Frontend — exam_review.js (bổ sung polling):**

```javascript
async function initReviewPage(config) {
    // ...existing code...
    await loadReviewData(config.examId);

    // Sau khi load, kiểm tra nếu đang Generating thì bật polling
    if (currentReviewData && currentReviewData.status === -1) {
        startGenerationPolling(config.examId);
    }
}

function startGenerationPolling(examId) {
    // Hiển thị UI "Đang tạo đề"
    const container = document.getElementById('view-info');
    // Giữ nguyên thông tin kỳ thi đã hiển thị
    // Thêm banner thông báo đang tạo đề
    const banner = document.createElement('div');
    banner.id = 'generating-banner';
    banner.className = 'text-center py-4';
    banner.innerHTML = `
        <div class="spinner-border text-primary mb-3" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
        <h5>Đang tạo đề thi...</h5>
        <p class="text-muted">Hệ thống đang xử lý, vui lòng chờ.</p>`;
    
    const paperSection = document.getElementById('view-paper-detail');
    if (paperSection) paperSection.prepend(banner);

    const pollInterval = setInterval(async () => {
        try {
            const status = await apiClient.get(`${API_BASE}/status/${examId}`);

            if (status.status === 'ready') {
                clearInterval(pollInterval);
                const bannerEl = document.getElementById('generating-banner');
                if (bannerEl) bannerEl.remove();
                showToast('Đề thi đã được tạo thành công!', 'success');
                await loadReviewData(examId);
            } else if (status.status === 'failed') {
                clearInterval(pollInterval);
                showToast(`Tạo đề thất bại: ${status.errorMessage}`, 'error');
            }
        } catch (err) {
            console.warn('Poll error:', err);
        }
    }, 10000); // 10 giây
}
```

#### 5.2.4 Lưu ý quan trọng

**Race condition:** Nếu giáo viên mở Review trước khi Worker xong → API Review trả về thông tin kỳ thi nhưng papers rỗng. Frontend hiển thị thông tin cơ bản + banner "Đang tạo đề...". API Polling riêng sẽ xử lý phần chờ.

**Mất dữ liệu khi restart:** Nếu .NET restart khi Worker đang chạy giữa chừng (đã insert 25/50 papers) → cần rollback. Giải pháp: xóa toàn bộ Papers của exam đó trước khi retry (xử lý idempotent).

---

### 5.3 OTP + Email — Thiết Kế Hoàn Chỉnh Với Redis

#### 5.3.1 Phân tích TẤT CẢ chức năng OTP trong hệ thống

Hệ thống có **4 endpoint** OTP, thuộc **2 nghiệp vụ** hoàn toàn khác nhau:

##### Nghiệp vụ A: ĐĂNG KÝ TÀI KHOẢN MỚI (3 endpoint)

**A1. Gửi OTP đăng ký — `POST /api/auth/send-otp`**
- Backend: `AuthService.cs` dòng 123-148
- Frontend: `register-otp.js` dòng 50
- Logic: Kiểm tra email chưa tồn tại → Tạo OTP 6 số → Lưu `{request + otp}` vào cache key `OTP_{email}` (TTL 10 phút) → Gửi email OTP → redirect sang trang VerifyOTP
- Vấn đề hiện tại:
  - ❌ Backend KHÔNG có rate limit → Postman gọi liên tục = spam email
  - ❌ Frontend không có cooldown trước khi redirect

**A2. Gửi lại OTP — `POST /api/auth/resend-otp`**
- Backend: `AuthService.cs` dòng 151-173
- Frontend: `verify-otp.js` dòng 134
- Logic: Đọc cache cũ `OTP_{email}` → Tạo OTP mới → Đè lên cache cũ → Gửi email mới
- Vấn đề hiện tại:
  - ✅ Frontend đã có timer 60s cho nút "Gửi lại" (verify-otp.js dòng 62-76)
  - ❌ Backend KHÔNG có rate limit → bypass timer bằng Postman

**A3. Xác thực OTP → Tạo tài khoản — `POST /api/auth/verify-otp`**
- Backend: `AuthService.cs` dòng 176-223
- Frontend: `verify-otp.js` dòng 102
- Logic: Đối chiếu OTP trong cache → Xóa cache (dùng 1 lần) → Tạo User mới (BCrypt.HashPassword) → Trả JWT token → User đăng nhập luôn
- Trạng thái: ✅ Không cần rate limit (OTP dùng 1 lần, xóa sau verify)

##### Nghiệp vụ B: QUÊN MẬT KHẨU (2 endpoint)

**B1. Gửi OTP quên mật khẩu — `POST /api/auth/forgot-password`**
- Backend: `AuthService.cs` dòng 263-286
- Frontend: `forgotPassword.js` dòng 22
- Logic: Tìm user theo email (không có → return luôn = chống enumeration) → Tạo OTP → Lưu `otp` vào cache key `RESET_OTP_{email}` (TTL 10 phút) → Gửi email → redirect sang ResetPassword
- Vấn đề hiện tại:
  - ❌ Backend KHÔNG có rate limit → spam email
  - ❌ Frontend redirect NGAY sau khi gửi, không hiển thị countdown
  - ❌ Không có cơ chế "Gửi lại" — user phải quay lại trang ForgotPassword bấm lại

**B2. Xác thực OTP + Đổi mật khẩu — `POST /api/auth/reset-password`**
- Backend: `AuthService.cs` dòng 288-315
- Frontend: Trang ResetPassword
- Logic: Đối chiếu OTP trong cache → Xóa cache → BCrypt.HashPassword → Cập nhật user
- Trạng thái: ✅ Không cần rate limit

#### 5.3.2 Thiết kế mới: OTP qua Redis + Rate Limit 60s

**Bảng tóm tắt thay đổi cho mỗi chức năng OTP:**

| Chức năng | Cache key cũ (IMemoryCache) | Key mới (Redis) | Rate limit | Frontend |
|:--|:--|:--|:--|:--|
| A1. Gửi OTP đăng ký | `OTP_{email}` | `otp_register:{email}` | `otp_rate:{email}` TTL=60s | Disable nút + countdown 60s |
| A2. Gửi lại OTP | Đọc `OTP_{email}` | Đọc `otp_register:{email}` | Dùng chung `otp_rate:{email}` | Đã có timer 60s ✅ |
| A3. Xác thực OTP | Đọc+xóa `OTP_{email}` | Đọc+xóa `otp_register:{email}` | Không cần | Không cần |
| B1. Quên mật khẩu | `RESET_OTP_{email}` | `otp_reset:{email}` | Dùng chung `otp_rate:{email}` | Countdown 60s, không redirect ngay |
| B2. Đặt lại mật khẩu | Đọc+xóa `RESET_OTP_{email}` | Đọc+xóa `otp_reset:{email}` | Không cần | Không cần |

**Giải thích `otp_rate:{email}` dùng chung:** Cả send-otp, resend-otp, forgot-password đều gửi email. Chỉ cần 1 rate-limit key theo email, TTL=60s. Dù user gọi endpoint nào, nếu đã gửi mail cho email đó trong 60s → chặn toàn bộ. Tránh kịch bản: gọi send-otp xong lập tức gọi forgot-password cùng email → vẫn bị chặn.

**Tại sao dùng Email làm key thay vì User ID:**
- Endpoint send-otp (Đăng ký): User CHƯA CÓ tài khoản → không có User ID
- Endpoint forgot-password: Backend trả thành công ngay cả khi email không tồn tại (chống enumeration) → không có User ID để dùng
- Kết luận: Email là key phù hợp nhất cho cả 2 nghiệp vụ

**Backend — Code mẫu cho send-otp (A1):**

```csharp
public async Task SendOtpAsync(RegisterRequest request)
{
    var existingUser = await _authRepository.GetUserByEmailAsync(request.Email);
    if (existingUser != null)
        throw new InvalidOperationException(ErrorMessages.EmailAlreadyRegistered);

    // Rate limit 60s
    var redis = _connectionMultiplexer.GetDatabase();
    var rateLimitKey = $"otp_rate:{request.Email}";
    if (await redis.KeyExistsAsync(rateLimitKey))
    {
        var ttl = await redis.KeyTimeToLiveAsync(rateLimitKey);
        throw new InvalidOperationException(
            $"Vui lòng chờ {ttl?.Seconds ?? 60} giây trước khi gửi lại OTP.");
    }

    var otp = new Random().Next(100000, 999999).ToString();

    // Lưu OTP vào Redis (OTP mới tự đè OTP cũ, TTL 10 phút)
    var otpData = JsonSerializer.Serialize(new { Request = request, Otp = otp });
    await redis.StringSetAsync($"otp_register:{request.Email}", otpData, TimeSpan.FromMinutes(10));

    // Đặt rate limit 60s
    await redis.StringSetAsync(rateLimitKey, "1", TimeSpan.FromSeconds(60));

    // Gửi email
    var htmlMessage = $"... OTP: {otp} ...";
    await _emailService.SendEmailAsync(request.Email, "Mã Xác Thực OTP", htmlMessage);
}
```

**Backend — Code mẫu cho forgot-password (B1):**

```csharp
public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
{
    var user = await _authRepository.GetUserByEmailAsync(request.Email);
    if (user == null) return; // Chống enumeration

    var redis = _connectionMultiplexer.GetDatabase();

    // Rate limit 60s
    var rateLimitKey = $"otp_rate:{request.Email}";
    if (await redis.KeyExistsAsync(rateLimitKey))
    {
        var ttl = await redis.KeyTimeToLiveAsync(rateLimitKey);
        throw new InvalidOperationException(
            $"Vui lòng chờ {ttl?.Seconds ?? 60} giây trước khi gửi lại.");
    }

    var otp = new Random().Next(100000, 999999).ToString();

    await redis.StringSetAsync($"otp_reset:{request.Email}", otp, TimeSpan.FromMinutes(10));
    await redis.StringSetAsync(rateLimitKey, "1", TimeSpan.FromSeconds(60));

    var htmlMessage = $"... OTP: {otp} ...";
    await _emailService.SendEmailAsync(request.Email, "Đặt Lại Mật Khẩu", htmlMessage);
}
```

**Frontend — forgotPassword.js (bổ sung countdown):**

```javascript
apiClient.post("/api/auth/forgot-password", { Email: email })
    .then(function (response) {
        // Không redirect ngay, hiển thị countdown 60s
        let countdown = 60;
        $btn.prop('disabled', true);
        const timer = setInterval(() => {
            countdown--;
            $btn.text(`Đã gửi OTP (${countdown}s)`);
            if (countdown <= 0) {
                clearInterval(timer);
                $btn.prop('disabled', false).text('Gửi Mã OTP');
            }
        }, 1000);
        $msg.text("Mã OTP đã được gửi đến email của bạn.")
            .removeClass('text-danger').addClass('text-success');
        // Redirect sau 3s để user thấy thông báo
        setTimeout(() => {
            window.location.href = `/Auth/ResetPassword?email=${encodeURIComponent(email)}`;
        }, 3000);
    })
    .catch(function (err) {
        $msg.text(err.message || "Có lỗi xảy ra...")
            .removeClass('text-success').addClass('text-danger');
        $btn.prop('disabled', false).text('Gửi Mã OTP');
    });
```

#### 5.3.3 Phân tích OTP trùng

**Xác suất trùng:** OTP 6 chữ số → 1.000.000 giá trị. Xác suất 2 user cùng OTP = 1/1.000.000 = 0.0001%.

**Có ảnh hưởng bảo mật không? KHÔNG**, vì:
1. OTP gắn với email cụ thể (`otp_register:{email}`). Kẻ tấn công biết OTP nhưng không biết thuộc email nào.
2. Phải biết cả email + đoán đúng OTP 6 số trong 10 phút → xác suất cực thấp.
3. OTP dùng 1 lần (xóa khỏi Redis sau verify).

**Các hệ thống lớn (Google, Microsoft, Meta):**
- Google CHO PHÉP OTP trùng giữa các user. Chỉ kiểm tra `(email + OTP)` khớp nhau.
- Google KHÔNG kiểm tra OTP global uniqueness.
- Biện pháp bổ sung: Rate limit, CAPTCHA, Device fingerprint, IP-based throttling.

**Kết luận:** OTP trùng giữa 2 user khác nhau hoàn toàn bình thường, không phải lỗ hổng bảo mật.

---

## 6. Hướng Dẫn Cài Đặt Redis Trên Máy Chủ

### 6.1 Cài đặt (Ubuntu/Debian)

```bash
sudo apt update && sudo apt install redis-server -y
```

### 6.2 Cấu hình Production

```bash
sudo nano /etc/redis/redis.conf
```

Các config quan trọng:
```conf
daemonize yes
maxmemory 128mb
maxmemory-policy allkeys-lru
bind 127.0.0.1
save ""
appendonly no
requirepass YourRedisPassword123
```

### 6.3 Khởi động & Kiểm tra

```bash
sudo systemctl enable redis-server
sudo systemctl start redis-server
redis-cli -a YourRedisPassword123 ping
# → PONG
```

### 6.4 Cấu hình trong dự án .NET

**NuGet packages cần cài:**
```xml
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.10" />
```

**Connection string (appsettings.json):**
```json
"ConnectionStrings": {
    "MyCnn": "...(giữ nguyên)...",
    "Redis": "localhost:6379,password=YourRedisPassword123,abortConnect=false"
}
```

**Program.cs:**
```csharp
// Thay thế AddMemoryCache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MTCA_";
});
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379")
);

// Đăng ký Background Worker
builder.Services.AddHostedService<ExamCreationWorker>();
```

### 6.5 Tài nguyên Redis tiêu thụ (ước tính)

| Metric | Giá trị |
|:--|:--|
| RAM khi idle | ~5-10 MB |
| RAM cho 1000 OTP entries | ~1-2 MB |
| RAM cho exam queue (50 jobs) | < 1 MB |
| CPU khi idle | ~0% |
| CPU khi 200 ops/s | < 1% |

---

## 7. Bảng Tổng Hợp Các Thay Đổi Cần Thực Hiện

| # | File | Thay đổi | Ưu tiên |
|:--|:--|:--|:--|
| 1 | `Backend/Hubs/ExamHub.cs` | **XÓA FILE** | 🔴 Cao |
| 2 | `Backend/Program.cs` | Xóa SignalR, đổi `AddMemoryCache` → Redis, thêm `AddHostedService` | 🔴 Cao |
| 3 | `Backend/Backend.csproj` | Thêm NuGet: `StackExchange.Redis`, `Microsoft.Extensions.Caching.StackExchangeRedis` | 🔴 Cao |
| 4 | `Backend/appsettings.json` | Thêm Redis connection string | 🔴 Cao |
| 5 | `Backend/Constants/ExamStatus.cs` | Thêm `Generating = -1` | 🔴 Cao |
| 6 | `Backend/Services/Implements/ExamCreationWorker.cs` | **FILE MỚI** — BackgroundService đọc Redis queue | 🔴 Cao |
| 7 | `Backend/Services/Implements/AssignExamService.cs` | Tách `CreateAssignExamAsync` → `EnqueueExamCreationAsync` + `GetExamCreationStatusAsync` | 🔴 Cao |
| 8 | `Backend/Controllers/AssignExamController.cs` | Sửa `CreateAssignExam` → trả 202 + Thêm `GET status/{examId}` | 🔴 Cao |
| 9 | `Backend/Services/Implements/AuthService.cs` | Đổi `IMemoryCache` → `IConnectionMultiplexer`, thêm rate limit 60s | 🟡 Trung bình |
| 10 | `Frontend/wwwroot/js/assign_exam.js` | Sửa response handling: không chờ result, redirect ngay | 🟡 Trung bình |
| 11 | `Frontend/wwwroot/js/exam_review.js` | Bổ sung `startGenerationPolling()` polling 10s | 🟡 Trung bình |
| 12 | `Frontend/wwwroot/js/Auth/forgotPassword.js` | Bổ sung countdown 60s, không redirect ngay | 🟢 Thấp |
| 13 | `Frontend/wwwroot/js/Auth/register-otp.js` | Lưu `otpSentAt`, disable form sau khi gửi | 🟢 Thấp |
| 14 | VPS Server | Cài đặt Redis Server | 🔴 Cao |

---

*Kết thúc báo cáo.*
