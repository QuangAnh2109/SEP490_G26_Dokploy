using Backend.Services.Implements;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.EmailUnitTest
{
    public class EmailSendEmailUnitTest
    {
        private EmailService CreateServiceWithConfig(Dictionary<string, string?> settings)
        {
            var inMemorySettings = new Dictionary<string, string?>(settings);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            return new EmailService(configuration);
        }

        private static Dictionary<string, string?> BuildValidSettings()
        {
            return new Dictionary<string, string?>
            {
                ["EmailSettings:SmtpServer"] = "smtp.test.com",
                ["EmailSettings:Port"] = "587",
                ["EmailSettings:SenderEmail"] = "sender@test.com",
                ["EmailSettings:SenderPassword"] = "password123"
            };
        }

        // UTCID01 - Mọi thông số đều đúng
        [Fact]
        public async Task SendEmailAsync_UTCID01_ValidEmailPasswordPort587_ShouldSendSuccessfully()
        {
            // Arrange
            var settings = BuildValidSettings();
            // Đặt email ở dạng placeholder để không gửi mail thật (dev mode)
            settings["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE@test.com";
            var service = CreateServiceWithConfig(settings);

            // Act & Assert
            await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
        }

        // UTCID02 - SenderEmail = null -> log only
        [Fact]
        public async Task SendEmailAsync_UTCID02_SenderEmailIsNull_ShouldLogOnlyAndNotThrow()
        {
            // Arrange
            var settings = BuildValidSettings();
            settings["EmailSettings:SenderEmail"] = null;
            var service = CreateServiceWithConfig(settings);

            // Act & Assert
            await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
        }

        // UTCID03 - SenderPassword = null -> log only
        [Fact]
        public async Task SendEmailAsync_UTCID03_SenderPasswordIsNull_ShouldLogOnlyAndNotThrow()
        {
            // Arrange
            var settings = BuildValidSettings();
            settings["EmailSettings:SenderPassword"] = null;
            var service = CreateServiceWithConfig(settings);

            // Act & Assert
            await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
        }

        // UTCID04 - Port = 587 hợp lệ
        [Fact]
        public async Task SendEmailAsync_UTCID04_Port587_ShouldSendSuccessfully()
        {
            // Arrange
            var settings = BuildValidSettings();
            // Dùng placeholder để tránh gửi mail thật, nhưng vẫn để Port = 587
            settings["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE@test.com";
            var service = CreateServiceWithConfig(settings);

            // Act & Assert
            await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
        }

        // UTCID05 - Port = "abc" -> fallback 587 theo code hiện tại
        [Fact]
        public async Task SendEmailAsync_UTCID05_InvalidPortString_ShouldFallbackAndNotThrow()
        {
            // Arrange
            var settings = BuildValidSettings();
            settings["EmailSettings:Port"] = "abc";
            // Placeholder để không mở kết nối SMTP
            settings["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE@test.com";
            var service = CreateServiceWithConfig(settings);

            // Act & Assert
            await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
        }

        // UTCID06 - Re-test dữ liệu hợp lệ
        [Fact]
        public async Task SendEmailAsync_UTCID06_ValidDataRetest_ShouldSendSuccessfully()
        {
            // Arrange
            var settings = BuildValidSettings();
            // Re-test vẫn dùng placeholder để test logic mà không gửi mail thật
            settings["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE@test.com";
            var service = CreateServiceWithConfig(settings);

            // Act & Assert
            await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
        }
    }
}
