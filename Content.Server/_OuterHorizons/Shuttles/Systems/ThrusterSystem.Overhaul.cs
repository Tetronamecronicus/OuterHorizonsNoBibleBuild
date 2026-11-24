using Content.Server.Shuttles.Components;
using Content.Shared.Camera;
using Content.Shared.Gravity;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ThrusterSystem
{
    // Тряска камеры. Сила толчка = sqrt(ускорение грида) * ShakeScale, не больше ShakeMax.
    private const float ShakeInterval = 0.05f;
    private const float ShakeScale = 0.06f;
    private const float ShakeMax = 0.2f;

    // Звук тряски: зацикленный, пока игрок трясётся. Громкость (дБ) растёт вместе с силой тряски.
    // После окончания тряски плавно затухает за ShakeFadeTime секунд до ShakeFadeFloor дБ.
    // TODO: заменить на свой звук.
    private static readonly SoundSpecifier ShakeSound = new SoundPathSpecifier("/Audio/_OuterHorizons/Effects/hullrattle.ogg");
    private const float ShakeSoundVolumeMin = -20f;
    private const float ShakeSoundVolumeMax = 20f;
    private const float ShakeFadeTime = 4f;
    private const float ShakeFadeFloor = -40f;

    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private IRobustRandom _random = default!;

    private sealed class ShakeStream(EntityUid audio, float volume)
    {
        public EntityUid Audio = audio;
        public float Volume = volume;
        public float? FadeElapsed;
    }

    private readonly Dictionary<EntityUid, float> _shakeThrust = new();
    private readonly Dictionary<EntityUid, ShakeStream> _shakeAudio = new();
    private readonly HashSet<EntityUid> _shakeActive = new();
    private readonly List<EntityUid> _shakeStale = new();
    private float _shakeTimer;

    /// <summary>
    /// Каждый тик ведёт затухание звуков, раз в <see cref="ShakeInterval"/> трясёт камеру и ведёт звук игрокам
    /// на гридах с работающими двигателями. Учитываются все двигатели с Firing, без исключений.
    /// </summary>
    private void UpdateOverhaul(float frameTime)
    {
        UpdateShakeFade(frameTime);

        _shakeTimer += frameTime;
        if (_shakeTimer < ShakeInterval)
            return;

        _shakeTimer = 0f;
        _shakeThrust.Clear();
        _shakeActive.Clear();

        var query = EntityQueryEnumerator<ThrusterComponent, TransformComponent>();
        while (query.MoveNext(out var comp, out var xform))
        {
            if (comp.Firing && xform.GridUid is { } grid)
                _shakeThrust[grid] = _shakeThrust.GetValueOrDefault(grid) + comp.Thrust;
        }

        if (_shakeThrust.Count > 0)
        {
            foreach (var session in _players.Sessions)
            {
                if (session.AttachedEntity is not { } player ||
                    _gravity.IsWeightless(player) ||
                    !TryComp(player, out TransformComponent? playerXform) ||
                    playerXform.GridUid is not { } playerGrid ||
                    !_shakeThrust.TryGetValue(playerGrid, out var thrust) ||
                    !TryComp(playerGrid, out PhysicsComponent? body) ||
                    body.Mass <= 0f)
                {
                    continue;
                }

                // Ускорение = суммарная тяга / масса грида.
                var kick = MathF.Min(MathF.Sqrt(thrust / body.Mass) * ShakeScale, ShakeMax);
                _recoil.KickCamera(player, _random.NextAngle().ToVec() * kick);

                _shakeActive.Add(player);
                PlayShakeSound(session, player, kick / ShakeMax);
            }
        }

        // Те, кто больше не трясётся, начинают затухание (если оно ещё не идёт).
        foreach (var (player, stream) in _shakeAudio)
        {
            if (!_shakeActive.Contains(player))
                stream.FadeElapsed ??= 0f;
        }
    }

    private void PlayShakeSound(ICommonSession session, EntityUid player, float intensity)
    {
        var volume = ShakeSoundVolumeMin + (ShakeSoundVolumeMax - ShakeSoundVolumeMin) * intensity;

        // Звук уже есть (в том числе затухающий): отменяем затухание и возвращаем громкость.
        if (_shakeAudio.TryGetValue(player, out var stream) && Exists(stream.Audio))
        {
            stream.Volume = volume;
            stream.FadeElapsed = null;
            _audio.SetVolume(stream.Audio, volume);
            return;
        }

        var audio = _audio.PlayGlobal(ShakeSound, Filter.SinglePlayer(session), true,
            AudioParams.Default.WithLoop(true).WithVolume(volume));

        if (audio != null)
            _shakeAudio[player] = new ShakeStream(audio.Value.Entity, volume);
    }

    /// <summary>
    /// Плавно снижает громкость затухающих звуков и останавливает их по окончании затухания.
    /// </summary>
    private void UpdateShakeFade(float frameTime)
    {
        if (_shakeAudio.Count == 0)
            return;

        _shakeStale.Clear();

        foreach (var (player, stream) in _shakeAudio)
        {
            if (!Exists(stream.Audio))
            {
                _shakeStale.Add(player);
                continue;
            }

            if (stream.FadeElapsed is not { } elapsed)
                continue;

            elapsed += frameTime;

            if (elapsed >= ShakeFadeTime)
            {
                _audio.Stop(stream.Audio);
                _shakeStale.Add(player);
                continue;
            }

            stream.FadeElapsed = elapsed;
            _audio.SetVolume(stream.Audio, stream.Volume + (ShakeFadeFloor - stream.Volume) * (elapsed / ShakeFadeTime));
        }

        foreach (var player in _shakeStale)
        {
            _shakeAudio.Remove(player);
        }
    }
}
