using System;
using System.Collections.Generic;
using GymManagementSystem.Models.Entities;

namespace GymManagementSystem.Models.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public int TodayAppointmentsCount { get; set; }
        public int TotalClients { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int UpcomingAppointmentsCount { get; set; }
        public int PendingRequestsCount { get; set; }
        public IEnumerable<Appointment> TodayAppointments { get; set; } = Array.Empty<Appointment>();
        public IEnumerable<Appointment> UpcomingAppointments { get; set; } = Array.Empty<Appointment>();
    }
}
