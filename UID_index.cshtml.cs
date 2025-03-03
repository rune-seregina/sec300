using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;
using MySqlConnector;

namespace MyFirstApplication.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "First name is required")]
        [StringLength(14, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 14 characters")]
        public string firstName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters")]
        [CustomEmailValidation(ErrorMessage = "Invalid email format")]
        public string email { get; set; }

        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnPost()
        {
            _logger.LogInformation($"OnPost called with firstName: {firstName}, email: {email}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                return Page();
            }

            SendConfirmationEmail();
            _logger.LogInformation($"Confirmation email sent for {firstName}, {email}");
            return RedirectToPage("Index");
        }

        private string RandomLinkGenerator(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private void StoreUidInDatabase(string uid, string email)
        {
            try
            {
                using (var con = new MySqlConnection("server=192.168.32.137;user id=webserver;database=registration;password=mypass"))
                {
                    con.Open();
                    using (var command = con.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO requests (fname, email, rdate, uid, used) VALUES (@fname, @email, @rdate, @uid, 0)";
                        command.Parameters.AddWithValue("@fname", firstName);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@rdate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.Parameters.AddWithValue("@uid", uid);
                        int rowsAffected = command.ExecuteNonQuery();
                        _logger.LogInformation($"Rows affected: {rowsAffected}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error storing UID in database: {ex.Message}");
            }
        }

        public void SendConfirmationEmail()
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            var from = "rune.seregina@mymail.champlain.edu";
            var username = "rune.seregina@mymail.champlain.edu";
            var password = "[redacted]";

            var uid = RandomLinkGenerator(32);

            var scheme = HttpContext.Request.Scheme;
            var host = HttpContext.Request.Host.ToUriComponent();
            var pathBase = HttpContext.Request.PathBase.ToUriComponent();
            var confirmationLink = $"{scheme}://{host}{pathBase}/Shared/Confirmation?uid={uid}";

            var subject = "Confirm Registration";
            var body = $"Hi {firstName}, here is your link to confirm: {confirmationLink}";
            var customerName = this.firstName;
            var to = this.email;

            MailMessage msg = new MailMessage(from, email, subject, body);
            SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);

            smtp.Credentials = new NetworkCredential(username, password);
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;

            try
            {
                smtp.Send(msg);
                _logger.LogInformation($"Confirmation email sent to {to}");
                StoreUidInDatabase(uid, to);
                _logger.LogInformation($"UID stored in database for {to}");

                // Update the requests table with the UID and set 'used' to 0
                using (var con = new MySqlConnection("server=192.168.32.137;user id=webserver;database=registration;password=mypass"))
                {
                    con.Open();
                    using (var command = con.CreateCommand())
                    {
                        command.CommandText = "UPDATE requests SET uid = @uid, used = 0, rdate = @rdate WHERE email = @email";
                        command.Parameters.AddWithValue("@uid", uid);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@rdate", DateTime.UtcNow);
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            // If no rows were updated, insert a new record
                            command.CommandText = "INSERT INTO requests (fname, email, rdate, uid, used) VALUES (@fname, @email, @rdate, @uid, 0)";
                            command.Parameters.AddWithValue("@fname", firstName);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                _logger.LogError($"Error sending confirmation email: {exp.Message}");
            }
        }
    }

    public class CustomEmailValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Email is required");
            }

            var email = value.ToString();

            // Check overall length
            if (email.Length < 5 || email.Length > 254)
            {
                return new ValidationResult("Email address must be between 5 and 254 characters");
            }

            // Check format using a more strict regex
            var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!regex.IsMatch(email))
            {
                return new ValidationResult("Invalid email format");
            }

            // Check local part and domain part lengths
            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return new ValidationResult("Invalid email format");
            }

            if (parts[0].Length < 1 || parts[0].Length > 64)
            {
                return new ValidationResult("The local part of the email must be between 1 and 64 characters");
            }

            if (parts[1].Length < 4 || parts[1].Length > 255)
            {
                return new ValidationResult("The domain part of the email must be between 4 and 255 characters");
            }

            // Check if the domain part has at least one dot and two characters after the last dot
            var domainParts = parts[1].Split('.');
            if (domainParts.Length < 2 || domainParts[domainParts.Length - 1].Length < 2)
            {
                return new ValidationResult("Invalid email format: domain must have at least one dot and two characters after the last dot");
            }

            return ValidationResult.Success;
        }
    }
}
