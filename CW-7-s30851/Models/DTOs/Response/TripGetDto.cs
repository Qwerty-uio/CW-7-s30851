namespace CW_7_s30851.Models.DTOs.Response;

public class TripGetDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public IEnumerable<string>? Countries { get; set; }
}