#region Header
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Websocket;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Text;

namespace Gauniv.WebServer.Services
{
    public class SetupService : IHostedService
    {
        private ApplicationDbContext? applicationDbContext;
        private readonly IServiceProvider serviceProvider;
        private Task? task;

        public SetupService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                applicationDbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetService<UserManager<User>>();
                var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();

                if (applicationDbContext is null || userManager is null || roleManager is null)
                {
                    throw new Exception("Required services are null");
                }

                // Create Admin role if it doesn't exist
                if (!roleManager.RoleExistsAsync("Admin").Result)
                {
                    roleManager.CreateAsync(new IdentityRole("Admin")).Wait();
                }

                // Create test user
                var testUser = userManager.FindByEmailAsync("test@test.com").Result;
                if (testUser == null)
                {
                    testUser = new User()
                    {
                        UserName = "test@test.com",
                        Email = "test@test.com",
                        EmailConfirmed = true,
                        FirstName = "Test",
                        LastName = "User"
                    };
                    userManager.CreateAsync(testUser, "password").Wait();
                }

                // Create admin user
                var adminUser = userManager.FindByEmailAsync("admin@gauniv.com").Result;
                if (adminUser == null)
                {
                    adminUser = new User()
                    {
                        UserName = "admin@gauniv.com",
                        Email = "admin@gauniv.com",
                        EmailConfirmed = true,
                        FirstName = "Admin",
                        LastName = "Gauniv"
                    };
                    userManager.CreateAsync(adminUser, "admin123").Wait();
                    userManager.AddToRoleAsync(adminUser, "Admin").Wait();
                }

                // Create sample categories
                if (!applicationDbContext.Categories.Any())
                {
                    var categories = new List<Category>
                    {
                        new Category { Name = "Action", Description = "Fast-paced games with physical challenges" },
                        new Category { Name = "Adventure", Description = "Story-driven exploration games" },
                        new Category { Name = "RPG", Description = "Role-playing games" },
                        new Category { Name = "Strategy", Description = "Games requiring planning and tactics" },
                        new Category { Name = "Simulation", Description = "Games simulating real-world activities" },
                        new Category { Name = "Puzzle", Description = "Games based on problem solving" }
                    };
                    applicationDbContext.Categories.AddRange(categories);
                    applicationDbContext.SaveChanges();
                }

                // Create sample games
                if (!applicationDbContext.Games.Any())
                {
                    var actionCategory = applicationDbContext.Categories.First(c => c.Name == "Action");
                    var adventureCategory = applicationDbContext.Categories.First(c => c.Name == "Adventure");
                    var rpgCategory = applicationDbContext.Categories.First(c => c.Name == "RPG");

                    // Create demo game files
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "games");
                    Directory.CreateDirectory(uploadsFolder);

                    // Create simple text files as demo games (in production, these would be real executables)
                    var demoFiles = new Dictionary<string, string>
                    {
                        { "epic_quest_demo.txt", "This is Epic Quest Demo Game\nPress any key to start adventure..." },
                        { "space_shooter_demo.txt", "Space Shooter Demo\nUse arrow keys to move, Space to shoot!" },
                        { "fantasy_world_demo.txt", "Fantasy World Demo\nBuild your kingdom and rule the lands!" }
                    };

                    var gamePaths = new List<string>();
                    foreach (var demo in demoFiles)
                    {
                        var filePath = Path.Combine(uploadsFolder, demo.Key);
                        if (!File.Exists(filePath))
                        {
                            File.WriteAllText(filePath, demo.Value);
                        }
                        gamePaths.Add(filePath);
                    }

                    var games = new List<Game>
                    {
                        new Game 
                        { 
                            Name = "Epic Quest", 
                            Description = "An epic adventure through mystical lands", 
                            Price = 29.99m,
                            PayloadPath = gamePaths[0],
                            Size = new FileInfo(gamePaths[0]).Length,
                            Categories = new List<Category> { adventureCategory, rpgCategory }
                        },
                        new Game 
                        { 
                            Name = "Space Shooter", 
                            Description = "Fast-paced space combat action", 
                            Price = 19.99m,
                            PayloadPath = gamePaths[1],
                            Size = new FileInfo(gamePaths[1]).Length,
                            Categories = new List<Category> { actionCategory }
                        },
                        new Game 
                        { 
                            Name = "Fantasy World", 
                            Description = "Build your own fantasy kingdom", 
                            Price = 39.99m,
                            PayloadPath = gamePaths[2],
                            Size = new FileInfo(gamePaths[2]).Length,
                            Categories = new List<Category> { rpgCategory, adventureCategory }
                        }
                    };
                    applicationDbContext.Games.AddRange(games);
                }

                applicationDbContext.SaveChanges();

                return Task.CompletedTask;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
