using ProRota.Areas.Management.ViewModels;
using ProRota.Models;

namespace ProRota.Services
{
    public interface IAlgorithmService
    {
        void CreateWeeklyRota(CreateWeeklyRotaViewModel viewModel, Site site);
    }
}
