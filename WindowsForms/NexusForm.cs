using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace NexusWinForms;

internal sealed class NexusForm : Form
{
    private static readonly Color Ink = Color.FromArgb(7, 8, 22);
    private static readonly Color Panel = Color.FromArgb(17, 15, 39);
    private static readonly Color Purple = Color.FromArgb(166, 69, 255);
    private static readonly Color Cyan = Color.FromArgb(89, 225, 255);
    private readonly RichTextBox output = new();
    private readonly TextBox input = new();
    private readonly Label prompt = new();
    private readonly ManualResetEventSlim inputReady = new(false);
    private readonly object inputLock = new();
    private readonly Queue<string> submitted = new();
    private string explorerName = "";
    private bool selectedFemale;
    private int selectedAge = 18;
    private int selectedFitness = 80;
    private int selectedCharacter = 1;
    private readonly NexusPersistence persistence = new();
    private volatile bool closing;
    private readonly string splashPath = Path.Combine(AppContext.BaseDirectory, "nexus-splash.png");
    private readonly string hubPath = Path.Combine(AppContext.BaseDirectory, "hub-background.png");
    private readonly string gameplayArtPath = Path.Combine(AppContext.BaseDirectory, "game-side-art.png");
    private readonly string charactersPath = AppContext.BaseDirectory;
    private System.Windows.Forms.Timer loadingTimer;
    private NexusProgressBar loadingBar;
    private Label loadingLabel;

    public NexusForm()
    {
        Text = "NEXUS SYSTEM v1.0";
        MinimumSize = new Size(980, 650);
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ink;
        ForeColor = Color.White;
        ShowSplash();
        FormClosing += (_, _) => { closing = true; inputReady.Set(); };
    }

    private void ShowSplash()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Ink, BackgroundImage = LoadArtwork(splashPath), BackgroundImageLayout = ImageLayout.Zoom };
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 145, BackColor = Color.FromArgb(220, 7, 8, 28), Padding = new Padding(45, 8, 45, 8) };
        page.Controls.Add(bottom);
        var loadLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
        loadLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        loadLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        loadLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bottom.Controls.Add(loadLayout);
        loadingLabel = new Label { Text = "SINCRONIZANDO NÚCLEO NEXUS · 0%", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Cyan, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        loadingBar = new NexusProgressBar { Dock = DockStyle.Fill, Value = 0 };
        var enter = StyledButton("PRESIONA ENTER PARA CONTINUAR", Purple, Color.White, 350, 44);
        enter.Anchor = AnchorStyles.None;
        enter.Enabled = false;
        KeyEventHandler splashEnter = (_, e) => { if (e.KeyCode == Keys.Enter && enter.Enabled) { e.SuppressKeyPress = true; enter.PerformClick(); } };
        KeyPreview = true;
        KeyDown += splashEnter;
        enter.Click += (_, _) => { KeyDown -= splashEnter; KeyPreview = false; ShowHub(); };
        loadLayout.Controls.Add(loadingLabel, 0, 0);
        loadLayout.Controls.Add(loadingBar, 0, 1);
        loadLayout.Controls.Add(enter, 0, 2);
        ReplacePage(page);

        int progress = 0;
        loadingTimer = new System.Windows.Forms.Timer { Interval = 35 };
        loadingTimer.Tick += (_, _) =>
        {
            progress = Math.Min(100, progress + 2);
            loadingBar.Value = progress;
            loadingLabel.Text = progress < 100 ? $"SINCRONIZANDO NÚCLEO NEXUS · {progress}%" : "NÚCLEO NEXUS EN LÍNEA · PULSA ENTER";
            if (progress == 100) { loadingTimer.Stop(); enter.Enabled = true; }
        };
        loadingTimer.Start();
    }

    private void ShowHub()
    {
        loadingTimer?.Stop();
        AcceptButton = null;
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        var top = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(12, 10, 31), Padding = new Padding(24, 0, 22, 0) };
        var logo = new Label { Text = "NEXUS", Dock = DockStyle.Left, Width = 240, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold) };
        var topInfo = new Label { Text = "CENTRO DE OPERACIONES     ◆     SISTEMA IRIS ACTIVO", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = Cyan, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        top.Controls.Add(topInfo); top.Controls.Add(logo);

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(12, 10, 31), Padding = new Padding(18, 22, 18, 18) };
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        var player = new Label { Text = "EXPLORADOR", Height = 26, Width = 180, ForeColor = Color.FromArgb(190, 180, 225), Font = new Font("Segoe UI", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        var name = new TextBox { Width = 180, Height = 34, Font = new Font("Segoe UI", 11), Text = explorerName, BackColor = Color.FromArgb(29, 24, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Escribe tu nombre" };
        nav.Controls.Add(player); nav.Controls.Add(name); nav.Controls.Add(Spacer(22));
        nav.Controls.Add(NavButton("▶   JUGAR", true, () =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show("Escribe el nombre de tu explorador para continuar.", "NEXUS"); name.Focus(); return; }
            explorerName = name.Text.Trim();
            ShowExplorerRegistration();
        }));
        nav.Controls.Add(NavButton("▣   Continuar / partidas", false, () => ShowSavedGames(true)));
        nav.Controls.Add(NavButton("⚙   Configuración SQL", false, InitializeDatabase));
        nav.Controls.Add(NavButton("▥   Estadísticas", false, ShowStatistics));
        nav.Controls.Add(NavButton("◇   Realidades", false, () => MessageBox.Show("Seleccionarás la realidad al comenzar la expedición.", "NEXUS")));
        nav.Controls.Add(NavButton("⏻   Salir", false, Close));
        sidebar.Controls.Add(nav);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22), BackColor = Color.FromArgb(9, 9, 27) };
        var hero = new Panel { Dock = DockStyle.Fill, BackColor = Panel, BackgroundImage = LoadArtwork(hubPath), BackgroundImageLayout = ImageLayout.Zoom, Padding = new Padding(28) };
        var welcome = new Label { Text = "BIENVENIDO A NEXUS", Dock = DockStyle.Top, Height = 62, ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        var subtitle = new Label { Text = "Una anomalía amenaza el equilibrio entre realidades.\r\nRegistra tu identidad y comienza la expedición.", Dock = DockStyle.Top, Height = 68, ForeColor = Color.FromArgb(222, 216, 245), Font = new Font("Segoe UI", 12), TextAlign = ContentAlignment.MiddleLeft };
        var badge = new Label { Text = "MISIÓN DISPONIBLE     //     LA ANOMALÍA PERDIDA", Dock = DockStyle.Bottom, Height = 44, ForeColor = Cyan, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        hero.Controls.Add(subtitle); hero.Controls.Add(welcome); hero.Controls.Add(badge);
        content.Controls.Add(hero);

        var footer = new Label { Text = "NEXUS SYSTEM v1.0     •     CONEXIÓN MULTIVERSAL ACTIVA", Dock = DockStyle.Bottom, Height = 34, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(130, 120, 170), Font = new Font("Segoe UI", 8, FontStyle.Bold), Padding = new Padding(24, 0, 0, 0) };
        page.Controls.Add(content); page.Controls.Add(sidebar); page.Controls.Add(top); page.Controls.Add(footer);
        ReplacePage(page);
    }

    private async void InitializeDatabase()
    {
        try
        {
            await persistence.InitializeAsync();
            MessageBox.Show("La base NEXUS quedó creada/verificada. Ya puedes guardar expediciones.", "NEXUS · SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo inicializar SQL Server. Verifica que SQL Server Express esté iniciado y que la cuenta de Windows pueda crear la base NEXUS.\r\n\r\n" + ex.Message, "NEXUS · conexión SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ShowStatistics()
    {
        try
        {
            var games = await persistence.ListAsync();
            int completed = games.Count(game => game.MissionProgress == 4);
            string recent = games.Count == 0 ? "Todavía no hay partidas guardadas." : string.Join("\r\n", games.Take(8).Select(game => $"• {game.Name} — {game.Explorer} — {game.Reality} — {game.MissionProgress}/4 objetivos — {game.UpdatedAt.ToLocalTime():g}"));
            MessageBox.Show($"Partidas guardadas: {games.Count}\r\nExpediciones con 4 objetivos: {completed}\r\n\r\n{recent}", "NEXUS · estadísticas", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudieron consultar las estadísticas. Inicializa la base de datos desde Configuración SQL.\r\n\r\n" + ex.Message, "NEXUS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task SaveCurrentGameAsync()
    {
        using var promptDialog = new Form { Text = "Guardar expedición", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(410, 140), MaximizeBox = false, MinimizeBox = false, BackColor = Panel, ForeColor = Color.White };
        var label = new Label { Text = "Nombre para esta partida", Left = 18, Top = 16, Width = 365, ForeColor = Color.White };
        var name = new TextBox { Text = $"Expedición {DateTime.Now:yyyy-MM-dd HHmm}", Left = 18, Top = 43, Width = 365, BackColor = Color.FromArgb(29, 24, 59), ForeColor = Color.White };
        var save = StyledButton("GUARDAR", Purple, Color.White, 120, 34); save.Left = 263; save.Top = 88; save.DialogResult = DialogResult.OK;
        var cancel = StyledButton("CANCELAR", Color.FromArgb(30, 22, 58), Color.White, 120, 34); cancel.Left = 133; cancel.Top = 88; cancel.DialogResult = DialogResult.Cancel;
        promptDialog.Controls.AddRange([label, name, cancel, save]); promptDialog.AcceptButton = save; promptDialog.CancelButton = cancel;
        if (promptDialog.ShowDialog(this) != DialogResult.OK) return;
        string saveName = name.Text.Trim();
        if (saveName.Length is < 1 or > 80) { MessageBox.Show("El nombre debe tener entre 1 y 80 caracteres.", "NEXUS"); return; }

        try
        {
            var snapshot = global::Program.CapturarPartida();
            await persistence.SaveAsync(saveName, snapshot);
            MessageBox.Show($"La expedición '{saveName}' se guardó correctamente en SQL Server.", "NEXUS · guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo guardar la partida. Si aún no lo hiciste, usa Configuración SQL en el menú principal.\r\n\r\n" + ex.Message, "NEXUS · guardado", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ShowSavedGames(bool allowContinue)
    {
        try
        {
            var saves = await persistence.ListAsync();
            if (saves.Count == 0) { MessageBox.Show("No hay partidas guardadas todavía.", "NEXUS"); return; }

            using var dialog = new Form { Text = "Partidas NEXUS", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(690, 390), MinimizeBox = false, MaximizeBox = false, BackColor = Panel, ForeColor = Color.White };
            var list = new ListBox { Left = 16, Top = 16, Width = 656, Height = 300, BackColor = Color.FromArgb(10, 9, 29), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            void RefreshList()
            {
                list.Items.Clear();
                foreach (var game in saves) list.Items.Add($"{game.Name}  |  {game.Explorer}  |  {game.Reality}  |  {game.Energy} energía  |  {game.MissionProgress}/4  |  {game.UpdatedAt.ToLocalTime():g}");
                if (list.Items.Count > 0) list.SelectedIndex = 0;
            }
            RefreshList();
            SavedGame Selected() => list.SelectedIndex >= 0 && list.SelectedIndex < saves.Count ? saves[list.SelectedIndex] : null;
            var close = StyledButton("CERRAR", Color.FromArgb(30, 22, 58), Color.White, 120, 36); close.Left = 536; close.Top = 330; close.DialogResult = DialogResult.Cancel;
            var delete = StyledButton("ELIMINAR", Color.FromArgb(104, 27, 61), Color.White, 120, 36); delete.Left = 405; delete.Top = 330;
            var continueButton = StyledButton("CONTINUAR", Purple, Color.White, 130, 36); continueButton.Left = 16; continueButton.Top = 330; continueButton.Enabled = allowContinue;
            delete.Click += async (_, _) =>
            {
                var selected = Selected(); if (selected == null) return;
                if (MessageBox.Show($"¿Eliminar la partida '{selected.Name}'?", "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                try { await persistence.DeleteAsync(selected.Id); saves.Remove(selected); RefreshList(); if (saves.Count == 0) dialog.Close(); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "NEXUS", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            continueButton.Click += async (_, _) =>
            {
                var selected = Selected(); if (selected == null) return;
                try
                {
                    var state = await persistence.LoadAsync(selected.Id);
                    dialog.Tag = state; dialog.DialogResult = DialogResult.OK; dialog.Close();
                }
                catch (Exception ex) { MessageBox.Show("No se pudo recuperar la partida.\r\n\r\n" + ex.Message, "NEXUS", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            dialog.Controls.AddRange([list, continueButton, delete, close]); dialog.CancelButton = close;
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Tag is GameSnapshot snapshot) StartGame(snapshot);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudieron leer las partidas. Inicializa la base de datos desde Configuración SQL.\r\n\r\n" + ex.Message, "NEXUS", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowExplorerRegistration()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Ink, BackgroundImage = LoadArtwork(hubPath), BackgroundImageLayout = ImageLayout.Stretch };
        var header = MakeSectionHeader("REGISTRO DEL EXPLORADOR", "IDENTIDAD · GÉNERO · CONDICIÓN INICIAL");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(18), BackColor = Color.FromArgb(200, 5, 7, 24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(13, 12, 33), Padding = new Padding(24), Margin = new Padding(0, 0, 12, 0) };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.Transparent };
        card.Controls.Add(stack);
        stack.Controls.Add(new Label { Text = "CONFIGURA TU EXPLORADOR", Width = 520, Height = 44, ForeColor = Color.White, Font = new Font("Segoe UI", 18, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        stack.Controls.Add(new Label { Text = "NOMBRE DEL JUGADOR", Width = 520, Height = 26, ForeColor = Cyan, Font = new Font("Segoe UI", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 6, 3, 0) });
        stack.Controls.Add(new Label { Text = explorerName, Width = 510, Height = 38, ForeColor = Color.White, BackColor = Color.FromArgb(26, 21, 53), Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) });
        stack.Controls.Add(new Label { Text = "SELECCIONA TU GÉNERO", Width = 520, Height = 28, ForeColor = Cyan, Font = new Font("Segoe UI", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 10, 3, 0) });

        var genderRow = new FlowLayoutPanel { Width = 520, Height = 48, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };
        var male = MakeRadio("HOMBRE", !selectedFemale);
        var female = MakeRadio("MUJER", selectedFemale);
        male.CheckedChanged += (_, _) => { if (male.Checked) selectedFemale = false; };
        female.CheckedChanged += (_, _) => { if (female.Checked) selectedFemale = true; };
        genderRow.Controls.Add(male); genderRow.Controls.Add(female); stack.Controls.Add(genderRow);

        stack.Controls.Add(new Label { Text = "DATOS DEL EXPLORADOR", Width = 520, Height = 28, ForeColor = Cyan, Font = new Font("Segoe UI", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 8, 3, 0) });
        var statsRow = new FlowLayoutPanel { Width = 520, Height = 82, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };
        var ageBox = MakeNumberField("EDAD", 1, 100, selectedAge);
        var fitnessBox = MakeNumberField("ESTADO FÍSICO", 0, 100, selectedFitness);
        statsRow.Controls.Add(ageBox); statsRow.Controls.Add(fitnessBox); stack.Controls.Add(statsRow);

        var actions = new FlowLayoutPanel { Width = 520, Height = 52, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, 14, 0, 0) };
        var back = StyledButton("← VOLVER", Color.FromArgb(30, 22, 58), Color.White, 150, 42);
        back.Click += (_, _) => ShowHub();
        var next = StyledButton("SIGUIENTE: PERSONAJE  →", Purple, Color.White, 280, 42);
        next.Click += (_, _) =>
        {
            selectedAge = (int)ageBox.Controls.OfType<NumericUpDown>().First().Value;
            selectedFitness = (int)fitnessBox.Controls.OfType<NumericUpDown>().First().Value;
            ShowCharacterSelection();
        };
        actions.Controls.Add(back); actions.Controls.Add(next); stack.Controls.Add(actions);

        var art = new PictureBox { Dock = DockStyle.Fill, BackColor = Ink, SizeMode = PictureBoxSizeMode.Zoom, Image = LoadArtwork(gameplayArtPath), Margin = new Padding(12, 0, 0, 0) };
        layout.Controls.Add(card, 0, 0); layout.Controls.Add(art, 1, 0);
        page.Controls.Add(layout); page.Controls.Add(header);
        ReplacePage(page);
        AcceptButton = next;
    }

    private void ShowCharacterSelection()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Ink, BackgroundImage = LoadArtwork(hubPath), BackgroundImageLayout = ImageLayout.Stretch };
        var header = MakeSectionHeader("SELECCIONA TU PERSONAJE", $"EXPLORADOR: {explorerName.ToUpperInvariant()}   ·   {(selectedFemale ? "MUJER" : "HOMBRE")}   ·   {selectedAge} AÑOS");
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Padding = new Padding(18, 12, 18, 12), BackColor = Color.FromArgb(215, 5, 6, 22) };
        for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        var humanoImagen = selectedFemale ? "humana-mujer.png" : "humano-hombre.png";
        AddCharacterCard(cards, 0, 1, selectedFemale ? "HUMANA" : "HUMANO", "Explorador NEXUS, listo para la aventura.", "portrait-" + humanoImagen, Color.FromArgb(34, 160, 255));
        AddCharacterCard(cards, 1, 2, "ROBOT ALIADO", "Unidad tecnológica con escudo reforzado.", "portrait-robot-aliado.png", Color.FromArgb(36, 219, 151));
        AddCharacterCard(cards, 2, 3, "HUMANO ENEMIGO", "Entidad hostil con gran capacidad de ataque.", "portrait-humano-enemigo.png", Color.FromArgb(255, 65, 91));
        AddCharacterCard(cards, 3, 4, "ROBOT ENEMIGO", "Máquina de combate con blindaje pesado.", "portrait-robot-enemigo.png", Color.FromArgb(255, 169, 43));

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 66, BackColor = Color.FromArgb(235, 8, 7, 26), Padding = new Padding(20, 10, 20, 10) };
        var back = StyledButton("← VOLVER", Color.FromArgb(30, 22, 58), Color.White, 145, 42);
        back.Dock = DockStyle.Left; back.Click += (_, _) => ShowExplorerRegistration();
        var confirm = StyledButton("CONFIRMAR PERSONAJE  ✓", Purple, Color.White, 270, 42);
        confirm.Dock = DockStyle.Right;
        confirm.Click += (_, _) => StartGame();
        footer.Controls.Add(confirm); footer.Controls.Add(back);
        page.Controls.Add(cards); page.Controls.Add(footer); page.Controls.Add(header);
        ReplacePage(page);
        AcceptButton = confirm;
    }

    private void AddCharacterCard(TableLayoutPanel grid, int column, int characterId, string name, string description, string imageName, Color accent)
    {
        var frame = new Panel { Dock = DockStyle.Fill, BackColor = accent, Padding = new Padding(2), Margin = new Padding(8, 8, 8, 10), Cursor = Cursors.Hand };
        var surface = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(13, 11, 32), Padding = new Padding(8), Cursor = Cursors.Hand };
        var portrait = new PictureBox { Dock = DockStyle.Top, Height = 320, BackColor = Ink, SizeMode = PictureBoxSizeMode.Zoom, Image = LoadArtwork(Path.Combine(charactersPath, imageName)), Cursor = Cursors.Hand };
        var title = new Label { Text = name, Dock = DockStyle.Top, Height = 42, ForeColor = accent, Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
        var details = new Label { Text = description, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(226, 221, 244), Font = new Font("Segoe UI", 9), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
        var choose = StyledButton(characterId == selectedCharacter ? "SELECCIONADO ✓" : "SELECCIONAR", Color.FromArgb(41, 22, 83), Color.White, 180, 38);
        choose.Dock = DockStyle.Bottom;
        surface.Controls.Add(details); surface.Controls.Add(choose); surface.Controls.Add(title); surface.Controls.Add(portrait);
        frame.Controls.Add(surface);
        void Select() { selectedCharacter = characterId; ShowCharacterSelection(); }
        frame.Click += (_, _) => Select(); portrait.Click += (_, _) => Select(); title.Click += (_, _) => Select(); details.Click += (_, _) => Select(); choose.Click += (_, _) => Select();
        frame.Paint += (_, e) => { if (selectedCharacter == characterId) { using var pen = new Pen(Color.White, 2); e.Graphics.DrawRectangle(pen, 1, 1, frame.Width - 3, frame.Height - 3); } };
        grid.Controls.Add(frame, column, 0);
    }

    private static Label MakeSectionHeader(string titleText, string subtitleText)
    {
        var header = new Label { Text = "NEXUS   //   " + titleText + "\r\n" + subtitleText, Dock = DockStyle.Top, Height = 74, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(28, 0, 0, 0), ForeColor = Color.White, BackColor = Color.FromArgb(238, 13, 9, 37), Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        return header;
    }

    private static RadioButton MakeRadio(string text, bool selected)
    {
        return new RadioButton { Text = text, Checked = selected, Width = 180, Height = 38, Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(29, 23, 60), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 0, 16, 0) };
    }

    private static Panel MakeNumberField(string title, int minimum, int maximum, int value)
    {
        var panel = new Panel { Width = 235, Height = 74, BackColor = Color.Transparent, Margin = new Padding(0, 0, 16, 0) };
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 24, ForeColor = Color.FromArgb(190, 180, 225), Font = new Font("Segoe UI", 8, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        var number = new NumericUpDown { Minimum = minimum, Maximum = maximum, Value = Math.Clamp(value, minimum, maximum), Width = 210, Height = 34, Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(29, 23, 60), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Left = 0, Top = 28 };
        panel.Controls.Add(number); return panel;
    }

    private void StartGame(GameSnapshot restored = null)
    {
        AcceptButton = null;
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        var title = new Label { Text = "N E X U S   //   CENTRO DE OPERACIONES                                      EXPLORADOR: " + explorerName.ToUpperInvariant(), Dock = DockStyle.Top, Height = 52, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0), Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(24, 15, 48) };

        output.Dock = DockStyle.Fill; output.ReadOnly = true; output.BackColor = Color.FromArgb(7, 8, 25); output.ForeColor = Color.FromArgb(231, 227, 250); output.BorderStyle = BorderStyle.None; output.Font = new Font("Cascadia Mono", 9.5f); output.WordWrap = true; output.DetectUrls = false; output.HideSelection = false;
        var outputFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3), BackColor = Purple, Margin = new Padding(12, 10, 6, 8) };
        outputFrame.Controls.Add(output);
        var artFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2), BackColor = Purple, Margin = new Padding(6, 10, 12, 8) };
        var gameplayArt = new PictureBox { Dock = DockStyle.Fill, BackColor = Ink, SizeMode = PictureBoxSizeMode.Zoom, Image = LoadArtwork(gameplayArtPath) };
        artFrame.Controls.Add(gameplayArt);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.FromArgb(8, 7, 25), Padding = new Padding(6, 4, 6, 4) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        body.Controls.Add(outputFrame, 0, 0);
        body.Controls.Add(artFrame, 1, 0);
        prompt.Text = "Responde aquí a las indicaciones del juego."; prompt.Dock = DockStyle.Top; prompt.Height = 28; prompt.ForeColor = Color.FromArgb(182, 165, 225); prompt.Padding = new Padding(2, 5, 0, 0);
        input.Dock = DockStyle.Fill; input.Font = new Font("Segoe UI", 11); input.BackColor = Color.FromArgb(24, 20, 47); input.ForeColor = Color.White; input.BorderStyle = BorderStyle.FixedSingle;
        input.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { SubmitInput(); e.SuppressKeyPress = true; } };
        var send = StyledButton("Enviar", Purple, Color.White, 105, 36); send.Dock = DockStyle.Right; send.Click += (_, _) => SubmitInput();
        var inputRow = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(0, 4, 0, 0) }; inputRow.Controls.Add(input); inputRow.Controls.Add(send);
        var entry = new Panel { Dock = DockStyle.Bottom, Height = 78, Padding = new Padding(16, 5, 16, 12), BackColor = Color.FromArgb(12, 10, 31) }; entry.Controls.Add(inputRow); entry.Controls.Add(prompt);
        var quick = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(12, 5, 12, 5), BackColor = Color.FromArgb(12, 10, 31), WrapContents = false };
        AddQuickButton(quick, "↑ W", "w"); AddQuickButton(quick, "← A", "a"); AddQuickButton(quick, "↓ S", "s"); AddQuickButton(quick, "→ D", "d"); AddQuickButton(quick, "Inventario (I)", "i"); AddQuickButton(quick, "Misión (M)", "m"); AddQuickButton(quick, "Menú (Q)", "q");
        var saveButton = StyledButton("Guardar partida", Color.FromArgb(41, 22, 83), Color.White, 150, 32);
        saveButton.AutoSize = true; saveButton.Margin = new Padding(4, 2, 4, 2);
        saveButton.Click += async (_, _) => await SaveCurrentGameAsync();
        quick.Controls.Add(saveButton);
        var voiceButton = StyledButton(IrisSpeechService.Enabled ? "Voz IRIS: ON" : "Voz IRIS: OFF", Color.FromArgb(31, 21, 62), Color.FromArgb(221, 208, 255), 115, 32);
        voiceButton.AutoSize = true; voiceButton.Margin = new Padding(4, 2, 4, 2);
        voiceButton.Click += (_, _) => voiceButton.Text = IrisSpeechService.Toggle() ? "Voz IRIS: ON" : "Voz IRIS: OFF";
        quick.Controls.Add(voiceButton);
        page.Controls.Add(body); page.Controls.Add(entry); page.Controls.Add(quick); page.Controls.Add(title);
        ReplacePage(page);

        UiConsole.Attach(this);
        if (restored == null)
        {
            global::Program.ConfigurarExploradorDesdeInterfaz(explorerName, selectedFemale, selectedAge, selectedFitness, selectedCharacter);
            Append("NEXUS // CONEXIÓN ESTABLECIDA\r\nBienvenido, " + explorerName + ". Perfil registrado: " + (selectedFemale ? "MUJER" : "HOMBRE") + ", " + selectedAge + " años.\r\nPersonaje seleccionado: " + selectedCharacter + ".\r\n\r\n");
        }
        else
        {
            explorerName = restored.ExplorerName;
            global::Program.RestaurarPartida(restored);
            Append("NEXUS // PARTIDA RECUPERADA\r\nExpedición de " + restored.ExplorerName + " · " + restored.Reality + " · energía " + restored.Energy + ".\r\n\r\n");
        }
        var gameThread = new Thread(() => { try { global::Program.Main(); } catch (Exception ex) { UiConsole.WriteLine("\r\n[ERROR] " + ex.Message); } }) { IsBackground = true, Name = "NEXUS game" };
        gameThread.Start();
    }

    private static Image LoadArtwork(string imagePath)
    {
        if (!File.Exists(imagePath)) return null;
        using var source = Image.FromFile(imagePath);
        return new Bitmap(source);
    }

    private static Control Spacer(int height) => new Panel { Height = height, Width = 180, BackColor = Color.Transparent };

    private static Button NavButton(string text, bool primary, Action action)
    {
        var b = new Button { Text = text, Width = 184, Height = primary ? 48 : 42, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), FlatStyle = FlatStyle.Flat, BackColor = primary ? Color.FromArgb(107, 24, 217) : Color.FromArgb(20, 16, 45), ForeColor = Color.White, Font = new Font("Segoe UI", primary ? 11 : 10, primary ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 0, 0, 8), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = primary ? Purple : Color.FromArgb(57, 42, 100); b.Click += (_, _) => action(); return b;
    }

    private static Button StyledButton(string text, Color back, Color fore, int width, int height)
    {
        var b = new Button { Text = text, Width = width, Height = height, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = Color.FromArgb(195, 117, 255); return b;
    }

    private void AddQuickButton(FlowLayoutPanel panel, string caption, string value)
    {
        var button = StyledButton(caption, Color.FromArgb(31, 21, 62), Color.FromArgb(221, 208, 255), 110, 32);
        button.AutoSize = true; button.Margin = new Padding(4, 2, 4, 2); button.Click += (_, _) => QueueInput(value); panel.Controls.Add(button);
    }

    private void SubmitInput() { string value = input.Text; input.Clear(); QueueInput(value); }
    private void QueueInput(string value)
    {
        lock (inputLock) { submitted.Enqueue(value ?? ""); inputReady.Set(); }
        input.Focus();
    }

    internal string ReadUserInput()
    {
        if (closing) return "";
        Ui(() => { prompt.Text = "NEXUS solicita una respuesta — escribe y pulsa Enter."; input.Focus(); });
        while (true)
        {
            if (closing) return "";
            lock (inputLock)
            {
                if (submitted.Count > 0)
                {
                    string line = submitted.Dequeue();
                    if (submitted.Count == 0) inputReady.Reset();
                    return line;
                }
                inputReady.Reset();
            }
            inputReady.Wait();
        }
    }

    internal void Append(string text)
    {
        if (closing || IsDisposed) return;
        Ui(() => { output.AppendText(text); output.SelectionStart = output.TextLength; output.ScrollToCaret(); });
    }

    internal void ClearOutput() { if (!closing && !IsDisposed) Ui(() => output.Clear()); }

    private void ReplacePage(Control page)
    {
        Controls.Clear(); page.Dock = DockStyle.Fill; Controls.Add(page); page.BringToFront();
    }

    private void Ui(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) Invoke(action); else action();
    }
}

internal static class UiConsole
{
    private static NexusForm form;
    internal static void Attach(NexusForm target) => form = target;
    internal static void Write(string value) => form?.Append(value);
    internal static void WriteLine(string value = "")
    {
        form?.Append(value + Environment.NewLine);
        string trimmed = value.TrimStart();
        if (trimmed.StartsWith("IRIS >", StringComparison.OrdinalIgnoreCase))
            IrisSpeechService.Speak(trimmed[6..].Trim());
    }
    internal static string ReadLine() => form?.ReadUserInput() ?? "";
    internal static void Clear() => form?.ClearOutput();
}

internal sealed class NexusProgressBar : Control
{
    private int value;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => value;
        set { this.value = Math.Clamp(value, 0, 100); Invalidate(); }
    }

    public NexusProgressBar()
    {
        Height = 18;
        MinimumSize = new Size(100, 18);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var background = new SolidBrush(Color.FromArgb(23, 17, 47));
        e.Graphics.FillRectangle(background, bounds);
        int fillWidth = (int)((Width - 4) * (value / 100f));
        if (fillWidth > 0)
        {
            using var gradient = new LinearGradientBrush(new Rectangle(2, 2, fillWidth, Math.Max(1, Height - 4)), Color.FromArgb(110, 32, 227), Color.FromArgb(69, 220, 255), LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(gradient, 2, 2, fillWidth, Math.Max(1, Height - 4));
        }
        using var border = new Pen(Color.FromArgb(154, 92, 255));
        e.Graphics.DrawRectangle(border, bounds);
    }
}
