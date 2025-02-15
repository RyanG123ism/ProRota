using ProRota.Models;

namespace ProRota.Services
{
    public interface ITimeOffRequestService
    {

        IEnumerable<TimeOffRequest> GetAllTimeOffRequests();
        IEnumerable<TimeOffRequest> GetAllTimeOffRequestsBySite();
        Task<TimeOffRequest?> ValidateTimeOffRequest(int id);
        Task<ApplicationUser?> ValidateUser(string id);

    }
}
