using Bogus;
using TestDataGenerator.Shared;

namespace TestDataGenerator.Profiles
{
    public class DemoProfile
    {
        public static List<RegisterDto> GetDemoUsers()
        {
            var users = new List<RegisterDto>();
            
            // 🎭 Persona-based demo users for consistent storytelling
            users.Add(new RegisterDto
            {
                Username = "alice_demo",
                Email = "demo.alice@example.com", 
                Password = "Demo123!",
                ConfirmPassword = "Demo123!",
                PhoneNumber = "4155551001",
                ProfilePicture = "https://i.pravatar.cc/150?u=alice"
            });
            
            users.Add(new RegisterDto
            {
                Username = "bob_demo",
                Email = "demo.bob@example.com",
                Password = "Demo123!",
                ConfirmPassword = "Demo123!",
                PhoneNumber = "4155551002",
                ProfilePicture = "https://i.pravatar.cc/150?u=bob"
            });
            
            users.Add(new RegisterDto
            {
                Username = "charlie_demo",
                Email = "demo.charlie@example.com",
                Password = "Demo123!",
                ConfirmPassword = "Demo123!",
                PhoneNumber = "4155551003",
                ProfilePicture = "https://i.pravatar.cc/150?u=charlie"
            });
            
            users.Add(new RegisterDto
            {
                Username = "diana_demo",
                Email = "demo.diana@example.com",
                Password = "Demo123!",
                ConfirmPassword = "Demo123!",
                PhoneNumber = "4155551004",
                ProfilePicture = "https://i.pravatar.cc/150?u=diana"
            });
            
            users.Add(new RegisterDto
            {
                Username = "eve_demo", 
                Email = "demo.eve@example.com",
                Password = "Demo123!",
                ConfirmPassword = "Demo123!",
                PhoneNumber = "4155551005",
                ProfilePicture = "https://i.pravatar.cc/150?u=eve"
            });
            
            // Generate additional realistic users using Bogus
            var faker = new Faker<RegisterDto>()
                .RuleFor(u => u.Username, f => f.Internet.UserName().ToLower() + "_demo")
                .RuleFor(u => u.Email, f => f.Internet.Email(provider: "demo.example.com"))
                .RuleFor(u => u.Password, f => "Demo123!")
                .RuleFor(u => u.ConfirmPassword, f => "Demo123!")
                .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("415555####"))
                .RuleFor(u => u.ProfilePicture, f => $"https://i.pravatar.cc/150?u={f.Random.Guid()}");
                
            users.AddRange(faker.Generate(45)); // Total 50 users (5 personas + 45 generated)
            
            return users;
        }
        
        public static List<DemoScenario> GetDemoScenarios()
        {
            return new List<DemoScenario>
            {
                new DemoScenario
                {
                    Name = "perfect_match_journey",
                    Description = "Alice and Bob - Perfect Match Story",
                    Users = new[] { "demo.alice@example.com", "demo.bob@example.com" },
                    Actions = new[]
                    {
                        "Both users swipe right on each other",
                        "Immediate match notification",
                        "Bob sends first message: 'Hi Alice! I see we both love hiking and cooking. What's your favorite trail in the Bay Area?'",
                        "Alice responds within minutes with enthusiasm",
                        "Natural conversation develops over 10+ messages",
                        "Eventually plan to meet for coffee"
                    }
                },
                new DemoScenario
                {
                    Name = "discovery_journey", 
                    Description = "Charlie's Profile Discovery Experience",
                    Users = new[] { "demo.charlie@example.com" },
                    Actions = new[]
                    {
                        "Charlie logs in and sees curated profile stack",
                        "Swipes through 15 profiles with realistic timing",
                        "Likes 3 profiles, passes on others", 
                        "Gets 1 immediate match, 1 match later",
                        "Receives notification about new matches"
                    }
                }
            };
        }
    }
    
    public class DemoScenario
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string[] Users { get; set; } = Array.Empty<string>();
        public string[] Actions { get; set; } = Array.Empty<string>();
    }
}
