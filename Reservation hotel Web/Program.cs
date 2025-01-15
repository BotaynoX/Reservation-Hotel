var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession(); // Add session service
builder.Services.AddHttpContextAccessor(); // Move it here!

// Register EmailService
//builder.Services.AddScoped<Reservation_hotel.Services.EmailService>(); // <-- This should be here
builder.Services.AddScoped<Reservation_hotel.Services.EmailService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// Enable session middleware
app.UseSession();

app.MapRazorPages();

app.Run();
