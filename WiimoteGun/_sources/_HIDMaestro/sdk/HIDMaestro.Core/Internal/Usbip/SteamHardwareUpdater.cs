using System;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace HIDMaestro.Internal.Usbip;

/// <summary>Issue #62. The firmware build Steam's own updater ships for a
/// Valve device family, read from the updater's config.
///
/// <para>Steam decides that a controller wants a firmware update by running
/// <c>bin\hardwareupdater\hardwareupdater.exe --check-for-updates</c> from
/// its install. The updater reads each device's build stamp, attribute tag
/// 4 of the 0x83 reply, and lists the device whenever that stamp differs
/// from the one <c>hardwareupdater.cfg</c> names for its family, older or
/// newer: a stamp past the current one is offered as a downgrade. Measured
/// with the 2026 Steam Controller persona against Steam's own updater, only
/// a stamp equal to the config's went unlisted. A persona answering with a
/// fixed capture was offered an update each time Valve shipped firmware,
/// and it could never take one.</para>
///
/// <para>Each firmware image carries its own build time, so a real
/// controller that has installed the firmware Steam offers reports exactly
/// the config's stamp. The persona answers with it too. Without Steam there
/// is no updater to ask, and the captured stamp stands.</para></summary>
internal static class SteamHardwareUpdater
{
    /// <summary>The updater's folder under Steam's install directory. The
    /// updater reads its config relative to the working directory, so it
    /// has to be started from here.</summary>
    internal const string RelativeDirectory = @"bin\hardwareupdater";
    internal const string ExeName = "hardwareupdater.exe";
    internal const string ConfigName = "hardwareupdater.cfg";

    /// <summary>The build stamp the config names for a key such as
    /// <c>TRITON_FW_TS</c>, or null when Steam, its updater config or the
    /// key is absent. Reads the file on every call, so a Steam update that
    /// ships new firmware is followed without recreating the device. Never
    /// throws: it runs on the USB control-transfer path.</summary>
    public static uint? CurrentStamp(string key)
    {
        try
        {
            string? dir = UpdaterDirectory();
            if (dir == null) return null;
            string cfg = Path.Combine(dir, ConfigName);
            return File.Exists(cfg) ? ParseStamp(File.ReadAllText(cfg), key) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The updater's folder, or null when Steam or its updater is
    /// not installed.</summary>
    internal static string? UpdaterDirectory()
    {
        string? steam = InstallDirectory();
        if (steam == null) return null;
        string dir = Path.Combine(steam, RelativeDirectory);
        return Directory.Exists(dir) ? dir : null;
    }

    /// <summary>Steam's install directory. PadForge's SteamWorkshop config
    /// store reads the per-user value first, in Steam's forward-slash form
    /// ("c:/program files (x86)/steam"), then the machine-wide installer
    /// value. DS4Windows' game audio detector reads the same two and falls
    /// back to the default location, as this does last.</summary>
    internal static string? InstallDirectory()
    {
        foreach (var (hive, subKey, name) in new[]
        {
            (Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
        })
        {
            try
            {
                using var key = hive.OpenSubKey(subKey);
                if (key?.GetValue(name) is string value && !string.IsNullOrWhiteSpace(value))
                {
                    string full = Path.GetFullPath(value);
                    if (Directory.Exists(full)) return full;
                }
            }
            catch
            {
                // An unreadable key is the same as an absent one.
            }
        }
        string fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        return Directory.Exists(fallback) ? fallback : null;
    }

    /// <summary>The value of one <c>KEY:HEX</c> line. The key matches whole,
    /// so <c>TRITON_FW_TS</c> never reads the <c>MUST_UPDATE_TRITON_FW_TS</c>
    /// line. Null for an absent key, a value that is not a 32-bit hex
    /// number, or zero.</summary>
    internal static uint? ParseStamp(string text, string key)
    {
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            int colon = line.IndexOf(':');
            if (colon <= 0 || !line.AsSpan(0, colon).Trim().SequenceEqual(key)) continue;

            var value = line.AsSpan(colon + 1).Trim();
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) value = value[2..];
            return value.Length is > 0 and <= 8
                   && uint.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint stamp)
                   && stamp != 0
                ? stamp : null;
        }
        return null;
    }
}
