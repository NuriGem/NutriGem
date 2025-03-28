using Microsoft.Data.SqlClient;

namespace NutrigemApi2
{
    public class DietPlanService
    {
        private readonly string _connectionString;
        private readonly ILogger<DietPlanService> _logger;

        public DietPlanService(string connectionString, ILogger<DietPlanService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        // Add a diet plan for a user
        public bool AddDietPlan(int userId, string planType)
        {
            try
            {
                if (string.IsNullOrEmpty(planType))
                {
                    _logger.LogWarning("AddDietPlan failed: PlanType is empty for UserId: {UserId}", userId);
                    return false;
                }

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO dbo.DietPlans (UserID, PlanType, MealID, CaloriesPerDay, ProteinsPerDay, CarbsPerDay, FatsPerDay) " +
                    "VALUES (@UserID, @PlanType, @MealID, @CaloriesPerDay, @ProteinsPerDay, @CarbsPerDay, @FatsPerDay)",
                    connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@PlanType", planType);
                command.Parameters.AddWithValue("@MealID", 1); // Default to MealID 1 (Breakfast); can be dynamic
                command.Parameters.AddWithValue("@CaloriesPerDay", 1800);
                command.Parameters.AddWithValue("@ProteinsPerDay", 90.0f);
                command.Parameters.AddWithValue("@CarbsPerDay", 200.0f);
                command.Parameters.AddWithValue("@FatsPerDay", 50.0f);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Diet plan added successfully for UserId: {UserId}", userId);
                    return true;
                }

                _logger.LogWarning("Diet plan insertion failed: No rows affected for UserId: {UserId}", userId);
                return false;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error adding diet plan for UserId: {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding diet plan for UserId: {UserId}: {Message}", userId, ex.Message);
                return false;
            }
        }

        // View a user's diet plan with meal details
        public object? ViewDietPlan(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand(
                    "SELECT dp.DietPlanID, dp.UserID, dp.PlanType, dp.MealID, dp.CaloriesPerDay, dp.ProteinsPerDay, dp.CarbsPerDay, dp.FatsPerDay, " +
                    "m.MealName, m.FoodItems " +
                    "FROM dbo.DietPlans dp " +
                    "LEFT JOIN dbo.Meals m ON dp.MealID = m.MealID " +
                    "WHERE dp.UserID = @UserID",
                    connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var dietPlan = new
                    {
                        DietPlanId = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        PlanType = reader.GetString(2),
                        MealId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                        CaloriesPerDay = reader.GetInt32(4),
                        ProteinsPerDay = reader.GetFloat(5),
                        CarbsPerDay = reader.GetFloat(6),
                        FatsPerDay = reader.GetFloat(7),
                        MealName = reader.IsDBNull(8) ? null : reader.GetString(8),
                        FoodItems = reader.IsDBNull(9) ? null : reader.GetString(9)
                    };
                    _logger.LogInformation("Diet plan retrieved successfully for UserId: {UserId}", userId);
                    return dietPlan;
                }

                _logger.LogWarning("No diet plan found for UserId: {UserId}", userId);
                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error viewing diet plan for UserId: {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error viewing diet plan for UserId: {UserId}: {Message}", userId, ex.Message);
                return null;
            }
        }

        // Delete a diet plan by DietPlanID
        public bool DeleteDietPlan(int dietPlanId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand(
                    "DELETE FROM dbo.DietPlans WHERE DietPlanID = @DietPlanID",
                    connection);
                command.Parameters.AddWithValue("@DietPlanID", dietPlanId);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Diet plan deleted successfully, DietPlanID: {DietPlanID}", dietPlanId);
                    return true;
                }

                _logger.LogWarning("Diet plan deletion failed: No rows affected for DietPlanID: {DietPlanID}", dietPlanId);
                return false;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error deleting diet plan for DietPlanID: {DietPlanID}: {Message}", dietPlanId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting diet plan for DietPlanID: {DietPlanID}: {Message}", dietPlanId, ex.Message);
                return false;
            }
        }
    }
}