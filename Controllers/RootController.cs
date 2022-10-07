using System.Text;
using AutoEqApi.Model;
using AutoEqApi.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace AutoEqApi.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("/")]
public class RootController : ControllerBase
{
    private readonly ILogger<RootController> _logger;

    public RootController(ILogger<RootController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    public ActionResult<string> Get()
    {
        return Redirect("https://github.com/ThePBone/AutoEqApi");
    }
}