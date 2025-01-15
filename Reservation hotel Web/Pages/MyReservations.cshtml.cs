using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Reservation_hotel.Services;
using System;
using System.Collections.Generic;

namespace Reservation_hotel.Pages
{
    public class MyReservationsModel : PageModel
    {
        public class Reservation
        {
            public int Id { get; set; }
            public int RoomNumber { get; set; }
            public string Type { get; set; }
            public decimal Price { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        private readonly EmailService _emailService;

        public MyReservationsModel(EmailService emailService)
        {
            _emailService = emailService;
        }

        public List<Reservation> Reservations { get; set; } = new List<Reservation>();
        public string ErrorMessage { get; set; } = "";
        public string SuccessMessage { get; set; } = "";

        public void OnGet()
        {
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                Response.Redirect("/Login");
                return;
            }

            string connectionString = "Server=DESKTOP-2K5N25U\\SQLEXPRESS;Database=HotelDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT R.Id, C.NumeroChambre, T.typeChambre, T.Prix, R.DateDebut, R.DateFin
                                FROM Reservation R
                                JOIN Chambres C ON R.ChambreId = C.Id
                                JOIN Type_Chambres T ON C.TypeChambreId = T.Id
                                WHERE R.ClientId = @ClientId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientId", clientId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Reservations.Add(new Reservation
                            {
                                Id = reader.GetInt32(0),
                                RoomNumber = reader.GetInt32(1),
                                Type = reader.GetString(2),
                                Price = Convert.ToDecimal(reader.GetValue(3)),
                                StartDate = reader.GetDateTime(4),
                                EndDate = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
        }

        public IActionResult OnPostCancel(int reservationId)
{
    string connectionString = "Server=DESKTOP-2K5N25U\\SQLEXPRESS;Database=HotelDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        connection.Open();
        SqlTransaction transaction = connection.BeginTransaction();

        try
        {
            // Step 1: Get room ID, client email, and client name
            int chambreId = 0;
            string clientEmail = "";
            string clientName = "";
            int roomNumber = 0;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;

            string getClientQuery = @"
                SELECT R.ChambreId, C.Email, C.Nom, CH.NumeroChambre, R.DateDebut, R.DateFin 
                FROM Reservation R 
                JOIN Client C ON R.ClientId = C.Id 
                JOIN Chambres CH ON R.ChambreId = CH.Id 
                WHERE R.Id = @ReservationId";
            using (SqlCommand getClientCommand = new SqlCommand(getClientQuery, connection, transaction))
            {
                getClientCommand.Parameters.AddWithValue("@ReservationId", reservationId);

                using (SqlDataReader reader = getClientCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        chambreId = reader.GetInt32(0);
                        clientEmail = reader.GetString(1);
                        clientName = reader.GetString(2);
                        roomNumber = reader.GetInt32(3);
                        startDate = reader.GetDateTime(4);
                        endDate = reader.GetDateTime(5);
                    }
                }
            }

            if (chambreId == 0)
            {
                transaction.Rollback();
                ErrorMessage = "Room not found for the specified reservation.";
                return RedirectToPage("/MyReservations");
            }

            // Step 2: Delete the reservation
            string deleteQuery = "DELETE FROM Reservation WHERE Id = @ReservationId";
            using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection, transaction))
            {
                deleteCommand.Parameters.AddWithValue("@ReservationId", reservationId);
                deleteCommand.ExecuteNonQuery();
            }

            // Step 3: Update room availability
            string updateQuery = "UPDATE Chambres SET Disponibilite = 1 WHERE Id = @ChambreId";
            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection, transaction))
            {
                updateCommand.Parameters.AddWithValue("@ChambreId", chambreId);
                updateCommand.ExecuteNonQuery();
            }

            // Step 4: Commit the transaction
            transaction.Commit();

            // Step 5: Send cancellation email
            string emailSubject = "Reservation Cancellation Confirmation";
            string emailBody = $@"
                <div style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #E74C3C;'>Reservation Cancellation</h2>
                    <p>Hello {clientName},</p>
                    <p>We regret to inform you that your reservation has been canceled.</p>

                    <li><strong>Details:</strong> Room : {roomNumber}</li> 
                    <li><strong>Start Date:</strong> {startDate:dd MMMM yyyy}</li>
                    <li><strong>End Date:</strong> {endDate:dd MMMM yyyy}</li>

                    <p>If you have any questions, please contact us.</p>
                    <p>Best Regards,<br>The Hotel Team</p>
                </div>";

            _emailService.SendEmail(clientEmail, emailSubject, emailBody);

            SuccessMessage = "Reservation canceled successfully!";
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    return RedirectToPage("/MyReservations");
}


    }
}
