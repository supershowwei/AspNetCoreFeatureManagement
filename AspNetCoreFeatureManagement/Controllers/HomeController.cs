using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AspNetCoreFeatureManagement.Models;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace AspNetCoreFeatureManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFeatureManager featureManager;

        public HomeController(ILogger<HomeController> logger, IFeatureManager featureManager)
        {
            _logger = logger;
            this.featureManager = featureManager;
        }

        public async Task<IActionResult> Index()
        {
            var a = await this.featureManager.GetFeatureNamesAsync().ToListAsync();

            var user = this.HttpContext.Request.Query["user"];

            if (!string.IsNullOrEmpty(user))
            {
                this.HttpContext.User = new ClaimsPrincipal(
                    new[] { new ClaimsIdentity(new[] { new Claim(System.Security.Claims.ClaimTypes.Name, user) }) });

                this.ViewBag.User = user;

                var group = this.HttpContext.Request.Query["group"];

                if (!string.IsNullOrEmpty(group))
                {
                    this.HttpContext.User.Identities.First().AddClaim(new Claim(ClaimTypes.GroupName, group));

                    this.ViewBag.Group = group;
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("/feature-management-settings")]
        public IActionResult FeatureManagementSettings()
        {
            return this.File(System.IO.File.OpenRead(@"E:\my-settings.json"), "application/json");
        }
    }
}
