using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using StudentHub.Services;

namespace StudentHub.Components;

public abstract class CampusPage : ComponentBase
{
    [Inject] protected CampusService Campus { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? Authentication { get; set; }
    protected Guid UserId { get; private set; }
    protected ProfileInfo? Current => Campus.GetProfile(UserId);

    protected override async Task OnInitializedAsync()
    {
        if (Authentication is not null)
        {
            var state = await Authentication;
            Guid.TryParse(state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id);
            UserId = id;
        }
    }
}
