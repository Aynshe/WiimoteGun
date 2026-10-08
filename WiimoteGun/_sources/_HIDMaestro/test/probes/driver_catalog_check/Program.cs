// Driver catalog check (issue #63). No elevation, no device.
//
// GenerateCatalogs builds the catalog for the machine's own INFs with the
// Inf2Cat that ships inside HIDMaestro.Core.dll. v1.9.0 asked for
// /os:10_ARM64 on ARM64, a value Inf2Cat rejects, so every ARM64 deploy
// stopped before the install. Inf2Cat only parses the INFs and hashes the
// files, so both architectures can be cataloged here, on any machine:
//
//   1. For x64 and ARM64: stage that architecture's payload and the Inf2Cat
//      tree exactly as EnsureExtracted does, run the embedded Inf2Cat with
//      the value GenerateCatalogs picks for that architecture, and require
//      exit 0 and one catalog per INF.
//   2. Each value is one Inf2Cat itself lists as accepted.
//   3. Controls that prove the check can fail: the v1.9.0 value is rejected
//      as an invalid operating system, and each architecture's INFs are
//      refused under the other architecture's value, because Inf2Cat checks
//      the INF's decoration against the target.
//
// Exit 0 PASS / 1 FAIL.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HIDMaestro;
using HIDMaestro.Internal;

internal static class Program
{
    static int s_total, s_failures;

    static void Check(string name, bool cond, string detail = "")
    {
        s_total++;
        if (!cond) s_failures++;
        Console.WriteLine($"  [{(cond ? "PASS" : "FAIL")}] {name}{(detail.Length > 0 ? "  " + detail : "")}");
    }

    static int Main()
    {
        Console.WriteLine("=== Driver catalog per architecture (issue #63) ===");
        var staged = new Dictionary<Architecture, string>();
        try
        {
            foreach (var arch in new[] { Architecture.X64, Architecture.Arm64 })
            {
                Console.WriteLine($"\n-- {arch} --");
                string? dir = Stage(arch);
                if (dir == null) continue;
                staged[arch] = dir;

                string os = DriverBuilder.CatalogOsFor(arch);
                var (rc, output) = Inf2Cat(dir, os);
                var infs = Directory.GetFiles(dir, "*.inf").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray();
                var cats = Directory.GetFiles(dir, "*.cat").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray();
                Check($"Inf2Cat accepts /os:{os} for the {arch} INFs", rc == 0, $"exit {rc}: {LastLine(output)}");
                Check($"one catalog per INF ({string.Join(", ", infs)})",
                      infs.Length == 2 && infs.SequenceEqual(cats),
                      $"catalogs [{string.Join(", ", cats)}]");
            }

            Console.WriteLine("\n-- The values are ones Inf2Cat lists --");
            if (staged.TryGetValue(Architecture.X64, out var anyDir))
            {
                var (_, help) = Run(Path.Combine(anyDir, "Inf2Cat.exe"), "/?", anyDir);
                var listed = help.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
                foreach (var arch in new[] { Architecture.X64, Architecture.Arm64 })
                    Check($"{DriverBuilder.CatalogOsFor(arch)} appears in Inf2Cat's own list",
                          listed.Contains(DriverBuilder.CatalogOsFor(arch)));
                Check("10_ARM64 does not appear in it", !listed.Contains("10_ARM64"));
            }

            Console.WriteLine("\n-- Controls: the check can fail --");
            if (staged.TryGetValue(Architecture.Arm64, out var armDir))
            {
                var (rc, output) = Inf2Cat(armDir, "10_ARM64");
                Check("the v1.9.0 value 10_ARM64 is rejected",
                      rc != 0 && output.Contains("Operating systems parameter invalid", StringComparison.OrdinalIgnoreCase),
                      $"exit {rc}: {LastLine(output)}");
                var (rc2, _) = Inf2Cat(armDir, DriverBuilder.CatalogOsFor(Architecture.X64));
                Check("the ARM64 INFs are refused under the x64 value", rc2 != 0 && NoCatalogs(armDir), $"exit {rc2}");
            }
            if (staged.TryGetValue(Architecture.X64, out var x64Dir))
            {
                var (rc, _) = Inf2Cat(x64Dir, DriverBuilder.CatalogOsFor(Architecture.Arm64));
                Check("the x64 INFs are refused under the ARM64 value", rc != 0 && NoCatalogs(x64Dir), $"exit {rc}");
            }
        }
        finally
        {
            foreach (var dir in staged.Values)
                try { Directory.Delete(dir, true); } catch { }
        }

        Console.WriteLine($"\n=== {s_total - s_failures}/{s_total} {(s_failures == 0 ? "PASS" : "FAIL")} ===");
        return s_failures == 0 ? 0 : 1;
    }

    /// <summary>Write an architecture's staging set to a fresh directory,
    /// every file from the resource EnsureExtracted would read it from. The
    /// directory name stays short: Inf2Cat is a .NET Framework program and
    /// fails to load a library whose full path reaches 260 characters.</summary>
    static string? Stage(Architecture arch)
    {
        string dir = Path.Combine(Path.GetTempPath(),
            $"hmcat{Environment.ProcessId}{(arch == Architecture.Arm64 ? "a" : "x")}");
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);

        var asm = typeof(HMContext).Assembly;
        var missing = new List<string>();
        int written = 0;
        foreach (var (file, logical) in DriverBuilder.PayloadFor(arch))
        {
            using var src = asm.GetManifestResourceStream(logical);
            if (src == null) { missing.Add(logical); continue; }
            using var dst = File.Create(Path.Combine(dir, file));
            src.CopyTo(dst);
            written++;
        }
        Check($"every {arch} staging resource is embedded ({written} files)", missing.Count == 0,
              missing.Count == 0 ? "" : "missing: " + string.Join(", ", missing));
        return missing.Count == 0 ? dir : null;
    }

    /// <summary>Run Inf2Cat the way GenerateCatalogs does, after clearing any
    /// catalog an earlier run left.</summary>
    static (int Rc, string Output) Inf2Cat(string dir, string os)
    {
        foreach (var cat in Directory.GetFiles(dir, "*.cat")) File.Delete(cat);
        return Run(Path.Combine(dir, "Inf2Cat.exe"), $"/driver:\"{dir}\" /os:{os}", dir);
    }

    static (int Rc, string Output) Run(string exe, string args, string workingDir)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi)!;
        // Both pipes read asynchronously: a synchronous ReadToEnd would
        // block until the child exits, and the timeout below would never
        // fire. The battery runs a probe with no timeout of its own.
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(120_000))
        {
            try { p.Kill(true); } catch { }
            return (-999, "(timed out after 120 s)");
        }
        Task.WaitAll(new Task[] { stdout, stderr }, 10_000);
        return (p.ExitCode, (stdout.IsCompletedSuccessfully ? stdout.Result : "")
                          + (stderr.IsCompletedSuccessfully ? stderr.Result : ""));
    }

    static bool NoCatalogs(string dir) => Directory.GetFiles(dir, "*.cat").Length == 0;

    static string LastLine(string output) =>
        output.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0) ?? "";
}
