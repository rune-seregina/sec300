using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyFirstApplication.Pages
{
    public class IndexModel : PageModel
    {

        public string firstName { get; set; }

        public string preference { get; set; }

        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(string firstName, string preference)
        {
            this.firstName = firstName;
            this.preference = preference;

        }
    }
