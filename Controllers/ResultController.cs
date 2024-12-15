using System.Diagnostics;
using System.Text;
using AutoEqApi.Model;
using AutoEqApi.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;

namespace AutoEqApi.Controllers;

[ApiController]
[Route("results")]
public class ResultController(ILogger<ResultController> logger) : ControllerBase
{
    private bool IsKnownClient()
    {
        var isKnown = Request.Headers.UserAgent.Any(x => x?.StartsWith("RootlessJamesDSP") == true);
        if(!isKnown) 
            logger.LogWarning($"Unknown UA: {string.Join(";", Request.Headers.UserAgent.ToArray())}");
        return isKnown;
    }

    private void Log(string str)
    {
        var ip = Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        var country = Request.Headers["CF-IPCountry"].FirstOrDefault();
        logger.LogInformation($"GET results/{str} - {ip} ({country})");
    }
    
    [Route("{id:long}")]
    [HttpGet]
    public async Task<ActionResult<string>> Get([FromRoute] long id)
    {
        var result = AeqIndexCache.LookupId(id);
        Log($"{id} ({result?.Name}; {result?.Source})");

        if (result == null || !IsKnownClient())
            return NotFound();

        try
        {
            await using var stream = new FileStream(result.AsPath(), FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            
            Response.Headers["X-Profile-Name"] = result.Name;
            Response.Headers["X-Profile-Source"] = result.Source;
            Response.Headers["X-Profile-Rank"] = result.Rank.ToString();
            Response.Headers["X-Profile-Id"] = result.Id.ToString();
            
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return StatusCode(500);
        }
    }
    
    [Route("search/{query}")]
    [HttpGet]
    public ActionResult<AeqSearchResult[]> Search([FromRoute] string query)
    {
        Log($"search/{query}");

        if (!IsKnownClient())
            return NotFound();

        const int limit = 100;
        var results = AeqIndexCache.Search(query, limit, out var isPartialBool);
        Response.Headers["X-Partial-Result"] = isPartialBool ? "1" : "0";
        Response.Headers["X-Limit"] = limit.ToString();
        return results;
    }
}