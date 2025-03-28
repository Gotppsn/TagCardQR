// Path: Controllers/HomeController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CardTagManager.Models;
using Microsoft.AspNetCore.Authorization;

namespace CardTagManager.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
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

    [AllowAnonymous]
    [Route("test-path")]
    public IActionResult TestPath()
    {
        var pathBase = HttpContext.Request.PathBase;
        var path = HttpContext.Request.Path;
        var queryString = HttpContext.Request.QueryString;
        var scheme = HttpContext.Request.Scheme;
        var host = HttpContext.Request.Host;

        return Content($"PathBase: {pathBase}, Path: {path}, QueryString: {queryString}, Scheme: {scheme}, Host: {host}");
    }
}