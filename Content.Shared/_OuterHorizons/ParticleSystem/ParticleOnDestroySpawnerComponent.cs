using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using Robust.Shared.Prototypes;

namespace Content.Shared._OuterHorizons.ParticleSystem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParticleOnDestroySpawnerComponent : Component
{
    [DataField, AutoNetworkedField] public string toSpawn = "TestParticle, ParticleSpread";
}
