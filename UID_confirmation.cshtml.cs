using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;

namespace MyFirstApplication.Pages.Shared
{
    public class ConfirmationModel : PageModel
    {
        private readonly ILogger<ConfirmationModel> _logger;
        private readonly string _connectionString = "server=192.168.32.137;user id=webserver;database=registration;password=mypass";

        public string Message { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }

        public ConfirmationModel(ILogger<ConfirmationModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                Message = "Invalid confirmation link.";
                return Page();
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                    SELECT rdate, fname, email 
                    FROM requests 
                    WHERE uid = @uid AND used = 0";
                    command.Parameters.AddWithValue("@uid", uid);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var createdAt = reader.GetDateTime("rdate");
                            var timeDifference = (DateTime.UtcNow - createdAt).TotalMinutes;

                            FirstName = reader.GetString("fname");
                            Email = reader.GetString("email");

                            if (timeDifference > 5) // 5 minutes expiration
                            {
                                Message = "The confirmation link has expired. Please request a new one.";
                            }
                            else
                            {
                                Message = "User registration successful! Your email has been confirmed.";

                                // Mark the link as used
                                reader.Close();
                                command.CommandText = "UPDATE requests SET used = 1 WHERE uid = @uid";
                                command.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            Message = "Invalid or already used confirmation link.";
                        }
                    }
                }
            }

            return Page();
        }
    }
}
