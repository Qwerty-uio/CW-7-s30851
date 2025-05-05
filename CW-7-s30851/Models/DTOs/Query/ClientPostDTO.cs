using System.ComponentModel.DataAnnotations;

namespace CW_7_s30851.Models.DTOs.Query;

public class ClientPostDTO
{
    [Length(1,120)]
    [Required(ErrorMessage = "Client first name is required")]
    public string? FirstName { get; set; }
    [Length(1,120)]
    [Required(ErrorMessage = "Client last name is required")]
    public string? LastName { get; set; }
    [Length(1,120)]
    [EmailAddress]
    [Required(ErrorMessage = "Client email is required")]
    public string? Email { get; set; }
    [Length(1,20)]
    [Required(ErrorMessage = "Client phone number is required")]
    [Phone]
    public string? Telephone { get; set; }
    [Length(11,11)]
    [RegularExpression("[0-9]*")]
    [Required(ErrorMessage = "Client pesel is required")]
    public string? Pesel { get; set; }
}