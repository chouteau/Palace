using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace PalaceServer.Models
{
    public class ApplicationRole
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }
}
