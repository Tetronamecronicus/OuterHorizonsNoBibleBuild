using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using Robust.Shared.Prototypes;

namespace Content.Shared._OuterHorizons.ParticleSystem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParticleOnHitSpawnerComponent : Component
{
    //[DataField] public string id = "Grassa1";
    [DataField, AutoNetworkedField] public string toSpawn = "TestParticle, ParticleSpread";
}
