using Microsoft.AspNetCore.Mvc;
using ProRota.Areas.Management.ViewModels;
using ProRota.Models;

namespace ProRota.Services
{
    public interface IAlgorithmService
    {
        Task<int> CreateWeeklyRota(CreateWeeklyRotaViewModel viewModel, Site site);
    }
}
