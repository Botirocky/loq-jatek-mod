using System.Text.Json;

namespace LoqJatekMod;

public sealed class Mentes
{
    private static readonly string Mappa = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LoqJatekMod");
    private static readonly string Fajl = Path.Combine(Mappa, "mentes.json");

    public Dictionary<string, string?> Ertekek { get; set; } = new();

    public static Mentes Betolt(out string? hiba)
    {
        hiba = null;
        try
        {
            if (File.Exists(Fajl))
                return JsonSerializer.Deserialize<Mentes>(File.ReadAllText(Fajl)) ?? new Mentes();
        }
        catch (JsonException)
        {
            var serult = Fajl + $".serult-{DateTime.Now:yyyyMMdd-HHmmss}";
            try
            {
                File.Move(Fajl, serult);
            }
            catch (IOException)
            {
            }
            hiba = $"A mentésfájl sérült volt, félretettem: {serult}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            hiba = $"A mentésfájl nem olvasható: {ex.Message}";
        }
        return new Mentes();
    }

    public void Ment()
    {
        Directory.CreateDirectory(Mappa);
        var ideiglenes = Fajl + ".uj";
        File.WriteAllText(ideiglenes, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(ideiglenes, Fajl, true);
    }

    public bool Van(string kulcs) => Ertekek.ContainsKey(kulcs);

    public string? Olvas(string kulcs) => Ertekek.GetValueOrDefault(kulcs);

    public void Elsore(string kulcs, string? ertek)
    {
        if (!Ertekek.ContainsKey(kulcs))
            Ertekek[kulcs] = ertek;
    }

    public void Torol(string kulcs) => Ertekek.Remove(kulcs);
}
