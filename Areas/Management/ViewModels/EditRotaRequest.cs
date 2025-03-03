namespace ProRota.Areas.Management.ViewModels
{
    public class EditRotaRequest
    {
        public string PublishStatus { get; set; }
        public string WeekEndingDate { get; set; }
        public Dictionary<string, Dictionary<string, ShiftUpdateModel>> EditedShifts { get; set; }
    }
}
