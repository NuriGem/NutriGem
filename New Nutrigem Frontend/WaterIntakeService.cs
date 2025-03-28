using Microsoft.Data.SqlClient;

namespace NutrigemApi2
{
    public class WaterIntakeService
    {
        private readonly string _connectionString;
        private readonly ILogger<WaterIntakeService> _logger;

        public WaterIntakeService(string connectionString, ILogger<WaterIntakeService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        // Log water intake for a user
        public bool LogWaterIntake(int userId, int waterIntake)
        {
            try
            {
                if (waterIntake <= 0)
                {
                    _logger.LogWarning("LogWaterIntake failed: Invalid water intake amount {Amount} for UserId: {UserId}", waterIntake, userId);
                    return false;
                }

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO dbo.WaterIntake (UserID, Amount, DateLogged) " +
                    "VALUES (@UserID, @Amount, @DateLogged)",
                    connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@Amount", waterIntake);
                command.Parameters.AddWithValue("@DateLogged", DateTime.Now);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Water intake logged successfully for UserId: {UserId}, Amount: {Amount} ml", userId, waterIntake);
                    return true;
                }

                _logger.LogWarning("Water intake logging failed: No rows affected for UserId: {UserId}", userId);
                return false;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error logging water intake for UserId: {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error logging water intake for UserId: {UserId}: {Message}", userId, ex.Message);
                return false;
            }
        }

        // View water intake history for a user
        public List<WaterIntakeEntry> ViewWaterIntake(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand(
                    "SELECT WaterIntakeID, Amount, DateLogged FROM dbo.WaterIntake WHERE UserID = @UserID ORDER BY DateLogged DESC",
                    connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using var reader = command.ExecuteReader();
                var entries = new List<WaterIntakeEntry>();
                while (reader.Read())
                {
                    entries.Add(new WaterIntakeEntry
                    {
                        WaterIntakeId = reader.GetInt32(0),
                        Amount = reader.GetInt32(1),
                        Date = reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                _logger.LogInformation("Water intake history retrieved for UserId: {UserId}, Entries: {Count}", userId, entries.Count);
                return entries;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error viewing water intake for UserId: {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error viewing water intake for UserId: {UserId}: {Message}", userId, ex.Message);
                return new List<WaterIntakeEntry>();
            }
        }

        // Delete a water intake entry by WaterIntakeID
        public bool DeleteWaterIntake(int waterIntakeId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand(
                    "DELETE FROM dbo.WaterIntake WHERE WaterIntakeID = @WaterIntakeID",
                    connection);
                command.Parameters.AddWithValue("@WaterIntakeID", waterIntakeId);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Water intake deleted successfully, WaterIntakeID: {WaterIntakeID}", waterIntakeId);
                    return true;
                }

                _logger.LogWarning("Water intake deletion failed: No rows affected for WaterIntakeID: {WaterIntakeID}", waterIntakeId);
                return false;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error deleting water intake for WaterIntakeID: {WaterIntakeID}: {Message}", waterIntakeId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting water intake for WaterIntakeID: {WaterIntakeID}: {Message}", waterIntakeId, ex.Message);
                return false;
            }
        }
    }

    public class WaterIntakeEntry
    {
        public int WaterIntakeId { get; set; }
        public int Amount { get; set; }
        public string Date { get; set; }
    }
}