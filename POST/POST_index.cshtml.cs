using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyFirstApplication.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string firstName { get; set; }

        [BindProperty]
        public string preference { get; set; }

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
            _logger.LogInformation($"firstName: {firstName}, preference: {preference}");

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
            }

            return Page();
        }
    }
}
