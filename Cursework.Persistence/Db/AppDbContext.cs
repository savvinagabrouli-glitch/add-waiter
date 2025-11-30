using System;
using System.Collections.Generic;
using Cursework.Domains.Models;
using Microsoft.EntityFrameworkCore;

namespace Cursework.Persistence.Db;

public partial class AppDbContext : DbContext
{
    
    public AppDbContext()
    {

    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CallWaiter> CallWaiters { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CategoryIngredient> CategoryIngredients { get; set; }

    public virtual DbSet<DiningTable> DiningTables { get; set; }

    public virtual DbSet<Dish> Dishes { get; set; }

    public virtual DbSet<DishIngredient> DishIngredients { get; set; }

    public virtual DbSet<Guest> Guests { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CallWaiter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CallWaiter_Id");

            entity.ToTable("CallWaiter");

            entity.HasIndex(e => new { e.IsHandled, e.CreatedAt }, "IX_CallWaiter_Open");

            entity.HasIndex(e => new { e.TableId, e.IsHandled }, "IX_CallWaiter_Table_IsHandled");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.Order).WithMany(p => p.CallWaiters)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CallWaiter_OrderId");

            entity.HasOne(d => d.Table).WithMany(p => p.CallWaiters)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CallWaiter_TableId");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Category_Id");

            entity.ToTable("Category");

            entity.HasIndex(e => e.Name, "UQ_Category_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<CategoryIngredient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CategoryIngredient_Id");

            entity.ToTable("CategoryIngredient");

            entity.HasIndex(e => e.Name, "UQ_CategoryIngredient_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<DiningTable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_DiningTable_Id");

            entity.ToTable("DiningTable");

            entity.HasIndex(e => e.Name, "UQ_DiningTable_Number").IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Free");
            entity.Property(e => e.Zone)
                .HasMaxLength(50)
                .HasDefaultValue("MainHall");
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Dish_Id");

            entity.ToTable("Dish");

            entity.HasIndex(e => e.CategoryId, "IX_Dish_CategoryId");

            entity.HasIndex(e => new { e.CategoryId, e.Name }, "UQ_Dish_NameInCategory").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PhotoUrl).HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dish_CategoryId");
        });

        modelBuilder.Entity<DishIngredient>(entity =>
        {
            entity.HasKey(e => new { e.DishId, e.IngredientId });

            entity.ToTable("DishIngredient");

            entity.HasIndex(e => e.IngredientId, "IX_DishIngredient_IngredientId");

            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Unit)
                .HasMaxLength(10)
                .HasDefaultValue("g");

            entity.HasOne(d => d.Dish).WithMany(p => p.DishIngredients)
                .HasForeignKey(d => d.DishId)
                .HasConstraintName("FK_DishIngredient_DishId");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.DishIngredients)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DishIngredient_IngredientId");
        });

        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Guest_Id");

            entity.ToTable("Guest");

            entity.HasIndex(e => e.OrderId, "IX_Guest_OrderId");

            entity.HasIndex(e => new { e.OrderId, e.Index }, "UQ_Guest_Order_Index").IsUnique();

            entity.HasOne(d => d.Order).WithMany(p => p.Guests)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Guest_OrderId");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Ingredient_Id");

            entity.ToTable("Ingredient");

            entity.HasIndex(e => e.CategoryIngredientId, "IX_Ingredient_CategoryIngredientId");

            entity.HasIndex(e => e.Name, "UQ_Ingredient_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.CategoryIngredient).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.CategoryIngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ingredient_CategoryIngredient");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Orders_Id");

            entity.HasIndex(e => e.CreatedAt, "IX_Orders_CreatedAt");

            entity.HasIndex(e => new { e.TableId, e.Status }, "IX_Orders_Table_Status");

            entity.HasIndex(e => e.TableId, "UX_Orders_ActivePerTable")
                .IsUnique()
                .HasFilter("([Status] IN ('New', 'Pending', 'ReadyToPay'))");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("New");

            entity.HasOne(d => d.Table)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_TableId");

            entity.HasOne(d => d.Waiter).WithMany(p => p.Orders)
                .HasForeignKey(d => d.WaiterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_WaiterId");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderItem_Id");

            entity.ToTable("OrderItem");

            entity.HasIndex(e => e.DishId, "IX_OrderItem_DishId");

            entity.HasIndex(e => e.GuestId, "IX_OrderItem_GuestId");

            entity.HasIndex(e => e.OrderId, "IX_OrderItem_OrderId");

            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Ordered");

            entity.HasOne(d => d.Dish).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.DishId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_DishId");

            entity.HasOne(d => d.Guest).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.GuestId)
                .HasConstraintName("FK_OrderItem_GuestId");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OrderItem_OrderId");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Payment_Id");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.OrderId, "IX_Payment_OrderId");

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Method).HasMaxLength(20);
            entity.Property(e => e.PaidAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Payment_OrderId");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Staff_Id");

            entity.HasIndex(e => e.Login, "UQ_Staff_Login").IsUnique();

            entity.Property(e => e.Login).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(200);
            entity.Property(e => e.Role).HasMaxLength(10);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
