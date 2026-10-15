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

    private static RegistryKey Gyoker(RegistryHive hive) => RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);

    private static object? Olvas(RegErtek e)
    {
        using var kulcs = Gyoker(e.Hive).OpenSubKey(e.Ut);
        return kulcs?.GetValue(e.Nev);
    }

    private string MentesKulcs(RegErtek e) => $"{Azonosito}|{e.Hive}|{e.Ut}|{e.Nev}";

    public override bool? Aktiv() =>
        _ertekek.All(e => string.Equals(Olvas(e)?.ToString(), e.Ertek.ToString(), StringComparison.OrdinalIgnoreCase));

    public override void Bekapcsol(Mentes mentes)
    {
        foreach (var e in _ertekek)
        {
            var regi = Olvas(e);
            mentes.Elsore(MentesKulcs(e), regi == null ? null : $"{Tipus(regi)}:{regi}");
            using var kulcs = Gyoker(e.Hive).CreateSubKey(e.Ut, true);
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
            var regi = mentes.Kivesz(k);
            using var kulcs = Gyoker(e.Hive).CreateSubKey(e.Ut, true);
            if (regi == null)
            {
                kulcs.DeleteValue(e.Nev, false);
                continue;
            }
            var tipus = regi[..regi.IndexOf(':')];
            var ertek = regi[(regi.IndexOf(':') + 1)..];
            if (tipus == "DWord")
                kulcs.SetValue(e.Nev, int.Parse(ertek), RegistryValueKind.DWord);
            else
                kulcs.SetValue(e.Nev, ertek, RegistryValueKind.String);
        }
    }

    private static string Tipus(object ertek) => ertek is int ? "DWord" : "String";
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
    private const string Nev_ = "LOQ Játék Mód";

    public override string Azonosito => "energia";
    public override string Nev => "Csúcsteljesítmény energiaséma";
    public override string Leiras => "„Ultimate Performance” energiaséma (ha a gép nem engedi, „Nagy teljesítmény”). Töltőn ajánlott.";

    private static string Powercfg(string arg)
    {
        var p = Process.Start(new ProcessStartInfo("powercfg", arg)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        var ki = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return ki;
    }

    private static string? Guid_(string szoveg) =>
        Regex.Match(szoveg, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}") is { Success: true } m ? m.Value : null;

    private static string? AktivSema() => Guid_(Powercfg("/getactivescheme"));

    private static string? SajatSema()
    {
        foreach (var sor in Powercfg("/list").Split('\n'))
            if (sor.Contains(Nev_))
                return Guid_(sor);
        return null;
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
            sajat = Guid_(Powercfg($"/duplicatescheme {Vegso}")) ?? Guid_(Powercfg($"/duplicatescheme {Magas}"));
            if (sajat == null)
                throw new InvalidOperationException("ezen a gépen nem hozható létre teljesítmény-séma (Modern Standby?)");
            Powercfg($"/changename {sajat} \"{Nev_}\"");
        }
        Powercfg($"/setactive {sajat}");
    }

    public override void Visszaallit(Mentes mentes)
    {
        var elozo = mentes.Kivesz("energia|elozo");
        if (elozo != null)
            Powercfg($"/setactive {elozo}");
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
        catch (ManagementException)
        {
            return null;
        }
    }

    public static int? Lekerdez()
    {
        using var gz = Gamezone();
        if (gz == null)
            return null;
        try
        {
            var ki = gz.InvokeMethod("GetSmartFanMode", null, null);
            return Convert.ToInt32(ki?["Data"]);
        }
        catch (ManagementException)
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
        var elozo = mentes.Kivesz("lenovo|elozo");
        if (int.TryParse(elozo, out var m))
            Beallit(m);
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
        new RegistryBeallitas("ablakos", "Optimalizálás ablakos játékokhoz",
            "Ablakos és keret nélküli játékok is a gyors „flip” megjelenítést kapják: kisebb késleltetés, VRR működik.", false,
            new RegErtek(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings", RegistryValueKind.String, "SwapEffectUpgradeEnable=1;")),
        new EgerGyorsitasBeallitas(),
    };
}
