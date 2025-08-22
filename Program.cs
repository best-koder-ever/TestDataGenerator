using System.Reflection;
using Bogus;
using MatchmakingService.Data;
using MatchmakingService.Models;
using Microsoft.EntityFrameworkCore;
using AuthService.Models; // Import the User model
using AuthService.Data; // Import ApplicationDbContext for AuthService
using Microsoft.AspNetCore.Identity; // Added for PasswordHasher
using System.Text;
using System.Net.Http; // Added for HttpClient
using System.Net.Http.Json; // Added for PostAsJsonAsync
using AuthService.DTOs; // Assuming RegisterDto is in this namespace
using UserService.Data; // For ApplicationDbContext
using UserService.Models; // For UserProfile

class Program
{
    private static readonly Dictionary<string, string> DbOptions = new()
    {
        { "1", "Server=127.0.0.1;Port=3307;Database=AuthServiceDb;User=authuser;Password=authuser_password;" },
        { "2", "Server=127.0.0.1;Port=3308;Database=UserServiceDb;User=userservice_user;Password=userservice_user_password;" },
        { "3", "Server=127.0.0.1;Port=3309;Database=MatchmakingServiceDb;User=matchmakingservice_user;Password=matchmakingservice_user_password;" },
        { "4", "Server=127.0.0.1;Port=3310;Database=SwipeServiceDb;User=swipeservice_user;Password=swipeservice_user_password;" }
    };
    private static string _selectedDb = "1"; // Default to AuthServiceDb
    private static string _connectionString = DbOptions[_selectedDb]; // Initialize based on default _selectedDb

    private static CreationMode _userCreationMode = CreationMode.DirectInsert; // Default
    private static string AuthApiServiceUrl = Environment.GetEnvironmentVariable("AUTH_API_URL") ?? "http://localhost:8081"; // Configurable: AuthService URL
    private static string UserServiceApiUrl = "http://localhost:8082"; // Default user-service URL

    private enum CreationMode
    {
        DirectInsert,
        ApiCall
    }

    static async Task Main(string[] args) // Changed to async Task
    {
        // --- Batch mode for automation ---
        if (args.Length > 0)
        {
            int userCount = 0;
            var explicitUsers = new List<RegisterDto>();
            bool useApi = false;
            bool useDirect = false;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--create-users":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int n))
                        {
                            userCount = n;
                            i++;
                        }
                        break;
                    case "--api":
                        useApi = true;
                        break;
                    case "--direct":
                        useDirect = true;
                        break;
                    case "--user":
                        if (i + 1 < args.Length)
                        {
                            var parts = args[i + 1].Split(':');
                            if (parts.Length >= 2)
                            {
                                explicitUsers.Add(new RegisterDto
                                {
                                    Email = parts[0],
                                    Password = parts[1],
                                    ConfirmPassword = parts[1],
                                    Username = parts[0].Split('@')[0],
                                    PhoneNumber = "1234567890"
                                });
                            }
                            i++;
                        }
                        break;
                    case "--user-service-url":
                        if (i + 1 < args.Length)
                        {
                            UserServiceApiUrl = args[i + 1];
                            i++;
                        }
                        break;
                    case "--create-fixed-testuser":
                        // Always add a fixed test user
                        explicitUsers.Add(new RegisterDto
                        {
                            Email = "testuser@example.com",
                            Password = "TestPassword123!",
                            ConfirmPassword = "TestPassword123!",
                            Username = "testuser",
                            PhoneNumber = "1234567890"
                        });
                        break;
                }
            }
            if (useApi) _userCreationMode = CreationMode.ApiCall;
            if (useDirect) _userCreationMode = CreationMode.DirectInsert;
            _selectedDb = "1"; // Always AuthServiceDb for user creation
            _connectionString = DbOptions[_selectedDb];
            // Create explicit users first
            if (explicitUsers.Count > 0)
            {
                if (_userCreationMode == CreationMode.ApiCall)
                {
                    await CreateExplicitUsersViaApiAsync(explicitUsers, seedUserService: true);
                }
                else
                {
                    CreateExplicitUsersDirectly(explicitUsers);
                }
            }
            // Then create random users if requested
            if (userCount > 0)
            {
                if (_userCreationMode == CreationMode.ApiCall)
                {
                    await CreateUsersViaApiAsync(userCount, seedUserService: true);
                }
                else
                {
                    CreateUsersDirectly(userCount);
                }
            }
            Console.WriteLine("Batch mode complete. Exiting.");
            return;
        }

        // END TEMPORARY TEST CODE

        // Show status dashboard immediately on startup
        ShowStatusOverview();

        // Original Main method loop:
        while (true)
        {
            Console.Clear();
            ShowStatusOverview();
            Console.WriteLine("========================================");
            Console.WriteLine("|         Test Data Generator          |");

            Console.WriteLine("========================================");
            Console.WriteLine("| R. Reset All Databases               |");
            Console.WriteLine("| 0. Select Target Database            |");
            Console.WriteLine("| 1. Create Users                      |");
            Console.WriteLine("| 2. Create Swipes                     |");
            Console.WriteLine("| 3. Create Mutual Matches             |");
            Console.WriteLine("| 4. Create Messages                   |");
            Console.WriteLine("| 5. Set Database Connection (Custom)  |");
            Console.WriteLine("| S. Show Status Overview              |");
            Console.WriteLine("| 7. Select User Creation Mode         |");
            Console.WriteLine("| 8. Create 2 Users via Auth API       |");
            Console.WriteLine("| 9. Create Users in Auth & User Svc   |");
            Console.WriteLine("| 6. Exit                              |");
            Console.WriteLine("========================================");
            Console.WriteLine($"Current DB: {GetDbName(_selectedDb)}");
            Console.WriteLine($"Current User Creation Mode: {_userCreationMode}");
            Console.Write("Select an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "R":
                case "r":
                    await ResetAllDatabasesMenu();
                    break;
                case "0":
                    SelectTargetDatabase();
                    break;
                case "1":
                    await CheckResetPromptAndCreateUsers();
                    break;
                case "2":
                    CreateSwipes();
                    break;
                case "3":
                    CreateMutualMatches();
                    break;
                case "4":
                    CreateMessages();
                    break;
                case "5":
                    SetDatabaseConnection();
                    break;
                case "S":
                case "s":
                    ShowStatusOverview();
                    break;
                case "6":
                    Console.WriteLine("Exiting...");
                    return;
                case "7":
                    SelectUserCreationMode();
                    break;
                case "8":
                    _selectedDb = "1";
                    _userCreationMode = CreationMode.ApiCall;
                    Console.WriteLine($"Current DB set to: {GetDbName(_selectedDb)}");
                    Console.WriteLine($"Current User Creation Mode set to: {_userCreationMode}");
                    await CheckResetPromptAndCreateUsers(2, true);
                    Console.WriteLine("Finished creating 2 users via API. Press any key to return to menu.");
                    Console.ReadKey();
                    break;
                case "9":
                    await CheckResetPromptAndCreateUsersInAuthAndUserServiceMenu();
                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to try again.");
                    Console.ReadKey();
                    break;
            }
        }
    }

    // Show status overview for AuthServiceDb and UserServiceDb
    static void ShowStatusOverview()
    {
        Console.Clear();
        Console.WriteLine("========== STATUS DASHBOARD ==========");
        var results = new List<(string Db, string Table, int? Count, string? Error)>();
        results.Add(GetDbUserCount("AuthServiceDb", DbOptions["1"], "AspNetUsers"));
        results.Add(GetDbUserCount("UserServiceDb", DbOptions["2"], "UserProfiles"));
        // Optionally add more DBs here

        Console.WriteLine("|   Database         |   Table         |   Count   |");
        Console.WriteLine("---------------------------------------------------");
        foreach (var r in results)
        {
            if (r.Error == null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"| {r.Db,-17} | {r.Table,-14} | {r.Count,8}   |");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"| {r.Db,-17} | {r.Table,-14} | ERROR    |");
                Console.WriteLine($"  [ERROR] {r.Error}");
            }
            Console.ResetColor();
        }
        Console.WriteLine("=====================================");
        Console.WriteLine("Press any key to return to menu.");
        Console.ReadKey();
    }

    static (string Db, string Table, int? Count, string? Error) GetDbUserCount(string dbName, string connectionString, string tableName)
    {
        try
        {
            using var conn = new MySqlConnector.MySqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return (dbName, tableName, count, null);
        }
        catch (Exception ex)
        {
            return (dbName, tableName, null, ex.Message);
        }
    }
    // Menu option to reset all databases
    public static async Task ResetAllDatabasesMenu()
    {
        try
        {
            Console.WriteLine("This will reset (drop and recreate/migrate) all service databases. Are you sure? (y/n)");
            var confirm = Console.ReadLine();
            if (confirm?.ToLower() == "y")
            {
                await ResetAllDatabases();
                Console.WriteLine("All databases have been reset. Press any key to return to menu.");
            }
            else
            {
                Console.WriteLine("Reset cancelled. Press any key to return to menu.");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Exception during database reset: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
        }
        Console.ReadKey();
    }

    // Actually reset all DBs (calls shell scripts or dotnet ef database update for each service)
    public static async Task ResetAllDatabases()
    {
        // You can replace these with your actual reset/migrate commands or scripts
        var resetCommands = new[]
        {
            "cd ../../auth-service && dotnet ef database drop -f && dotnet ef database update",
            "cd ../../user-service && dotnet ef database drop -f && dotnet ef database update",
            "cd ../../matchmaking-service && dotnet ef database drop -f && dotnet ef database update",
            "cd ../../swipe-service && dotnet ef database drop -f && dotnet ef database update"
        };
        foreach (var cmd in resetCommands)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("bash", $"-c \"{cmd}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                    Console.WriteLine($"[Reset] {cmd}\n{output}\n{error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to reset DB with command: {cmd}\n{ex.Message}");
            }
        }
    }

    // Prompt to reset DBs before creating users
    static async Task CheckResetPromptAndCreateUsers(int userCount = -1, bool seedUserService = false)
    {
        try
        {
            Console.Write("Do you want to reset all databases before creating users? (y/n): ");
            var input = Console.ReadLine();
            if (input?.ToLower() == "y")
            {
                await ResetAllDatabases();
            }
            if (userCount > 0)
            {
                if (_userCreationMode == CreationMode.ApiCall)
                    await CreateUsersViaApiAsync(userCount, seedUserService);
                else
                    CreateUsersDirectly(userCount);
            }
            else
            {
                await CreateUsers();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Exception during user creation: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
        }
    }

    // Prompt to reset DBs before creating users in both Auth & User Service
    static async Task CheckResetPromptAndCreateUsersInAuthAndUserServiceMenu()
    {
        try
        {
            Console.Write("Do you want to reset all databases before creating users? (y/n): ");
            var input = Console.ReadLine();
            if (input?.ToLower() == "y")
            {
                await ResetAllDatabases();
            }
            await CreateUsersInAuthAndUserServiceMenu();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Exception during user creation in both services: {ex.Message}\n{ex.StackTrace}");
            Console.ResetColor();
        }
    }

    // New menu option for creating users in both Auth and User Service
    public static async Task CreateUsersInAuthAndUserServiceMenu()
    {
        Console.Write("Enter the number of users to create in both Auth and User Service: ");
        if (int.TryParse(Console.ReadLine(), out int userCount) && userCount > 0)
        {
            await CreateUsersViaApiAsync(userCount, seedUserService: true);
            Console.WriteLine($"Created {userCount} users in both Auth and User Service. Press any key to return to menu.");
        }
        else
        {
            Console.WriteLine("Invalid number. Press any key to return to the menu.");
        }
        Console.ReadKey();
    }

    static void SelectUserCreationMode()
    {
        Console.WriteLine("Select User Creation Mode:");
        Console.WriteLine("1. Direct Database Insert");
        Console.WriteLine("2. API Call");
        Console.Write("Enter choice: ");
        string? choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                _userCreationMode = CreationMode.DirectInsert;
                Console.WriteLine("User creation mode set to Direct Database Insert.");
                break;
            case "2":
                _userCreationMode = CreationMode.ApiCall;
                Console.WriteLine("User creation mode set to API Call.");
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
        Console.WriteLine("Returning to main menu...");
        Console.ReadKey();
    }

    static string GetDbName(string dbKey)
    {
        return dbKey switch
        {
            "1" => "AuthServiceDb",
            "2" => "UserServiceDb",
            "3" => "MatchmakingServiceDb",
            "4" => "SwipeServiceDb",
            _ => "Custom/Unknown"
        };
    }

    static bool TestDatabaseConnection(string connectionString)
    {
        try
        {
            var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
            using var conn = new MySqlConnector.MySqlConnection(builder.ConnectionString);
            conn.Open();
            conn.Close();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Could not connect to database: {ex.Message}");
            return false;
        }
    }

    static void SelectTargetDatabase()
    {
        Console.WriteLine("Select the target database:");
        Console.WriteLine("1. Auth Service DB");
        Console.WriteLine("2. User Service DB");
        Console.WriteLine("3. Matchmaking Service DB");
        Console.WriteLine("4. Swipe Service DB");
        Console.Write("Enter choice: ");
        var dbChoice = Console.ReadLine();
        if (!string.IsNullOrEmpty(dbChoice) && DbOptions.TryGetValue(dbChoice, out var connStr))
        {
            _connectionString = connStr;
            _selectedDb = dbChoice;
            Console.WriteLine($"Target database set to {GetDbName(dbChoice)}!");
            // Test connection before proceeding
            if (!TestDatabaseConnection(_connectionString))
            {
                Console.WriteLine("[ERROR] Database is not available. Please start the database container or check your connection settings.");
            }
            else
            {
                Console.WriteLine("Database connection successful!");
            }
        }
        else
        {
            Console.WriteLine("Invalid choice.");
        }
        Console.WriteLine("Returning to main menu...");
        Console.ReadKey();
    }

    static async Task CreateUsers() // Changed to async Task
    {
        Console.Write("Enter the number of users to create: ");
        if (int.TryParse(Console.ReadLine(), out int userCount))
        {
            if (_userCreationMode == CreationMode.ApiCall)
            {
                await CreateUsersViaApiAsync(userCount);
            }
            else // DirectInsert
            {
                CreateUsersDirectly(userCount);
            }
            Console.WriteLine("Users creation process finished.");
        }
        else
        {
            Console.WriteLine("Invalid number. Press any key to return to the menu.");
        }
        Console.ReadKey();
    }

    static void CreateUsersDirectly(int userCount)
    {
        Console.WriteLine($"Creating {userCount} users directly in {GetDbName(_selectedDb)}...");

        try
        {
            if (_selectedDb == "1") // AuthServiceDb
            {
                var authOptions = new DbContextOptionsBuilder<AuthService.Data.ApplicationDbContext>()
                    .UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 28)))
                    .Options;
                using var authContext = new AuthService.Data.ApplicationDbContext(authOptions);

                var passwordHasher = new PasswordHasher<AuthService.Models.User>();

                var faker = new Faker<AuthService.Models.User>()
                    .RuleFor(u => u.UserName, (f, u) => f.Internet.UserName())
                    .RuleFor(u => u.NormalizedUserName, (f, u) => u.UserName?.ToUpperInvariant())
                    .RuleFor(u => u.Email, (f, u) => f.Internet.Email())
                    .RuleFor(u => u.NormalizedEmail, (f, u) => u.Email?.ToUpperInvariant())
                    .RuleFor(u => u.EmailConfirmed, f => false)
                    .RuleFor(u => u.PasswordHash, (f, u) => passwordHasher.HashPassword(u, "P@$$wOrd"))
                    .RuleFor(u => u.SecurityStamp, f => Guid.NewGuid().ToString().ToUpperInvariant())
                    .RuleFor(u => u.ConcurrencyStamp, f => Guid.NewGuid().ToString())
                    .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
                    .RuleFor(u => u.PhoneNumberConfirmed, f => false)
                    .RuleFor(u => u.TwoFactorEnabled, f => false)
                    .RuleFor(u => u.LockoutEnabled, f => true)
                    .RuleFor(u => u.LockoutEnd, f => (DateTimeOffset?)null)
                    .RuleFor(u => u.AccessFailedCount, f => 0)
                    .RuleFor(u => u.DateOfBirth, (f, u) => f.Date.Past(50, DateTime.Now.AddYears(-18)))
                    .RuleFor(u => u.Bio, (f, u) => f.Lorem.Sentence(10))
                    .RuleFor(u => u.ProfilePicture, (f, u) => $"https://i.pravatar.cc/150?u={u.Email}")
                    .RuleFor(u => u.Gender, (f, u) => f.PickRandom(new[] { "Male", "Female", "Other", "Prefer not to say" }))
                    .RuleFor(u => u.Location, (f, u) => f.Address.City())
                    .RuleFor(u => u.Interests, (f, u) => string.Join(", ", f.Lorem.Words(f.Random.Int(3, 7))))
                    .RuleFor(u => u.LastActive, (f, u) => f.Date.Recent(30));

                var users = faker.Generate(userCount);
                authContext.Users.AddRange(users);
                authContext.SaveChanges();
                Console.WriteLine($"Created {userCount} users directly in AuthServiceDb.");
            }
            else if (_selectedDb == "2") // UserServiceDb direct insert
            {
                var options = new DbContextOptionsBuilder<UserService.Data.ApplicationDbContext>()
                    .UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 28)))
                    .Options;
                using var context = new UserService.Data.ApplicationDbContext(options);
                var faker = new Faker<UserService.Models.UserProfile>()
                    .RuleFor(u => u.Name, (f, u) => f.Name.FullName())
                    .RuleFor(u => u.Bio, (f, u) => f.Lorem.Sentence(10))
                    .RuleFor(u => u.ProfilePictureUrl, (f, u) => $"https://i.pravatar.cc/150?u={f.Internet.Email()}")
                    .RuleFor(u => u.Preferences, (f, u) => string.Join(", ", f.Lorem.Words(5)))
                    .RuleFor(u => u.Email, (f, u) => f.Internet.Email())
                    .RuleFor(u => u.Gender, (f, u) => f.PickRandom(new[] { "Male", "Female", "Other" }))
                    .RuleFor(u => u.Location, (f, u) => f.Address.City())
                    .RuleFor(u => u.Interests, (f, u) => string.Join(", ", f.Lorem.Words(3)))
                    .RuleFor(u => u.DateOfBirth, (f, u) => f.Date.Past(30, DateTime.Now.AddYears(-18)))
                    .RuleFor(u => u.CreatedAt, (f, u) => DateTime.Now)
                    .RuleFor(u => u.LastActiveAt, (f, u) => f.Date.Recent(30))
                    .RuleFor(u => u.IsVerified, (f, u) => false);
                var profiles = faker.Generate(userCount);
                context.UserProfiles.AddRange(profiles);
                context.SaveChanges();
                Console.WriteLine($"Created {userCount} user profiles directly in UserServiceDb.");
            }
            else
            {
                Console.WriteLine($"User creation for {GetDbName(_selectedDb)} is not implemented for direct insert.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to create users: {ex.Message}");
        }
    }

    static async Task CreateUsersViaApiAsync(int userCount, bool seedUserService = false)
    {
        Console.WriteLine($"Creating {userCount} users via API call to AuthService ({AuthApiServiceUrl})...");
        if (_selectedDb != "1")
        {
            Console.WriteLine("API user creation is currently only configured for AuthServiceDb (selectedDb = 1).");
            Console.WriteLine("Please select AuthServiceDb (option 1) as the target database to use API creation mode.");
            return;
        }
        using var httpClient = new HttpClient();
        for (int i = 0; i < userCount; i++)
        {
            var fakerForDto = new Bogus.Faker<RegisterDto>()
                .RuleFor(dto => dto.Username, f => f.Internet.UserName())
                .RuleFor(dto => dto.Email, f => f.Internet.Email())
                .RuleFor(dto => dto.Password, f => "P@$$wOrd123!")
                .RuleFor(dto => dto.ConfirmPassword, (f, dto) => dto.Password)
                .RuleFor(dto => dto.PhoneNumber, f => f.Phone.PhoneNumber())
                .RuleFor(dto => dto.ProfilePicture, (f, dto) => $"https://i.pravatar.cc/150?u={dto.Email}");
            var registerDto = fakerForDto.Generate();
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{AuthApiServiceUrl}/api/auth/register", registerDto);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully created user: {registerDto.Username} via API.");
                    if (seedUserService)
                    {
                        var token = await LoginAndGetJwtAsync(registerDto.Email, registerDto.Password);
                        if (token != null)
                        {
                            var ok = await PostUserProfileAsync(token, registerDto.Username, "Test user bio", registerDto.ProfilePicture, "Testing, Automation");
                            if (ok)
                                Console.WriteLine($"Seeded user profile for {registerDto.Email} in user-service.");
                            else
                                Console.WriteLine($"Failed to seed user profile for {registerDto.Email} in user-service.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to login for {registerDto.Email} to seed user-service profile.");
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create user {registerDto.Username} via API. Status: {response.StatusCode}, Error: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"API request failed for user {registerDto.Username}: {ex.Message}. Ensure AuthService is running at {AuthApiServiceUrl}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while calling API for user {registerDto.Username}: {ex.Message}");
            }
        }
    }

    // --- UserService profile seeding helpers ---
    private static async Task<string?> LoginAndGetJwtAsync(string email, string password)
    {
        using var httpClient = new HttpClient();
        var loginPayload = new { email, password };
        var response = await httpClient.PostAsJsonAsync($"{AuthApiServiceUrl}/api/Auth/login", loginPayload);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return json != null && json.ContainsKey("token") ? json["token"]?.ToString() : null;
    }

    private static async Task<bool> PostUserProfileAsync(string token, string name, string bio, string profilePictureUrl, string preferences)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        var profile = new
        {
            name,
            bio,
            profilePictureUrl,
            preferences
        };
        var response = await httpClient.PostAsJsonAsync($"{UserServiceApiUrl}/api/UserProfiles", profile);
        return response.IsSuccessStatusCode;
    }

    static async Task CreateExplicitUsersViaApiAsync(List<RegisterDto> users, bool seedUserService = false)
    {
        using var httpClient = new HttpClient();
        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.ProfilePicture))
                user.ProfilePicture = $"https://i.pravatar.cc/150?u={user.Email}";
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{AuthApiServiceUrl}/api/auth/register", user);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully created user: {user.Email} via API.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create user {user.Email} via API. Status: {response.StatusCode}, Error: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while calling API for user {user.Email}: {ex.Message}");
            }
            // Always try to seed user-service profile if requested
            if (seedUserService)
            {
                var token = await LoginAndGetJwtAsync(user.Email, user.Password);
                if (token != null)
                {
                    var ok = await PostUserProfileAsync(token, user.Username, "Test user bio", user.ProfilePicture, "Testing, Automation");
                    if (ok)
                        Console.WriteLine($"Seeded user profile for {user.Email} in user-service.");
                    else
                        Console.WriteLine($"Failed to seed user profile for {user.Email} in user-service.");
                }
                else
                {
                    Console.WriteLine($"Failed to login for {user.Email} to seed user-service profile.");
                }
            }
        }
    }

    static void CreateExplicitUsersDirectly(List<RegisterDto> users)
    {
        var authOptions = new DbContextOptionsBuilder<AuthService.Data.ApplicationDbContext>()
            .UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 28)))
            .Options;
        using var authContext = new AuthService.Data.ApplicationDbContext(authOptions);
        var passwordHasher = new PasswordHasher<AuthService.Models.User>();
        foreach (var dto in users)
        {
            var user = new AuthService.Models.User
            {
                UserName = dto.Username,
                NormalizedUserName = dto.Username.ToUpperInvariant(),
                Email = dto.Email,
                NormalizedEmail = dto.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                PhoneNumber = dto.PhoneNumber,
                PhoneNumberConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString().ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                DateOfBirth = DateTime.Now.AddYears(-25),
                Bio = "Test user bio",
                ProfilePicture = $"https://i.pravatar.cc/150?u={dto.Email}",
                Gender = "Other",
                Location = "Test City",
                Interests = "Testing",
                LastActive = DateTime.Now
            };
            user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
            authContext.Users.Add(user);
            Console.WriteLine($"Created user {dto.Email} directly in DB.");
        }
        authContext.SaveChanges();
    }

    // --- STUBS for missing menu methods to fix build ---
    static void CreateSwipes() { Console.WriteLine("[STUB] CreateSwipes not implemented."); Console.ReadKey(); }
    static void CreateMutualMatches() { Console.WriteLine("[STUB] CreateMutualMatches not implemented."); Console.ReadKey(); }
    static void CreateMessages() { Console.WriteLine("[STUB] CreateMessages not implemented."); Console.ReadKey(); }
    static void SetDatabaseConnection() { Console.WriteLine("[STUB] SetDatabaseConnection not implemented."); Console.ReadKey(); }
}
