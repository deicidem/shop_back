using Microsoft.AspNetCore.Identity;
using shop.Models;

namespace shop.Data;

public class DataSeeder
{
    public static void Seed(IHost app)
    {
        var scopedFactory = app.Services.GetService<IServiceScopeFactory>();

        using (var scope = scopedFactory.CreateScope())
        {
            using (var db = scope.ServiceProvider.GetRequiredService<ShopContext>())
            {
                db.Database.EnsureCreated();
                
                if (!db.Roles.Any())
                {
                    db.Roles.Add(new IdentityRole("Admin"));
                    db.Roles.Add(new IdentityRole("User"));
                    db.SaveChanges();
                }

                if (!db.Products.Any())
                {
                    db.Products.Add(new Product()
                    {
                        Description = "Some description of Laptop",
                        Name = "Laptop",
                        Price = 1000,
                        Stock = 10
                    });
                    db.Products.Add(new Product()
                    {
                        Description = "Some description of Phone",
                        Name = "Phone",
                        Price = 500,
                        Stock = 8
                    });
                    db.Products.Add(new Product()
                    {
                        Description = "Some description of Headphones",
                        Name = "Headphones",
                        Price = 300,
                        Stock = 20
                    });
                    db.SaveChanges();
                }
            }
        }
    }

}

// using Microsoft.AspNetCore.Identity;
// using shop.Models;
//
// namespace shop.Data;
//
// public class DataSeeder
// {
//     public static void Seed(IHost app)
//     {
//         var scopedFactory = app.Services.GetService<IServiceScopeFactory>();
//         var scope = scopedFactory.CreateScope();
//         var db = scope.ServiceProvider.GetRequiredService<ShopContext>();
//         
//         var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//         var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
//
//         var roles = new[] { "Admin", "User" };
//         
//         db.Database.EnsureCreated();
//
//         foreach (var role in roles)
//         {
//             var roleExist = roleManager.RoleExistsAsync(role).Result;
//             if (!roleExist)
//             {
//                 roleManager.CreateAsync(new IdentityRole(role)).Wait();
//             }
//         }
//
//         var user = userManager.FindByEmailAsync("admin@example.com").Result;
//         if (user == null)
//         {
//             var newUser = new User
//             {
//                 UserName = "admin@example.com",
//                 Email = "admin@example.com"
//             };
//             userManager.CreateAsync(newUser, "password").Wait();
//             userManager.AddToRoleAsync(newUser, "Admin").Wait();
//         }
//
//
//         if (db.Products.Any()) return;
//         
//         db.Products.Add(new Product()
//         {
//             Description = "Some description of Laptop",
//             Name = "Laptop",
//             Price = 1000,
//             Stock = 10
//         });
//         db.Products.Add(new Product()
//         {
//             Description = "Some description of Phone",
//             Name = "Phone",
//             Price = 500,
//             Stock = 8
//         });
//         db.Products.Add(new Product()
//         {
//             Description = "Some description of Headphones",
//             Name = "Headphones",
//             Price = 300,
//             Stock = 20
//         });
//         db.SaveChanges();
//     }
//
// }