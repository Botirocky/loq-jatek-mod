using System.Text.Json;

namespace LoqJatekMod;

public sealed class Mentes
{
    private static readonly string Mappa = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LoqJatekMod");
    private static readonly string Fajl = Path.Combine(Mappa, "mentes.json");

    public Dictionary<string, string?> Ertekek { get; set; } = new();

    public static Mentes Betolt()
    {
        try
        {
            if (File.Exists(Fajl))
                return JsonSerializer.Deserialize<Mentes>(File.ReadAllText(Fajl)) ?? new Mentes();
        }
        catch (JsonException)
        {
        }
        return new Mentes();
    }

    public void Ment()
    {
        Directory.CreateDirectory(Mappa);
        File.WriteAllText(Fajl, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool Van(string kulcs) => Ertekek.ContainsKey(kulcs);

    public void Elsore(string kulcs, string? ertek)
    {
        if (!Ertekek.ContainsKey(kulcs))
            Ertekek[kulcs] = ertek;
    }

    public string? Kivesz(string kulcs)
    {
        Ertekek.Remove(kulcs, out var ertek);
        return ertek;
    }
}
