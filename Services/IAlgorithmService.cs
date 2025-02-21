using Microsoft.AspNetCore.Mvc;
using ProRota.Areas.Management.ViewModels;
using ProRota.Models;

namespace ProRota.Services
{
    public interface IAlgorithmService
    {
        Task<Dictionary<DateTime, Dictionary<string, List<Shift>>>> CreateWeeklyRota(CreateWeeklyRotaViewModel viewModel, Site site);
        Task<Dictionary<string, List<TimeOffRequest>>> MapTimeOffRequests(DateTime weekEndingDate, int siteId);
    }
}
