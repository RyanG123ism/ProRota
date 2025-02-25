using ProRota.Models;

namespace ProRota.Services
{
    public interface ITimeOffRequestService
    {
        Task<IEnumerable<TimeOffRequest>> GetAllTimeOffRequestsBySite();
        Task<TimeOffRequest?> ValidateTimeOffRequest(int id);
        Task<ApplicationUser?> ValidateUser(string id);

    }
}
