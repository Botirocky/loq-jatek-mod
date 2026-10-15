using System.Management;
using Microsoft.Win32;

namespace LoqJatekMod;

public sealed class Gep
{
    public string Gyarto { get; private init; } = "";
    public string Modell { get; private init; } = "";
    public string Processzor { get; private init; } = "";
    public double RamGb { get; private init; }
    public List<(string Nev, string Driver)> Videokartyak { get; } = new();

    public bool LenovoLoq => Gyarto.Contains("LENOVO", StringComparison.OrdinalIgnoreCase)
                             && Modell.Contains("LOQ", StringComparison.OrdinalIgnoreCase);
    public bool Rtx5060 => Videokartyak.Any(v => v.Nev.Contains("RTX 5060", StringComparison.OrdinalIgnoreCase));
    public bool EzAGep => LenovoLoq && Rtx5060;

    private static IEnumerable<ManagementObject> Lekerdez(string wql)
    {
        try
        {
            using var kereso = new ManagementObjectSearcher(wql);
            return kereso.Get().Cast<ManagementObject>().ToList();
        }
        catch (ManagementException)
        {
            return Array.Empty<ManagementObject>();
        }
    }

    private static string Szoveg(ManagementObject? o, string mezo) => o?[mezo]?.ToString()?.Trim() ?? "";

    public static string NvidiaVerzio(string driver)
    {
        var szamok = driver.Replace(".", "");
        return szamok.Length >= 5 ? $"{szamok[^5..^2]}.{szamok[^2..]}" : driver;
    }

    public static Gep Felmer()
    {
        var rendszer = Lekerdez("SELECT Manufacturer, Model FROM Win32_ComputerSystem").FirstOrDefault();
        var termek = Lekerdez("SELECT Version FROM Win32_ComputerSystemProduct").FirstOrDefault();
        var cpu = Lekerdez("SELECT Name FROM Win32_Processor").FirstOrDefault();
        var ram = Lekerdez("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem").FirstOrDefault();
        var gep = new Gep
        {
            Gyarto = Szoveg(rendszer, "Manufacturer"),
            Modell = $"{Szoveg(termek, "Version")} ({Szoveg(rendszer, "Model")})".Trim(),
            Processzor = Szoveg(cpu, "Name"),
            RamGb = double.TryParse(Szoveg(ram, "TotalPhysicalMemory"), out var b) ? b / 1024 / 1024 / 1024 : 0,
        };
        foreach (var v in Lekerdez("SELECT Name, DriverVersion FROM Win32_VideoController"))
            gep.Videokartyak.Add((Szoveg(v, "Name"), Szoveg(v, "DriverVersion")));
        return gep;
    }

    public string Osszefoglalo()
    {
        var sorok = new List<string>
        {
            $"Gép: {Gyarto} {Modell}",
            $"Processzor: {Processzor}",
            $"Memória: {RamGb:0.#} GB",
        };
        foreach (var (nev, driver) in Videokartyak)
            sorok.Add(nev.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
                ? $"GPU: {nev} – driver {NvidiaVerzio(driver)}"
                : $"GPU: {nev} – driver {driver}");
        var mod = LenovoModBeallitas.Lekerdez();
        sorok.Add("Lenovo mód: " + mod switch
        {
            1 => "Csendes",
            2 => "Kiegyensúlyozott",
            3 => "Teljesítmény",
            255 => "Egyéni",
            null => "nem elérhető",
            _ => mod.ToString(),
        });
        return string.Join(Environment.NewLine, sorok);
    }
}

public static class JatekGpu
{
    private const string Ut = @"Software\Microsoft\DirectX\UserGpuPreferences";

    public static void DgpuraTesz(string exe)
    {
        using var kulcs = Registry.CurrentUser.CreateSubKey(Ut, true);
        kulcs.SetValue(exe, "GpuPreference=2;", RegistryValueKind.String);
    }

    public static void Torol(string exe)
    {
        using var kulcs = Registry.CurrentUser.CreateSubKey(Ut, true);
        kulcs.DeleteValue(exe, false);
    }

    public static List<string> Lista()
    {
        using var kulcs = Registry.CurrentUser.OpenSubKey(Ut);
        return kulcs?.GetValueNames()
            .Where(n => n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        && kulcs.GetValue(n)?.ToString()?.Contains("GpuPreference=2") == true)
            .ToList() ?? new List<string>();
    }
}
