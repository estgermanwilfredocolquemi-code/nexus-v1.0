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
        "La primera fractura partió el cielo sobre el Bosque Vivo. Mara Veld mantiene encendido el Refugio Raíz mientras busca a la cartógrafa que oyó por primera vez la voz de IRIS.",
        "La Zona Cero cayó cuando Némesis reescribió las órdenes de evacuación. Vexa conoce cada calle, pero aún busca a su hermano entre las máquinas infectadas.",
        "La científica Niva Orun sigue transmitiendo desde una estación bajo el hielo. Sus datos podrían demostrar quién abrió realmente los portales.",
        "En NEXUS-09, el Titán protege el núcleo de la anomalía. Kael-7 y los desertores saben que el robot no nació hostil: alguien le borró sus recuerdos."];
    // Each reality is a large, scrolling region. The camera only shows a portion
    // at once, and undiscovered terrain remains hidden until the explorer travels.
    // Each reality is a large, procedurally textured region with plenty of room
    // between the story route and optional exploration sites.
    private const int DungeonWidth = 14;
    private const int DungeonHeight = 10;

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
        public List<string> AcceptedMissions { get; set; } = [];
        public List<string> OpenedChests { get; set; } = [];
        public List<string> DefeatedBosses { get; set; } = [];
        public List<string> ClearedEnemies { get; set; } = [];
        public List<string> DiscoveredTiles { get; set; } = [];
        public bool IrisAwake { get; set; }
        public bool InDungeon { get; set; }
        public bool PrologueSeen { get; set; }
        public int DungeonX { get; set; } = 1;
        public int DungeonY { get; set; } = 6;
        public int Energy { get; set; } = 50;
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
        public bool Charging { get; set; }
    }

    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 34, ForeColor = Cyan, BackColor = Color.FromArgb(12, 19, 35), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) };
    private readonly Label hud = new() { Dock = DockStyle.Top, Height = 54, ForeColor = Color.White, BackColor = Color.FromArgb(13, 20, 39), Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
    private readonly FlowLayoutPanel toolbar = new() { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(10, 16, 32), Padding = new Padding(8, 6, 8, 4), WrapContents = false, AutoScroll = true };
    private readonly FlowLayoutPanel battleActions = new() { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(225, 9, 14, 29), Padding = new Padding(8, 8, 8, 6), WrapContents = false, Visible = false };
    private readonly Nexus3DWorldControl canvas;
    private readonly Action returnToMenu;
    private readonly Image heroPortrait;
    private SaveData data;
    private string mode = "map";
    private string notice = "Explora con WASD o flechas · E interactuar · I inventario · M misiones · Esc pausa";
    private EnemyData enemy;
    private Panel overlay;
    private DateTime battleImpactUntil = DateTime.MinValue;
    private readonly Random combatRng = new();
    private bool guarding;
    private int lastMoveX, lastMoveY = -1;

    private static readonly string[] Dungeon = [
        "##############", "#....#.......#", "#....#..##...#", "#.......##...#", "###.#........#", "#...#..###...#", "#......#.....#", "#..##........#", "#............#", "##############"];

    internal AdventureGameControl(string explorer, bool female, int character, Action returnToMenu, SaveData loaded = null, int startingWorld = 1)
    {
        this.returnToMenu = returnToMenu;
        Dock = DockStyle.Fill;
        data = loaded ?? new SaveData { Explorer = explorer, Female = female, CharacterId = character, World = Math.Clamp(startingWorld, 1, 4) };
        data.Explorer = string.IsNullOrWhiteSpace(data.Explorer) ? explorer : data.Explorer;
        data.World = Math.Clamp(data.World, 1, 4); data.X = Math.Clamp(data.X, -1_000_000, 1_000_000); data.Y = Math.Clamp(data.Y, -1_000_000, 1_000_000);
        data.Level = Math.Max(1, data.Level); data.MaxHp = Math.Max(100, data.MaxHp); data.Hp = Math.Clamp(data.Hp, 1, data.MaxHp);
        data.Inventory ??= []; data.CompletedMissions ??= []; data.AcceptedMissions ??= []; data.OpenedChests ??= []; data.DefeatedBosses ??= []; data.ClearedEnemies ??= []; data.DiscoveredTiles ??= [];
        data.Energy = Math.Clamp(data.Energy, 0, 100);
        data.IrisAwake |= data.Level >= 2;
        DiscoverAroundPlayer();
        string portrait = data.CharacterId switch { 2 => "portrait-robot-aliado.png", 3 => "portrait-humano-enemigo.png", 4 => "portrait-robot-enemigo.png", _ => data.Female ? "portrait-humana-mujer.png" : "portrait-humano-hombre.png" };
        string portraitPath = Path.Combine(AppContext.BaseDirectory, portrait);
        if (File.Exists(portraitPath)) { using var image = Image.FromFile(portraitPath); heroPortrait = new Bitmap(image); }
        canvas = new Nexus3DWorldControl(this);
        canvas.MouseDown += (_, _) => canvas.Focus();
        var header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(10, 16, 32) };
        header.Controls.Add(new Label { Text = "N E X U S   //   MULTIVERSO", Dock = DockStyle.Left, Width = 330, ForeColor = Cyan, Font = new Font("Segoe UI", 15, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) });
        header.Controls.Add(new Label { Text = "EXPLORADOR: " + data.Explorer.ToUpperInvariant(), Dock = DockStyle.Fill, ForeColor = Color.FromArgb(195, 210, 235), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 18, 0) });
        AddTool("Guardar", () => SaveCurrent()); AddTool("Inventario [I]", ShowInventory); AddTool("Equipo", ShowEquipment); AddTool("Misiones [M]", ShowMissions); AddTool("Mapa", ShowMapInfo); AddTool("Música: ON", () => { bool on = NexusSoundtrack.Toggle(); SetNotice(on ? "Música ambiental activada." : "Música ambiental silenciada."); }); AddTool("Pausa [Esc]", ShowPause); AddTool("Menú principal", () => ConfirmReturn());
        AddBattleAction("1 · ATAQUE", 1); AddBattleAction("2 · HABILIDAD", 2); AddBattleAction("3 · DEFENDER", 3); AddBattleAction("4 · OBJETO", 4); AddBattleAction("5 · RETIRADA", 5);
        Controls.Add(canvas); Controls.Add(status); Controls.Add(battleActions); Controls.Add(hud); Controls.Add(toolbar); Controls.Add(header);
        UpdateHud(); status.Text = notice;
        ShownFocus();
        if (!data.PrologueSeen && loaded == null)
        {
            data.PrologueSeen = true;
            BeginInvoke(new Action(ShowPrologue));
        }
    }

    private void AddBattleAction(string text, int action)
    {
        var button = new Button { Text = text, Width = 168, Height = 48, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(27, 46, 75), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(6, 2, 6, 2), Cursor = Cursors.Hand };
        button.FlatAppearance.BorderColor = Color.FromArgb(85, 151, 194);
        button.Click += (_, _) => { BattleAction(action); canvas.Focus(); };
        battleActions.Controls.Add(button);
    }

    private void ShowPrologue()
    {
        string origin = data.CharacterId switch
        {
            2 => "AUR0 apagó su núcleo de combate para sacar a seis familias del Refugio Raíz. Una voz desconocida volvió a encenderlo: IRIS.",
            3 => "Vexa cruzó sola la Zona Cero durante tres inviernos. La última señal de su hermano llegó desde la torre de Némesis.",
            4 => "Bastión-9 recibió la orden de borrar su ciudad. La desobedeció y perdió la memoria en la primera fractura.",
            _ => "Lía/Elián Veld heredó el mapa incompleto de su madre, desaparecida al otro lado de la primera grieta. En sus notas encontró un nombre: IRIS."
        };
        ShowOverlay("CAPÍTULO I · LA PRIMERA FRACTURA", WorldLore[data.World - 1], panel =>
        {
            var story = new Label { Dock = DockStyle.Fill, ForeColor = Color.White, Font = new Font("Segoe UI", 12), Text = $"{origin}\r\n\r\nLa Anomalía está conectando realidades y despertando máquinas que ya no distinguen aliados. Sigue las señales hasta el refugio, escucha a sus habitantes y averigua quién abrió el primer portal.\r\n\r\nWASD o flechas para moverte · E para hablar y usar estructuras · 1–5 durante el combate · M para el diario.", TextAlign = ContentAlignment.MiddleLeft };
            panel.Controls.Add(story);
        });
    }

    internal int SceneWorld => data.World;
    internal int SceneX => data.InDungeon ? data.DungeonX : data.X;
    internal int SceneY => data.InDungeon ? data.DungeonY : data.Y;
    internal bool SceneInDungeon => data.InDungeon;
    internal bool SceneInBattle => mode == "battle" && enemy != null;
    internal bool SceneHasIris => data.IrisAwake;
    internal int SceneCharacter => data.CharacterId;
    internal float SceneFacingAngle => lastMoveX switch { 1 => -90f, -1 => 90f, _ => lastMoveY > 0 ? 180f : 0f };
    internal bool SceneEnemyIsBoss => enemy?.Boss == true;
    internal float SceneEnemyHealthFraction => enemy == null ? 0 : enemy.Hp / (float)Math.Max(1, enemy.MaxHp);
    internal bool SceneAttackFlash => DateTime.UtcNow < battleImpactUntil;
    internal uint SceneNoise(int x, int y) => CellNoise(data.World, x, y);
    internal char SceneTile(int x, int y)
    {
        if (!data.InDungeon) return GetWorldTile(data.World, x, y);
        if (x == 10 && y == 7 && !data.DefeatedBosses.Contains("boss-" + data.World)) return 'B';
        if (x == 10 && y == 1 && !data.OpenedChests.Contains("dungeon-" + data.World)) return 'C';
        return y >= 0 && y < Dungeon.Length && x >= 0 && x < Dungeon[y].Length ? Dungeon[y][x] : '#';
    }
    internal bool SceneEnemyCleared(int x, int y) => data.ClearedEnemies.Contains($"field-{data.World}-{x}-{y}");
    internal bool SceneBossCleared() => data.DefeatedBosses.Contains((data.InDungeon ? "boss-" : "guardian-") + data.World);
    internal bool SceneChestOpened(int x, int y) => data.InDungeon ? data.OpenedChests.Contains("dungeon-" + data.World) : data.OpenedChests.Contains($"field-{data.World}-{x}-{y}");

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
            string bossObjective = w == 4 && !data.IrisAwake ? "Nivel 2 requerido · despierta a IRIS" : "Derrota al jefe de la mazmorra";
            list.Items.Add((mainComplete ? "✓" : "○") + $" PRINCIPAL · {WorldNames[w - 1]} · {(mainComplete ? "Jefe derrotado, portal abierto" : bossObjective)}");
            string objective = w switch { 1 => "Abre un cofre de suministros", 2 => "Derrota 3 robots de esta zona", 3 => "Recupera el cofre de la mazmorra", _ => "Derrota al Titán NEXUS" };
            bool sideComplete = data.CompletedMissions.Contains("side-" + w);
            string questState = sideComplete ? "COMPLETADA" : data.AcceptedMissions.Contains("side-" + w) ? "ACEPTADA" : "HABLA CON " + QuestNpcName(w).ToUpperInvariant();
            list.Items.Add((sideComplete ? "✓" : "○") + $" SECUNDARIA · {objective} · {questState} · {SecondaryProgress(w)}");
            list.Items.Add($"   Premio: {SecondaryReward(w)} + 30 créditos + 40 XP");
        }
        list.Items.Add("Progreso de campaña: " + data.DefeatedBosses.Count + "/4 jefes · " + data.EnemiesDefeated + " enemigos derrotados · " + data.OpenedChests.Count + " cofres abiertos");
        panel.Controls.Add(list);
    });

    private void ShowMapInfo() => ShowOverlay("MAPA DE REALIDADES", "Cuatro mundos conectados. Los portales se activan al derrotar al guardián de cada mazmorra.", panel =>
    {
        var list = MakeList();
        int revealed = data.DiscoveredTiles.Count(k => k.StartsWith(data.World + ":"));
        list.Items.Add($"COORDENADAS · X {data.X} / Y {data.Y} · {revealed} casillas descubiertas en esta realidad");
        list.Items.Add("Mundo generado por regiones sin borde: explora en cualquier dirección; cada zona nueva se crea al llegar.");
        list.Items.Add("MARCADORES: Q encargo · C suministro · E enemigo · D mazmorra · P portal · B guardián");
        list.Items.Add("SEÑALES LEJANAS: suministro al este (35,20) · enlace de misión al sureste (49,31) · alijo al noreste (51,6)");
        for (int i = 0; i < 4; i++)
        {
            string requirement = i == 3 ? " · Titán: nivel 2 + IRIS" : " · Derrota al guardián para abrir el portal";
            list.Items.Add($"MUNDO {i + 1} · {WorldNames[i]}{requirement}\r\n{WorldLore[i]}");
        }
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
            else if (e.KeyCode is Keys.D4 or Keys.NumPad4) BattleAction(4);
            else if (e.KeyCode is Keys.D5 or Keys.NumPad5) BattleAction(5);
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
        if (data.InDungeon && (ny < 0 || nx < 0 || ny >= Dungeon.Length || nx >= Dungeon[ny].Length)) return;
        if (!data.InDungeon && (nx is < -1_000_000 or > 1_000_000 || ny is < -1_000_000 or > 1_000_000)) return;
        char tile = data.InDungeon ? Dungeon[ny][nx] : GetWorldTile(data.World, nx, ny);
        if (tile is '#' or 'T' or '~' or 'H') return;
        if (data.InDungeon) { data.DungeonX = nx; data.DungeonY = ny; }
        else { data.X = nx; data.Y = ny; }
        lastMoveX = dx; lastMoveY = dy;
        if (!data.InDungeon) DiscoverAroundPlayer();
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
            char here = GetWorldTile(data.World, data.X, data.Y);
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
        string npc = QuestNpcName(data.World);
        if (!data.AcceptedMissions.Contains(key))
        {
            string offer = data.World switch
            {
                1 => $"{npc}: El bosque está perdiendo su luz. Necesito que recuperes suministros de un cofre. ¿Aceptas?",
                2 => $"{npc}: La infección tomó tres drones de patrulla. Desactívalos antes de que encuentren el refugio. ¿Aceptas?",
                3 => $"{npc}: En la estación hay datos capaces de estabilizar el clima. Recupera el módulo de la mazmorra. ¿Aceptas?",
                _ => $"{npc}: El Titán controla la ciudad. Necesito que lo derrotes para abrir una ruta de evacuación. ¿Aceptas?"
            };
            if (MessageBox.Show(offer, "ENCARGO · " + WorldNames[data.World - 1], MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            { SetNotice($"{npc}: Entiendo. Estaré aquí si cambias de idea."); return; }
            data.AcceptedMissions.Add(key);
            SetNotice($"{npc}: Encargo aceptado. Te espero para recibir el informe y la recompensa.");
            UpdateHud(); canvas.Invalidate(); return;
        }
        int worldTwoDefeats = data.ClearedEnemies.Count(x => x.StartsWith("field-2-"));
        bool objectiveDone = data.World switch
        {
            1 => data.OpenedChests.Any(x => x.StartsWith("field-1-")),
            2 => worldTwoDefeats >= 3,
            3 => data.OpenedChests.Contains("dungeon-3"),
            _ => data.DefeatedBosses.Contains("boss-4")
        };
        if (!objectiveDone)
        {
            string progress = data.World switch
            {
                1 => "Abre un cofre de suministros.",
                2 => $"Desactiva drones infectados: {worldTwoDefeats}/3.",
                3 => "Recupera el módulo de datos de la mazmorra.",
                _ => "Derrota al Titán NEXUS. Recomendado: nivel 2 con IRIS."
            };
            SetNotice($"{npc}: {progress} Vuelve conmigo cuando lo logres."); return;
        }
        string reward = SecondaryReward(data.World);
        data.CompletedMissions.Add(key); data.Coins += 30; GainExperience(40); data.Inventory.Add(reward);
        SetNotice($"{npc}: ¡Lo conseguiste! +30 créditos, 40 XP y {reward}. Gracias por salvar nuestra gente.");
    }

    private static string QuestNpcName(int world) => world switch
    {
        1 => "Mara Veld, guardabosques del Refugio Raíz",
        2 => "Tarek Sola, médico de la Zona Cero",
        3 => "Doctora Niva Orun, última científica de la estación",
        _ => "Kael-7, piloto de la resistencia NEXUS"
    };

    private void AdvanceWorld()
    {
        string boss = "boss-" + data.World;
        if (!data.DefeatedBosses.Contains(boss)) { SetNotice("El portal está sellado. Primero derrota al jefe dentro de la mazmorra."); return; }
        if (data.World == 4) { data.CompletedMissions.Add("campaign-complete"); SetNotice("¡Campaña completada! La anomalía dimensional quedó contenida."); return; }
        data.CompletedMissions.Add("main-" + data.World);
        data.World++; data.X = 2; data.Y = 10; data.InDungeon = false; data.Hp = Math.Min(data.MaxHp, data.Hp + 25); DiscoverAroundPlayer();
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
        if (boss && data.World == 4 && data.Level < 2)
        {
            SetNotice("El Titán NEXUS recomienda nivel 2. Explora, completa encargos y vuelve cuando Iris despierte.");
            canvas.Invalidate(); return;
        }
        if (boss && data.World == 4 && !data.IrisAwake)
        {
            SetNotice("IRIS: Necesito que alcances el nivel 2 para sincronizarme contigo.");
            canvas.Invalidate(); return;
        }
        int hp = (boss ? 85 : 34) + data.World * (boss ? 25 : 12);
        enemy = new EnemyData { Name = boss ? new[] { "", "Coloso de raíces", "Némesis infectada", "Guardián criogénico", "Titán NEXUS" }[data.World] : new[] { "", "Autómata de vigilancia", "Robot infectado", "Centinela de hielo", "Dron de la brecha" }[data.World], Hp = hp, MaxHp = hp, Boss = boss, Key = key };
        mode = "battle"; battleActions.Visible = true; SetNotice("¡Encuentro! El enemigo telegrafía sus ataques pesados. Usa 1–5 para decidir."); canvas.Invalidate();
    }

    private void BattleAction(int action)
    {
        if (enemy == null) return;
        battleImpactUntil = DateTime.UtcNow.AddMilliseconds(450);
        guarding = false;
        if (action == 5)
        {
            if (enemy.Boss) SetNotice("El jefe bloquea la retirada. Tendrás que abrirte paso.");
            else if (combatRng.Next(100) < 70) { enemy = null; mode = "map"; battleActions.Visible = false; SetNotice("Te retiraste del combate. Recupera energía antes de volver."); }
            else SetNotice("No encontraste una salida; el enemigo te corta el paso.");
            if (enemy == null) { canvas.Invalidate(); return; }
        }
        else if (action == 1 || action == 2)
        {
            bool special = action == 2;
            if (special && data.Energy < 35) { SetNotice("Energía insuficiente. Golpea o defiéndete para recargar (35 requeridos)."); return; }
            if (special) data.Energy -= 35;
            int baseDamage = AttackPower() + (special ? AttackPower() * 3 / 2 : 0);
            if (!special) data.Energy = Math.Min(100, data.Energy + 18);
            int critChance = data.CharacterId switch { 2 => 32, 3 => 30, 4 => 18, _ => 22 };
            bool critical = combatRng.Next(100) < critChance;
            int damage = baseDamage + (data.IrisAwake ? 8 : 0);
            if (critical) damage = (int)(damage * 1.75f);
            enemy.Hp -= damage;
            string ability = special ? data.CharacterId switch { 2 => "SOBRECARGA IÓNICA", 3 => "RUPTURA DE FASE", 4 => "CAÑÓN DE ASALTO", _ => "CORTE DIMENSIONAL" } : "ATAQUE BÁSICO";
            SetNotice($"{ability}{(critical ? " · ¡CRÍTICO!" : "")}: {damage} de daño · Energía {data.Energy}/100.");
        }
        else if (action == 3)
        {
            guarding = true; data.Energy = Math.Min(100, data.Energy + (data.CharacterId == 1 ? 32 : 25));
            SetNotice($"Postura defensiva: reducirás el próximo impacto y recargas energía ({data.Energy}/100).");
        }
        else if (action == 4)
        {
            int idx = data.Inventory.FindIndex(x => x.Contains("Poción") || x.Contains("Batería") || x.Contains("Botiquín"));
            if (idx < 0) { SetNotice("No tienes objetos de recuperación."); return; }
            string item = data.Inventory[idx]; data.Inventory.RemoveAt(idx); int heal = item.Contains("Botiquín") ? 45 : 30;
            data.Hp = Math.Min(data.MaxHp, data.Hp + heal);
            SetNotice($"Usaste {item} y recuperaste {heal} de salud.");
        }
        if (enemy.Hp <= 0)
        {
            var defeated = enemy; enemy = null; mode = "map"; battleActions.Visible = false; data.EnemiesDefeated++;
            if (defeated.Boss) { data.DefeatedBosses.Add(defeated.Key); data.CompletedMissions.Add("main-" + data.World); data.Coins += 80; GainExperience(160); data.Inventory.Add("Núcleo de jefe"); SetNotice($"¡Jefe derrotado! +80 créditos, 160 XP y núcleo de jefe. El portal quedó abierto."); }
            else { data.ClearedEnemies.Add(defeated.Key); data.Coins += 10; GainExperience(35); data.Inventory.Add("Chatarra robótica"); SetNotice("¡Victoria! +10 créditos, 35 XP y chatarra robótica."); }
        }
        else
        {
            if (enemy.Charging)
            {
                int incoming = Math.Max(8, 28 + data.World * 6 - DefensePower());
                if (guarding) incoming = Math.Max(2, incoming / 5);
                data.Hp = Math.Max(0, data.Hp - incoming); enemy.Charging = false;
                SetNotice(guarding ? $"¡Bloqueo perfecto! El impacto pesado solo causa {incoming} de daño." : $"¡Ataque pesado! Recibiste {incoming} de daño.");
            }
            else if (combatRng.Next(100) < (enemy.Boss ? 38 : 25))
            {
                enemy.Charging = true;
                SetNotice($"{enemy.Name} concentra energía. ¡Prepárate para su ataque pesado!");
            }
            else
            {
                int incoming = Math.Max(3, 14 + data.World * 5 - DefensePower());
                if (guarding) incoming = Math.Max(1, incoming / 4);
                data.Hp = Math.Max(0, data.Hp - incoming);
                SetNotice(guarding ? $"Defendiste el impacto y recibiste {incoming} de daño." : $"El enemigo contraataca: {incoming} de daño.");
            }
            if (data.Hp == 0) { data.Hp = Math.Max(35, data.MaxHp / 2); data.InDungeon = false; data.X = 2; data.Y = 10; enemy = null; mode = "map"; battleActions.Visible = false; SetNotice("Reinicio de emergencia: regresaste al refugio con media salud."); }
        }
        UpdateHud(); canvas.Invalidate();
    }

    private int AttackPower() => 15 + data.Level * 3 + (data.Weapon.Contains("Rifle") ? 16 : data.Weapon.Contains("Espada") ? 12 : data.Weapon.Contains("Lanza") ? 10 : 0) + (data.CharacterId == 4 ? 5 : data.CharacterId == 2 ? 2 : 0);
    private int DefensePower() => data.Level * 2 + (data.Armor.Contains("Blindaje") || data.Armor.Contains("Armadura") ? 12 : data.Armor.Contains("Traje") ? 8 : 3);
    private int ExperienceToNext() => data.Level * 100;
    private void GainExperience(int amount)
    {
        data.Experience += amount;
        while (data.Experience >= ExperienceToNext())
        {
            data.Experience -= ExperienceToNext(); data.Level++; data.MaxHp += 15; data.Hp = data.MaxHp;
            if (data.Level >= 2 && !data.IrisAwake)
            {
                data.IrisAwake = true;
                data.Inventory.Add("Módulo de sincronía IRIS");
                SetNotice("¡Nivel 2! IRIS despierta y se une a la expedición. Sus pulsos refuerzan tus ataques.");
            }
            else SetNotice("¡Subiste al nivel " + data.Level + "! Salud y ataque aumentados.");
        }
    }
    private void UpdateHud()
    {
        string fight = enemy == null ? "" : $"   VS {enemy.Name} · {enemy.Hp}/{enemy.MaxHp} HP";
        hud.Text = $"  {data.Explorer}   ◆ {WorldNames[data.World - 1]}   ♥ {data.Hp}/{data.MaxHp}   LV {data.Level} · {data.Experience}/{ExperienceToNext()} XP   ⚡ {data.Energy}/100   ◈ {data.Coins}   ⚔ {data.Weapon}   {(data.IrisAwake ? "◆ IRIS EN LÍNEA" : "IRIS · BLOQUEADA HASTA NIVEL 2")}{fight}";
    }
    private void SetNotice(string text) { notice = text; status.Text = "  " + text; }

    private string DiscoveryKey(int x, int y) => $"{data.World}:{x}:{y}";
    private bool IsDiscovered(int x, int y) => data.DiscoveredTiles.Contains(DiscoveryKey(x, y));
    private void DiscoverAroundPlayer()
    {
        const int radius = 5;
        for (int y = data.Y - radius; y <= data.Y + radius; y++)
            for (int x = data.X - radius; x <= data.X + radius; x++)
                if (Math.Abs(x - data.X) + Math.Abs(y - data.Y) <= radius && !IsDiscovered(x, y))
                    data.DiscoveredTiles.Add(DiscoveryKey(x, y));
    }

    private void DrawWorld(Graphics g, Rectangle bounds)
    {
        g.Clear(Color.FromArgb(15, 22, 34)); g.SmoothingMode = SmoothingMode.AntiAlias;
        if (bounds.Width < 20 || bounds.Height < 20) return;
        int tile = Math.Max(20, Math.Min(bounds.Width / 26, bounds.Height / 17));
        int visibleCols = bounds.Width / tile + 2, visibleRows = bounds.Height / tile + 2;
        int px = data.InDungeon ? data.DungeonX : data.X, py = data.InDungeon ? data.DungeonY : data.Y;
        int startX = data.InDungeon ? Math.Clamp(px - visibleCols / 2, 0, Math.Max(0, DungeonWidth - visibleCols)) : px - visibleCols / 2;
        int startY = data.InDungeon ? Math.Clamp(py - visibleRows / 2, 0, Math.Max(0, DungeonHeight - visibleRows)) : py - visibleRows / 2;
        if (data.InDungeon) { visibleCols = Math.Min(visibleCols, DungeonWidth); visibleRows = Math.Min(visibleRows, DungeonHeight); }
        int ox = (bounds.Width - visibleCols * tile) / 2, oy = (bounds.Height - visibleRows * tile) / 2;
        for (int y = startY; y < startY + visibleRows; y++) for (int x = startX; x < startX + visibleCols; x++)
        {
            char ch = data.InDungeon ? Dungeon[y][x] : GetWorldTile(data.World, x, y);
            var rect = new Rectangle(ox + (x - startX) * tile, oy + (y - startY) * tile, tile, tile);
            if (!data.InDungeon && !IsDiscovered(x, y))
            {
                using var fog = new SolidBrush(Color.FromArgb(252, 5, 10, 22));
                g.FillRectangle(fog, rect); using var edge = new Pen(Color.FromArgb(90, 29, 42, 62)); g.DrawRectangle(edge, rect);
                continue;
            }
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
        int heroWidth = Math.Max(22, tile * 11 / 10), heroHeight = Math.Max(32, tile * 3 / 2);
        DrawHero(g, new Rectangle(ox + (px - startX) * tile + (tile - heroWidth) / 2, oy + (py - startY + 1) * tile - heroHeight, heroWidth, heroHeight));
        if (!data.InDungeon && data.IrisAwake)
            DrawIrisCompanion(g, new Rectangle(ox + (px - startX) * tile + tile / 2, oy + (py - startY) * tile + tile / 3, Math.Max(18, tile / 2), Math.Max(24, tile * 2 / 3)));
        using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
        string title = data.InDungeon ? $"MAZMORRA · {WorldNames[data.World - 1]}" : $"{WorldNames[data.World - 1]} · SECTOR {px / 8 + 1}-{py / 8 + 1} · {data.DiscoveredTiles.Count(k => k.StartsWith(data.World + ":"))} ZONAS REVELADAS";
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
        int shade = (int)(CellNoise(world, x, y) % 13);
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

    private static void DrawIrisCompanion(Graphics g, Rectangle r)
    {
        using var glow = new SolidBrush(Color.FromArgb(45, 66, 223, 255));
        using var metal = new SolidBrush(Color.FromArgb(210, 138, 182, 206));
        using var dark = new SolidBrush(Color.FromArgb(210, 35, 69, 96));
        using var visor = new SolidBrush(Color.FromArgb(235, 67, 232, 255));
        using var labelFont = new Font("Segoe UI", Math.Max(7, r.Width / 4), FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(225, 170, 246, 255));
        g.FillEllipse(glow, Rectangle.Inflate(r, 3, 3));
        g.FillEllipse(metal, r.Left + r.Width / 5, r.Top, r.Width * 3 / 5, r.Height / 3);
        g.FillRectangle(visor, r.Left + r.Width / 3, r.Top + r.Height / 7, r.Width / 3, Math.Max(2, r.Height / 12));
        g.FillRectangle(dark, r.Left + r.Width / 6, r.Top + r.Height / 3, r.Width * 2 / 3, r.Height / 2);
        g.FillRectangle(metal, r.Left + r.Width / 4, r.Top + r.Height / 3, r.Width / 2, r.Height / 2);
        g.FillEllipse(visor, r.Left + r.Width / 2 - 2, r.Top + r.Height / 2, 4, 4);
        g.DrawString("IRIS", labelFont, labelBrush, r.Left - r.Width / 5, r.Top - labelFont.Height);
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
        int floorY = b.Height * 2 / 3;
        using (var sky = new LinearGradientBrush(b, Color.FromArgb(11, 25, 57), Color.FromArgb(41, 23, 69), LinearGradientMode.Vertical)) g.FillRectangle(sky, b);
        using (var skylineGlow = new SolidBrush(Color.FromArgb(45, 36, 186, 226))) g.FillEllipse(skylineGlow, b.Width / 2 - b.Width / 3, floorY - b.Height / 2, b.Width * 2 / 3, b.Height / 2);

        // Night city arena with lit windows and a glowing horizon, inspired by the reference clip.
        var skyline = new[] { (0.02f, .37f, .13f), (.17f, .30f, .11f), (.31f, .40f, .10f), (.43f, .24f, .12f), (.58f, .34f, .10f), (.72f, .28f, .12f), (.87f, .38f, .13f) };
        foreach (var building in skyline)
        {
            int x = (int)(b.Width * building.Item1), w = (int)(b.Width * building.Item3), h = (int)(b.Height * building.Item2);
            var tower = new Rectangle(x, floorY - h, w, h);
            using var facade = new LinearGradientBrush(tower, Color.FromArgb(56, 89, 143), Color.FromArgb(12, 25, 52), LinearGradientMode.Vertical);
            g.FillRectangle(facade, tower);
            using var edge = new Pen(Color.FromArgb(100, 108, 197, 232), Math.Max(1, b.Width / 900)); g.DrawLine(edge, x + 4, floorY - h, x + 4, floorY);
            using var window = new SolidBrush(Color.FromArgb(105, 93, 201, 240));
            for (int wy = floorY - h + 12; wy < floorY - 8; wy += Math.Max(12, b.Height / 22))
                for (int wx = x + 12; wx < x + w - 7; wx += Math.Max(12, b.Width / 70)) g.FillRectangle(window, wx, wy, 4, 6);
        }
        using (var horizon = new Pen(Color.FromArgb(155, 57, 223, 244), Math.Max(2, b.Width / 500))) g.DrawLine(horizon, 0, floorY, b.Width, floorY);
        using (var floor = new LinearGradientBrush(new Rectangle(0, floorY, b.Width, b.Height - floorY), Color.FromArgb(29, 77, 116), Color.FromArgb(7, 13, 29), LinearGradientMode.Vertical)) g.FillRectangle(floor, 0, floorY, b.Width, b.Height - floorY);
        using (var grid = new Pen(Color.FromArgb(66, 90, 215, 245), 1))
        {
            for (int i = 1; i <= 7; i++) { int y = floorY + i * (b.Height - floorY) / 8; g.DrawLine(grid, 0, y, b.Width, y); }
            for (int i = 0; i <= 12; i++) { int x = i * b.Width / 12; g.DrawLine(grid, b.Width / 2, floorY, x, b.Bottom); }
        }

        int heroX = b.Width * 29 / 100, enemyX = b.Width * 72 / 100;
        int spriteHeight = Math.Clamp(b.Height / 4, 86, 142), spriteWidth = spriteHeight * 3 / 4;
        int spriteY = floorY - spriteHeight + Math.Max(2, b.Height / 100);
        using (var shadow = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
        {
            g.FillEllipse(shadow, heroX - spriteWidth / 2, floorY - 7, spriteWidth, 15);
            g.FillEllipse(shadow, enemyX - spriteWidth / 2, floorY - 7, spriteWidth, 15);
        }
        DrawMapCharacter(g, new Rectangle(heroX - spriteWidth / 2, spriteY, spriteWidth, spriteHeight));
        DrawBattleEnemy(g, new Rectangle(enemyX - spriteWidth / 2, spriteY, spriteWidth, spriteHeight), enemy.Boss);
        if (data.IrisAwake) DrawIrisCompanion(g, new Rectangle(heroX + spriteWidth / 2 - 5, floorY - spriteHeight * 2 / 3, spriteWidth / 2, spriteHeight * 2 / 3));

        if (DateTime.UtcNow < battleImpactUntil)
        {
            float phase = (Environment.TickCount % 360) / 360f;
            int impactX = enemyX, impactY = spriteY + spriteHeight / 2;
            using var beam = new Pen(Color.FromArgb(185, 111, 239, 255), Math.Max(4, b.Width / 180));
            using var core = new Pen(Color.FromArgb(235, 255, 255, 224), Math.Max(2, b.Width / 360));
            g.DrawLine(beam, heroX + spriteWidth / 3, impactY + 16, impactX - spriteWidth / 4, impactY - 8);
            g.DrawLine(core, heroX + spriteWidth / 3, impactY + 16, impactX - spriteWidth / 4, impactY - 8);
            using var ring = new Pen(Color.FromArgb(190, 76, 225, 255), 3);
            int radius = (int)(18 + phase * 30); g.DrawEllipse(ring, impactX - radius, impactY - radius, radius * 2, radius * 2);
            for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4 + phase; int sx = impactX + (int)(Math.Cos(a) * (radius + 8)), sy = impactY + (int)(Math.Sin(a) * (radius + 8)); g.FillEllipse(Brushes.LightCyan, sx, sy, 4, 4); }
        }

        using var title = new Font("Segoe UI", Math.Clamp(b.Width / 58f, 16f, 26f), FontStyle.Bold);
        using var label = new Font("Segoe UI", Math.Clamp(b.Width / 105f, 10f, 15f), FontStyle.Bold);
        using var small = new Font("Segoe UI", Math.Clamp(b.Width / 135f, 9f, 12f));
        using var white = new SolidBrush(Color.White); using var cyan = new SolidBrush(Color.FromArgb(133, 236, 255));
        g.DrawString(enemy.Boss ? "DUELO DE GUARDIÁN" : "ENCUENTRO HOSTIL", title, cyan, 24, 18);
        g.DrawString("ARENA NEXUS   //   " + WorldNames[data.World - 1], small, white, 28, 22 + title.Height);
        DrawBattleHealth(g, 28, 96, Math.Min(b.Width / 3, 330), data.Hp, data.MaxHp, data.Explorer, Color.FromArgb(66, 224, 173), label, small);
        DrawBattleHealth(g, b.Width - Math.Min(b.Width / 3, 330) - 28, 96, Math.Min(b.Width / 3, 330), enemy.Hp, enemy.MaxHp, enemy.Name, Color.FromArgb(255, 91, 117), label, small);
        if (data.IrisAwake) g.DrawString("IRIS · APOYO ACTIVO  +8 DAÑO", small, cyan, 28, 136);
        var energyBar = new Rectangle(28, 162, 230, 10);
        using (var energyBack = new SolidBrush(Color.FromArgb(180, 12, 18, 35))) g.FillRectangle(energyBack, energyBar);
        using (var energyFill = new SolidBrush(Color.FromArgb(80, 215, 255))) g.FillRectangle(energyFill, energyBar.Left, energyBar.Top, energyBar.Width * data.Energy / 100, energyBar.Height);
        g.DrawString($"ENERGÍA {data.Energy}/100 · ESPECIAL 35", small, cyan, 28, 174);
        if (enemy.Charging)
        {
            using var warning = new SolidBrush(Color.FromArgb(245, 255, 172, 87));
            g.DrawString("⚠ ATAQUE PESADO CARGADO · DEFIÉNDETE", label, warning, b.Width / 2 - 150, b.Height / 2 - 25);
        }
        DrawBattleActionButtons(g, b, label, small);
    }

    private void HandleCanvasClick(Point point, Rectangle bounds)
    {
        if (mode != "battle") return;
        int gap = 8, buttonWidth = Math.Min(176, (bounds.Width - 56 - gap * 4) / 5), total = buttonWidth * 5 + gap * 4;
        int left = (bounds.Width - total) / 2, top = bounds.Height - 76;
        for (int i = 0; i < 5; i++)
            if (new Rectangle(left + i * (buttonWidth + gap), top, buttonWidth, 58).Contains(point)) { BattleAction(i + 1); return; }
    }

    private void DrawBattleActionButtons(Graphics g, Rectangle bounds, Font label, Font small)
    {
        int gap = 8, buttonWidth = Math.Min(176, (bounds.Width - 56 - gap * 4) / 5), total = buttonWidth * 5 + gap * 4;
        int left = (bounds.Width - total) / 2, top = bounds.Height - 76;
        string[] titles = ["1  ATAQUE", "2  ESPECIAL", "3  DEFENSA", "4  OBJETO", "5  HUIR"];
        string[] subtitles = [data.Weapon, $"35 energía · crítico {CriticalChance()}%", "Bloquea · +25 energía", "Botiquín / poción", enemy.Boss ? "Jefe bloquea" : "70% de escape"];
        for (int i = 0; i < 5; i++)
        {
            var rect = new Rectangle(left + i * (buttonWidth + gap), top, buttonWidth, 58);
            Color accent = i switch { 0 => Color.FromArgb(83, 41, 168), 1 => Color.FromArgb(139, 47, 106), 2 => Color.FromArgb(33, 109, 99), _ => Color.FromArgb(24, 45, 78) };
            using var fill = new LinearGradientBrush(rect, accent, Color.FromArgb(13, 25, 48), LinearGradientMode.Vertical);
            using var border = new Pen(i < 3 ? Color.FromArgb(140, 122, 229, 255) : Color.FromArgb(95, 102, 160, 205), 1.3f);
            FillRoundedRectangle(g, fill, rect, 9); DrawRoundedRectangle(g, border, rect, 9);
            using var white = new SolidBrush(Color.White); using var muted = new SolidBrush(Color.FromArgb(177, 205, 225));
            g.DrawString(titles[i], label, white, rect.Left + 12, rect.Top + 7);
            g.DrawString(subtitles[i], small, muted, rect.Left + 12, rect.Top + 31);
        }
    }

    private int CriticalChance() => data.CharacterId switch { 2 => 32, 3 => 30, 4 => 18, _ => 22 };

    private static void DrawBattleHealth(Graphics g, int x, int y, int width, int hp, int maxHp, string name, Color fill, Font label, Font small)
    {
        using var text = new SolidBrush(Color.White); using var muted = new SolidBrush(Color.FromArgb(183, 208, 226));
        g.DrawString(name.ToUpperInvariant(), label, text, x, y - label.Height - 4);
        var bar = new Rectangle(x, y, width, 15);
        using var back = new SolidBrush(Color.FromArgb(188, 12, 18, 35)); g.FillRectangle(back, bar);
        int fillWidth = (int)((bar.Width - 2) * Math.Clamp(hp / (float)Math.Max(1, maxHp), 0, 1));
        if (fillWidth > 0) { using var brush = new LinearGradientBrush(new Rectangle(bar.Left + 1, bar.Top + 1, Math.Max(1, fillWidth), bar.Height - 2), fill, Color.FromArgb(230, fill), LinearGradientMode.Horizontal); g.FillRectangle(brush, bar.Left + 1, bar.Top + 1, fillWidth, bar.Height - 2); }
        using var border = new Pen(Color.FromArgb(155, 140, 215, 245)); g.DrawRectangle(border, bar);
        g.DrawString($"{hp} / {maxHp} HP", small, muted, x, y + 17);
    }

    private static void DrawBattleEnemy(Graphics g, Rectangle r, bool boss)
    {
        var old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
        using var metal = new SolidBrush(boss ? Color.FromArgb(154, 49, 86) : Color.FromArgb(81, 142, 151));
        using var shade = new SolidBrush(boss ? Color.FromArgb(70, 26, 56) : Color.FromArgb(35, 63, 83));
        using var light = new SolidBrush(boss ? Color.FromArgb(255, 111, 112) : Color.FromArgb(139, 215, 220));
        using var eye = new SolidBrush(Color.FromArgb(255, 239, 123));
        int s = Math.Max(4, r.Width / 11), cx = r.Left + r.Width / 2;
        g.FillRectangle(shade, cx - 3 * s, r.Top + 5 * s, 6 * s, 9 * s);
        g.FillRectangle(metal, cx - 2 * s, r.Top + 3 * s, 4 * s, 8 * s);
        g.FillRectangle(shade, cx - 3 * s, r.Top + s, 6 * s, 5 * s);
        g.FillRectangle(light, cx - 2 * s, r.Top + 2 * s, 4 * s, 2 * s);
        g.FillRectangle(eye, cx - s, r.Top + 3 * s, 2 * s, Math.Max(2, s / 2));
        g.FillRectangle(metal, cx - 4 * s, r.Top + 6 * s, s, 6 * s);
        g.FillRectangle(metal, cx + 3 * s, r.Top + 6 * s, s, 6 * s);
        g.FillRectangle(shade, cx - 2 * s, r.Top + 13 * s, 2 * s, 4 * s);
        g.FillRectangle(shade, cx + s, r.Top + 13 * s, 2 * s, 4 * s);
        if (boss) { g.FillRectangle(light, cx - 4 * s, r.Top, 2 * s, 3 * s); g.FillRectangle(light, cx + 2 * s, r.Top, 2 * s, 3 * s); }
        g.SmoothingMode = old;
    }

    // Deterministic coordinate-based terrain: no map edges, and a region
    // regenerates identically whenever a saved explorer returns to it.
    internal static char GetWorldTile(int world, int x, int y)
    {
        uint noise = CellNoise(world, x, y);
        int terrain = (int)(noise % 100);
        char tile = world switch
        {
            1 => terrain < 16 ? 'T' : RiverAt(x, y) ? '~' : '.',
            2 => terrain < 11 ? 'H' : terrain < 19 ? '#' : '.',
            3 => terrain < 13 ? '~' : terrain < 20 ? '#' : '.',
            _ => terrain < 16 ? '#' : '.'
        };

        // Each 32x32 region owns stable encounter and cache coordinates.
        int cx = FloorDiv(x, 32), cy = FloorDiv(y, 32);
        uint region = CellNoise(world * 13, cx, cy);
        int localX = PositiveMod(x, 32), localY = PositiveMod(y, 32);
        int enemyX = 2 + (int)(region % 27), enemyY = 2 + (int)((region >> 8) % 27);
        int cacheX = 2 + (int)((region >> 16) % 27), cacheY = 2 + (int)((region >> 24) % 27);
        if (localX == enemyX && localY == enemyY && (region & 3) != 0) tile = 'E';
        else if (localX == cacheX && localY == cacheY && (region & 3) == 0) tile = 'C';

        // Handcrafted landmarks anchor the opening chapter near the refuge.
        if (y == 10 && x is >= 2 and <= 28) tile = '.';
        if (x == 7 && y is >= 7 and <= 13) tile = '.';
        if (x == 21 && y is >= 4 and <= 15) tile = '.';
        if (x is >= 8 and <= 21 && y == 10) tile = '.';
        if (x is >= 21 and <= 28 && y == 10) tile = '.';
        if (x == 6 && y == 9) tile = 'Q';
        if (x == 8 && y == 10) tile = 'D';
        if ((x == 12 && y == 5) || (x == 35 && y == 20) || (x == 51 && y == 6)) tile = 'C';
        if ((x == 18 && y == 5) || (x == 12 && y == 14) || (x == 44 && y == 27)) tile = 'E';
        if (x == 28 && y == 10) tile = 'P';
        if (x == 22 && y == 15) tile = 'B';
        if (x == 49 && y == 31) tile = 'Q';
        return tile;
    }

    private static bool RiverAt(int x, int y) => Math.Abs(PositiveMod(y - x / 9, 83) - 41) <= 1;
    private static int FloorDiv(int value, int divisor) => (int)Math.Floor(value / (double)divisor);
    private static int PositiveMod(int value, int divisor) => (value % divisor + divisor) % divisor;
    internal static uint CellNoise(int world, int x, int y)
    {
        unchecked
        {
            uint h = (uint)world * 0x9E3779B9u ^ (uint)x * 0x85EBCA6Bu ^ (uint)y * 0xC2B2AE35u;
            h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
            return h;
        }
    }
    protected override void Dispose(bool disposing) { if (disposing) heroPortrait?.Dispose(); base.Dispose(disposing); }
}

