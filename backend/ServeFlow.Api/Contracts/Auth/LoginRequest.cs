
using System.ComponentModel.DataAnnotations;

namespace ServeFlow.Api.Contracts.Auth;

public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password
);
