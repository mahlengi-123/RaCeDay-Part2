using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RaCeDay.Data
{
    public enum Gender
    {
        Male,
        Female,
        Other,
        PreferNotToSay
    }
    public class ApplicationUser:IdentityUser
    {
        [Required, MaxLength (50)]

        public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength (50)]

        public string LastName { get; set; } = string.Empty;

        public Gender Gender { get; set; }

        [MaxLength(260)]

        public string? ProfileimagePath { get; set; }

        public string FullName
        {
            get { return $"{FirstName} {LastName}"; }
        }



    }
}
