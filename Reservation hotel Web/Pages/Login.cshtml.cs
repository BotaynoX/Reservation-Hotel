using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;

namespace Reservation_hotel.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }
        public string ErrorMessage { get; set; }

        public IActionResult OnPost()
        {
            // Check if inputs are not empty
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please fill in all fields.";
                return Page();
            }

            // Database connection string
            string connectionString = "Server=DESKTOP-2K5N25U\\SQLEXPRESS;Database=HotelDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // SQL query to check client existence
                string query = "SELECT Id, Nom, Prenom FROM Client WHERE Email = @Email AND Password = @Password";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", Email);
                    command.Parameters.AddWithValue("@Password", Password);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // If client exists
                        {
                            // Store session variables
                            HttpContext.Session.SetInt32("ClientId", reader.GetInt32(0));
                            HttpContext.Session.SetString("ClientName", reader.GetString(1) + " " + reader.GetString(2));

                            // Redirect to home page
                            return RedirectToPage("/Index");
                        }
                        else
                        {
                            ErrorMessage = "Invalid Email or Password.";
                        }
                    }
                }
            }

            return Page();
        }
    }
}
