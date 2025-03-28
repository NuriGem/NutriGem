using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NutrigemApi2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Get connection string and validate
var connectionString = builder.Configuration.GetConnectionString("NutrigemDb");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'NutrigemDb' not found in appsettings.json");
}

// Register services with logger and connection string
builder.Services.AddSingleton<UserService>(provider =>
    new UserService(connectionString, provider.GetRequiredService<ILogger<UserService>>()));
builder.Services.AddSingleton<DietPlanService>(provider =>
    new DietPlanService(connectionString, provider.GetRequiredService<ILogger<DietPlanService>>()));
builder.Services.AddSingleton<WaterIntakeService>(provider =>
    new WaterIntakeService(connectionString, provider.GetRequiredService<ILogger<WaterIntakeService>>()));

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Set the port to 8080
builder.WebHost.UseUrls("http://localhost:8080");

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");
app.UseStaticFiles(); // Serve static files from wwwroot
app.UseRouting();
app.UseEndpoints(endpoints => endpoints.MapControllers());

// Redirect root to index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// Health check endpoint
app.MapGet("/api/health", (ILogger<Program> logger) =>
{
    logger.LogInformation("Health check requested");
    return Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
});

// Register endpoint
app.MapPost("/api/user/register", async (HttpContext context, UserService userService, ILogger<Program> logger) =>
{
    UserData? data = null;
    try
    {
        data = await context.Request.ReadFromJsonAsync<UserData>();
        if (data == null || string.IsNullOrEmpty(data.FullName) || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Password))
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            logger.LogError("Invalid data received in /api/user/register: {Body}", body);
            return Results.BadRequest(new { message = "Invalid or missing data" });
        }

        logger.LogInformation("Register attempt for email: {Email}", data.Email);
        var (success, userId) = userService.RegisterUser(
            data.FullName, data.Email, data.Password, data.Age,
            data.Height, data.Weight, data.HealthConditions, data.Role
        );
        if (!success)
        {
            logger.LogWarning("Registration failed for email: {Email}, possibly due to duplicate email", data.Email);
            return Results.BadRequest(new { message = "Registration failed, email may already exist" });
        }

        logger.LogInformation("User registered successfully, UserId: {UserId}, Email: {Email}", userId, data.Email);
        return Results.Ok(new { message = "User registered successfully", userId });
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/user/register for email {Email}: {Message}", data?.Email ?? "unknown", ex.Message);
        return Results.Json(new { message = "Database error during registration" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/user/register for email {Email}: {Message}", data?.Email ?? "unknown", ex.Message);
        return Results.Json(new { message = $"Registration failed: {ex.Message}" }, statusCode: 500);
    }
});

// Login endpoint
app.MapPost("/api/user/login", async (HttpContext context, UserService userService, ILogger<Program> logger) =>
{
    LoginData? data = null;
    try
    {
        logger.LogInformation("Received login request");
        data = await context.Request.ReadFromJsonAsync<LoginData>();
        if (data == null || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Password))
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            logger.LogError("Invalid data received in /api/user/login: {Body}", body);
            return Results.BadRequest(new { message = "Invalid or missing data" });
        }

        logger.LogInformation("Login attempt for email: {Email}", data.Email);
        var (success, role, userId) = userService.LoginUser(data.Email, data.Password);
        if (success && role != null)
        {
            var user = userService.GetUserById(userId);
            if (user == null)
            {
                logger.LogWarning("User data not found after successful login for UserId: {UserId}", userId);
                return Results.NotFound(new { message = "User data not found after login" });
            }

            logger.LogInformation("User logged in successfully: {Email}, UserId: {UserId}, Role: {Role}", data.Email, userId, role);
            return Results.Ok(new { fullName = user.FullName, role = role, userId = userId });
        }

        logger.LogWarning("Login failed for email: {Email}", data.Email);
        return Results.Json(new { message = "Invalid email or password" }, statusCode: 401);
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/user/login for email {Email}: {Message}", data?.Email ?? "unknown", ex.Message);
        return Results.Json(new { message = "Database error occurred during login" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/user/login for email {Email}: {Message}", data?.Email ?? "unknown", ex.Message);
        return Results.Json(new { message = "An unexpected error occurred during login" }, statusCode: 500);
    }
});

// Get user data
app.MapGet("/api/user/{userId}", (int userId, UserService userService, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Fetching user data for UserId: {UserId}", userId);
        var user = userService.GetUserById(userId);
        if (user != null)
        {
            logger.LogInformation("Retrieved user data for UserId: {UserId}", userId);
            return Results.Ok(new { fullName = user.FullName, height = user.Height, weight = user.Weight, bmi = user.BMI });
        }
        logger.LogWarning("User not found for UserId: {UserId}", userId);
        return Results.NotFound(new { message = "User not found" });
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/user/{userId}: {Message}", userId, ex.Message);
        return Results.Json(new { message = "Database error fetching user data" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/user/{userId}: {Message}", userId, ex.Message);
        return Results.Json(new { message = $"Failed to fetch user data: {ex.Message}" }, statusCode: 500);
    }
});

// Get all users
app.MapGet("/api/users", (UserService userService, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Fetching all users for admin dashboard");
        var users = userService.GetAllUsers();
        logger.LogInformation("Retrieved {Count} users for admin dashboard", users.Count);
        return Results.Ok(users);
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error fetching all users: {Message}", ex.Message);
        return Results.Json(new { message = "Database error fetching users" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error fetching all users: {Message}", ex.Message);
        return Results.Json(new { message = $"Failed to fetch users: {ex.Message}" }, statusCode: 500);
    }
});

// Update height and weight
app.MapPost("/api/user/update-height-weight", async (HttpContext context, UserService userService, ILogger<Program> logger) =>
{
    UpdateHeightWeightRequest? data = null;
    try
    {
        data = await context.Request.ReadFromJsonAsync<UpdateHeightWeightRequest>();
        if (data == null || data.Height <= 0 || data.Weight <= 0)
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            logger.LogError("Invalid data received in /api/user/update-height-weight: {Body}", body);
            return Results.BadRequest(new { message = "Invalid or missing data" });
        }

        logger.LogInformation("Updating height and weight for UserId: {UserId}", data.UserId);
        bool success = userService.UpdateHeightWeight(data.UserId, data.Height, data.Weight);
        if (success)
        {
            logger.LogInformation("Height and weight updated for UserId: {UserId}", data.UserId);
            return Results.Ok(new { message = "Height and weight updated successfully" });
        }

        logger.LogWarning("Failed to update height and weight for UserId: {UserId}", data.UserId);
        return Results.NotFound(new { message = "User not found or update failed" });
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/user/update-height-weight for UserId {UserId}: {Message}", data?.UserId ?? 0, ex.Message);
        return Results.Json(new { message = "Database error during update" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/user/update-height-weight for UserId {UserId}: {Message}", data?.UserId ?? 0, ex.Message);
        return Results.Json(new { message = $"Update failed: {ex.Message}" }, statusCode: 500);
    }
});

// DietPlan endpoints
app.MapPost("/api/dietplan", async (HttpContext context, DietPlanService dietPlanService, ILogger<Program> logger) =>
{
    AddDietPlanRequest? data = null;
    try
    {
        data = await context.Request.ReadFromJsonAsync<AddDietPlanRequest>();
        if (data == null)
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            logger.LogError("Invalid data received in /api/dietplan: {Body}", body);
            return Results.BadRequest(new { message = "Invalid data" });
        }
        logger.LogInformation("Adding diet plan for UserId: {UserId}, PlanType: {PlanType}", data.UserId, data.PlanType);
        bool success = dietPlanService.AddDietPlan(data.UserId, data.PlanType);
        if (success)
        {
            logger.LogInformation("Diet plan added for UserId: {UserId}", data.UserId);
            return Results.Ok(new { message = "Diet plan added successfully" });
        }
        logger.LogWarning("Failed to add diet plan for UserId: {UserId}", data.UserId);
        return Results.Json(new { message = "Failed to add diet plan" }, statusCode: 500);
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/dietplan for UserId {UserId}: {Message}", data?.UserId ?? 0, ex.Message);
        return Results.Json(new { message = "Database error adding diet plan" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/dietplan for UserId {UserId}: {Message}", data?.UserId ?? 0, ex.Message);
        return Results.Json(new { message = $"Failed to add diet plan: {ex.Message}" }, statusCode: 500);
    }
});

app.MapGet("/api/dietplan/{userId}", (int userId, DietPlanService dietPlanService, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Fetching diet plan for UserId: {UserId}", userId);
        var dietPlan = dietPlanService.ViewDietPlan(userId);
        if (dietPlan != null)
        {
            logger.LogInformation("Retrieved diet plan for UserId: {UserId}", userId);
            return Results.Ok(dietPlan);
        }
        logger.LogWarning("Diet plan not found for UserId: {UserId}", userId);
        return Results.NotFound(new { message = "Diet plan not found" });
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/dietplan/{userId}: {Message}", userId, ex.Message);
        return Results.Json(new { message = "Database error fetching diet plan" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/dietplan/{userId}: {Message}", userId, ex.Message);
        return Results.Json(new { message = $"Failed to fetch diet plan: {ex.Message}" }, statusCode: 500);
    }
});

// WaterIntake endpoints
app.MapPost("/api/waterintake", async (HttpContext context, WaterIntakeService waterIntakeService, ILogger<Program> logger) =>
{
    WaterIntakeRequest? data = null;
    try
    {
        data = await context.Request.ReadFromJsonAsync<WaterIntakeRequest>();
        if (data == null || data.WaterIntake <= 0)
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            logger.LogError("Invalid data received in /api/waterintake: {Body}", body);
            return Results.BadRequest(new { message = "Invalid or missing data" });
        }
        logger.LogInformation("Logging water intake for UserId: {UserId}, Amount: {WaterIntake} ml", data.UserId, data.WaterIntake);
        bool success = waterIntakeService.LogWaterIntake(data.UserId, data.WaterIntake);
        if (success)
        {
            logger.LogInformation("Water intake logged for UserId: {UserId}, Amount: {WaterIntake} ml", data.UserId, data.WaterIntake);
            return Results.Ok(new { message = "Water intake logged successfully" });
        }
        logger.LogWarning("Failed to log water intake for UserId: {UserId}", data.UserId);
        return Results.Json(new { message = "Failed to log water intake" }, statusCode: 500);
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/waterintake for UserId {UserId}: {Message}", data?.UserId ?? 0, ex.Message);
        return Results.Json(new { message = "Database error logging water intake" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/waterintake for UserId {UserId}: {Message}", data?.UserId ?? 0, ex.Message);
        return Results.Json(new { message = $"Failed to log water intake: {ex.Message}" }, statusCode: 500);
    }
});

app.MapGet("/api/waterintake/{userId}", (int userId, WaterIntakeService waterIntakeService, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Fetching water intake history for UserId: {UserId}", userId);
        var waterIntake = waterIntakeService.ViewWaterIntake(userId);
        if (waterIntake != null)
        {
            logger.LogInformation("Retrieved water intake history for UserId: {UserId}", userId);
            return Results.Ok(waterIntake);
        }
        logger.LogWarning("Water intake history not found for UserId: {UserId}", userId);
        return Results.NotFound(new { message = "Water intake history not found" });
    }
    catch (SqlException ex)
    {
        logger.LogError(ex, "Database error in /api/waterintake/{userId}: {Message}", userId, ex.Message);
        return Results.Json(new { message = "Database error fetching water intake" }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in /api/waterintake/{userId}: {Message}", userId, ex.Message);
        return Results.Json(new { message = $"Failed to fetch water intake history: {ex.Message}" }, statusCode: 500);
    }
});

// Run the application
app.Run();

// Record types for request data
public record UserData
{
    public int UserId { get; init; }
    public string FullName { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
    public int Age { get; init; }
    public decimal Height { get; init; }
    public decimal Weight { get; init; }
    public string? HealthConditions { get; init; }
    public string Role { get; init; }
    public float? BMI { get; init; }

    public UserData(string fullName, string email, string password, int age, decimal height, decimal weight, string? healthConditions, string role, float? bmi = null)
    {
        FullName = fullName;
        Email = email;
        Password = password;
        Age = age;
        Height = height;
        Weight = weight;
        HealthConditions = healthConditions;
        Role = role;
        BMI = bmi;
    }
}

public record LoginData(string Email, string Password);
public record AddDietPlanRequest(int UserId, string PlanType);
public record WaterIntakeRequest(int UserId, int WaterIntake);
public record UpdateHeightWeightRequest(int UserId, decimal Height, decimal Weight);