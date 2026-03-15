using Microsoft.EntityFrameworkCore;

namespace TelegramNutritionMockBot.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<MealEntity> Meals => Set<MealEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.TelegramChatId).HasColumnName("telegram_chat_id");
            e.Property(x => x.Goal).HasColumnName("goal");
            e.Property(x => x.Gender).HasColumnName("gender");
            e.Property(x => x.Age).HasColumnName("age");
            e.Property(x => x.Height).HasColumnName("height");
            e.Property(x => x.Weight).HasColumnName("weight");
            e.Property(x => x.Activity).HasColumnName("activity");
            e.Property(x => x.DailyCalories).HasColumnName("daily_calories");
            e.Property(x => x.ProteinTarget).HasColumnName("protein_target");
            e.Property(x => x.FatTarget).HasColumnName("fat_target");
            e.Property(x => x.CarbTarget).HasColumnName("carb_target");
            e.Property(x => x.StreakDays).HasColumnName("streak_days");
            e.HasIndex(x => x.TelegramChatId).IsUnique();
        });

        modelBuilder.Entity<MealEntity>(e =>
        {
            e.ToTable("meals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.MealName).HasColumnName("meal_name");
            e.Property(x => x.Calories).HasColumnName("calories");
            e.Property(x => x.Protein).HasColumnName("protein");
            e.Property(x => x.Fat).HasColumnName("fat");
            e.Property(x => x.Carbs).HasColumnName("carbs");
            e.Property(x => x.Photo).HasColumnName("photo");
            e.HasOne(x => x.User).WithMany(u => u.Meals).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
