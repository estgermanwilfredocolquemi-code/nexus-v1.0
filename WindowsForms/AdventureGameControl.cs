using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace NexusWinForms;

/// <summary>The graphical, four-reality adventure layer for the original NEXUS game.</summary>
internal sealed class AdventureGameControl : UserControl
{
    private static readonly Color Ink = Color.FromArgb(8, 12, 27);
    private static readonly Color PanelColor = Color.FromArgb(17, 29, 49);
    private static readonly Color Cyan = Color.FromArgb(119, 226, 255);
    private static readonly string SaveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NEXUS", "Partidas");
    private static readonly string[] Realities = ["TIERRA-01", "TIERRA-02", "TIERRA-07", "NEXUS-09"];
    private static readonly string[] WorldNames = ["TIERRA-01 · BOSQUE VIVO", "TIERRA-02 · ZONA ZOMBI", "TIERRA-07 · FRONTERA CRIOGÉNICA", "NEXUS-09 · CIUDAD DIGITAL"];
    private static readonly string[] WorldLore = [
        "Bosques antiguos, caminos de piedra y un puesto de exploración. Encuentra la primera llave dimensional.",
        "Una ciudad evacuada quedó cubierta por la infección. Los robots de contención ya no distinguen aliados.",
        "La nieve oculta las ruinas de una estación científica. Sus guardianes protegen el núcleo de salto.",
        "La ciudad central de NEXUS está en manos de una inteligencia hostil. Derrota al Titán para cerrar la anomalía."];
    private const int MapWidth = 32;
    private const int MapHeight = 20;
    private static readonly string[][] Maps = BuildMaps();

    internal sealed class SaveData
    {
        public string Name { get; set; } = "Expedición";
        public string Explorer { get; set; } = "Explorador";
        public bool Female { get; set; }
        public int CharacterId { get; set; } = 1;
        public int World { get; set; } = 1;
        public int X { get; set; } = 2;
        public int Y { get; set; } = 10;
        public int Hp { get; set; } = 100;
        public int MaxHp { get; set; } = 100;
        public int Coins { get; set; } = 25;
        public int EnemiesDefeated { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; } = 1;
        public List<string> Inventory { get; set; } = ["Batería de campo", "Poción médica"];
        public string Weapon { get; set; } = "Hoja de supervivencia";
        public string Armor { get; set; } = "Uniforme NEXUS";
        public List<string> CompletedMissions { get; set; } = [];
        public List<string> OpenedChests { get; set; } = [];
        public List<string> DefeatedBosses { get; set; } = [];
        public List<string> ClearedEnemies { get; set; } = [];
        public bool InDungeon { get; set; }
        public int DungeonX { get; set; } = 1;
        public int DungeonY { get; set; } = 6;
        public DateTime Updated { get; set; } = DateTime.Now;
    }

    internal sealed record SaveSummary(string Name, string Explorer, int World, int Level, int Hp, int MaxHp, int Coins, string Weapon, string Armor, DateTime Updated);

    private sealed class EnemyData
    {
        public string Name { get; init; }
        public int Hp { get; set; }
        public int MaxHp { get; init; }
        public bool Boss { get; init; }
        public string Key { get; init; }
    }

    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 34, ForeColor = Cyan, BackColor = Color.FromArgb(12, 19, 35), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
    private readonly Label hud = new() { Dock = DockStyle.Top, Height = 54, ForeColor = Color.White, BackColor = Color.FromArgb(13, 20, 39), Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
    private readonly FlowLayoutPanel toolbar = new() { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(10, 16, 32), Padding = new Padding(8, 6, 8, 4), WrapContents = false, AutoScroll = true };
    private readonly GameCanvas canvas;
    private readonly Action returnToMenu;
    private readonly Image heroPortrait;
    private SaveData data;
    private string mode = "map";
    private string notice = "Explora con WASD o flechas · E interactuar · I inventario · M misiones · Esc pausa";
    private EnemyData enemy;
    private Panel overlay;

    private static readonly string[] Dungeon = [
        "##############", "#....#.......#", "#....#..##...#", "#.......##...#", "###.#........#", "#...#..###...#", "#......#.....#", "#..##........#", "#............#", "##############"];

    private sealed class GameCanvas : Control
    {
        private readonly AdventureGameControl owner;
        internal GameCanvas(AdventureGameControl owner)
        {
            this.owner = owner; Dock = DockStyle.Fill; TabStop = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            MouseDown += (_, _) => Focus();
        }
        protected override void OnPaint(PaintEventArgs e) => owner.DrawWorld(e.Graphics, ClientRectangle);
        protected override void OnKeyDown(KeyEventArgs e) { owner.HandleKey(e); base.OnKeyDown(e); }
    }

    internal AdventureGameControl(string explorer, bool female, int character, Action returnToMenu, SaveData loaded = null, int startingWorld = 1)
    {
        this.returnToMenu = returnToMenu;
        Dock = DockStyle.Fill;
        data = loaded ?? new SaveData { Explorer = explorer, Female = female, CharacterId = character, World = Math.Clamp(startingWorld, 1, 4) };
        data.Explorer = string.IsNullOrWhiteSpace(data.Explorer) ? explorer : data.Explorer;
        data.World = Math.Clamp(data.World, 1, 4); data.X = Math.Clamp(data.X, 1, MapWidth - 2); data.Y = Math.Clamp(data.Y, 1, MapHeight - 2);
        data.Level = Math.Max(1, data.Level); data.MaxHp = Math.Max(100, data.MaxHp); data.Hp = Math.Clamp(data.Hp, 1, data.MaxHp);
        data.Inventory ??= []; data.CompletedMissions ??= []; data.OpenedChests ??= []; data.DefeatedBosses ??= []; data.ClearedEnemies ??= [];
        string portrait = data.CharacterId switch { 2 => "portrait-robot-aliado.png", 3 => "portrait-humano-enemigo.png", 4 => "portrait-robot-enemigo.png", _ => data.Female ? "portrait-humana-mujer.png" : "portrait-humano-hombre.png" };
        string portraitPath = Path.Combine(AppContext.BaseDirectory, portrait);
        if (File.Exists(portraitPath)) { using var image = Image.FromFile(portraitPath); heroPortrait = new Bitmap(image); }
        canvas = new GameCanvas(this);
        var header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(10, 16, 32) };
        header.Controls.Add(new Label { Text = "N E X U S   //   MULTIVERSO", Dock = DockStyle.Left, Width = 330, ForeColor = Cyan, Font = new Font("Segoe UI", 15, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) });
        header.Controls.Add(new Label { Text = "EXPLORADOR: " + data.Explorer.ToUpperInvariant(), Dock = DockStyle.Fill, ForeColor = Color.FromArgb(195, 210, 235), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 18, 0) });
        AddTool("Guardar", () => SaveCurrent()); AddTool("Inventario [I]", ShowInventory); AddTool("Equipo", ShowEquipment); AddTool("Misiones [M]", ShowMissions); AddTool("Mapa", ShowMapInfo); AddTool("Pausa [Esc]", ShowPause); AddTool("Menú principal", () => ConfirmReturn());
        Controls.Add(canvas); Controls.Add(status); Controls.Add(hud); Controls.Add(toolbar); Controls.Add(header);
        UpdateHud(); status.Text = notice;
        ShownFocus();
    }

    private void ShownFocus() { if (IsHandleCreated) canvas.Focus(); else HandleCreated += (_, _) => canvas.Focus(); }
    private void AddTool(string text, Action action)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(31, 48, 75), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(4, 0, 4, 0), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = Color.FromArgb(75, 135, 170); b.Click += (_, _) => action(); toolbar.Controls.Add(b);
    }

    internal static List<SaveSummary> ListSaves()
    {
        Directory.CreateDirectory(SaveFolder);
        return Directory.GetFiles(SaveFolder, "*.json").Select(path =>
        {
            try
            {
                var s = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path));
                return s == null ? null : new SaveSummary(s.Name, s.Explorer, Math.Clamp(s.World, 1, 4), Math.Max(1, s.Level), Math.Max(0, s.Hp), Math.Max(1, s.MaxHp), Math.Max(0, s.Coins), s.Weapon ?? "Sin arma", s.Armor ?? "Sin protección", s.Updated);
            }
            catch { return null; }
        }).Where(x => x != null).OrderByDescending(x => x.Updated).ToList();
    }
    internal static SaveData LoadSave(string name)
    {
        Directory.CreateDirectory(SaveFolder);
        foreach (var path in Directory.GetFiles(SaveFolder, "*.json")) { try { var s = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path)); if (s?.Name == name) return s; } catch { } }
        return null;
    }
    internal static void DeleteSave(string name)
    {
        foreach (var path in Directory.GetFiles(SaveFolder, "*.json")) { try { var s = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path)); if (s?.Name == name) File.Delete(path); } catch { } }
    }

    private bool SaveCurrent()
    {
        using var dialog = new Form { Text = "Guardar expedición", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, ClientSize = new Size(460, 235), BackColor = PanelColor, ForeColor = Color.White, MaximizeBox = false, MinimizeBox = false };
        var label = new Label { Text = "Nombre de la partida", Left = 18, Top = 16, Width = 420, ForeColor = Cyan, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        var input = new TextBox { Text = data.Name, Left = 18, Top = 46, Width = 420, BackColor = Color.FromArgb(29, 43, 67), ForeColor = Color.White };
        var details = new Label { Text = $"{WorldNames[data.World - 1]}   ·   NIVEL {data.Level}\r\nSalud {data.Hp}/{data.MaxHp}   ·   Créditos {data.Coins}\r\nArma: {data.Weapon}   ·   Protección: {data.Armor}", Left = 18, Top = 82, Width = 420, Height = 76, ForeColor = Color.FromArgb(205, 219, 239), Font = new Font("Segoe UI", 9) };
        var cancel = MakeButton("CANCELAR", 115, 36); cancel.Left = 177; cancel.Top = 176; cancel.DialogResult = DialogResult.Cancel;
        var save = MakeButton("GUARDAR", 125, 36); save.Left = 313; save.Top = 176; save.DialogResult = DialogResult.OK;
        dialog.Controls.AddRange([label, input, details, cancel, save]); dialog.AcceptButton = save; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return false;
        string name = input.Text.Trim();
        if (name.Length is < 1 or > 60) { MessageBox.Show("Usa un nombre de 1 a 60 caracteres.", "NEXUS"); return false; }
        data.Name = name; data.Updated = DateTime.Now; Directory.CreateDirectory(SaveFolder);
        string safe = string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        string savePath = Path.Combine(SaveFolder, safe + ".json");
        if (File.Exists(savePath) && MessageBox.Show($"Ya existe una partida llamada '{name}'. ¿Quieres reemplazarla?", "NEXUS · confirmar reemplazo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return false;
        File.WriteAllText(savePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        SetNotice($"Partida '{data.Name}' guardada · {WorldNames[data.World - 1]} · nivel {data.Level}.");
        MessageBox.Show($"Partida guardada correctamente.\r\n\r\n{data.Name}\r\n{WorldNames[data.World - 1]} · Nivel {data.Level}\r\nSalud {data.Hp}/{data.MaxHp} · {data.Coins} créditos", "NEXUS · guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        canvas.Focus();
        return true;
    }

    private void ShowInventory()
    {
        ShowOverlay("INVENTARIO", $"Nivel {data.Level} · {data.Experience}/{ExperienceToNext()} XP · créditos {data.Coins}", panel =>
        {
            var list = MakeList();
            foreach (string item in data.Inventory) list.Items.Add("◆  " + item);
            if (list.Items.Count == 0) list.Items.Add("El inventario está vacío.");
            panel.Controls.Add(list);
            panel.Controls.Add(MakeAction("USAR OBJETO SELECCIONADO", () =>
            {
                int index = list.SelectedIndex; if (index < 0 || index >= data.Inventory.Count) return;
                string item = data.Inventory[index];
                if (item.Contains("Poción") || item.Contains("Batería") || item.Contains("Botiquín")) { int heal = item.Contains("Botiquín") ? 45 : 30; data.Hp = Math.Min(data.MaxHp, data.Hp + heal); data.Inventory.RemoveAt(index); CloseOverlay(); SetNotice($"Usaste {item}; recuperaste {heal} de salud."); }
                else if (item.Contains("Espada") || item.Contains("Rifle") || item.Contains("Lanza") || item.Contains("Hoja")) { data.Weapon = item; CloseOverlay(); SetNotice("Arma equipada: " + item); }
                else if (item.Contains("Armadura") || item.Contains("Blindaje") || item.Contains("Traje")) { data.Armor = item; CloseOverlay(); SetNotice("Protección equipada: " + item); }
                else { SetNotice("Ese objeto se conserva para una misión."); }
                UpdateHud(); canvas.Invalidate();
            }));
        });
    }

    private void ShowEquipment() => ShowOverlay("EQUIPAMIENTO", "El equipo cambia tu poder de ataque y defensa.", panel =>
    {
        var text = new Label { Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI", 13), Text = $"ARMA ACTUAL\r\n{data.Weapon}\r\n\r\nPROTECCIÓN ACTUAL\r\n{data.Armor}\r\n\r\nAtaque: {AttackPower()}   ·   Defensa: {DefensePower()}", TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(text);
        panel.Controls.Add(MakeAction("EQUIPAR OBJETO DEL INVENTARIO", () => { CloseOverlay(); ShowInventory(); }));
    });

    private void ShowMissions() => ShowOverlay("DIARIO DE MISIÓN", "Objetivo principal y encargos secundarios de las cuatro realidades.", panel =>
    {
        var list = MakeList();
        for (int w = 1; w <= 4; w++)
        {
            string key = "main-" + w;
            bool mainComplete = data.CompletedMissions.Contains(key) || data.DefeatedBosses.Contains("boss-" + w);
            list.Items.Add((mainComplete ? "✓" : "○") + $" PRINCIPAL · {WorldNames[w - 1]} · {(mainComplete ? "Jefe derrotado, portal abierto" : "Derrota al jefe de la mazmorra")}");
            string objective = w switch { 1 => "Abre un cofre de suministros", 2 => "Derrota 3 robots de esta zona", 3 => "Recupera el cofre de la mazmorra", _ => "Derrota al Titán NEXUS" };
            bool sideComplete = data.CompletedMissions.Contains("side-" + w);
            list.Items.Add((sideComplete ? "✓" : "○") + $" SECUNDARIA · {objective} · {SecondaryProgress(w)}");
            list.Items.Add($"   Premio: {SecondaryReward(w)} + 30 créditos + 40 XP");
        }
        list.Items.Add("Progreso de campaña: " + data.DefeatedBosses.Count + "/4 jefes · " + data.EnemiesDefeated + " enemigos derrotados · " + data.OpenedChests.Count + " cofres abiertos");
        panel.Controls.Add(list);
    });

    private void ShowMapInfo() => ShowOverlay("MAPA DE REALIDADES", "Cuatro mundos conectados. Los portales se activan al derrotar al guardián de cada mazmorra.", panel =>
    {
        var list = MakeList();
        for (int i = 0; i < 4; i++) list.Items.Add($"MUNDO {i + 1} · {WorldNames[i]}\r\n{WorldLore[i]}");
        panel.Controls.Add(list);
    });

    private void ShowPause() => ShowOverlay("JUEGO EN PAUSA", "Tu expedición queda en su estado actual.", panel =>
    {
        panel.Controls.Add(MakeAction("GUARDAR PARTIDA", () => SaveCurrent()));
        panel.Controls.Add(MakeAction("CONTINUAR", CloseOverlay));
        panel.Controls.Add(MakeAction("VOLVER AL MENÚ", ConfirmReturn));
    });

    private void ShowOverlay(string title, string subtitle, Action<Panel> build)
    {
        CloseOverlay();
        overlay = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(225, 5, 10, 25), Padding = new Padding(30) };
        var card = new Panel { Width = 640, Height = 440, BackColor = PanelColor, Padding = new Padding(22) };
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 44, ForeColor = Cyan, Font = new Font("Segoe UI", 18, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Top, Height = 54, ForeColor = Color.FromArgb(198, 215, 234), Font = new Font("Segoe UI", 9) });
        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        card.Controls.Add(content);
        var close = MakeAction("CERRAR", CloseOverlay); close.Dock = DockStyle.Bottom; close.Height = 38; card.Controls.Add(close);
        overlay.Controls.Add(card);
        void Center() => card.Location = new Point(Math.Max(0, (overlay.ClientSize.Width - card.Width) / 2), Math.Max(0, (overlay.ClientSize.Height - card.Height) / 2));
        overlay.Resize += (_, _) => Center(); Center(); Controls.Add(overlay); overlay.BringToFront(); build(content);
    }
    private ListBox MakeList() => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 22, 39), ForeColor = Color.White, Font = new Font("Segoe UI", 11), BorderStyle = BorderStyle.None, IntegralHeight = false };
    private Button MakeAction(string text, Action action) { var b = MakeButton(text, 250, 38); b.Dock = DockStyle.Bottom; b.Click += (_, _) => action(); return b; }
    private static Button MakeButton(string text, int width, int height) => new() { Text = text, Width = width, Height = height, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(77, 39, 145), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
    private void CloseOverlay() { if (overlay != null) { Controls.Remove(overlay); overlay.Dispose(); overlay = null; canvas.Focus(); } }
    private void ConfirmReturn()
    {
        var result = MessageBox.Show("¿Guardar la expedición antes de volver al menú principal?", "NEXUS", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (result == DialogResult.Cancel) return;
        if (result == DialogResult.Yes && !SaveCurrent()) return;
        CloseOverlay(); returnToMenu();
    }

    private void HandleKey(KeyEventArgs e)
    {
        if (overlay != null) { if (e.KeyCode == Keys.Escape) CloseOverlay(); return; }
        if (e.KeyCode == Keys.I) { ShowInventory(); e.SuppressKeyPress = true; return; }
        if (e.KeyCode == Keys.M) { ShowMissions(); e.SuppressKeyPress = true; return; }
        if (e.KeyCode == Keys.Escape) { ShowPause(); e.SuppressKeyPress = true; return; }
        if (mode == "battle")
        {
            if (e.KeyCode is Keys.D1 or Keys.NumPad1) BattleAction(1);
            else if (e.KeyCode is Keys.D2 or Keys.NumPad2) BattleAction(2);
            else if (e.KeyCode is Keys.D3 or Keys.NumPad3) BattleAction(3);
            e.SuppressKeyPress = true; return;
        }
        int dx = 0, dy = 0;
        if (e.KeyCode is Keys.Up or Keys.W) dy = -1;
        else if (e.KeyCode is Keys.Down or Keys.S) dy = 1;
        else if (e.KeyCode is Keys.Left or Keys.A) dx = -1;
        else if (e.KeyCode is Keys.Right or Keys.D) dx = 1;
        else if (e.KeyCode == Keys.E) { Interact(); e.SuppressKeyPress = true; return; }
        else return;
        MovePlayer(dx, dy); e.SuppressKeyPress = true;
    }

    internal void ProcessGameKey(KeyEventArgs e) => HandleKey(e);
    internal void FocusGame() { if (canvas.CanFocus) canvas.Focus(); }

    private void MovePlayer(int dx, int dy)
    {
        int x = data.InDungeon ? data.DungeonX : data.X, y = data.InDungeon ? data.DungeonY : data.Y;
        int nx = x + dx, ny = y + dy;
        string[] map = data.InDungeon ? Dungeon : Maps[data.World - 1];
        if (ny < 0 || nx < 0 || ny >= map.Length || nx >= map[ny].Length || map[ny][nx] is '#' or 'T' or '~' or 'H') return;
        if (data.InDungeon) { data.DungeonX = nx; data.DungeonY = ny; }
        else { data.X = nx; data.Y = ny; }
        char tile = map[ny][nx];
        if (tile == 'E' && !data.InDungeon) StartBattle(false, "field-" + data.World + "-" + nx + "-" + ny);
        else if (tile == 'B' && !data.InDungeon) StartBattle(true, "guardian-" + data.World);
        else if (tile == 'C' && !data.InDungeon) OpenChest("field-" + data.World + "-" + nx + "-" + ny);
        else if (data.InDungeon && nx == 12 && ny == 8) Interact();
        UpdateHud(); canvas.Invalidate();
    }

    private void Interact()
    {
        if (data.InDungeon)
        {
            if (data.DungeonX <= 1 && data.DungeonY >= 6) { data.InDungeon = false; SetNotice("Saliste de la mazmorra."); }
            else if (data.DungeonX == 10 && data.DungeonY == 1) OpenChest("dungeon-" + data.World);
            else if (data.DungeonX == 10 && data.DungeonY == 7 && !data.DefeatedBosses.Contains("boss-" + data.World)) StartBattle(true, "boss-" + data.World);
            else if (data.DungeonX == 10 && data.DungeonY == 7) SetNotice("La mazmorra quedó despejada. Regresa a la salida.");
        }
        else
        {
            char here = Maps[data.World - 1][data.Y][data.X];
            if (here == 'D') { data.InDungeon = true; data.DungeonX = 1; data.DungeonY = 6; SetNotice("Entraste a la mazmorra. Explora, abre el cofre y derrota al jefe."); }
            else if (here == 'Q') GiveQuest();
            else if (here == 'P') AdvanceWorld();
            else if (here == 'C') OpenChest("field-" + data.World + "-" + data.X + "-" + data.Y);
            else if (here == 'H') SetNotice("Refugio seguro. Aquí puedes reorganizar tu equipo.");
            else SetNotice("No hay nada que activar aquí. Busca el símbolo de misión, los cofres y la mazmorra.");
        }
        UpdateHud(); canvas.Invalidate();
    }

    private void GiveQuest()
    {
        string key = "side-" + data.World;
        if (data.CompletedMissions.Contains(key)) { SetNotice("La misión secundaria de esta realidad ya está completa."); return; }
        if (data.World == 1 && !data.OpenedChests.Any(x => x.StartsWith("field-1-"))) { SetNotice("Misión secundaria: encuentra y abre un cofre de suministros."); return; }
        int worldTwoDefeats = data.ClearedEnemies.Count(x => x.StartsWith("field-2-"));
        if (data.World == 2 && worldTwoDefeats < 3) { SetNotice("Misión secundaria: derrota 3 robots infectados. Progreso: " + worldTwoDefeats + "/3."); return; }
        if (data.World == 3 && !data.OpenedChests.Contains("dungeon-3")) { SetNotice("Misión secundaria: recupera los datos del cofre de la mazmorra."); return; }
        if (data.World == 4 && !data.DefeatedBosses.Contains("boss-4")) { SetNotice("Misión secundaria: derrota al Titán NEXUS."); return; }
        string reward = SecondaryReward(data.World);
        data.CompletedMissions.Add(key); data.Coins += 30; GainExperience(40); data.Inventory.Add(reward);
        SetNotice($"Misión secundaria completada: +30 créditos, 40 XP y {reward}. Revisa el inventario para equiparlo.");
    }

    private void AdvanceWorld()
    {
        string boss = "boss-" + data.World;
        if (!data.DefeatedBosses.Contains(boss)) { SetNotice("El portal está sellado. Primero derrota al jefe dentro de la mazmorra."); return; }
        if (data.World == 4) { data.CompletedMissions.Add("campaign-complete"); SetNotice("¡Campaña completada! La anomalía dimensional quedó contenida."); return; }
        data.CompletedMissions.Add("main-" + data.World);
        data.World++; data.X = 2; data.Y = 10; data.InDungeon = false; data.Hp = Math.Min(data.MaxHp, data.Hp + 25);
        SetNotice("Portal activado: " + WorldNames[data.World - 1] + ". Recuperaste salud al cruzar.");
    }

    private void OpenChest(string key)
    {
        if (data.OpenedChests.Contains(key)) { SetNotice("Este cofre ya fue abierto."); return; }
        data.OpenedChests.Add(key); data.Coins += 20 + data.World * 5;
        string reward = data.World switch { 1 => "Espada de guardabosques", 2 => "Blindaje antiplaga", 3 => "Rifle criogénico", _ => "Armadura NEXUS" };
        data.Inventory.Add(reward); data.Inventory.Add("Poción médica");
        if (key.StartsWith("dungeon-")) data.CompletedMissions.Add("cache-" + data.World);
        SetNotice($"Cofre abierto: {reward}, una poción y {20 + data.World * 5} créditos."); UpdateHud(); canvas.Invalidate();
    }

    private static string SecondaryReward(int world) => world switch
    {
        1 => "Botiquín de campo",
        2 => "Blindaje reforzado",
        3 => "Rifle de precisión",
        _ => "Armadura de fase"
    };

    private string SecondaryProgress(int world)
    {
        if (data.CompletedMissions.Contains("side-" + world)) return "COMPLETADA";
        return world switch
        {
            1 => data.OpenedChests.Any(x => x.StartsWith("field-1-")) ? "1/1 cofre" : "0/1 cofres",
            2 => $"{Math.Min(3, data.ClearedEnemies.Count(x => x.StartsWith("field-2-")))}/3 robots",
            3 => data.OpenedChests.Contains("dungeon-3") ? "datos recuperados" : "cofre de mazmorra pendiente",
            _ => data.DefeatedBosses.Contains("boss-4") ? "Titán derrotado" : "Titán pendiente"
        };
    }

    private void StartBattle(bool boss, string key)
    {
        if (data.ClearedEnemies.Contains(key) || data.DefeatedBosses.Contains(key)) return;
        int hp = (boss ? 85 : 34) + data.World * (boss ? 25 : 12);
        enemy = new EnemyData { Name = boss ? new[] { "", "Coloso de raíces", "Némesis infectada", "Guardián criogénico", "Titán NEXUS" }[data.World] : new[] { "", "Autómata de vigilancia", "Robot infectado", "Centinela de hielo", "Dron de la brecha" }[data.World], Hp = hp, MaxHp = hp, Boss = boss, Key = key };
        mode = "battle"; SetNotice("¡Encuentro! Pulsa 1 para atacar · 2 para usar objeto · 3 para retirarte."); canvas.Invalidate();
    }

    private void BattleAction(int action)
    {
        if (enemy == null) return;
        if (action == 3) { if (enemy.Boss) SetNotice("No puedes abandonar un combate contra un jefe."); else { enemy = null; mode = "map"; SetNotice("Te retiraste del combate."); } canvas.Invalidate(); return; }
        if (action == 1) { int damage = AttackPower(); enemy.Hp -= damage; SetNotice($"Golpeaste con {data.Weapon}: {damage} de daño."); }
        else
        {
            int idx = data.Inventory.FindIndex(x => x.Contains("Poción") || x.Contains("Batería") || x.Contains("Botiquín"));
            if (idx < 0) { SetNotice("No tienes objetos de recuperación."); return; }
            string item = data.Inventory[idx]; data.Inventory.RemoveAt(idx); int heal = item.Contains("Botiquín") ? 45 : 30;
            data.Hp = Math.Min(data.MaxHp, data.Hp + heal); SetNotice($"Usaste {item} y recuperaste {heal} de salud.");
        }
        if (enemy.Hp <= 0)
        {
            var defeated = enemy; enemy = null; mode = "map"; data.EnemiesDefeated++;
            if (defeated.Boss) { data.DefeatedBosses.Add(defeated.Key); data.CompletedMissions.Add("main-" + data.World); data.Coins += 80; GainExperience(160); data.Inventory.Add("Núcleo de jefe"); SetNotice($"¡Jefe derrotado! +80 créditos, 160 XP y núcleo de jefe. El portal quedó abierto."); }
            else { data.ClearedEnemies.Add(defeated.Key); data.Coins += 10; GainExperience(35); data.Inventory.Add("Chatarra robótica"); SetNotice("¡Victoria! +10 créditos, 35 XP y chatarra robótica."); }
        }
        else
        {
            int incoming = Math.Max(4, 14 + data.World * 5 - DefensePower()); data.Hp = Math.Max(0, data.Hp - incoming);
            if (data.Hp == 0) { data.Hp = Math.Max(35, data.MaxHp / 2); data.InDungeon = false; data.X = 2; data.Y = 10; enemy = null; mode = "map"; SetNotice("Reinicio de emergencia: regresaste al refugio con media salud."); }
            else SetNotice($"El enemigo contraataca: {incoming} de daño. {enemy.Name}: {enemy.Hp}/{enemy.MaxHp}.");
        }
        UpdateHud(); canvas.Invalidate();
    }

    private int AttackPower() => 16 + data.Level * 3 + (data.Weapon.Contains("Rifle") ? 16 : data.Weapon.Contains("Espada") ? 12 : data.Weapon.Contains("Lanza") ? 10 : 0);
    private int DefensePower() => data.Level * 2 + (data.Armor.Contains("Blindaje") || data.Armor.Contains("Armadura") ? 12 : data.Armor.Contains("Traje") ? 8 : 3);
    private int ExperienceToNext() => data.Level * 100;
    private void GainExperience(int amount)
    {
        data.Experience += amount;
        while (data.Experience >= ExperienceToNext()) { data.Experience -= ExperienceToNext(); data.Level++; data.MaxHp += 15; data.Hp = data.MaxHp; SetNotice("¡Subiste al nivel " + data.Level + "! Salud y ataque aumentados."); }
    }
    private void UpdateHud() => hud.Text = $"  {data.Explorer}   ◆ {WorldNames[data.World - 1]}   ♥ {data.Hp}/{data.MaxHp}   LV {data.Level} · {data.Experience}/{ExperienceToNext()} XP   ◈ {data.Coins}   ⚔ {data.Weapon}";
    private void SetNotice(string text) { notice = text; status.Text = "  " + text; }

    private void DrawWorld(Graphics g, Rectangle bounds)
    {
        g.Clear(Color.FromArgb(15, 22, 34)); g.SmoothingMode = SmoothingMode.AntiAlias;
        if (bounds.Width < 20 || bounds.Height < 20) return;
        string[] map = data.InDungeon ? Dungeon : Maps[data.World - 1];
        int cols = map[0].Length, rows = map.Length;
        int tile = Math.Max(14, Math.Min(bounds.Width / cols, bounds.Height / rows));
        int ox = (bounds.Width - cols * tile) / 2, oy = (bounds.Height - rows * tile) / 2;
        for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++)
        {
            char ch = map[y][x]; var rect = new Rectangle(ox + x * tile, oy + y * tile, tile, tile);
            DrawTile(g, rect, ch, x, y);
            if (!data.InDungeon && ch == 'C' && data.OpenedChests.Contains($"field-{data.World}-{x}-{y}")) DrawGround(g, rect, data.World, x, y);
            if (!data.InDungeon && ch == 'E' && data.ClearedEnemies.Contains($"field-{data.World}-{x}-{y}")) DrawGround(g, rect, data.World, x, y);
            if (data.InDungeon && x == 10 && y == 1 && !data.OpenedChests.Contains("dungeon-" + data.World)) DrawChest(g, rect);
            if (data.InDungeon && x == 10 && y == 7 && !data.DefeatedBosses.Contains("boss-" + data.World)) DrawEnemy(g, rect, true);
            if (!data.InDungeon && ch == 'B' && !data.DefeatedBosses.Contains("guardian-" + data.World)) DrawEnemy(g, rect, true);
            if (!data.InDungeon && ch == 'E' && !data.ClearedEnemies.Contains($"field-{data.World}-{x}-{y}")) DrawEnemy(g, rect, false);
            if (!data.InDungeon && ch == 'C' && !data.OpenedChests.Contains($"field-{data.World}-{x}-{y}")) DrawChest(g, rect);
            if (!data.InDungeon && ch == 'Q') DrawQuestNpc(g, rect);
            if (!data.InDungeon && ch == 'P') DrawPortal(g, rect);
            if (data.InDungeon && x == 1 && y == 7) DrawDoor(g, rect);
        }
        int px = data.InDungeon ? data.DungeonX : data.X, py = data.InDungeon ? data.DungeonY : data.Y;
        int heroWidth = Math.Max(22, tile * 11 / 10), heroHeight = Math.Max(32, tile * 3 / 2);
        DrawHero(g, new Rectangle(ox + px * tile + (tile - heroWidth) / 2, oy + (py + 1) * tile - heroHeight, heroWidth, heroHeight));
        using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
        string title = data.InDungeon ? $"MAZMORRA · {WorldNames[data.World - 1]}" : WorldNames[data.World - 1];
        using var titleBrush = new SolidBrush(Color.FromArgb(230, 231, 244, 255));
        g.DrawString(title, titleFont, titleBrush, 14, 12);
        if (mode == "battle") DrawBattle(g, bounds);
    }

    private void DrawTile(Graphics g, Rectangle r, char ch, int x, int y)
    {
        if (data.InDungeon)
        {
            DrawDungeonFloor(g, r, x, y);
            if (ch == '#') { using var wall = new LinearGradientBrush(r, Color.FromArgb(55, 66, 89), Color.FromArgb(25, 31, 48), LinearGradientMode.Vertical); g.FillRectangle(wall, r); using var pen = new Pen(Color.FromArgb(90, 126, 145, 166), 2); g.DrawLine(pen, r.Left, r.Top + 2, r.Right, r.Top + 2); }
            return;
        }
        DrawGround(g, r, data.World, x, y);
        if (ch == '~') { using var water = new LinearGradientBrush(r, Color.FromArgb(36, 107, 154), Color.FromArgb(25, 66, 113), LinearGradientMode.Vertical); g.FillRectangle(water, r); using var wave = new Pen(Color.FromArgb(115, 143, 221, 242), Math.Max(1, r.Width / 24)); g.DrawArc(wave, r.Left + 3, r.Top + r.Height / 3, r.Width - 7, r.Height / 3, 195, 140); }
        else if (ch == 'T') DrawTree(g, r, data.World, x, y);
        else if (ch == 'H') DrawBuilding(g, r, data.World, x, y);
        else if (ch == '#') { using var rock = new SolidBrush(Color.FromArgb(63, 75, 89)); g.FillRectangle(rock, r); using var shade = new Pen(Color.FromArgb(95, 20, 26, 36), 3); g.DrawLine(shade, r.Left + 3, r.Bottom - 5, r.Right - 5, r.Bottom - 5); }
        else if (ch == 'D') DrawDungeonGate(g, r);
        else if (ch == 'P') DrawGround(g, r, data.World, x, y);
        else if (ch == 'C') DrawGround(g, r, data.World, x, y);
        else if (ch == 'E' || ch == 'B' || ch == 'Q') DrawGround(g, r, data.World, x, y);
    }
    private static void DrawGround(Graphics g, Rectangle r, int world, int x, int y)
    {
        Color baseColor = world switch { 2 => Color.FromArgb(93, 89, 72), 3 => Color.FromArgb(133, 157, 169), 4 => Color.FromArgb(76, 83, 125), _ => Color.FromArgb(79, 124, 77) };
        using var b = new SolidBrush(baseColor); g.FillRectangle(b, r);
        int shade = ((x * 17 + y * 31) % 13);
        using var d = new SolidBrush(Color.FromArgb(20 + shade, world == 3 ? 255 : 18, world == 3 ? 255 : 31, world == 4 ? 255 : 20));
        g.FillEllipse(d, r.Left + r.Width / 5, r.Top + r.Height / 4, Math.Max(3, r.Width / 4), Math.Max(3, r.Height / 5));
    }
    private static void DrawDungeonFloor(Graphics g, Rectangle r, int x, int y)
    {
        using var floor = new SolidBrush(((x + y) % 2 == 0) ? Color.FromArgb(58, 61, 73) : Color.FromArgb(50, 54, 66)); g.FillRectangle(floor, r);
        using var seam = new Pen(Color.FromArgb(26, 10, 13, 22)); g.DrawRectangle(seam, r);
    }
    private static void DrawTree(Graphics g, Rectangle r, int world, int x, int y)
    {
        int s = r.Width; Color leaves = world == 2 ? Color.FromArgb(48, 75, 54) : world == 3 ? Color.FromArgb(65, 104, 119) : world == 4 ? Color.FromArgb(45, 59, 112) : Color.FromArgb(42, 102, 56);
        using var shadow = new SolidBrush(Color.FromArgb(65, 7, 15, 13)); g.FillEllipse(shadow, r.Left + s / 9, r.Top + s * 3 / 5, s * 4 / 5, s / 3);
        using var trunk = new SolidBrush(Color.FromArgb(91, 64, 45)); g.FillRectangle(trunk, r.Left + s * 2 / 5, r.Top + s * 2 / 5, s / 5, s / 2);
        using var crownDark = new SolidBrush(leaves); g.FillEllipse(crownDark, r.Left + s / 10, r.Top + s / 12, s * 4 / 5, s * 3 / 4);
        using var crown = new SolidBrush(Color.FromArgb(Math.Min(255, leaves.R + 18), Math.Min(255, leaves.G + 20), Math.Min(255, leaves.B + 12))); g.FillEllipse(crown, r.Left + s / 4, r.Top + s / 8, s / 2, s / 2);
        using var glint = new SolidBrush(Color.FromArgb(95, 205, 225, 155)); g.FillEllipse(glint, r.Left + s / 3, r.Top + s / 5, s / 7, s / 9);
    }
    private static void DrawBuilding(Graphics g, Rectangle r, int world, int x, int y)
    {
        int inset = Math.Max(2, r.Width / 10); var body = Rectangle.Inflate(r, -inset, -inset);
        using var shadow = new SolidBrush(Color.FromArgb(90, 5, 8, 16)); g.FillRectangle(shadow, body.Left + 3, body.Top + 4, body.Width, body.Height);
        Color wall = world == 2 ? Color.FromArgb(117, 99, 77) : world == 3 ? Color.FromArgb(144, 165, 174) : world == 4 ? Color.FromArgb(73, 87, 143) : Color.FromArgb(160, 130, 87);
        using var wallBrush = new SolidBrush(wall); g.FillRectangle(wallBrush, body);
        Point[] roofPts = [new(body.Left - 2, body.Top + body.Height / 3), new(body.Left + body.Width / 2, body.Top - inset), new(body.Right + 2, body.Top + body.Height / 3)];
        using var roof = new SolidBrush(world == 4 ? Color.FromArgb(51, 213, 226) : world == 2 ? Color.FromArgb(70, 72, 68) : Color.FromArgb(117, 55, 54)); g.FillPolygon(roof, roofPts);
        using var door = new SolidBrush(Color.FromArgb(39, 35, 43)); g.FillRectangle(door, body.Left + body.Width * 2 / 5, body.Top + body.Height / 2, body.Width / 5, body.Height / 2);
        using var window = new SolidBrush(Color.FromArgb(173, 223, 228, 179)); g.FillRectangle(window, body.Left + body.Width / 8, body.Top + body.Height / 2, Math.Max(3, body.Width / 5), Math.Max(3, body.Height / 5));
    }
    private static void DrawDungeonGate(Graphics g, Rectangle r)
    {
        using var glow = new SolidBrush(Color.FromArgb(55, 88, 220, 255)); g.FillEllipse(glow, r.Left, r.Top, r.Width, r.Height);
        using var stone = new SolidBrush(Color.FromArgb(54, 58, 69)); g.FillRectangle(stone, r.Left + r.Width / 6, r.Top + r.Height / 6, r.Width * 2 / 3, r.Height * 5 / 6);
        using var gate = new Pen(Color.FromArgb(141, 211, 241), Math.Max(2, r.Width / 12)); g.DrawArc(gate, r.Left + r.Width / 5, r.Top + r.Height / 9, r.Width * 3 / 5, r.Height * 3 / 5, 180, 180); g.DrawLine(gate, r.Left + r.Width / 5, r.Top + r.Height / 2, r.Left + r.Width / 5, r.Bottom - 2); g.DrawLine(gate, r.Right - r.Width / 5, r.Top + r.Height / 2, r.Right - r.Width / 5, r.Bottom - 2);
    }
    private static void DrawChest(Graphics g, Rectangle r)
    {
        int w = r.Width * 3 / 5, h = r.Height * 2 / 5, x = r.Left + (r.Width - w) / 2, y = r.Top + r.Height / 3;
        using var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)); g.FillEllipse(shadow, x, y + h, w, h / 2);
        using var wood = new SolidBrush(Color.FromArgb(137, 79, 37)); g.FillRectangle(wood, x, y, w, h);
        using var lid = new SolidBrush(Color.FromArgb(190, 111, 44)); g.FillRectangle(lid, x, y - h / 4, w, h / 3);
        using var gold = new SolidBrush(Color.FromArgb(248, 208, 85)); g.FillRectangle(gold, x + w / 2 - Math.Max(2, w / 12), y + h / 3, Math.Max(4, w / 6), h / 3);
    }
    private static void DrawQuestNpc(Graphics g, Rectangle r)
    {
        using var robe = new SolidBrush(Color.FromArgb(150, 94, 212)); g.FillEllipse(robe, r.Left + r.Width / 4, r.Top + r.Height / 5, r.Width / 2, r.Height * 3 / 4);
        using var head = new SolidBrush(Color.FromArgb(229, 194, 160)); g.FillEllipse(head, r.Left + r.Width / 3, r.Top + r.Height / 10, r.Width / 3, r.Height / 3);
        using var q = new SolidBrush(Color.Gold); g.FillEllipse(q, r.Right - r.Width / 3, r.Top, Math.Max(4, r.Width / 4), Math.Max(4, r.Height / 4));
    }
    private static void DrawPortal(Graphics g, Rectangle r)
    {
        using var glow = new SolidBrush(Color.FromArgb(48, 88, 212, 255)); g.FillEllipse(glow, r.Left, r.Top, r.Width, r.Height);
        using var outer = new Pen(Color.FromArgb(117, 230, 255), Math.Max(2, r.Width / 12)); g.DrawEllipse(outer, r.Left + r.Width / 5, r.Top + r.Height / 12, r.Width * 3 / 5, r.Height * 5 / 6);
        using var inner = new SolidBrush(Color.FromArgb(80, 90, 67, 220)); g.FillEllipse(inner, r.Left + r.Width / 3, r.Top + r.Height / 4, r.Width / 3, r.Height / 2);
    }
    private static void DrawDoor(Graphics g, Rectangle r)
    {
        using var d = new SolidBrush(Color.FromArgb(145, 176, 221)); g.FillRectangle(d, r.Left + r.Width / 3, r.Top + r.Height / 5, r.Width / 3, r.Height * 3 / 5);
    }
    private void DrawHero(Graphics g, Rectangle r)
    {
        // Use a full-body sprite on the map; keep the portrait for combat panels.
        if (r.Height < 80)
        {
            DrawMapCharacter(g, r);
            return;
        }
        if (heroPortrait != null) { g.DrawImage(heroPortrait, r); return; }
        using var shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)); g.FillEllipse(shadow, r.Left, r.Bottom - r.Height / 5, r.Width, r.Height / 4);
        using var body = new SolidBrush(data.CharacterId == 2 ? Color.FromArgb(78, 185, 205) : Color.FromArgb(220, 231, 239)); g.FillEllipse(body, r.Left + r.Width / 5, r.Top + r.Height / 4, r.Width * 3 / 5, r.Height * 3 / 4);
        using var head = new SolidBrush(Color.FromArgb(227, 197, 168)); g.FillEllipse(head, r.Left + r.Width / 4, r.Top, r.Width / 2, r.Height / 3);
        using var visor = new SolidBrush(Color.FromArgb(46, 199, 242)); g.FillRectangle(visor, r.Left + r.Width / 3, r.Top + r.Height / 8, r.Width / 3, r.Height / 10);
    }

    private void DrawMapCharacter(Graphics g, Rectangle r)
    {
        var oldMode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float sx = r.Width / 32f, sy = r.Height / 44f;
        float ox = r.Left, oy = r.Top;
        RectangleF Box(float x, float y, float w, float h) => new(ox + x * sx, oy + y * sy, Math.Max(1, w * sx), Math.Max(1, h * sy));
        using var outline = new Pen(Color.FromArgb(35, 27, 38), Math.Max(1f, sx * 1.4f));
        using var shadow = new SolidBrush(Color.FromArgb(90, 3, 8, 14));
        g.FillEllipse(shadow, Box(3, 38, 26, 5));

        if (data.CharacterId is 2 or 4)
        {
            Color metal = data.CharacterId == 2 ? Color.FromArgb(65, 177, 194) : Color.FromArgb(177, 82, 77);
            using var metalBrush = new SolidBrush(metal);
            using var darkMetal = new SolidBrush(Color.FromArgb(45, 63, 82));
            using var visor = new SolidBrush(Color.FromArgb(85, 231, 255));
            FillRoundedRectangle(g, darkMetal, Box(6, 14, 20, 23), 5 * sx);
            FillRoundedRectangle(g, metalBrush, Box(7, 17, 18, 20), 5 * sx);
            g.FillEllipse(metalBrush, Box(7, 2, 18, 17));
            g.DrawEllipse(outline, Box(7, 2, 18, 17));
            FillRoundedRectangle(g, visor, Box(10, 8, 12, 4), 2 * sx);
            FillRoundedRectangle(g, darkMetal, Box(7, 35, 8, 7), 2 * sx);
            FillRoundedRectangle(g, darkMetal, Box(18, 35, 8, 7), 2 * sx);
            using var joint = new SolidBrush(Color.FromArgb(222, 190, 102));
            g.FillEllipse(joint, Box(5, 19, 5, 7)); g.FillEllipse(joint, Box(22, 19, 5, 7));
            g.SmoothingMode = oldMode;
            return;
        }

        bool female = data.Female;
        Color hairColor = female ? Color.FromArgb(85, 42, 35) : Color.FromArgb(35, 31, 42);
        Color jacket = data.CharacterId == 3 ? Color.FromArgb(162, 51, 66) : Color.FromArgb(38, 113, 157);
        using var hair = new SolidBrush(hairColor);
        using var skin = new SolidBrush(Color.FromArgb(229, 180, 143));
        using var coat = new SolidBrush(jacket);
        using var coatShade = new SolidBrush(Color.FromArgb(25, 47, 77));
        using var shirt = new SolidBrush(Color.FromArgb(230, 236, 237));
        using var trousers = new SolidBrush(Color.FromArgb(42, 52, 70));
        using var boot = new SolidBrush(Color.FromArgb(30, 35, 46));
        using var pack = new SolidBrush(Color.FromArgb(193, 117, 51));
        using var trim = new SolidBrush(Color.FromArgb(83, 218, 226));
        using var eye = new SolidBrush(Color.FromArgb(33, 37, 48));

        FillRoundedRectangle(g, pack, Box(22, 17, 7, 16), 3 * sx); DrawRoundedRectangle(g, outline, Box(22, 17, 7, 16), 3 * sx);
        FillRoundedRectangle(g, trousers, Box(9, 30, 7, 10), 2 * sx);
        FillRoundedRectangle(g, trousers, Box(17, 30, 7, 10), 2 * sx);
        FillRoundedRectangle(g, boot, Box(7, 38, 10, 4), 2 * sx);
        FillRoundedRectangle(g, boot, Box(16, 38, 11, 4), 2 * sx);
        DrawRoundedRectangle(g, outline, Box(9, 30, 7, 10), 2 * sx);
        DrawRoundedRectangle(g, outline, Box(17, 30, 7, 10), 2 * sx);
        FillRoundedRectangle(g, coatShade, Box(4, 18, 7, 15), 3 * sx);
        FillRoundedRectangle(g, coatShade, Box(21, 18, 7, 15), 3 * sx);
        g.FillEllipse(skin, Box(4, 29, 7, 6)); g.FillEllipse(skin, Box(21, 29, 7, 6));
        FillRoundedRectangle(g, skin, Box(13, 14, 7, 6), 2 * sx);
        g.FillPolygon(coat, new[] { new PointF(ox + 10 * sx, oy + 18 * sy), new PointF(ox + 22 * sx, oy + 18 * sy), new PointF(ox + 25 * sx, oy + 31 * sy), new PointF(ox + 7 * sx, oy + 31 * sy) });
        g.DrawLine(outline, ox + 10 * sx, oy + 19 * sy, ox + 8 * sx, oy + 30 * sy);
        g.DrawLine(outline, ox + 22 * sx, oy + 19 * sy, ox + 24 * sx, oy + 30 * sy);
        g.FillPolygon(shirt, new[] { new PointF(ox + 14 * sx, oy + 19 * sy), new PointF(ox + 18 * sx, oy + 19 * sy), new PointF(ox + 19 * sx, oy + 30 * sy), new PointF(ox + 13 * sx, oy + 30 * sy) });
        FillRoundedRectangle(g, trim, Box(15, 21, 2, 7), sx);
        g.DrawLine(outline, ox + 10 * sx, oy + 30 * sy, ox + 22 * sx, oy + 30 * sy);
        g.FillEllipse(skin, Box(9, 2, 14, 16));
        g.DrawEllipse(outline, Box(9, 2, 14, 16));
        g.FillEllipse(hair, Box(8, 0, 16, 10));
        g.FillPolygon(hair, new[] { new PointF(ox + 9 * sx, oy + 5 * sy), new PointF(ox + 16 * sx, oy + 2 * sy), new PointF(ox + 14 * sx, oy + 8 * sy), new PointF(ox + 10 * sx, oy + 9 * sy) });
        if (female) g.FillEllipse(hair, Box(20, 6, 5, 14));
        g.FillEllipse(eye, Box(12, 9, 1.8f, 2)); g.FillEllipse(eye, Box(19, 9, 1.8f, 2));
        g.DrawLine(outline, ox + 14 * sx, oy + 14 * sy, ox + 17 * sx, oy + 14 * sy);
        FillRoundedRectangle(g, trim, Box(10, 3, 12, 2), sx);
        using var badge = new SolidBrush(Color.FromArgb(247, 204, 85));
        g.FillEllipse(badge, Box(20, 23, 3, 3));
        g.SmoothingMode = oldMode;
    }

    private static GraphicsPath RoundedPath(RectangleF rect, float radius)
    {
        float d = Math.Max(1, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundedPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRectangle(Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = RoundedPath(rect, radius);
        g.DrawPath(pen, path);
    }
    private static void DrawEnemy(Graphics g, Rectangle r, bool boss)
    {
        int pad = boss ? 7 : 12; var body = Rectangle.Inflate(r, -pad, -pad);
        using var shadow = new SolidBrush(Color.FromArgb(75, 0, 0, 0)); g.FillEllipse(shadow, body.Left, body.Bottom - 3, body.Width, Math.Max(3, body.Height / 4));
        using var armor = new SolidBrush(boss ? Color.FromArgb(138, 45, 63) : Color.FromArgb(74, 126, 101)); g.FillEllipse(armor, body);
        using var head = new SolidBrush(boss ? Color.FromArgb(182, 61, 71) : Color.FromArgb(101, 150, 109)); g.FillEllipse(head, body.Left + body.Width / 5, body.Top - body.Height / 8, body.Width * 3 / 5, body.Height / 2);
        using var eye = new SolidBrush(Color.FromArgb(255, 225, 90)); g.FillEllipse(eye, body.Left + body.Width / 3, body.Top + body.Height / 5, Math.Max(3, body.Width / 7), Math.Max(3, body.Height / 10)); g.FillEllipse(eye, body.Left + body.Width * 3 / 5, body.Top + body.Height / 5, Math.Max(3, body.Width / 7), Math.Max(3, body.Height / 10));
        if (boss) { using var bossFont = new Font("Segoe UI", Math.Max(8, r.Width / 7), FontStyle.Bold); using var gold = new SolidBrush(Color.Gold); g.DrawString("BOSS", bossFont, gold, r.Left, r.Top); }
    }
    private void DrawBattle(Graphics g, Rectangle b)
    {
        using var veil = new SolidBrush(Color.FromArgb(218, 5, 10, 24)); g.FillRectangle(veil, b);
        using var ground = new LinearGradientBrush(new Rectangle(0, b.Height * 2 / 3, b.Width, b.Height / 3), Color.FromArgb(61, 83, 96), Color.FromArgb(20, 28, 44), LinearGradientMode.Vertical); g.FillRectangle(ground, 0, b.Height * 2 / 3, b.Width, b.Height / 3);
        DrawHero(g, new Rectangle(b.Width / 4 - 48, b.Height / 2, 96, 124)); DrawEnemy(g, new Rectangle(b.Width * 3 / 4 - 62, b.Height / 2, 124, 124), enemy.Boss);
        using var title = new Font("Segoe UI", 24, FontStyle.Bold); using var small = new Font("Segoe UI", 13); using var white = new SolidBrush(Color.White); using var cyan = new SolidBrush(Cyan);
        g.DrawString(enemy.Boss ? "JEFE DE MAZMORRA" : "ENCUENTRO HOSTIL", title, cyan, 32, 26);
        g.DrawString(data.Explorer + $" · {data.Hp}/{data.MaxHp} HP", small, white, b.Width / 4 - 90, b.Height * 2 / 3 + 10);
        g.DrawString(enemy.Name + $" · {enemy.Hp}/{enemy.MaxHp} HP", small, white, b.Width * 3 / 4 - 120, b.Height * 2 / 3 + 10);
        g.DrawString("1  ATACAR       2  USAR OBJETO       3  RETIRARSE", small, cyan, b.Width / 2 - 210, b.Height - 45);
    }

    private static string[][] BuildMaps()
    {
        var result = new string[4][];
        for (int world = 1; world <= 4; world++)
        {
            var map = Enumerable.Range(0, MapHeight).Select(_ => Enumerable.Repeat('.', MapWidth).ToArray()).ToArray();
            // Give each reality a distinct silhouette before carving its walkable streets.
            for (int y = 1; y < MapHeight - 1; y++) for (int x = 1; x < MapWidth - 1; x++)
            {
                if (world == 1 && ((x * 13 + y * 7 + 11) % 8 < 4) && (x < 8 || x > 23 || y < 4 || y > 15)) map[y][x] = 'T';
                if (world == 1 && x >= 13 && x <= 15 && y >= 2 && y <= 7) map[y][x] = '~';
                if (world == 2 && ((x >= 3 && x <= 8 && y >= 3 && y <= 5) || (x >= 12 && x <= 16 && y >= 3 && y <= 5) || (x >= 24 && x <= 29 && y >= 3 && y <= 5))) map[y][x] = 'H';
                if (world == 2 && ((x * 5 + y * 3) % 11 == 0)) map[y][x] = '#';
                if (world == 3 && ((x >= 11 && x <= 13 && y >= 2 && y <= 8) || (x >= 16 && x <= 18 && y >= 12 && y <= 18))) map[y][x] = '~';
                if (world == 3 && ((x + y * 2) % 13 == 0)) map[y][x] = '#';
                if (world == 4 && ((x * 3 + y * 5) % 17 == 0)) map[y][x] = '#';
            }
            if (world == 1)
                foreach (var p in new[] { (3, 6), (5, 6), (3, 8), (25, 6), (27, 6), (25, 8) }) PlaceBuilding(map, p.Item1, p.Item2);
            else if (world == 2)
                foreach (var p in new[] { (3, 7), (11, 7), (15, 7), (24, 7), (4, 14), (26, 14) }) PlaceBuilding(map, p.Item1, p.Item2);
            else if (world == 3)
                foreach (var p in new[] { (4, 5), (6, 5), (25, 5), (27, 5), (25, 7) }) PlaceBuilding(map, p.Item1, p.Item2);
            else
                foreach (var p in new[] { (3, 4), (5, 4), (12, 4), (14, 4), (23, 4), (25, 4), (3, 15), (5, 15), (24, 15), (26, 15) }) PlaceBuilding(map, p.Item1, p.Item2);
            for (int x = 1; x < MapWidth - 1; x++) map[10][x] = '.';
            for (int y = 7; y <= 13; y++) map[y][7] = '.';
            for (int y = 4; y <= 10; y++) map[y][21] = '.';
            // Carve bridges and streets so every important location stays reachable.
            if (world == 3) for (int x = 10; x <= 19; x++) map[10][x] = '.';
            if (world == 4) for (int y = 4; y <= 15; y++) { map[y][10] = '.'; map[y][16] = '.'; }
            map[10][2] = '.'; map[9][6] = 'Q'; map[10][8] = 'D'; map[5][12] = 'C'; map[5][18] = 'E'; map[14][12] = 'E'; map[10][28] = 'P'; map[15][22] = 'B';
            // Ensure a clear path from the spawn through the town to the dungeon and portal.
            for (int x = 2; x <= 10; x++) map[10][x] = '.';
            for (int y = 8; y <= 10; y++) map[y][8] = '.';
            for (int x = 8; x <= 21; x++) map[10][x] = '.';
            for (int y = 10; y <= 15; y++) map[y][21] = '.';
            for (int x = 21; x <= 28; x++) map[10][x] = '.';
            map[9][6] = 'Q'; map[10][8] = 'D'; map[5][12] = 'C'; map[5][18] = 'E'; map[14][12] = 'E'; map[10][28] = 'P'; map[15][22] = 'B';
            // Mazmorra approach is kept clear; the actual interior is its own navigable tile map.
            result[world - 1] = map.Select(row => new string(row)).ToArray();
        }
        return result;
    }

    private static void PlaceBuilding(char[][] map, int x, int y)
    {
        for (int row = y; row < Math.Min(MapHeight - 1, y + 2); row++)
            for (int column = x; column < Math.Min(MapWidth - 1, x + 2); column++)
                map[row][column] = 'H';
    }

    protected override void Dispose(bool disposing) { if (disposing) heroPortrait?.Dispose(); base.Dispose(disposing); }
}
