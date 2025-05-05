using CW_7_s30851.Exception;
using CW_7_s30851.Models;
using CW_7_s30851.Models.DTOs;
using CW_7_s30851.Models.DTOs.Query;
using CW_7_s30851.Services;
using Microsoft.AspNetCore.Mvc;

namespace CW_7_s30851.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly IDbService _dbService;

    public ClientsController(IDbService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all trips associated with client
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("/api/clients/{clientId}/trips")]
    public async Task<IActionResult> GetTrips([FromRoute] int clientId)
    {
        try
        {
            var trips = await _dbService.GetTripsForClientAsync(clientId);
            return Ok(trips);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    /// <summary>
    /// Create new client
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("/api/clients")]
    public async Task<IActionResult> CreateClient([FromBody] ClientPostDTO client)
    {
        var result = await _dbService.CreateClientAsync(client);
        return Created($"{result.Id}", result);
    }

    /// <summary>
    /// Register client to a trip
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="tripId"></param>
    /// <returns></returns>
    [HttpPut]
    [Route("/api/clients/{clientId}/trips/{tripId}")]
    public async Task<IActionResult> RegisterToTrip([FromRoute] int clientId, [FromRoute] int tripId)
    {
        try
        {
            await _dbService.RegisterClientToTripAsync(clientId, tripId);
            return NoContent();
        }
        catch (AlreadyExistException e)
        {
            return BadRequest(e.Message);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    /// <summary>
    /// Delete client registration to a trip
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="tripId"></param>
    /// <returns></returns>
    [HttpDelete]
    [Route("/api/clients/{clientId}/trips/{tripId}")]
    public async Task<IActionResult> DeleteClient([FromRoute] int clientId, [FromRoute] int tripId)
    {
        try
        {
            await _dbService.DeleteClientFromTripAsync(clientId, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}