using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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

        public void OnGet()
        {
            // This method is called when the page is first loaded
        }

        public IActionResult OnPost()
        {
            _logger.LogInformation("OnPost method called");
            _logger.LogInformation($"firstName: {firstName}, email: {email}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"Model error: {error.ErrorMessage}");
                    }
                }
                return Page(); // Return the page with validation errors
            }

            // If we reach here, the form is valid
            _logger.LogInformation("Form is valid, processing submission");
            // Process the form data here

            // Redirect to a success page or do something else
            return RedirectToPage("Success");
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
