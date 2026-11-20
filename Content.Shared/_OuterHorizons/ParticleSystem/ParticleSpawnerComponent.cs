using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using Robust.Shared.Prototypes;

namespace Content.Shared._OuterHorizons.ParticleSystem;

[RegisterComponent, NetworkedComponent]
public sealed partial class ParticleSpawnerComponent : Component
{
    //[DataField] public string id = "Grassa1";
    [DataField] public string paritcle = "TestParticle";
    [DataField] public string spawnType = "ParticleSpread";
}
