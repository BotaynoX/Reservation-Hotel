using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Reservation_hotel.Pages
{
    public class RegisterModel : PageModel
    {
        // Bind properties to retain values
        [BindProperty]
        public string Nom { get; set; }

        [BindProperty]
        public string Prenom { get; set; }

        [BindProperty]
        public string Cin { get; set; }

        [BindProperty]
        public string Telephone { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // Validate inputs
            if (string.IsNullOrEmpty(Nom) || string.IsNullOrEmpty(Prenom) || string.IsNullOrEmpty(Cin) ||
                string.IsNullOrEmpty(Telephone) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "All fields are required.";
                return Page(); // Return the page with filled inputs
            }

            // Validate email format
            if (!Email.Contains("@") || !Email.Contains("."))
            {
                ErrorMessage = "Invalid email format.";
                return Page(); // Retain inputs
            }

            // Validate telephone (digits only)
            if (!Telephone.All(char.IsDigit))
            {
                ErrorMessage = "Telephone must contain only digits.";
                return Page(); // Retain inputs
            }

            // Database connection string
            string connectionString = "Server=DESKTOP-2K5N25U\\SQLEXPRESS;Database=HotelDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if email already exists
                    string checkEmailQuery = "SELECT COUNT(*) FROM Client WHERE Email = @Email";
                    using (SqlCommand checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Email);
                        int count = (int)checkCommand.ExecuteScalar();

                        if (count > 0)
                        {
                            ErrorMessage = "Email already exists. Please use a different email.";
                            return Page(); // Retain inputs
                        }
                    }

                    // Insert new client into the database
                    string query = "INSERT INTO Client (Nom, Prenom, Cin, Telephone, Email, Password) " +
                                   "VALUES (@Nom, @Prenom, @Cin, @Telephone, @Email, @Password)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nom", Nom);
                        command.Parameters.AddWithValue("@Prenom", Prenom);
                        command.Parameters.AddWithValue("@Cin", Cin);
                        command.Parameters.AddWithValue("@Telephone", Telephone);
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Password", Password);

                        command.ExecuteNonQuery();
                    }

                    // Redirect to Login page after success
                    return RedirectToPage("/Login");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred: " + ex.Message;
                return Page(); // Retain inputs
            }
        }
    }
}
