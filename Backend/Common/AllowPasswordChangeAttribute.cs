namespace Backend.Common;

// Marker để FirstLoginPasswordMiddleware bỏ qua check claim mcp.
// Gắn lên action cho phép user mcp=true gọi (đổi password lần đầu, logout).
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AllowPasswordChangeAttribute : Attribute;
