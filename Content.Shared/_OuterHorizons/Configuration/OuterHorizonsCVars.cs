using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._OuterHorizons.Configuration;

[CVarDefs]
public sealed class OuterHorizonsCVars : CVars
{
    public static readonly CVarDef<bool>
        PostProcess = CVarDef.Create("graphics.post_process", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
