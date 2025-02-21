using ProRota.Models;

namespace ProRota.Services
{
    public interface ICompanyService
    {
        public Task<int> GetCompanyIdFromSessionOrUser();
        public Task CreateSession();
    }
}
