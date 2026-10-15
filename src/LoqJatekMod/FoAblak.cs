using System.Diagnostics;

namespace LoqJatekMod;

public sealed class FoAblak : Form
{
    private static readonly Color Hatter = Color.FromArgb(24, 26, 31);
    private static readonly Color Panel = Color.FromArgb(34, 37, 44);
    private static readonly Color Szoveg = Color.FromArgb(230, 232, 236);
    private static readonly Color Halvany = Color.FromArgb(150, 156, 168);
    private static readonly Color Kiemel = Color.FromArgb(118, 185, 0);

    private readonly List<Beallitas> _beallitasok = Keszlet.Mind();
    private readonly Mentes _mentes = Mentes.Betolt();
    private readonly ListView _lista = new();
    private readonly ListBox _jatekok = new();
    private readonly TextBox _gepInfo = new();
    private readonly TextBox _naplo = new();
    private readonly Label _figyelmeztetes = new();
    private Gep? _gep;

    public FoAblak()
    {
        Text = "LOQ Játék Mód";
        Size = new Size(980, 760);
        MinimumSize = new Size(820, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Hatter;
        ForeColor = Szoveg;
        Font = new Font("Segoe UI", 10f);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        var fo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(14) };
        fo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fo.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        fo.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        fo.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fo.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        Controls.Add(fo);

        fo.Controls.Add(new Label
        {
            Text = "LOQ Játék Mód",
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Kiemel,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
        });

        _figyelmeztetes.AutoSize = true;
        _figyelmeztetes.ForeColor = Color.FromArgb(255, 196, 0);
        _figyelmeztetes.Margin = new Padding(0, 0, 0, 8);
        fo.Controls.Add(_figyelmeztetes);

        _gepInfo.Multiline = true;
        _gepInfo.ReadOnly = true;
        _gepInfo.Dock = DockStyle.Fill;
        _gepInfo.BackColor = Panel;
        _gepInfo.ForeColor = Szoveg;
        _gepInfo.BorderStyle = BorderStyle.None;
        _gepInfo.Font = new Font("Consolas", 10f);
        fo.Controls.Add(_gepInfo);

        _lista.View = View.Details;
        _lista.CheckBoxes = true;
        _lista.FullRowSelect = true;
        _lista.Dock = DockStyle.Fill;
        _lista.BackColor = Panel;
        _lista.ForeColor = Szoveg;
        _lista.BorderStyle = BorderStyle.None;
        _lista.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _lista.ShowItemToolTips = true;
        _lista.Columns.Add("Beállítás", 300);
        _lista.Columns.Add("Állapot", 130);
        _lista.Columns.Add("Leírás", 500);
        fo.Controls.Add(_lista);

        var gombok = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 8) };
        gombok.Controls.Add(Gomb("Játék mód BE", (_, _) => Alkalmaz(), true));
        gombok.Controls.Add(Gomb("Minden visszaállítása", (_, _) => Visszaallit()));
        gombok.Controls.Add(Gomb("Frissítés", (_, _) => Frissit()));
        gombok.Controls.Add(Gomb("Játék hozzáadása (NVIDIA GPU)", (_, _) => JatekHozzaad()));
        gombok.Controls.Add(Gomb("Kijelölt játék törlése", (_, _) => JatekTorol()));
        gombok.Controls.Add(Gomb("NVIDIA driver letöltése", (_, _) => Megnyit("https://www.nvidia.com/hu-hu/software/nvidia-app/")));
        gombok.Controls.Add(Gomb("Lenovo driverek", (_, _) => Megnyit("https://pcsupport.lenovo.com/")));
        fo.Controls.Add(gombok);

        var also = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        also.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        also.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        _jatekok.Dock = DockStyle.Fill;
        _jatekok.BackColor = Panel;
        _jatekok.ForeColor = Szoveg;
        _jatekok.BorderStyle = BorderStyle.None;
        _naplo.Multiline = true;
        _naplo.ReadOnly = true;
        _naplo.ScrollBars = ScrollBars.Vertical;
        _naplo.Dock = DockStyle.Fill;
        _naplo.BackColor = Panel;
        _naplo.ForeColor = Halvany;
        _naplo.BorderStyle = BorderStyle.None;
        _naplo.Font = new Font("Consolas", 9.5f);
        also.Controls.Add(Csoport("Játékok a NVIDIA GPU-n", _jatekok), 0, 0);
        also.Controls.Add(Csoport("Napló", _naplo), 1, 0);
        fo.Controls.Add(also);

        Shown += (_, _) => Frissit();
    }

    private static Button Gomb(string szoveg, EventHandler kattint, bool fo = false)
    {
        var g = new Button
        {
            Text = szoveg,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = fo ? Kiemel : Panel,
            ForeColor = fo ? Color.Black : Szoveg,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 6),
        };
        g.FlatAppearance.BorderColor = fo ? Kiemel : Color.FromArgb(60, 64, 74);
        g.Click += kattint;
        return g;
    }

    private static Control Csoport(string cim, Control tartalom)
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Margin = new Padding(0, 0, 8, 0) };
        p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        p.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        p.Controls.Add(new Label { Text = cim, AutoSize = true, ForeColor = Halvany, Margin = new Padding(0, 0, 0, 4) });
        p.Controls.Add(tartalom);
        return p;
    }

    private void Naplo(string sor) => _naplo.AppendText($"[{DateTime.Now:HH:mm:ss}] {sor}{Environment.NewLine}");

    private void Frissit()
    {
        Cursor = Cursors.WaitCursor;
        try
        {
            _gep = Gep.Felmer();
            _gepInfo.Text = _gep.Osszefoglalo();
            _figyelmeztetes.Text = _gep.EzAGep
                ? "Lenovo LOQ + RTX 5060 felismerve – minden beállítás erre a gépre van hangolva."
                : "Figyelem: ez nem a Lenovo LOQ / RTX 5060 gép. A beállítások működhetnek, de nem erre készültek.";
            var kijelolt = _lista.Items.Count == 0
                ? null
                : _lista.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => (string)i.Tag!).ToHashSet();
            _lista.Items.Clear();
            foreach (var b in _beallitasok)
            {
                bool? aktiv;
                try
                {
                    aktiv = b.Aktiv();
                }
                catch (Exception)
                {
                    aktiv = null;
                }
                var allapot = aktiv switch { true => "BE", false => "ki", null => "nem elérhető" };
                if (b.UjraindulasKell)
                    allapot += aktiv == true ? "" : " (újraind.)";
                var elem = new ListViewItem(new[] { b.Nev, allapot, b.Leiras })
                {
                    Tag = b.Azonosito,
                    Checked = kijelolt?.Contains(b.Azonosito) ?? aktiv != null,
                    ToolTipText = b.Leiras,
                };
                elem.UseItemStyleForSubItems = false;
                elem.SubItems[1].ForeColor = aktiv switch { true => Kiemel, false => Szoveg, null => Halvany };
                _lista.Items.Add(elem);
            }
            _jatekok.Items.Clear();
            foreach (var j in JatekGpu.Lista())
                _jatekok.Items.Add(j);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void Alkalmaz()
    {
        var ujraindulas = false;
        foreach (ListViewItem elem in _lista.Items)
        {
            if (!elem.Checked)
                continue;
            var b = _beallitasok.First(x => x.Azonosito == (string)elem.Tag!);
            try
            {
                if (b.Aktiv() == true)
                {
                    Naplo($"{b.Nev}: már be van kapcsolva");
                    continue;
                }
                b.Bekapcsol(_mentes);
                _mentes.Ment();
                ujraindulas |= b.UjraindulasKell;
                Naplo($"{b.Nev}: BEKAPCSOLVA");
            }
            catch (Exception ex)
            {
                Naplo($"{b.Nev}: HIBA – {ex.Message}");
            }
        }
        Frissit();
        if (ujraindulas)
            MessageBox.Show(this, "Egyes beállításokhoz (HAGS) újraindítás kell.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Visszaallit()
    {
        if (MessageBox.Show(this, "Minden módosítást visszaállítsak az eredeti értékre?", Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        foreach (var b in _beallitasok)
        {
            try
            {
                b.Visszaallit(_mentes);
                Naplo($"{b.Nev}: visszaállítva");
            }
            catch (Exception ex)
            {
                Naplo($"{b.Nev}: HIBA – {ex.Message}");
            }
        }
        _mentes.Ment();
        Frissit();
    }

    private void JatekHozzaad()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Játék .exe kiválasztása",
            Filter = "Programok (*.exe)|*.exe",
            Multiselect = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        foreach (var f in dlg.FileNames)
        {
            JatekGpu.DgpuraTesz(f);
            Naplo($"NVIDIA GPU-ra téve: {Path.GetFileName(f)}");
        }
        Frissit();
    }

    private void JatekTorol()
    {
        if (_jatekok.SelectedItem is not string exe)
            return;
        JatekGpu.Torol(exe);
        Naplo($"Eltávolítva: {Path.GetFileName(exe)}");
        Frissit();
    }

    private void Megnyit(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Naplo($"Nem sikerült megnyitni: {ex.Message}");
        }
    }
}
