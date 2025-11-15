using System;
using System.Collections.Generic;
using GymManagementSystem.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int TotalTrainers { get; set; }
        public int PendingAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public IEnumerable<Member> RecentMembers { get; set; } = Array.Empty<Member>();
        public IEnumerable<Appointment> RecentAppointments { get; set; } = Array.Empty<Appointment>();
    }

    public class AppointmentListViewModel
    {
        public IEnumerable<Appointment> Appointments { get; set; } = Array.Empty<Appointment>();
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class MemberListViewModel
    {
        public IEnumerable<Member> Members { get; set; } = Array.Empty<Member>();
        public string? SearchTerm { get; set; }
    }

    public class TrainerFormViewModel
    {
        public int? TrainerId { get; set; }
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        [Required]
        public string Specialization { get; set; } = string.Empty;
        public string? Bio { get; set; }
        [Range(0, double.MaxValue)]
        public decimal HourlyRate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminReportsViewModel
    {
        public IEnumerable<MonthlyRevenuePoint> MonthlyRevenue { get; set; } = Array.Empty<MonthlyRevenuePoint>();
        public IEnumerable<TopTrainerStat> TopTrainers { get; set; } = Array.Empty<TopTrainerStat>();
        public MemberReportStats MemberStats { get; set; } = new MemberReportStats();
        public IEnumerable<StatusDistributionPoint> StatusDistribution { get; set; } = Array.Empty<StatusDistributionPoint>();
    }

    public record MonthlyRevenuePoint(int Year, int Month, decimal Revenue);

    public record TopTrainerStat(string TrainerName, int Count, decimal Revenue);

    public record MemberReportStats(int TotalMembers = 0, int ActiveMembers = 0, int NewThisMonth = 0);

    public record StatusDistributionPoint(string Status, int Count);
}
