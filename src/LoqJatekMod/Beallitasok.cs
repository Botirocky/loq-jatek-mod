using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LoqJatekMod;

public abstract class Beallitas
{
    public abstract string Azonosito { get; }
    public abstract string Nev { get; }
    public abstract string Leiras { get; }
    public virtual bool UjraindulasKell => false;
    public abstract bool? Aktiv();
    public abstract void Bekapcsol(Mentes mentes);
    public abstract void Visszaallit(Mentes mentes);
}

public sealed record RegErtek(RegistryHive Hive, string Ut, string Nev, RegistryValueKind Tipus, object Ertek);

public static class RegMentes
{
    public static string? Szerializal(RegistryKey? kulcs, string nev)
    {
        var ertek = kulcs?.GetValue(nev, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (kulcs == null || ertek == null)
            return null;
        var tipus = kulcs.GetValueKind(nev);
        var adat = ertek switch
        {
            byte[] b => Convert.ToBase64String(b),
            string[] t => string.Join('\0', t),
            _ => ertek.ToString(),
        };
        return $"{tipus}:{adat}";
    }

    public static void Visszair(RegistryKey kulcs, string nev, string? mentett)
    {
        if (mentett == null)
        {
            kulcs.DeleteValue(nev, false);
            return;
        }
        var i = mentett.IndexOf(':');
        var tipus = Enum.Parse<RegistryValueKind>(mentett[..i]);
        var adat = mentett[(i + 1)..];
        object ertek = tipus switch
        {
            RegistryValueKind.DWord => int.Parse(adat),
            RegistryValueKind.QWord => long.Parse(adat),
            RegistryValueKind.Binary => Convert.FromBase64String(adat),
            RegistryValueKind.MultiString => adat.Split('\0'),
            _ => adat,
        };
        kulcs.SetValue(nev, ertek, tipus);
    }

    public static RegistryKey Gyoker(RegistryHive hive) => RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
}

public class RegistryBeallitas : Beallitas
{
    private readonly RegErtek[] _ertekek;

    public RegistryBeallitas(string azonosito, string nev, string leiras, bool ujraindulas, params RegErtek[] ertekek)
    {
        Azonosito = azonosito;
        Nev = nev;
        Leiras = leiras;
        UjraindulasKell = ujraindulas;
        _ertekek = ertekek;
    }

    public override string Azonosito { get; }
    public override string Nev { get; }
    public override string Leiras { get; }
    public override bool UjraindulasKell { get; }

    private static object? Olvas(RegErtek e)
    {
        using var kulcs = RegMentes.Gyoker(e.Hive).OpenSubKey(e.Ut);
        return kulcs?.GetValue(e.Nev);
    }

    private string MentesKulcs(RegErtek e) => $"{Azonosito}|{e.Hive}|{e.Ut}|{e.Nev}";

    public override bool? Aktiv() =>
        _ertekek.All(e => string.Equals(Olvas(e)?.ToString(), e.Ertek.ToString(), StringComparison.OrdinalIgnoreCase));

    public override void Bekapcsol(Mentes mentes)
    {
        foreach (var e in _ertekek)
        {
            using var kulcs = RegMentes.Gyoker(e.Hive).CreateSubKey(e.Ut, true);
            mentes.Elsore(MentesKulcs(e), RegMentes.Szerializal(kulcs, e.Nev));
            kulcs.SetValue(e.Nev, e.Ertek, e.Tipus);
        }
    }

    public override void Visszaallit(Mentes mentes)
    {
        foreach (var e in _ertekek)
        {
            var k = MentesKulcs(e);
            if (!mentes.Van(k))
                continue;
            using var kulcs = RegMentes.Gyoker(e.Hive).CreateSubKey(e.Ut, true);
            RegMentes.Visszair(kulcs, e.Nev, mentes.Olvas(k));
            mentes.Torol(k);
        }
    }
}

public sealed class DirectXJelzoBeallitas : Beallitas
{
    private const string Ut = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string Ertek = "DirectXUserGlobalSettings";
    private const string Jelzo = "SwapEffectUpgradeEnable";
    private const string MentesKulcs = "ablakos|eredeti";

    public override string Azonosito => "ablakos";
    public override string Nev => "Optimalizálás ablakos játékokhoz";
    public override string Leiras => "Ablakos és keret nélküli játékok is a gyors „flip” megjelenítést kapják: kisebb késleltetés, VRR működik.";

    private static Dictionary<string, string> Bont(string? s) =>
        (s ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

    private static string Osszerak(Dictionary<string, string> d) => string.Concat(d.Select(p => $"{p.Key}={p.Value};"));

    public override bool? Aktiv()
    {
        using var kulcs = Registry.CurrentUser.OpenSubKey(Ut);
        return Bont(kulcs?.GetValue(Ertek)?.ToString()).GetValueOrDefault(Jelzo) == "1";
    }

    public override void Bekapcsol(Mentes mentes)
    {
        using var kulcs = Registry.CurrentUser.CreateSubKey(Ut, true);
        var jelenlegi = kulcs.GetValue(Ertek)?.ToString();
        mentes.Elsore(MentesKulcs, Bont(jelenlegi).GetValueOrDefault(Jelzo) ?? "");
        var d = Bont(jelenlegi);
        d[Jelzo] = "1";
        kulcs.SetValue(Ertek, Osszerak(d), RegistryValueKind.String);
    }

    public override void Visszaallit(Mentes mentes)
    {
        if (!mentes.Van(MentesKulcs))
            return;
        using var kulcs = Registry.CurrentUser.CreateSubKey(Ut, true);
        var d = Bont(kulcs.GetValue(Ertek)?.ToString());
        var eredeti = mentes.Olvas(MentesKulcs);
        if (string.IsNullOrEmpty(eredeti))
            d.Remove(Jelzo);
        else
            d[Jelzo] = eredeti;
        if (d.Count == 0)
            kulcs.DeleteValue(Ertek, false);
        else
            kulcs.SetValue(Ertek, Osszerak(d), RegistryValueKind.String);
        mentes.Torol(MentesKulcs);
    }
}

public sealed class EgerGyorsitasBeallitas : RegistryBeallitas
{
    private const uint SpiSetMouse = 0x0004;
    private const uint SpifSendChange = 0x01 | 0x02;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, int[] vparam, uint winIni);

    public EgerGyorsitasBeallitas() : base(
        "eger", "Egérgyorsítás ki",
        "A „mutató pontosságának javítása” kikapcsolása: az egér mozgása egyenletes lesz, célzásnál pontosabb.",
        false,
        new RegErtek(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", RegistryValueKind.String, "0"),
        new RegErtek(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", RegistryValueKind.String, "0"),
        new RegErtek(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", RegistryValueKind.String, "0"))
    {
    }

    public override void Bekapcsol(Mentes mentes)
    {
        base.Bekapcsol(mentes);
        SystemParametersInfo(SpiSetMouse, 0, new[] { 0, 0, 0 }, SpifSendChange);
    }

    public override void Visszaallit(Mentes mentes)
    {
        base.Visszaallit(mentes);
        using var kulcs = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse");
        int Ertek(string n) => int.TryParse(kulcs?.GetValue(n)?.ToString(), out var v) ? v : 0;
        SystemParametersInfo(SpiSetMouse, 0, new[] { Ertek("MouseThreshold1"), Ertek("MouseThreshold2"), Ertek("MouseSpeed") }, SpifSendChange);
    }
}

public sealed class EnergiaSemaBeallitas : Beallitas
{
    private const string Vegso = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private const string Magas = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string SemaNev = "LOQ Jatek Mod";
    private const string AppKulcs = @"SOFTWARE\Botirocky\LoqJatekMod";

    public override string Azonosito => "energia";
    public override string Nev => "Csúcsteljesítmény energiaséma";
    public override string Leiras => "„Ultimate Performance” energiaséma (ha a gép nem engedi, „Nagy teljesítmény”). Töltőn ajánlott.";

    private static (int Kod, string Ki) Powercfg(string arg)
    {
        using var p = Process.Start(new ProcessStartInfo("powercfg", arg)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        var ki = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, ki);
    }

    private static string? Guid_(string szoveg) =>
        Regex.Match(szoveg, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}") is { Success: true } m ? m.Value : null;

    private static string? AktivSema() => Guid_(Powercfg("/getactivescheme").Ki);

    private static string? SajatSema()
    {
        using var kulcs = Registry.LocalMachine.OpenSubKey(AppKulcs);
        var guid = kulcs?.GetValue("SajatSema")?.ToString();
        return guid != null && Powercfg($"/query {guid}").Kod == 0 ? guid : null;
    }

    public override bool? Aktiv()
    {
        var sajat = SajatSema();
        return sajat != null && string.Equals(AktivSema(), sajat, StringComparison.OrdinalIgnoreCase);
    }

    public override void Bekapcsol(Mentes mentes)
    {
        mentes.Elsore("energia|elozo", AktivSema());
        var sajat = SajatSema();
        if (sajat == null)
        {
            sajat = Guid_(Powercfg($"/duplicatescheme {Vegso}").Ki) ?? Guid_(Powercfg($"/duplicatescheme {Magas}").Ki);
            if (sajat == null)
                throw new InvalidOperationException("ezen a gépen nem hozható létre teljesítmény-séma (Modern Standby?)");
            Powercfg($"/changename {sajat} \"{SemaNev}\"");
            using var kulcs = Registry.LocalMachine.CreateSubKey(AppKulcs, true);
            kulcs.SetValue("SajatSema", sajat, RegistryValueKind.String);
        }
        if (Powercfg($"/setactive {sajat}").Kod != 0)
            throw new InvalidOperationException("a séma nem aktiválható");
    }

    public override void Visszaallit(Mentes mentes)
    {
        var elozo = mentes.Olvas("energia|elozo");
        if (elozo == null)
            return;
        if (Powercfg($"/setactive {elozo}").Kod != 0)
            throw new InvalidOperationException("az előző séma nem aktiválható");
        mentes.Torol("energia|elozo");
    }
}

public sealed class LenovoModBeallitas : Beallitas
{
    private const int Teljesitmeny = 3;

    public override string Azonosito => "lenovo";
    public override string Nev => "Lenovo teljesítmény-mód (Fn+Q)";
    public override string Leiras => "A Lenovo GameZone „Performance” módja: magasabb CPU/GPU-teljesítménykeret, erősebb ventilátor.";

    private static ManagementObject? Gamezone()
    {
        try
        {
            using var kereso = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM LENOVO_GAMEZONE_DATA");
            return kereso.Get().Cast<ManagementObject>().FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static int? Lekerdez()
    {
        try
        {
            using var gz = Gamezone();
            var ki = gz?.InvokeMethod("GetSmartFanMode", null, null);
            return ki == null ? null : Convert.ToInt32(ki["Data"]);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void Beallit(int mod)
    {
        using var gz = Gamezone() ?? throw new InvalidOperationException("a Lenovo GameZone WMI nem érhető el (Lenovo Vantage / driver hiányzik?)");
        var be = gz.GetMethodParameters("SetSmartFanMode");
        be["Data"] = mod;
        gz.InvokeMethod("SetSmartFanMode", be, null);
    }

    public override bool? Aktiv() => Lekerdez() is { } m ? m == Teljesitmeny : null;

    public override void Bekapcsol(Mentes mentes)
    {
        mentes.Elsore("lenovo|elozo", Lekerdez()?.ToString());
        Beallit(Teljesitmeny);
    }

    public override void Visszaallit(Mentes mentes)
    {
        var elozo = mentes.Olvas("lenovo|elozo");
        if (int.TryParse(elozo, out var m))
            Beallit(m);
        mentes.Torol("lenovo|elozo");
    }
}

public static class Keszlet
{
    public static List<Beallitas> Mind() => new()
    {
        new EnergiaSemaBeallitas(),
        new LenovoModBeallitas(),
        new RegistryBeallitas("jatekmod", "Windows Játék mód",
            "A Windows a futó játéknak adja az elsőbbséget, háttérfrissítéseket visszafog.", false,
            new RegErtek(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", RegistryValueKind.DWord, 1),
            new RegErtek(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", RegistryValueKind.DWord, 1)),
        new RegistryBeallitas("hags", "Hardveres GPU-ütemezés (HAGS)",
            "A GPU maga ütemezi a memóriáját: kisebb késleltetés, DLSS 3 képkocka-generáláshoz szükséges.", true,
            new RegErtek(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", RegistryValueKind.DWord, 2)),
        new RegistryBeallitas("dvr", "Háttérfelvétel (Game DVR) ki",
            "Az Xbox Game Bar folyamatos háttér-videófelvétele ki: kevesebb GPU-terhelés és akadás.", false,
            new RegErtek(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", RegistryValueKind.DWord, 0),
            new RegErtek(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", RegistryValueKind.DWord, 0)),
        new DirectXJelzoBeallitas(),
        new EgerGyorsitasBeallitas(),
    };

    public static List<string> VisszaallitMind(Mentes mentes)
    {
        var hibak = new List<string>();
        foreach (var b in Mind())
        {
            try
            {
                b.Visszaallit(mentes);
            }
            catch (Exception ex)
            {
                hibak.Add($"{b.Nev}: {ex.Message}");
            }
        }
        mentes.Ment();
        return hibak;
    }
}
