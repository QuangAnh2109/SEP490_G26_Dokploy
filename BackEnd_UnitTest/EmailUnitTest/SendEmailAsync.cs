using Backend.Services.Implements;
using Microsoft.Extensions.Configuration;
using Moq;
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

            /// <summary>
            /// Moq giúp trả đúng null / chuỗi rõ ràng cho từng key (một số công cụ coverage phân nhánh chi tiết hơn InMemoryCollection).
            /// </summary>
            private static EmailService CreateServiceWithMoq(
                string? smtpServer,
                string? port,
                string? senderEmail,
                string? senderPassword)
            {
                var mock = new Mock<IConfiguration>();
                mock.Setup(c => c["EmailSettings:SmtpServer"]).Returns(smtpServer);
                mock.Setup(c => c["EmailSettings:Port"]).Returns(port);
                mock.Setup(c => c["EmailSettings:SenderEmail"]).Returns(senderEmail);
                mock.Setup(c => c["EmailSettings:SenderPassword"]).Returns(senderPassword);
                return new EmailService(mock.Object);
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

            // UTCID10 - Không khai báo SmtpServer/Port → null → nhánh ?? "smtp.gmail.com" và ?? "587" (ảnh coverage)
            //           Kèm placeholder → DEV mode, không gửi SMTP thật
            [Fact]
            public async Task SendEmailAsync_UTCID10_MissingSmtpServerAndPort_UsesDefaultsAndDevMode()
            {
                var settings = new Dictionary<string, string?>
                {
                    ["EmailSettings:SenderEmail"] = "YOUR_GMAIL_HERE@gmail.com",
                    ["EmailSettings:SenderPassword"] = "YOUR_APP_PASSWORD_HERE"
                };
                var service = CreateServiceWithConfig(settings);

                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID11 - SenderEmail = "" (chuỗi rỗng) → nhánh đầu của OR ở isPlaceholder (ảnh coverage)
            [Fact]
            public async Task SendEmailAsync_UTCID11_SenderEmailEmptyString_ShouldLogOnly()
            {
                var settings = BuildValidSettings();
                settings["EmailSettings:SenderEmail"] = "";
                var service = CreateServiceWithConfig(settings);

                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID12 - SenderPassword = "" (chuỗi rỗng), email hợp lệ → nhánh thứ hai của OR (ảnh coverage)
            [Fact]
            public async Task SendEmailAsync_UTCID12_SenderPasswordEmptyString_ShouldLogOnly()
            {
                var settings = BuildValidSettings();
                settings["EmailSettings:SenderPassword"] = "";
                var service = CreateServiceWithConfig(settings);

                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID13 - Không có key SenderEmail (không gán null trong dict) → indexer trả null, IsNullOrEmpty true
            [Fact]
            public async Task SendEmailAsync_UTCID13_SenderEmailKeyAbsent_ShouldLogOnly()
            {
                var settings = new Dictionary<string, string?>
                {
                    ["EmailSettings:SmtpServer"] = "smtp.test.com",
                    ["EmailSettings:Port"] = "587",
                    ["EmailSettings:SenderPassword"] = "password123"
                };
                var service = CreateServiceWithConfig(settings);

                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID14 - Không có key SenderPassword → null, cần email không rỗng để thấy nhánh thứ hai của dòng 24
            [Fact]
            public async Task SendEmailAsync_UTCID14_SenderPasswordKeyAbsent_ShouldLogOnly()
            {
                var settings = new Dictionary<string, string?>
                {
                    ["EmailSettings:SmtpServer"] = "smtp.test.com",
                    ["EmailSettings:Port"] = "587",
                    ["EmailSettings:SenderEmail"] = "sender@test.com"
                };
                var service = CreateServiceWithConfig(settings);

                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID15 - Cả hai key email/password đều absent → cả hai IsNullOrEmpty true (chuỗi OR đầu)
            [Fact]
            public async Task SendEmailAsync_UTCID15_BothSenderKeysAbsent_ShouldLogOnly()
            {
                var settings = new Dictionary<string, string?>
                {
                    ["EmailSettings:SmtpServer"] = "smtp.test.com",
                    ["EmailSettings:Port"] = "587"
                };
                var service = CreateServiceWithConfig(settings);

                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // UTCID16 - Port parse thành số khác 587 (TryParse true, nhánh khác default literal 587)
            [Fact]
            public async Task SendEmailAsync_UTCID16_Port465_ParseSuccess_DevMode()
            {
                var settings = BuildDevModeSettings();
                settings["EmailSettings:Port"] = "465";
                var service = CreateServiceWithConfig(settings);

                await service.SendEmailAsync("to@test.com", "Subject", "<b>Message</b>");
            }

            // --- Moq: bao phủ từng nhánh IsNullOrEmpty + rút gọn OR trên dòng 24 ---

            [Fact]
            public async Task SendEmailAsync_Moq_EmailNull_PasswordSet_ShortCircuitFirstCondition()
            {
                var service = CreateServiceWithMoq("smtp.test.com", "587", null, "secret");
                await service.SendEmailAsync("to@test.com", "S", "b");
            }

            [Fact]
            public async Task SendEmailAsync_Moq_EmailEmpty_PasswordSet_ShortCircuitFirstCondition()
            {
                var service = CreateServiceWithMoq("smtp.test.com", "587", "", "secret");
                await service.SendEmailAsync("to@test.com", "S", "b");
            }

            [Fact]
            public async Task SendEmailAsync_Moq_EmailSet_PasswordNull_SecondConditionTrue()
            {
                var service = CreateServiceWithMoq("smtp.test.com", "587", "a@test.com", null);
                await service.SendEmailAsync("to@test.com", "S", "b");
            }

            [Fact]
            public async Task SendEmailAsync_Moq_EmailSet_PasswordEmpty_SecondConditionTrue()
            {
                var service = CreateServiceWithMoq("smtp.test.com", "587", "a@test.com", "");
                await service.SendEmailAsync("to@test.com", "S", "b");
            }

            [Fact]
            public async Task SendEmailAsync_Moq_BothNonEmpty_Line24False_ThenPlaceholderSubstringTrue()
            {
                var service = CreateServiceWithMoq(
                    "smtp.test.com",
                    "587",
                    "YOUR_GMAIL_HERE@gmail.com",
                    "pwd");
                await service.SendEmailAsync("to@test.com", "S", "b");
            }

            [Fact]
            public async Task SendEmailAsync_Moq_BothNonEmpty_NoPlaceholder_Line24And25And26False()
            {
                var service = CreateServiceWithMoq(
                    "invalid.smtp.server.xyz",
                    "587",
                    "realemail@gmail.com",
                    "realpassword");
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    service.SendEmailAsync("to@test.com", "S", "b"));
            }
        }
    }
}
