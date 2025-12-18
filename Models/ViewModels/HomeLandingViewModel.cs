using System.Collections.Generic;
using GymManagementSystem.Models.Entities;

namespace GymManagementSystem.Models.ViewModels
{
    public class HomeLandingViewModel
    {
        public int TotalMembers { get; set; }
        public int ActiveTrainers { get; set; }
        public int CompletedAppointments { get; set; }
        public IEnumerable<Trainer> FeaturedTrainers { get; set; } = new List<Trainer>();
    }
}
