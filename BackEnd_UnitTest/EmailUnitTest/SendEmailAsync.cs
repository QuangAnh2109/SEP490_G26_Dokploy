using Backend.Services.Implements;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.EmailUnitTest
{
    namespace Backend_UnitTest
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

            private static Dictionary<string, string?> BuildDevModeSettings()
            {
                return new Dictionary<string, string?>
                {
                    ["EmailSettings:SmtpServer"] = "smtp.test.com",
                    ["EmailSettings:Port"] = "587",
                    ["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE@gmail.com",
                    ["EmailSettings:SenderPassword"] = "YOUR_APP_PASSWORD_HERE"
                };
            }

            // UTCID01 - Normal: Mọi thông số đều đúng (DEV mode - placeholder)
            //           → Log only, không gửi mail thật
            [Fact]
            public async Task SendEmailAsync_UTCID01_ValidEmailPasswordPort587_ShouldSendSuccessfully()
            {
                // Arrange
                var settings = BuildDevModeSettings();
                var service = CreateServiceWithConfig(settings);

                // Act & Assert - Không throw là thành công
                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID02 - SenderEmail = null → isPlaceholder = true → DEV mode
            //           → Log only, không throw
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

            // UTCID03 - SenderPassword = null → isPlaceholder = true → DEV mode
            //           → Log only, không throw
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

            // UTCID04 - Cấu hình SMTP không hợp lệ (SmtpServer sai)
            //           → SmtpException khi kết nối thật
            [Fact]
            public async Task SendEmailAsync_UTCID04_InvalidSmtpServer_ShouldThrowSmtpException()
            {
                // Arrange - Dùng email/password thật (không phải placeholder)
                // để bypass DEV mode và chạm tới SmtpClient thật
                var settings = new Dictionary<string, string?>
                {
                    ["EmailSettings:SmtpServer"] = "invalid.smtp.server.xyz",
                    ["EmailSettings:Port"] = "587",
                    ["EmailSettings:SenderEmail"] = "realemail@gmail.com",
                    ["EmailSettings:SenderPassword"] = "realpassword"
                };
                var service = CreateServiceWithConfig(settings);

                // Act & Assert - SmtpClient không kết nối được → throw
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>"));
            }

            // UTCID05 - Port = "abc" (không hợp lệ) → fallback Port = 587
            //           → DEV mode nên chỉ log, không throw
            [Fact]
            public async Task SendEmailAsync_UTCID05_InvalidPortString_ShouldFallbackAndNotThrow()
            {
                // Arrange
                var settings = BuildDevModeSettings();
                settings["EmailSettings:Port"] = "abc";
                var service = CreateServiceWithConfig(settings);

                // Act & Assert - DEV mode → log only, Port fallback = 587
                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID06 - SMTP hợp lệ nhưng Password = null
            //           → isPlaceholder = true → DEV mode → log only
            [Fact]
            public async Task SendEmailAsync_UTCID06_ValidSmtp_NullPassword_ShouldLogOnly()
            {
                // Arrange
                var settings = BuildValidSettings();
                settings["EmailSettings:SenderPassword"] = null; // Password null → DEV mode
                var service = CreateServiceWithConfig(settings);

                // Act & Assert - isPlaceholder = true → không reach SmtpClient
                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID07 - DEV mode: SenderEmail chứa "YOUR_GMAIL_HERE"
            //           → isPlaceholder = true → log only
            [Fact]
            public async Task SendEmailAsync_UTCID07_DevMode_PlaceholderEmail_ShouldLogOnly()
            {
                // Arrange
                var settings = BuildValidSettings();
                settings["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE@gmail.com";
                var service = CreateServiceWithConfig(settings);

                // Act & Assert
                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID08 - DEV mode: SenderPassword chứa "YOUR_APP_PASSWORD_HERE"
            //           → isPlaceholder = true → log only
            [Fact]
            public async Task SendEmailAsync_UTCID08_DevMode_PlaceholderPassword_ShouldLogOnly()
            {
                // Arrange
                var settings = BuildValidSettings();
                settings["EmailSettings:SenderPassword"] = "YOUR_APP_PASSWORD_HERE";
                var service = CreateServiceWithConfig(settings);

                // Act & Assert
                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID09 - DEV mode: Cả Email và Password đều là placeholder
            //           → isPlaceholder = true → log only
            [Fact]
            public async Task SendEmailAsync_UTCID09_DevMode_BothPlaceholders_ShouldLogOnly()
            {
                // Arrange
                var settings = BuildDevModeSettings(); // Email + Password đều là placeholder
                var service = CreateServiceWithConfig(settings);

                // Act & Assert
                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }
        }
    }
}
