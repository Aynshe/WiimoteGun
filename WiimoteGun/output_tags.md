# Wiimote4Guns - OutputHooker integration

OutputHooker runs the **literal command** and replaces only `%s%` with the live value, so
everything after `udp 1 127.0.0.1 41111 ` is exactly what Wiimote4Guns receives.

## Commands (short semantic form)

```
P1 FIRE <1/0>     a shot (player 1)      -> rumble pulse (1 = shot, 0 = released)
P2 FIRE %s%       shot on player 2
P1 DMG %s%        damage taken           -> impact rumble (priority over fire)
P1 AMMO %s%       bullets left           -> recorded only, not used (no effect on the rumble)
GameOutputStart   MAME started a game    -> enables "Rumble only during a game"
GameOutputEnd     MAME left a game       -> ends the game state
```

Player = `P1`..`P4` | events = `FIRE` | `DMG` | `AMMO`

## Notes

- **FIRE 0/1 alone drives the rumble**: the game itself sends `0` when the trigger is released
  or the magazine is empty, so the rumble stops by itself.
- **AMMO is recorded but nothing is done with it** (kept for future output projects).
- Games that expose no ammo output (e.g. 2 Spicy / Lindbergh) just send FIRE — nothing to configure.

- **Seul FIRE 0/1 pilote la vibration** : le jeu envoie lui-même `0` au relâchement de la gâchette
  ou chargeur vide, donc la vibration s'arrête d'elle-même.
- **AMMO est relevé mais on n'en fait rien** (conservé pour de futurs projets output).
- Les jeux sans sortie munitions (ex. 2 Spicy / Lindbergh) n'envoient que FIRE — rien à configurer.

## OutputHooker INI - global `default.ini` (per-game files are usually unnecessary)

The commands below go into OutputHooker's **`default.ini`** (the `ini` folder next to OutputHooker.exe),
which applies them to every game. A per-game ini (e.g. `ini\2spicy.ini`) is only needed when a game
exposes a tag the default ini does not cover.

```ini
[General]
MameStart = udp 1 127.0.0.1 41111 GameOutputStart
MameStop  = udp 1 127.0.0.1 41111 GameOutputEnd

[Output]
mainpcb:Player1_Gun_Recoil = udp 1 127.0.0.1 41111 P1 FIRE %s%
mainpcb:Player2_Gun_Recoil = udp 1 127.0.0.1 41111 P2 FIRE %s%
P1_Damaged                = udp 1 127.0.0.1 41111 P1 DMG %s%
```

OutputHooker replaces only `%s%` with the live value; the rest is sent verbatim.
A built-in tag vocabulary (`Player1..4_Gun_Recoil`, `TriggerPress_P1..4`, `P1..4_GunRecoil`,
`_Damaged`, `_Gun_Hit`, ...) is already handled by Wiimote4Guns, so you normally do NOT need to
define any tag mapping.

## Custom tag rules (optional)

If a raw output name is still not recognised, add a rule line made of three fields separated by
a pipe character: the pattern, the player number (1-4) and the event (fire, ammo, reload, damage).
Matching is case-insensitive CONTAINS (works with or without a MAME device prefix), first match wins.

```
; example (player 3 fire) - lines starting with ; are comments
; myboard:gun_recoil_p3 | 3 | fire
```
