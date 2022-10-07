using System.Text;
using AutoEqApi.Model;
using AutoEqApi.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace AutoEqApi.Controllers;

[ApiController]
[Route("results")]
public class ResultController : ControllerBase
{
    private readonly ILogger<ResultController> _logger;

    public ResultController(ILogger<ResultController> logger)
    {
        _logger = logger;
    }

    private bool IsKnownClient()
    {
        var isKnown = Request.Headers["User-Agent"].Any(x => x.StartsWith("RootlessJamesDSP"));
        if(!isKnown) 
            _logger.LogWarning($"Unknown UA: {string.Join(";", Request.Headers["User-Agent"])}");
        return isKnown;
    }
    
    [Route("{id:long}")]
    [HttpGet]
    public async Task<ActionResult<string>> Get([FromRoute] long id)
    {
        var result = AeqIndexCache.LookupId(id);
        if (result == null || !IsKnownClient())
            return NotFound();

        try
        {
            await using var stream = new FileStream(result.AsPath(), FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            
            Response.Headers.Add("X-Profile-Name", result.Name);
            Response.Headers.Add("X-Profile-Source", result.Source);
            Response.Headers.Add("X-Profile-Rank", result.Rank.ToString());
            Response.Headers.Add("X-Profile-Id", result.Id.ToString());
            
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return StatusCode(500);
        }
    }
    
    [Route("search/{query}")]
    [HttpGet]
    public ActionResult<AeqSearchResult[]> Search([FromRoute] string query)
    {
        if (!IsKnownClient())
            return NotFound();

        const int limit = 50;
        var results = AeqIndexCache.Search(query, limit, out var isPartialBool);
        Response.Headers.Add("X-Partial-Result", isPartialBool ? "1" : "0");
        Response.Headers.Add("X-Limit", limit.ToString());
        return results;
    }
}