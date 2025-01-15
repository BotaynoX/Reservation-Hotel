using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Reservation_hotel.Services;
using System;

namespace Reservation_hotel.Pages
{
    public class ReservationModel : PageModel
    {
        public class Chambre
        {
            public int Id { get; set; }
            public int NumeroChambre { get; set; }
            public string TypeChambre { get; set; }
            public decimal Prix { get; set; }
        }

        private readonly EmailService _emailService;

        public ReservationModel(EmailService emailService)
        {
            _emailService = emailService;
        }

        public Chambre SelectedRoom { get; set; } = new Chambre();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        [BindProperty]
        public DateTime? DateDebut { get; set; }

        [BindProperty]
        public DateTime? DateFin { get; set; }

        [BindProperty]
        public int RoomId { get; set; }

        public void OnGet(int roomId)
        {
            LoadRoomDetails(roomId);
        }

        public IActionResult OnPost()
        {
            LoadRoomDetails(RoomId);

            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToPage("/Login");
            }

            if (!DateDebut.HasValue || !DateFin.HasValue || DateDebut >= DateFin)
            {
                ErrorMessage = "Start Date must be before End Date.";
                return Page();
            }

            string connectionString = "Server=DESKTOP-2K5N25U\\SQLEXPRESS;Database=HotelDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string insertQuery = @"
                        INSERT INTO Reservation (DateDebut, DateFin, ChambreId, ClientId)
                        VALUES (@DateDebut, @DateFin, @ChambreId, @ClientId)";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@DateDebut", DateDebut);
                        command.Parameters.AddWithValue("@DateFin", DateFin);
                        command.Parameters.AddWithValue("@ChambreId", RoomId);
                        command.Parameters.AddWithValue("@ClientId", clientId);
                        command.ExecuteNonQuery();
                    }

                    string updateQuery = "UPDATE Chambres SET Disponibilite = 0 WHERE Id = @ChambreId";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@ChambreId", RoomId);
                        command.ExecuteNonQuery();
                    }

                    // Fetch client name and email
                    string clientName = "";
                    string clientEmail = "";
                    using (SqlCommand clientCommand = new SqlCommand("SELECT Nom, Email FROM Client WHERE Id = @ClientId", connection, transaction))
                    {
                        clientCommand.Parameters.AddWithValue("@ClientId", clientId);
                        using (SqlDataReader reader = clientCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                clientName = reader.GetString(0); // Get client name
                                clientEmail = reader.GetString(1); // Get client email
                            }
                        }
                    }

                    // Email content
                    string emailSubject = "Your Reservation Details";
                    string emailBody = $@"
                        <div style='font-family: Arial, sans-serif;'>
                            <h2 style='color: #2E86C1;'>Reservation Confirmation</h2>
                            <p>Hello {clientName},</p>
                            <p>We are pleased to confirm your reservation with the following details:</p>
                            <ul>
                                <li><strong>Start Date:</strong> {DateDebut?.ToString("dd MMMM yyyy")}</li>
                                <li><strong>End Date:</strong> {DateFin?.ToString("dd MMMM yyyy")}</li>
                                <li><strong>Room Number:</strong> {SelectedRoom.NumeroChambre}</li>
                                <li><strong>Room Type:</strong> {SelectedRoom.TypeChambre}</li>
                                <li><strong>Total Price:</strong> {(SelectedRoom.Prix * ((DateFin - DateDebut)?.Days ?? 1)).ToString("C")}</li>
                            </ul>
                            <p>Thank you for choosing our hotel. We look forward to hosting you!</p>
                            <p>Best Regards,<br>The Hotel Team</p>
                        </div>";

                    _emailService.SendEmail(clientEmail, emailSubject, emailBody);

                    transaction.Commit();
                    SuccessMessage = "Reservation confirmed successfully!";
                    return RedirectToPage("/Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ErrorMessage = $"Database error: {ex.Message}";
                    return Page();
                }
            }
        }

        private void LoadRoomDetails(int roomId)
        {
            RoomId = roomId;

            string connectionString = "Server=DESKTOP-2K5N25U\\SQLEXPRESS;Database=HotelDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT C.Id, C.NumeroChambre, T.typeChambre, T.Prix
                    FROM Chambres C
                    JOIN Type_Chambres T ON C.TypeChambreId = T.Id
                    WHERE C.Id = @RoomId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RoomId", roomId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            SelectedRoom.Id = reader.GetInt32(0);
                            SelectedRoom.NumeroChambre = reader.GetInt32(1);
                            SelectedRoom.TypeChambre = reader.GetString(2);
                            SelectedRoom.Prix = Convert.ToDecimal(reader.GetValue(3));
                        }
                        else
                        {
                            ErrorMessage = "Room not found.";
                        }
                    }
                }
            }
        }
    }
}
