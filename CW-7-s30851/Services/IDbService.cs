using CW_7_s30851.Models;
using CW_7_s30851.Models.DTOs.Query;
using CW_7_s30851.Models.DTOs.Response;

namespace CW_7_s30851.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDto>> GetTripsAsync();
    public Task<IEnumerable<TripForClientGetDTO>> GetTripsForClientAsync(int clientId);
    public Task<Client> CreateClientAsync(ClientPostDTO client);
    public Task RegisterClientToTripAsync(int clientId, int tripId);
    public Task DeleteClientFromTripAsync(int clientId, int tripId);
}