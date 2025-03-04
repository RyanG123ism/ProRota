using Microsoft.AspNetCore.Mvc;
using ProRota.Areas.Management.ViewModels;
using ProRota.Models;

namespace ProRota.Services
{
    public interface IRotaService
    {

        string CalculateNextSundayDateToString(Shift shift);
        string CalculateNextSundayDateToString(DateTime date);

        Task<Dictionary<string, Dictionary<string, List<Shift>>>> GeterateWeeklyRotaListForSiteAsync(int siteId);
        Task<Dictionary<string, ViewRotaViewModel>> GetWeeklyRota(int siteId, DateTime weekEndingDate);
        Task<bool> EditRota(Dictionary<string, Dictionary<string, ShiftUpdateModel>> editedShifts, int siteId, bool publishStatus);
        Task<bool> SaveRota(Dictionary<string, ViewRotaViewModel> model, int siteId, bool publishStatus);
        Task<bool> UnpublishRota(Dictionary<string, ViewRotaViewModel> model, int siteId);

        Task<Dictionary<string, ViewRotaViewModel>> CreateRotaViewModel(Dictionary<DateTime, Dictionary<string, List<Shift>>> rota, 
            Dictionary<string, List<TimeOffRequest>> timeOffRequests, int siteId, int siteConfigId);

    }
}
