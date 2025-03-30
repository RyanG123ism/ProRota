using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProRota.Areas.Management.ViewModels;
using ProRota.Data;
using ProRota.Models;
using ProRota.Services;
using Rotativa.AspNetCore;

namespace ProRota.Areas.Management.Controllers
{
    [Authorize(Roles = "Owner, Admin, General Manager, Assistant Manager, Head Chef, Executive Chef")]
    [Area("Management")]
    public class AnalyticsController : Controller
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private readonly ISiteService _siteService;
        private readonly IRotaService _rotaService;
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISiteService siteService, INewsFeedService newsFeedService, IRotaService rotaService, IAnalyticsService analyticsService)
        {
            _context = context;
            _userManager = userManager;
            _siteService = siteService;
            _rotaService = rotaService;
            _analyticsService = analyticsService;
        }
        public async Task<IActionResult> Index(DateTime? dateInput)
        {
            //get the site
            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = await _context.Sites.FindAsync(siteId);

            //get all users in site
            var usersInSite = await _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed).ToListAsync();

            //get the current week date range
            var date = dateInput ?? DateTime.Now.Date; //defaults to todays date if no parameter was provided
            var weekEndingDate = DateTime.Parse(_rotaService.CalculateNextSundayDateToString(date));
            var weekStartingDate = weekEndingDate.AddDays(-6);

            var totalHours = await _analyticsService.GetTotalHoursBarChartValues(site, weekStartingDate, weekEndingDate);

            var viewModel = new AnalyticsDashboardViewModel
            {
                TotalHoursByDay = totalHours,
                StaffingByRole = await _analyticsService.GetRoleDistributionPieChartValues(site, usersInSite, weekStartingDate, weekEndingDate),
                WagesExpenditureByDay = await _analyticsService.GetWageDataLineGraphValues(site, totalHours, weekStartingDate, weekEndingDate),
                HeatmapData = await _analyticsService.GetShiftTimeHeatmapDataValues(site, weekStartingDate, weekEndingDate),
                WeekStartingDate = weekStartingDate,
                WeekEndingDate = weekEndingDate
            };

            if(viewModel.TotalHoursByDay.Count <= 0)
            {
                ViewBag.Alert = "Oops! there doesnt seem to be any data to display for this week, make sure you have created your rota first!";
            }

            if (TempData["Error"] != null) ViewBag.Error = TempData["Error"];

            return View(viewModel);
        }

        public async Task<IActionResult> DownloadPdf(DateTime? dateInput)
        {
            if(dateInput == DateTime.MinValue)
            {
                TempData["Error"] = "A problem occurred generating a PDF for the date range you entered. Please try another date";
                return RedirectToAction("index");
            }

            var siteId = _siteService.GetSiteIdFromSessionOrUser();
            var site = await _context.Sites.FindAsync(siteId);
            var users = await _context.ApplicationUsers.Where(u => u.SiteId == siteId && u.EmailConfirmed).ToListAsync();

            //get the current week date range
            var date = dateInput ?? DateTime.Now.Date; //defaults to todays date if no parameter was provided
            var weekEndingDate = DateTime.Parse(_rotaService.CalculateNextSundayDateToString(date));
            var weekStartingDate = weekEndingDate.AddDays(-6);

            var totalHours = await _analyticsService.GetTotalHoursBarChartValues(site, weekStartingDate, weekEndingDate);

            var viewModel = new AnalyticsDashboardViewModel
            {
                TotalHoursByDay = totalHours,
                StaffingByRole = await _analyticsService.GetRoleDistributionPieChartValues(site, users, weekStartingDate, weekEndingDate),
                WagesExpenditureByDay = await _analyticsService.GetWageDataLineGraphValues(site, totalHours, weekStartingDate, weekEndingDate),
                WeekStartingDate = weekStartingDate,
                WeekEndingDate = weekEndingDate
            };

            return new ViewAsPdf("PdfReport", viewModel)
            {
                FileName = $"RotaAnalytics_{weekEndingDate:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
            };
        }
    }
}
