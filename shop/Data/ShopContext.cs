using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using shop.Models;

namespace shop.Data;

public class ShopContext : IdentityDbContext<User>
{
    public ShopContext(DbContextOptions<ShopContext> options) : base(options) {}

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    
}