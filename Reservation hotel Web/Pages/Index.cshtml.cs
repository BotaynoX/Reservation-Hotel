using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Reservation_hotel.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // Chambre class with properties
        public class Chambre
        {
            public int Id { get; set; } // Added Id property
            public int NumeroChambre { get; set; }
            public bool Disponibilite { get; set; }
            public string TypeChambre { get; set; }
            public decimal Prix { get; set; }

            public string ImageUrl { get; set; }
        }

        // List to hold the room details
        public List<Chambre> Chambres { get; set; } = new List<Chambre>();

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        private const int PageSize = 6;

        // Filters and Search options
        public string SearchQuery { get; set; } = "";
        public string AvailabilityFilter { get; set; } = "";
        public string TypeFilter { get; set; } = "";

        // OnGet Method
        public void OnGet(int pageNumber = 1, string searchQuery = "", string availability = "", string type = "")
        {
            // Assigning filter and search values
            CurrentPage = pageNumber;
            SearchQuery = searchQuery;
            AvailabilityFilter = availability;
            TypeFilter = type;

            // Connection string
            string connectionString = "Server=DESKTOP-2K5N25U\\SQLEXPRESS;Database=HotelDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // SQL Query
                string query = @"
                            SELECT C.Id, C.NumeroChambre, C.Disponibilite, T.typeChambre, T.Prix, C.ImageUrl
                            FROM Chambres C
                            JOIN Type_Chambres T ON C.TypeChambreId = T.Id
                            WHERE 1=1";

                // Apply filters dynamically
                if (!string.IsNullOrEmpty(SearchQuery))
                    query += " AND CAST(C.NumeroChambre AS VARCHAR) LIKE '%' + @SearchQuery + '%'";
                if (!string.IsNullOrEmpty(availability))
                    query += " AND C.Disponibilite = @AvailabilityFilter";
                if (!string.IsNullOrEmpty(type))
                    query += " AND T.typeChambre = @TypeFilter";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Parameters to avoid SQL injection
                    command.Parameters.AddWithValue("@SearchQuery", SearchQuery ?? "");
                    command.Parameters.AddWithValue("@AvailabilityFilter", availability == "true" ? 1 : 0); // Fixed boolean filter
                    command.Parameters.AddWithValue("@TypeFilter", type ?? "");

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Reading data from SQL
                        while (reader.Read())
                        {
                            Chambres.Add(new Chambre
                            {
                                Id = reader.GetInt32(0), // Get Id
                                NumeroChambre = reader.GetInt32(1), // Get Room Number
                                Disponibilite = reader.GetBoolean(2), // Fixed Boolean conversion
                                TypeChambre = reader.GetString(3), // Get Room Type
                                Prix = Convert.ToDecimal(reader.GetValue(4)), // Fixed Decimal conversion
                                ImageUrl = reader.GetString(5)
                            });
                        }
                    }
                }
            }

            // Pagination Logic
            int totalItems = Chambres.Count;
            TotalPages = (int)System.Math.Ceiling(totalItems / (double)PageSize);
            Chambres = Chambres.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList();
        }
    }
}
