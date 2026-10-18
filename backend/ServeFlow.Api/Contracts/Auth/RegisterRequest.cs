
using System.ComponentModel.DataAnnotations;

namespace ServeFlow.Api.Contracts.Auth;

public record RegisterRequest(
    [Required]
    [StringLength(150, MinimumLength = 2)]
    string FullName,

    [Required]
    [EmailAddress]
    [StringLength(256)]
    string Email,

    [Required]
    [StringLength(128, MinimumLength = 8)]
    string Password
);
