using ProRota.Models;

namespace ProRota.Services
{
    public interface IRotaService
    {

        string CalculateNextSundayDateToString(Shift shift);
        string CalculateNextSundayDateToString(DateTime date);

    }
}
