using System;

namespace WiimoteGun.Core.OutputRumble
{
    /// <summary>
    /// EN: [V62-OutputRumble] Semantic rumble events derived from a raw emulator output tag.
    /// FR: [V62-OutputRumble] Evenements de vibration semantiques issus d'un tag d'output brut.
    /// </summary>
    public enum OutputEvent
    {
        None = 0,
        Fire,
        Ammo,
        Reload,
        Damage,
        GameStart,
        GameEnd
    }

    /// <summary>
    /// EN: [V62-OutputRumble] A resolved output event: which player, which semantic event, and the
    ///     numeric value carried by the output (ammo count, damage amount, 0/1 trigger).
    /// FR: [V62-OutputRumble] Un output resolu : joueur, evenement semantique et la valeur numerique
    ///     portee par l'output (nb de balles, degats, 0/1 gachette).
    /// </summary>
    public sealed class OutputResolvedEvent
    {
        public int Player;                 // 1..4
        public OutputEvent Event;
        public double Value;               // ammo count / damage / 0-1
        public DateTime TimestampUtc;
    }
}
