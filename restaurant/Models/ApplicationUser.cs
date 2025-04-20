using Microsoft.AspNetCore.Identity;

namespace restaurant.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Order>? Orders { get; set; }

    }
}
