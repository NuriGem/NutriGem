using System;
using System.Threading;

class Program
{
    static void Main()
    {
        UserService userService = new UserService();
        DietPlanService dietPlanService = new DietPlanService();

        while (true) // Keeps the program running until explicitly exited
        {
            Console.Clear();
            Console.WriteLine("🔹 Welcome to NutriGem!");
            Console.WriteLine("1️⃣ Register as User");
            Console.WriteLine("2️⃣ Register as Admin");
            Console.WriteLine("3️⃣ Login");
            Console.WriteLine("0️⃣ Exit");
            Console.Write("Enter choice: ");
            string choice = GetNonEmptyInput("choice");

            if (choice == "0")
            {
                Console.WriteLine("👋 Exiting NutriGem. Goodbye!");
                break;
            }

            switch (choice)
            {
                case "1":
                case "2":
                    HandleRegistration(userService, dietPlanService, choice == "1" ? "User" : "Admin");
                    break;

                case "3":
                    HandleLogin(userService, dietPlanService);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Invalid choice. Please try again.");
                    Console.ResetColor();
                    Thread.Sleep(1000);
                    break;
            }
        }
    }

    private static void HandleRegistration(UserService userService, DietPlanService dietPlanService, string role)
    {
        Console.Write("👤 Full Name: ");
        string fullName = GetNonEmptyInput("Full Name");

        Console.Write("📧 Email: ");
        string email = GetValidEmail();

        Console.Write("🔒 Password (min 8 characters): ");
        string password = GetValidPassword();

        Console.Write("🎂 Age: ");
        int age = GetValidIntegerInput("Age");

        Console.Write("📏 Height (cm): ");
        decimal height = GetValidDecimalInput("Height");

        Console.Write("⚖️ Weight (kg): ");
        decimal weight = GetValidDecimalInput("Weight");

        Console.Write("🏥 Health Conditions (if any, or type 'None'): ");
        string healthConditions = Console.ReadLine()?.Trim() ?? "None";

        bool registrationSuccess = userService.RegisterUser(fullName, email, password, age, height, weight, healthConditions, role);

        if (registrationSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ {role} registered successfully! Redirecting to menu...");
            Console.ResetColor();
            Thread.Sleep(1500);

            if (role == "User")
            {
                int userId = userService.GetUserIdByEmail(email);
                if (userId != -1)
                    ShowUserMenu(dietPlanService, userService, userId);
            }
            else
            {
                ShowAdminMenu(userService, dietPlanService);
            }
        }
    }

    private static void HandleLogin(UserService userService, DietPlanService dietPlanService)
    {
        Console.Write("📧 Enter Email: ");
        string email = GetValidEmail();

        Console.Write("🔒 Enter Password: ");
        string password = GetNonEmptyInput("Password");

        var (success, role, userId) = userService.LoginUser(email, password);

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Logged in as {role}!");
            Console.ResetColor();
            Thread.Sleep(1500);

            if (role == "User")
                ShowUserMenu(dietPlanService, userService, userId);
            else
                ShowAdminMenu(userService, dietPlanService);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid email or password. Please try again.");
            Console.ResetColor();
            Thread.Sleep(1500);
        }
    }

    private static void ShowUserMenu(DietPlanService dietPlanService, UserService userService, int userId)
    {
        bool keepRunning = true;
        while (keepRunning)
        {
            Console.Clear();
            Console.WriteLine("👤 User Dashboard");
            Console.WriteLine("1️⃣ View Your Diet Plan");
            Console.WriteLine("2️⃣ Add Diet Plan");
            Console.WriteLine("3️⃣ Update Diet Plan");
            Console.WriteLine("4️⃣ Delete Diet Plan");
            Console.WriteLine("5️⃣ Log Water Intake");
            Console.WriteLine("6️⃣ View Water Intake History");
            Console.WriteLine("7️⃣ Log Weight Progress");
            Console.WriteLine("8️⃣ View Weight Progress");
            Console.WriteLine("0️⃣ Logout");

            Console.Write("Enter your choice: ");
            string action = GetNonEmptyInput("choice");

            switch (action)
            {
                case "1":
                    dietPlanService.ViewDietPlan(userId);
                    break;

                case "2":
                    AddDietPlan(dietPlanService, userId);
                    break;

                case "3":
                    UpdateDietPlan(dietPlanService, userId);
                    break;

                case "4":
                    dietPlanService.DeleteDietPlan(userId);
                    break;

                case "5":
                    LogWaterIntake(userService, userId);
                    break;

                case "6":
                    userService.ViewWaterIntake(userId);
                    break;

                case "7":
                    LogWeightProgress(userService, userId);
                    break;

                case "8":
                    userService.ViewWeightProgress(userId);
                    break;

                case "0":
                    keepRunning = false;
                    Console.WriteLine("👋 Logging out...");
                    Thread.Sleep(1000);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Invalid choice.");
                    Console.ResetColor();
                    break;
            }
            Console.WriteLine("🔄 Press Enter to continue...");
            Console.ReadLine();
        }
    }

    private static void ShowAdminMenu(UserService userService, DietPlanService dietPlanService)
    {
        bool keepRunning = true;
        while (keepRunning)
        {
            Console.Clear();
            Console.WriteLine("🛠️ Admin Dashboard");
            Console.WriteLine("1️⃣ View All Users");
            Console.WriteLine("2️⃣ Delete a User");
            Console.WriteLine("3️⃣ Promote/Demote User Role");
            Console.WriteLine("4️⃣ View All Diet Plans");
            Console.WriteLine("0️⃣ Logout");

            Console.Write("Enter your choice: ");
            string adminChoice = GetNonEmptyInput("choice");

            switch (adminChoice)
            {
                case "1":
                    userService.ViewAllUsers();
                    break;

                case "2":
                    DeleteUser(userService);
                    break;

                case "3":
                    UpdateUserRole(userService);
                    break;

                case "4":
                    dietPlanService.ViewAllDietPlans();
                    break;

                case "0":
                    keepRunning = false;
                    Console.WriteLine("👋 Logging out...");
                    Thread.Sleep(1000);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Invalid choice.");
                    Console.ResetColor();
                    break;
            }
            Console.WriteLine("🔄 Press Enter to continue...");
            Console.ReadLine();
        }
    }

    private static void AddDietPlan(DietPlanService dietPlanService, int userId)
    {
        Console.WriteLine("🍽️ Choose a Plan Type:");
        Console.WriteLine("1️⃣ Weight Loss");
        Console.WriteLine("2️⃣ Muscle Gain");
        Console.Write("Enter choice: ");
        string planType = Console.ReadLine()?.Trim() == "1" ? "Weight Loss" : "Muscle Gain";

        Console.Write("🔥 Calories: ");
        int calories = GetValidIntegerInput("Calories");

        Console.Write("💪 Proteins (g): ");
        decimal proteins = GetValidDecimalInput("Proteins");

        Console.Write("🥖 Carbs (g): ");
        decimal carbs = GetValidDecimalInput("Carbs");

        Console.Write("🧈 Fats (g): ");
        decimal fats = GetValidDecimalInput("Fats");

        dietPlanService.AddDietPlan(userId, planType, calories, proteins, carbs, fats);
    }

    private static void UpdateDietPlan(DietPlanService dietPlanService, int userId)
    {
        Console.Write("🔥 New Calories: ");
        int calories = GetValidIntegerInput("Calories");

        Console.Write("💪 New Proteins (g): ");
        decimal proteins = GetValidDecimalInput("Proteins");

        Console.Write("🥖 New Carbs (g): ");
        decimal carbs = GetValidDecimalInput("Carbs");

        Console.Write("🧈 New Fats (g): ");
        decimal fats = GetValidDecimalInput("Fats");

        dietPlanService.UpdateDietPlan(userId, calories, proteins, carbs, fats);
    }

    private static void LogWaterIntake(UserService userService, int userId)
    {
        Console.Write("💧 Enter water intake (ml): ");
        int waterIntake = GetValidIntegerInput("Water Intake");
        userService.LogWaterIntake(userId, waterIntake);
    }

    private static void LogWeightProgress(UserService userService, int userId)
    {
        Console.Write("⚖️ Enter your weight (kg): ");
        decimal weight = GetValidDecimalInput("Weight");
        userService.LogWeightProgress(userId, weight);
    }

    private static void DeleteUser(UserService userService)
    {
        Console.Write("📌 Enter User Email to delete: ");
        string email = GetValidEmail();
        userService.DeleteUser(email);
    }

    private static void UpdateUserRole(UserService userService)
    {
        Console.Write("📌 Enter User Email to change role: ");
        string email = GetValidEmail();

        Console.Write("🔄 New Role (User/Admin): ");
        string newRole = GetNonEmptyInput("Role");

        userService.UpdateUserRole(email, newRole);
    }

    // ✅ Helper methods for input validation
    private static string GetNonEmptyInput(string fieldName)
    {
        string input;
        do
        {
            input = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"❌ {fieldName} cannot be empty. Please enter again: ");
                Console.ResetColor();
            }
        } while (string.IsNullOrEmpty(input));
        return input;
    }

    private static string GetValidEmail()
    {
        string email;
        do
        {
            email = GetNonEmptyInput("Email");
            if (!email.Contains("@") || !email.Contains("."))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("❌ Invalid email format. Please enter a valid email: ");
                Console.ResetColor();
            }
        } while (!email.Contains("@") || !email.Contains("."));
        return email;
    }

    private static string GetValidPassword()
    {
        string password;
        do
        {
            password = GetNonEmptyInput("Password");
            if (password.Length < 8)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("❌ Password must be at least 8 characters. Please try again: ");
                Console.ResetColor();
            }
        } while (password.Length < 8);
        return password;
    }

    private static int GetValidIntegerInput(string fieldName)
    {
        int value;
        while (true)
        {
            string input = GetNonEmptyInput(fieldName);
            if (int.TryParse(input, out value) && value > 0)
                return value;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"❌ Invalid {fieldName}. Please enter a positive number: ");
            Console.ResetColor();
        }
    }

    private static decimal GetValidDecimalInput(string fieldName)
    {
        decimal value;
        while (true)
        {
            string input = GetNonEmptyInput(fieldName);
            if (decimal.TryParse(input, out value) && value > 0)
                return value;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"❌ Invalid {fieldName}. Please enter a positive number: ");
            Console.ResetColor();
        }
    }
}