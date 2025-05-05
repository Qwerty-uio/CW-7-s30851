using CW_7_s30851.Exception;
using CW_7_s30851.Models;
using CW_7_s30851.Models.DTOs;
using CW_7_s30851.Models.DTOs.Query;
using CW_7_s30851.Models.DTOs.Response;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace CW_7_s30851.Services;

public class DbService : IDbService
{
    private readonly string? _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    /// <summary>
    /// Get all trips
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<TripGetDto>> GetTripsAsync()
    {
        var result = new List<TripGetDto>();

        await using var connection = new SqlConnection(_connectionString);
        const string sql = "select IdTrip,Name, Description, DateFrom, DateTo, MaxPeople from Trip";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDto()
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
            });
        }

        foreach (var trip in result)
        {
            trip.Countries = await GetCountriesAsync(trip.Id);
        }

        return result;
    }

    /// <summary>
    /// Get trips associated with client
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    /// <exception cref="NotFoundException">Thrown when client doesn't exist or have no trips associated with him</exception>
    public async Task<IEnumerable<TripForClientGetDTO>> GetTripsForClientAsync(int clientId)
    {
        var result = new List<TripForClientGetDTO>();

        await using var connection = new SqlConnection(_connectionString);
        const string sql =
            "select t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, coalesce(ct.PaymentDate,0) from Trip t join Client_Trip ct on t.IdTrip = ct.IdTrip join Client c on c.IdClient = ct.IdClient where c.IdClient = @idClient";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idClient", clientId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new TripForClientGetDTO()
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.GetInt32(6),
                PaymentDate = reader.GetInt32(7)==0?null:reader.GetInt32(7)
            });
        }

        if (result.IsNullOrEmpty())
        {
            throw new NotFoundException("Client not found or have no trips associated with it.");
        }

        return result;
    }
    
    /// <summary>
    /// Creates new client
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    public async Task<Client> CreateClientAsync(ClientPostDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql =
            "insert into Client (FirstName, LastName, Email, Telephone, Pesel) values (@FirstName, @LastName, @Email, @Telephone, @Pesel); select scope_identity();";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new Client()
        {
            Id = id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }

    /// <summary>
    /// Register client to a trip
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="tripId"></param>
    /// <exception cref="NotFoundException">Thrown when there is no client or trip</exception>
    /// <exception cref="AlreadyExistException">Thrown when client already registered to a trip</exception>
    /// <exception cref="OutOfLimitException">Thrown when there is no space in trip</exception>
    public async Task RegisterClientToTripAsync(int clientId, int tripId)
    {
        if (!await CheckIfClientExistsAsync(clientId))
        {
            throw new NotFoundException($"Client doesn't exist.");
        }
        if (!await CheckIfTripExistsAsync(tripId))
        {
            throw new NotFoundException($"Trip doesn't exist.");
        }
        if (await CheckIfClientRegisteredToTripAsync(clientId, tripId))
        {
            throw new AlreadyExistException($"Client already registered for this trip.");
        }
        if (!await CheckTripCapacityAsync(tripId))
        {
            throw new OutOfLimitException("Trip is full.");
        }
        
        await using var connection = new SqlConnection(_connectionString);
        const string sql =
            "insert into Client_Trip (IdClient,IdTrip,RegisteredAt) values (@IdClient,@IdTrip,@RegisteredAt);";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", clientId);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        command.Parameters.AddWithValue("@RegisteredAt", DateTime.Now.ToString("yyyyMMdd"));
        
        await command.ExecuteNonQueryAsync();
    }
    
    /// <summary>
    /// Delete client's registration to a trip
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="tripId"></param>
    /// <exception cref="NotFoundException">Thrown when there is no registration</exception>
    public async Task DeleteClientFromTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "delete from Client_Trip where IdClient = @IdClient and IdTrip = @IdTrip";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", clientId);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        
        var numberOfRows = await command.ExecuteNonQueryAsync();

        if (numberOfRows==0)
        {
            throw new NotFoundException("Client to trip registration not found.");
        }
    }

    /// <summary>
    /// Check if client already registered to a trip
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="tripId"></param>
    /// <returns></returns>
    private async Task<bool> CheckIfClientRegisteredToTripAsync(int clientId, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        string sql = "select 1 from Client_Trip where IdClient = @IdClient and IdTrip = @IdTrip";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", clientId);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    /// <summary>
    /// Compare max people and amount of people registered to this trip
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns>True if there is free place, false otherwise</returns>
    private async Task<bool> CheckTripCapacityAsync(int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        string sql = "select count(*), t.MaxPeople from Client_Trip ct join Trip t on t.IdTrip = ct.IdTrip where t.IdTrip=@IdTrip group by t.MaxPeople";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetInt32(0)<reader.GetInt32(1);
    }
    
    /// <summary>
    /// Check if client exists
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    private async Task<bool> CheckIfClientExistsAsync(int clientId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "select IdClient from Client where IdClient = @IdClient";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdClient", clientId);
        await using var reader = await command.ExecuteReaderAsync();
        return reader.HasRows;
    }
    
    /// <summary>
    /// Check if trip exists
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    private async Task<bool> CheckIfTripExistsAsync(int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        const string sql = "select IdTrip from Trip where IdTrip = @IdTrip";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        await using var reader = await command.ExecuteReaderAsync();
        return reader.HasRows;
    }

    /// <summary>
    /// Gets countries associated with trip
    /// </summary>
    /// <param name="tripId"></param>
    /// <returns></returns>
    private async Task<IEnumerable<string>> GetCountriesAsync(int tripId)
    {
        var result = new List<string>();

        await using var connection = new SqlConnection(_connectionString);
        const string sql =
            "select Name from Country c join Country_Trip t on c.IdCountry = t.IdCountry where t.IdTrip=@Id";
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", tripId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
}