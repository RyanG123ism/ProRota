using ProRota.Models;
using System.Text.Json.Serialization;

namespace ProRota.Areas.Management.ViewModels
{
    public class ViewRotaViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<Shift> Shifts { get; set; } = new List<Shift>();
        public List<TimeOffRequest> TimeOffRequests { get; set; } = new List<TimeOffRequest>();
    }

}
