
using WebShoppingAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore; //import packet for db management

namespace WebShoppingAPI;

public class AppDbContext : IdentityDbContext<UserModel, RoleModel, Guid>
{
    public DbSet<ProductModel> Products { get; set; }
    public DbSet<CartModel> Carts { get; set; }
    public DbSet<CartItemModel> CartItems { get; set; }
    public DbSet<OrderProductModel> OrderProducts { get; set; }
    public DbSet<OrderModel> Orders { get; set; }
    public DbSet<CategoryModel> Categories { get; set; }
    public DbSet<DiscountModel> Discounts { get; set; }
    public DbSet<CouponModel> Coupons { get; set; }
    public DbSet<UsedCouponModel> UsedCoupons { get; set; }
    public DbSet<ReviewCommentModel> ReviewComments { get; set; }
    public DbSet<ProductCategoryModel> ProductCategories { get; set; }
    public DbSet<AddressModel> Addresses { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {

        base.OnModelCreating(builder);
        // ทำ many-to-many และ สร้าง id ด้วยการ join id เข้าด้วยกัน
        builder.Entity<ProductCategoryModel>()
        .HasKey(pc => new { pc.ProductId, pc.CategoryId });

        builder.Entity<ProductCategoryModel>()
            .HasOne(pc => pc.Product)
            .WithMany(p => p.ProductCategories)
            .HasForeignKey(pc => pc.ProductId);

        builder.Entity<ProductCategoryModel>()
            .HasOne(pc => pc.Category)
            .WithMany(c => c.ProductCategories)
            .HasForeignKey(pc => pc.CategoryId);
        //
        //ลบคำว่า AspNet ของหน้าชื่อTable ออก (ให้ง่ายต่อการอ่านtable)
        var entityTypes = builder.Model.GetEntityTypes();
        foreach (var type in entityTypes)
        {
            builder.Entity(type.ClrType).ToTable(type.GetTableName()!.Replace("AspNet", ""));
        }


        //เพิ่ม Role เข้าไปใน db ให้มี 3 Role 1.Admin 2.Sale 3.Customer  (Seed data)
        builder.Entity<RoleModel>().HasData(
        new RoleModel { Id = Guid.Parse("dfb04ae0-0184-41ce-ac5d-1bee1ade19b3"), Name = "Admin", NormalizedName = "ADMIN", Description = "for admin" },
        new RoleModel { Id = Guid.Parse("4513f626-85c3-476e-917b-595f2c97f6f6"), Name = "Sale", NormalizedName = "SALE", Description = "for sale,teacher" },
        new RoleModel { Id = Guid.Parse("d1b172ba-4d15-4505-8de0-b43588da3359"), Name = "Customer", NormalizedName = "CUSTOMER", Description = "for Customer" }

    );
        //init category 3 ประเภท (ไว้ทดสอบเล่น)
        builder.Entity<CategoryModel>().HasData(
            new CategoryModel { Id = Guid.Parse("c02885d0-8807-458c-8649-8cf3eb6ead13"), Name = "อิเล็กทรอนิกส์", NormalizedName = "ELECTRICAL", Description = "เครื่องใช้ไฟฟ้า" },
            new CategoryModel { Id = Guid.Parse("81f571d3-ccb0-4e72-be06-27a9cfc0414f"), Name = "อุปกรณ์คอมพิวเตอร์", NormalizedName = "COMPUTER", Description = "อุปกรณ์คอมพิวเตอร์" },
            new CategoryModel { Id = Guid.Parse("f8caa24d-823e-4592-ba8a-7d7bc4f610ad"), Name = "สินค้าอุปโภคบริโภค", NormalizedName = "CONSUMER", Description = "สินค้าอื่นๆ" }
        );
    }
}

