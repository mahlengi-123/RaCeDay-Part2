
using Microsoft.EntityFrameworkCore;
using RaCeDay.Data;
using System.ComponentModel.DataAnnotations;

namespace RaceDayy.Models;

public enum EventType
{
    Run,
    Walk,
    Cycle
}

public class Event
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.DateTime)]
    [DisplayFormat(
        DataFormatString = "{0:yyyy-MM-dd HH:mm}",
        ApplyFormatInEditMode = true)]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Precision(6, 2)]
    [Range(
        0.1,
        1000,
        ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Display(Name = "Event type")]
    public EventType EventType { get; set; }

    // The organiser who owns the event
    [Required]
    public string OrganiserId { get; set; } = string.Empty;

    public ApplicationUser? Organiser { get; set; }

    // Related categories
    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    // Related enrolments
    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}
