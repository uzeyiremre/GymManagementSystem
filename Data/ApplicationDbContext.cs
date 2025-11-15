using System;
using System.Collections.Generic;
using GymManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Veritabanı tabloları
        public DbSet<Gym> Gyms { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; }
        public DbSet<MembershipPlan> MembershipPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Gym - Service ilişkisi
            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Services)
                .WithOne(s => s.Gym)
                .HasForeignKey(s => s.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            // Gym - Trainer ilişkisi
            modelBuilder.Entity<Gym>()
                .HasMany(g => g.Trainers)
                .WithOne(t => t.Gym)
                .HasForeignKey(t => t.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trainer - Availability ilişkisi
            modelBuilder.Entity<Trainer>()
                .HasMany(t => t.Availabilities)
                .WithOne(a => a.Trainer)
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Trainer - Appointment ilişkisi
            modelBuilder.Entity<Trainer>()
                .HasMany(t => t.Appointments)
                .WithOne(a => a.Trainer)
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trainer - User ilişkisi
            modelBuilder.Entity<Trainer>()
                .HasOne(t => t.User)
                .WithOne(u => u.Trainer)
                .HasForeignKey<Trainer>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Member - User ilişkisi
            modelBuilder.Entity<Member>()
                .HasOne(m => m.User)
                .WithOne(u => u.Member)
                .HasForeignKey<Member>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Member - Appointment ilişkisi
            modelBuilder.Entity<Member>()
                .HasMany(m => m.Appointments)
                .WithOne(a => a.Member)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Member - MembershipPlan ilişkisi
            modelBuilder.Entity<Member>()
                .HasOne(m => m.MembershipPlan)
                .WithMany(p => p.Members)
                .HasForeignKey(m => m.MembershipPlanId)
                .OnDelete(DeleteBehavior.SetNull);

            // Appointment - Service ilişkisi
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index tanımları
            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.AppointmentDate);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.Status);

            modelBuilder.Entity<Trainer>()
                .HasIndex(t => t.Specialization);

            // Başlangıç verileri
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Spor salonu
            modelBuilder.Entity<Gym>().HasData(
                new Gym
                {
                    GymId = 1,
                    Name = "Sakarya Premium Fitness",
                    Address = "Serdivan, Sakarya",
                    Phone = "+90 264 123 45 67",
                    OpeningTime = new TimeSpan(6, 0, 0),
                    ClosingTime = new TimeSpan(23, 0, 0),
                    Description = "Modern ekipman ve profesyonel eğitmenler"
                }
            );

            // Üyelik planları
            modelBuilder.Entity<MembershipPlan>().HasData(
                new MembershipPlan { MembershipPlanId = 1, Name = "Aylık", Description = "30 günlük standart üyelik", DurationDays = 30, Price = 500m, IsActive = true },
                new MembershipPlan { MembershipPlanId = 2, Name = "3 Aylık", Description = "3 ay boyunca sınırsız giriş", DurationDays = 90, Price = 1350m, IsActive = true },
                new MembershipPlan { MembershipPlanId = 3, Name = "Yıllık", Description = "12 aylık premium üyelik", DurationDays = 365, Price = 4800m, IsActive = true }
            );

            // Hizmetler
            modelBuilder.Entity<Service>().HasData(
                new Service { ServiceId = 1, Name = "Kas Geliştirme", Description = "Profesyonel program", DurationMinutes = 60, Price = 150m, Category = "Fitness", GymId = 1 },
                new Service { ServiceId = 2, Name = "Kilo Verme", Description = "Kişiye özel program", DurationMinutes = 60, Price = 150m, Category = "Fitness", GymId = 1 },
                new Service { ServiceId = 3, Name = "Yoga", Description = "Hatha ve Vinyasa", DurationMinutes = 75, Price = 100m, Category = "Yoga", GymId = 1 },
                new Service { ServiceId = 4, Name = "Pilates", Description = "Mat ve reformer", DurationMinutes = 60, Price = 120m, Category = "Pilates", GymId = 1 }
            );

            // Antrenörler
            modelBuilder.Entity<Trainer>().HasData(
                new Trainer { TrainerId = 1, FirstName = "Ahmet", LastName = "Yılmaz", Email = "ahmet@gym.com", Phone = "+90 555 111 22 33", Specialization = "Kas Geliştirme", Bio = "15 yıl deneyim", HourlyRate = 150m, GymId = 1, IsActive = true },
                new Trainer { TrainerId = 2, FirstName = "Ayşe", LastName = "Demir", Email = "ayse@gym.com", Phone = "+90 555 222 33 44", Specialization = "Yoga", Bio = "Sertifikalı eğitmen", HourlyRate = 100m, GymId = 1, IsActive = true }
            );

            // Müsaitlikler
            var availabilities = new List<TrainerAvailability>();
            int availabilityId = 1;
            for (int trainerId = 1; trainerId <= 2; trainerId++)
            {
                for (int day = 1; day <= 5; day++)
                {
                    availabilities.Add(new TrainerAvailability
                    {
                        AvailabilityId = availabilityId++,
                        TrainerId = trainerId,
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(18, 0, 0),
                        IsAvailable = true
                    });
                }
            }
            modelBuilder.Entity<TrainerAvailability>().HasData(availabilities);
        }
    }
}
