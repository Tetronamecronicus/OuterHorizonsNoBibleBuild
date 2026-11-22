using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._OuterHorizons.ParticleSystem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParticlePeriodicSpawnerComponent : Component
{
    //[DataField] public string id = "Grassa1";
    [DataField, AutoNetworkedField] public float every = 1;
    public TimeSpan nextSpawn = TimeSpan.FromSeconds(0);
    [DataField, AutoNetworkedField] public string toSpawn = "TestParticle, ParticleSpread";
}
