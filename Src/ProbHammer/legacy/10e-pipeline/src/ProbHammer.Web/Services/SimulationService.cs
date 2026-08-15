using System.Text.Json;
using ProbHammer.Core.Contracts;
using ProbHammer.Core.Simulation;
using ProbHammer.Web.Helpers;

namespace ProbHammer.Web.Services;

public interface ISimulationService
{
    Task<IResult> RunAsync(HttpContext ctx, SimulationRequest simReq);
}

public class SimulationService : ISimulationService
{
    private readonly SimulationAdapter _adapter;

    public SimulationService(SimulationAdapter adapter) => _adapter = adapter;

    public async Task<IResult> RunAsync(HttpContext ctx, SimulationRequest simReq)
    {
        await ctx.Session.LoadAsync();
        var attackerJson = ctx.Session.GetString("attacker_army");
        var defenderJson = ctx.Session.GetString("defender_army");
        if (attackerJson is null || defenderJson is null)
            return Results.BadRequest(new { error = "No armies in session — submit armies first" });

        var attackers = JsonSerializer.Deserialize<List<UnitProfile>>(attackerJson, SessionJson.Options)!;
        var defenders = JsonSerializer.Deserialize<List<UnitProfile>>(defenderJson, SessionJson.Options)!;

        var defender = defenders.FirstOrDefault(u =>
            string.Equals(u.Name, simReq.DefenderName, StringComparison.OrdinalIgnoreCase));
        if (defender is null)
            return Results.BadRequest(new { error = $"Defender '{simReq.DefenderName}' not found in session" });

        // Validate phase constraint: all weapons must be the same type
        var weaponTypes = simReq.WeaponSelections
            .Where(w => !string.IsNullOrEmpty(w.WeaponType))
            .Select(w => w.WeaponType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (weaponTypes.Count > 1)
            return Results.BadRequest(new { error = "Cannot mix ranged and melee weapons in one simulation run" });

        var response = _adapter.Adapt(simReq, attackers, defender);
        return Results.Json(response, SessionJson.CamelCaseOptions);
    }
}