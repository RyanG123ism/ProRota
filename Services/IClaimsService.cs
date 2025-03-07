namespace ProRota.Services
{
    public interface IClaimsService
    {
        string GetUserId();
        int GetCompanyId();
        int GetSiteId();
        Task<bool> SetCompanyId(string userId, int companyId);
        Task<bool> SetSiteId();
        Task<bool> SetSiteId(string userId);
    }
}
