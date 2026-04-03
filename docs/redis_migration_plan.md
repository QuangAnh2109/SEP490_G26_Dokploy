# Kế hoạch triển khai Redis cho Cache và Message Queue

Kế hoạch này vạch ra các bước chi tiết để thay thế hệ thống cache nội bộ (`IMemoryCache`) sang Redis, và triển khai một hàng đợi tác vụ (Message Queue) cũng bằng Redis để xử lý các logic lâu dài mà không làm treo API.

## Dữ liệu thu thập từ mã nguồn hiện tại

Sau khi quét toàn bộ backend (`grep_search` với từ khóa `IMemoryCache` và `cache`), tôi phát hiện:
- `IMemoryCache` hiện đang được cấu hình tại `Program.cs`.
- `AuthService.cs` đang sử dụng `_cache.Set()`, `_cache.TryGetValue()`, và `_cache.Remove()` để tạm lưu các thông tin liên quan tới **OTP**:
  - Gửi mã OTP đăng ký người dùng: `OTP_{request.Email}`.
  - Gửi mã OTP đặt lại mật khẩu: `RESET_OTP_{request.Email}`.
- Các dữ liệu OTP đang được cấp thời hạn sống là 10 phút. Dữ liệu là đối tượng vô danh (anonymous object) cho Register, hoặc là chuỗi `string` cho Reset Password.

### Lên kế hoạch đưa các chỗ đang dùng Cache vào Redis
- Thay vì dùng `IMemoryCache`, ta sẽ chuyển sang dùng `IDistributedCache` mang lại bởi package của Redis, hoặc tương tác thẳng với giao diện `IConnectionMultiplexer` của bảng `StackExchange.Redis`.
- Do `IDistributedCache` chỉ chấp nhận lưu trữ mảng `byte[]` hay `string`, dữ liệu Object OTP (gồm thông tin request và mã OTP) sẽ cần dùng thư viện `System.Text.Json` để **mã hóa/giải mã (Serialize/Deserialize)** ra chuỗi JSON trước khi lưu và sau khi lấy từ cache.
- Thay thế các phương thức: `_cache.Set` thành `_distributedCache.SetStringAsync` (với thông số hết hạn 10 phút), `_cache.TryGetValue` thành `_distributedCache.GetStringAsync`.

## Cài đặt thư viện và cấu hình cần thiết

### 1. Packages (NuGet) cần bổ sung:
- `StackExchange.Redis` (thư viện gốc cực kỳ linh hoạt để kết nối Redis, giúp thao tác list cho Message Queue).
- `Microsoft.Extensions.Caching.StackExchangeRedis` (giúp cung cấp implementation cho `IDistributedCache`).

Có thể chạy trực tiếp qua CLI: `dotnet add package StackExchange.Redis` và `dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis`.

### 2. Bổ sung Connection String:
Trong file `.env`:
```env
REDIS_CONNECTION_STRING=172.19.0.x:6379,password=your_redis_password
```
Trong `appsettings.json`:
```json
"ConnectionStrings": {
  "MyCnn": "...",
  "Redis": "" // Sẽ được nạp từ biến môi trường
}
```

Tại `Program.cs`, chúng ta đăng ký:
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MtcaAPI_";
});
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis"))
);
```

## Giải đáp thắc mắc về Hàng Đợi (Message Queue)

1. **Khi nào thì hàng đợi hoạt động?**  
   Hàng đợi hoạt động ngầm **liên tục** ngay khi Backend Server (.NET API) được khởi động. Ta sẽ tạo ra một lớp Worker kế thừa `BackgroundService` hoặc `IHostedService`. Background worker này sẽ liên tục kiểm tra trên Redis (sử dụng lệnh `BBLPOP` hoặc Pub/Sub) để "lắng nghe". Ngay khi có bất kì giá trị nào bị đưa vào định dạng List trên Redis, worker sẽ ngay lập tức kéo ra (pop) và đem đi xử lý tiếp mà không chặn thread gốc của người dùng.

2. **Tối đa bao nhiêu phần tử nằm trong hàng đợi?**  
   Về phía Redis: Một List của Redis có thể chứa được **hơn 4 tỷ phần tử** (2^32 - 1). Do đó, giới hạn duy nhất của hàng đợi chính là **Dung lượng bộ nhớ RAM** cung cấp cho container Redis server của bạn.
   
3. **Giả sử 2 API dùng chung hàng đợi thì nó dùng chung hay riêng?**  
   Trạng thái này hoàn toàn phụ thuộc vào **Tên hàng đợi (Queue Key / Topic)** mà bạn định nghĩa. 
   - Nếu bạn lập trình API A và API B đẩy dữ liệu vào **cùng một Key** trên Redis (VD: code ghi `await db.ListRightPushAsync("TasksQueue", data);`), thì 2 API sẽ xài **CHUNG một hàng đợi**, các item sẽ được đưa chung vào một hàng và những worker rảnh sẽ lần lượt bốc ra xử lý.
   - Nếu bạn cấu hình API A đẩy vào key `TasksQueue_A`, API B đẩy vào `TasksQueue_B`, thì bạn sẽ có **2 hàng đợi RIÊNG BIỆT**, và cần tạo 2 loại worker (tách biệt về logic) để xử lý song lặp cho chúng.

## Đề xuất bộ Controller, Service và Repository mẫu

Ta sẽ tạo ra một luồng tác vụ mẫu: API lấy thông tin User qua ID nhưng sẽ bị **làm nghẽn 2s**. Vì bị delay cản trở tốc độ nếu gọi bình thường, nên tác vụ lấy thông tin sẽ được API nhét thẳng vào Hàng đợi trên Redis, và Worker chạy nền (background tasks) sẽ lo việc hoàn thành tác vụ đó.

### `Backend/Controllers/SampleQueueController.cs`
API Endpoint: `POST /api/samplequeue/process-user/{userId}`
Controller nhận request, gọi hàm Service để cấu trúc Job rồi vứt nó xuống Redis Queue, sau đó return kết quả ngay HTTP 202 (Accepted) cho người dùng mà không cần chờ.

### `Backend/Services/Interfaces/ISampleQueueService.cs` & `Backend/Services/Implements/SampleQueueService.cs`
Chứa các logic:
- `EnqueueUserTaskAsync(int userId)`: Serializing thành JSON sau đó đẩy dữ liệu xuống Redis List (VD: với Key là `UserTaskQueue`).

### `Backend/Repositories/Interfaces/ISampleUserRepository.cs` & `Backend/Repositories/Implements/SampleUserRepository.cs`
Chứa hàm `GetUserInfoSlowAsync(int userId)` xử lý lấy User từ DB. Ta sẽ thêm hàm `await Task.Delay(2000)` trước khi query (fake delay). Hàm này **không** được gọi từ Controller, mà sẽ do **Worker nằm trong phần chạy nền** gọi.

### `Backend/Workers/RedisQueueWorker.cs`
Đây là lớp nền kế thừa `BackgroundService` chạy cùng server. Tại hàm `ExecuteAsync`, ta dùng vòng lặp vô tận (cho đến khi hệ thống stop) quét qua Redis queue. Nếu bắt được job nằm ở cuối hàng (`ListRightPopAsync`), tiến hành gọi `ISampleUserRepository` để xử lí 2s và in thành quả ra giao diện Log của console server.
