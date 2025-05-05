using CW_7_s30851.Services;
using Microsoft.AspNetCore.Mvc;

namespace CW_7_s30851.Controllers;

[Route("api/trips")]
[ApiController]
public class TripsController:ControllerBase
{
    private readonly IDbService _dbService;

    public TripsController(IDbService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all trips
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = await _dbService.GetTripsAsync();
        return Ok(trips);
    }
}