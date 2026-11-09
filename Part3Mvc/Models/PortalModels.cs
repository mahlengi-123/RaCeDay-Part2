using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Part3.Mvc.Models;

public sealed class CategoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
}
public sealed class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string? BannerImageUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}
public sealed class EnrolmentDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string ParticipantName { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime EnrolledOn { get; set; }
    public string Status { get; set; } = "Pending";
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public bool HasResult { get; set; }
}
public sealed class RaceResultDto
{
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Category { get; set; } = "";
    public string FinishTime { get; set; } = "";
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}
public sealed class ProfileDto
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Gender { get; set; } = "PreferNotToSay";
    public string? ProfileImageUrl { get; set; }
}
public sealed class EventForm
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, StringLength(2000)] public string Description { get; set; } = "";
    [Required] public DateTime Date { get; set; } = DateTime.Now.AddDays(7);
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; } = 5;
    public string EventType { get; set; } = "Run";
    public IFormFile? Banner { get; set; }
}
public class AuthInput
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}
public sealed class RegisterInput : AuthInput
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
}
public sealed class AuthResult
{
    public string UserId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}
public sealed class ApiMessage { public string Message { get; set; } = ""; }

