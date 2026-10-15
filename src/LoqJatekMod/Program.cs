namespace LoqJatekMod;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--visszaallit", StringComparer.OrdinalIgnoreCase))
        {
            var mentes = Mentes.Betolt(out _);
            return Keszlet.VisszaallitMind(mentes).Count == 0 ? 0 : 1;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new FoAblak());
        return 0;
    }
}
