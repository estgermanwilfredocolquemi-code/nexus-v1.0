using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NexusWinForms;

/// <summary>Native Windows OpenGL scene for the NEXUS third-person adventure.</summary>
internal sealed class Nexus3DWorldControl : Control
{
    private const uint PfdDrawToWindow = 0x00000004, PfdSupportOpenGl = 0x00000020, PfdDoubleBuffer = 0x00000001;
    private const uint ColorBufferBit = 0x00004000, DepthBufferBit = 0x00000100;
    private const uint DepthTest = 0x0B71, Lighting = 0x0B50, Light0 = 0x4000, ColorMaterial = 0x0B57;
    private const uint Fog = 0x0B60, CullFace = 0x0B44, Back = 0x0405, FrontAndBack = 0x0408, AmbientAndDiffuse = 0x1602;
    private const uint Projection = 0x1701, ModelView = 0x1700, Quads = 0x0007, QuadStrip = 0x0008, Triangles = 0x0004, Lines = 0x0001;
    private const uint Smooth = 0x1D01, FogMode = 0x0B65, FogDensity = 0x0B62, FogColor = 0x0B66, Exp2 = 0x0801;
    private readonly AdventureGameControl owner;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer frameTimer = new() { Interval = 24 };
    private IntPtr dc, context;
    private bool ready;
    private float visualX, visualY;
    private bool visualPositionReady;

    [StructLayout(LayoutKind.Sequential)]
    private struct PixelFormatDescriptor
    {
        public ushort Size, Version;
        public uint Flags;
        public byte PixelType, ColorBits, RedBits, RedShift, GreenBits, GreenShift, BlueBits, BlueShift, AlphaBits, AlphaShift;
        public byte AccumBits, AccumRedBits, AccumGreenBits, AccumBlueBits, AccumAlphaBits, DepthBits, StencilBits, AuxBuffers, LayerType, Reserved;
        public uint LayerMask, VisibleMask, DamageMask;
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern int ChoosePixelFormat(IntPtr hdc, ref PixelFormatDescriptor descriptor);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool SetPixelFormat(IntPtr hdc, int format, ref PixelFormatDescriptor descriptor);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool SwapBuffers(IntPtr hdc);
    [DllImport("opengl32.dll", SetLastError = true)] private static extern IntPtr wglCreateContext(IntPtr hdc);
    [DllImport("opengl32.dll", SetLastError = true)] private static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);
    [DllImport("opengl32.dll", SetLastError = true)] private static extern bool wglDeleteContext(IntPtr hglrc);

    [DllImport("opengl32.dll")] private static extern void glViewport(int x, int y, int width, int height);
    [DllImport("opengl32.dll")] private static extern void glClearColor(float r, float g, float b, float a);
    [DllImport("opengl32.dll")] private static extern void glClear(uint mask);
    [DllImport("opengl32.dll")] private static extern void glEnable(uint cap);
    [DllImport("opengl32.dll")] private static extern void glDisable(uint cap);
    [DllImport("opengl32.dll")] private static extern void glShadeModel(uint mode);
    [DllImport("opengl32.dll")] private static extern void glMatrixMode(uint mode);
    [DllImport("opengl32.dll")] private static extern void glLoadIdentity();
    [DllImport("opengl32.dll")] private static extern void glFrustum(double left, double right, double bottom, double top, double near, double far);
    [DllImport("opengl32.dll")] private static extern void glTranslatef(float x, float y, float z);
    [DllImport("opengl32.dll")] private static extern void glRotatef(float angle, float x, float y, float z);
    [DllImport("opengl32.dll")] private static extern void glScalef(float x, float y, float z);
    [DllImport("opengl32.dll")] private static extern void glPushMatrix();
    [DllImport("opengl32.dll")] private static extern void glPopMatrix();
    [DllImport("opengl32.dll")] private static extern void glBegin(uint mode);
    [DllImport("opengl32.dll")] private static extern void glEnd();
    [DllImport("opengl32.dll")] private static extern void glColor3f(float r, float g, float b);
    [DllImport("opengl32.dll")] private static extern void glNormal3f(float x, float y, float z);
    [DllImport("opengl32.dll")] private static extern void glVertex3f(float x, float y, float z);
    [DllImport("opengl32.dll")] private static extern void glColorMaterial(uint face, uint mode);
    [DllImport("opengl32.dll")] private static extern void glLightfv(uint light, uint pname, [In] float[] values);
    [DllImport("opengl32.dll")] private static extern void glFogi(uint pname, int value);
    [DllImport("opengl32.dll")] private static extern void glFogf(uint pname, float value);
    [DllImport("opengl32.dll")] private static extern void glFogfv(uint pname, [In] float[] values);

    internal Nexus3DWorldControl(AdventureGameControl owner)
    {
        this.owner = owner;
        Dock = DockStyle.Fill;
        TabStop = true;
        BackColor = Color.FromArgb(10, 17, 26);
        SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        frameTimer.Tick += (_, _) => Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            dc = GetDC(Handle);
            var pfd = new PixelFormatDescriptor { Size = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(), Version = 1, Flags = PfdDrawToWindow | PfdSupportOpenGl | PfdDoubleBuffer, PixelType = 0, ColorBits = 32, DepthBits = 24, LayerType = 0 };
            int format = ChoosePixelFormat(dc, ref pfd);
            if (format == 0 || !SetPixelFormat(dc, format, ref pfd)) throw new InvalidOperationException("Windows no pudo crear el formato de pantalla OpenGL.");
            context = wglCreateContext(dc);
            if (context == IntPtr.Zero || !wglMakeCurrent(dc, context)) throw new InvalidOperationException("No se pudo iniciar el contexto gráfico 3D.");
            glEnable(DepthTest); glEnable(Lighting); glEnable(Light0); glEnable(ColorMaterial); glDisable(CullFace);
            glShadeModel(Smooth); glColorMaterial(FrontAndBack, AmbientAndDiffuse);
            glLightfv(Light0, 0x1200, [0.42f, 0.44f, 0.48f, 1f]);
            glLightfv(Light0, 0x1201, [0.9f, 0.88f, 0.82f, 1f]);
            glLightfv(Light0, 0x1203, [-0.35f, 0.8f, 0.3f, 0f]);
            glEnable(Fog); glFogi(FogMode, (int)Exp2); glFogf(FogDensity, 0.018f);
            glViewport(0, 0, Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
            frameTimer.Start(); ready = true;
        }
        catch (Exception ex)
        {
            ready = false;
            Tag = "3D_ERROR:" + ex.Message;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (!ready || dc == IntPtr.Zero) return;
        wglMakeCurrent(dc, context);
        glViewport(0, 0, Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!ready)
        {
            e.Graphics.Clear(BackColor);
            using var font = new Font("Segoe UI", 14, FontStyle.Bold);
            using var brush = new SolidBrush(Color.FromArgb(140, 225, 255));
            e.Graphics.DrawString(Tag?.ToString().StartsWith("3D_ERROR:") == true ? Tag.ToString()[10..] : "Iniciando mundo 3D…", font, brush, 24, 24);
            return;
        }
        if (!wglMakeCurrent(dc, context)) return;
        RenderScene();
        SwapBuffers(dc);
    }

    private void RenderScene()
    {
        int world = owner.SceneWorld;
        float skyR = world switch { 1 => .31f, 2 => .23f, 3 => .45f, _ => .13f };
        float skyG = world switch { 1 => .57f, 2 => .31f, 3 => .69f, _ => .20f };
        float skyB = world switch { 1 => .76f, 2 => .34f, 3 => .82f, _ => .42f };
        glClearColor(skyR, skyG, skyB, 1f); glFogfv(FogColor, [skyR, skyG, skyB, 1f]); glClear(ColorBufferBit | DepthBufferBit);
        glMatrixMode(Projection); glLoadIdentity();
        float aspect = Math.Max(1, ClientSize.Width) / (float)Math.Max(1, ClientSize.Height);
        glFrustum(-aspect * .42, aspect * .42, -.42, .42, .55, 85.0);
        glMatrixMode(ModelView); glLoadIdentity();
        glRotatef(27f, 1f, 0f, 0f); glTranslatef(0f, -3.0f, -8.6f);

        int px = owner.SceneX, py = owner.SceneY;
        if (!visualPositionReady) { visualX = px; visualY = py; visualPositionReady = true; }
        visualX += (px - visualX) * .19f; visualY += (py - visualY) * .19f;
        float heroOffsetX = visualX - px, heroOffsetZ = visualY - py;
        float t = (float)clock.Elapsed.TotalSeconds;

        if (owner.SceneInBattle) DrawBattleArena(world, t, heroOffsetX, heroOffsetZ);
        else DrawOpenWorld(world, px, py, heroOffsetX, heroOffsetZ, t);
    }

    private void DrawOpenWorld(int world, int px, int py, float heroX, float heroZ, float t)
    {
        int radiusX = owner.SceneInDungeon ? 7 : 16;
        int radiusZ = owner.SceneInDungeon ? 5 : 16;
        for (int z = -radiusZ; z <= radiusZ; z++)
            for (int x = -radiusX; x <= radiusX; x++)
            {
                int mapX = px + x, mapY = py + z;
                char tile = owner.SceneTile(mapX, mapY);
                DrawTerrainTile(world, tile, x, z, owner.SceneNoise(mapX, mapY));
                if (tile == 'T') DrawTree(x, z, owner.SceneNoise(mapX, mapY));
                else if (tile == 'H') DrawBuilding(x, z, owner.SceneNoise(mapX, mapY));
                else if (tile == '~') DrawWater(x, z, t, owner.SceneNoise(mapX, mapY));
                else if (tile == '#' && world == 4) DrawBox(x, .52f, z, .72f, 1.04f, .72f, .34f, .22f, .49f);
                else if (tile == '#') DrawRock(x, z, owner.SceneNoise(mapX, mapY));
                else if (tile == '.' && owner.SceneNoise(mapX, mapY) % 23 == 0) DrawBush(x, z, owner.SceneNoise(mapX, mapY));

                if (tile == 'E' && !owner.SceneEnemyCleared(mapX, mapY)) DrawCharacter(x, z, .92f, (mapX * 31 + mapY * 17) & 1, false, t, true, 0f);
                if (tile == 'B' && !owner.SceneBossCleared()) DrawCharacter(x, z, 1.42f, 4, false, t, true, 0f);
                if (tile == 'Q') DrawQuestGiver(x, z, world, t);
                if (tile == 'C' && !owner.SceneChestOpened(mapX, mapY)) DrawChest(x, z, t);
                if (tile == 'D') DrawDungeonGate(x, z, t);
                if (tile == 'P') DrawPortal(x, z, t);
            }
        DrawCharacter(heroX, heroZ, 1f, owner.SceneCharacter, true, t, Math.Abs(heroX) + Math.Abs(heroZ) > .06f, owner.SceneFacingAngle);
        if (owner.SceneHasIris) DrawIris(heroX + .75f, heroZ + .15f, t);
        DrawGuideBeacons(t);
    }

    private void DrawBattleArena(int world, float t, float heroX, float heroZ)
    {
        for (int z = -10; z <= 10; z++)
            for (int x = -12; x <= 12; x++)
            {
                DrawTerrainTile(world, '.', x, z, owner.SceneNoise(x, z));
                if (Math.Abs(x) > 7 && Math.Abs(z) > 4 && (owner.SceneNoise(x, z) % 5 == 0)) DrawBush(x, z, owner.SceneNoise(x, z));
                if (Math.Abs(x) == 11 && Math.Abs(z) > 2 && z % 3 == 0) DrawTree(x, z, owner.SceneNoise(x, z));
            }
        DrawCharacter(-2.0f + heroX, 0.6f + heroZ, 1f, owner.SceneCharacter, true, t, true, 90f);
        DrawCharacter(2.2f, -1.0f, owner.SceneEnemyIsBoss ? 1.5f : 1.05f, owner.SceneEnemyIsBoss ? 4 : 3, false, t, false, -90f);
        DrawHealthBar3D(2.2f, owner.SceneEnemyHealthFraction, t);
        if (owner.SceneAttackFlash) DrawEnergyStrike(t);
    }

    private static void DrawTerrainTile(int world, char tile, int x, int z, uint noise)
    {
        float variation = ((noise >> 8) % 17) / 240f;
        (float r, float g, float b) baseColor = world switch
        {
            1 => (.19f + variation, .39f + variation, .17f),
            2 => (.28f + variation, .25f + variation, .20f),
            3 => (.62f + variation, .74f + variation, .79f),
            _ => (.22f + variation, .19f, .33f + variation)
        };
        if (tile == '~') baseColor = world == 3 ? (.22f, .55f, .72f) : (.08f, .30f, .47f);
        if (tile == '#') baseColor = (.25f, .27f, .30f);
        if (tile == 'H') baseColor = world == 2 ? (.30f, .28f, .24f) : (.20f, .24f, .31f);
        if (world == 4 && tile == '#') baseColor = (.32f, .20f, .48f);
        glColor3f(baseColor.r, baseColor.g, baseColor.b); glNormal3f(0, 1, 0);
        float y = tile == '~' ? -.06f : 0f;
        glBegin(Quads);
        glVertex3f(x - .5f, y, z - .5f); glVertex3f(x + .5f, y, z - .5f); glVertex3f(x + .5f, y, z + .5f); glVertex3f(x - .5f, y, z + .5f);
        glEnd();
    }

    private static void DrawTree(float x, float z, uint seed)
    {
        float h = 1.65f + (seed % 100) / 210f;
        DrawCylinder(x, 0, z, .14f, 0, h * .55f, .34f, .24f, .12f);
        DrawEllipsoid(x, h * .76f, z, .62f, .68f, .58f, .10f, .32f, .13f);
        DrawEllipsoid(x - .29f, h * .65f, z + .06f, .38f, .45f, .37f, .15f, .39f, .15f);
        DrawEllipsoid(x + .28f, h * .68f, z - .05f, .38f, .43f, .37f, .13f, .36f, .14f);
    }

    private static void DrawBush(float x, float z, uint seed)
    {
        float green = .28f + (seed % 20) / 160f;
        DrawEllipsoid(x, .22f, z, .39f, .24f, .34f, .08f, green, .07f);
        DrawEllipsoid(x - .18f, .24f, z + .05f, .24f, .23f, .24f, .10f, green + .08f, .08f);
        DrawEllipsoid(x + .17f, .20f, z - .08f, .25f, .20f, .24f, .07f, green, .06f);
    }

    private static void DrawBuilding(float x, float z, uint seed)
    {
        float h = 1.1f + (seed % 11) * .10f;
        DrawBox(x, h / 2, z, .82f, h, .82f, .27f, .28f, .32f);
        DrawBox(x, h + .04f, z, .91f, .10f, .91f, .18f, .17f, .19f);
        for (int side = -1; side <= 1; side += 2)
            DrawBox(x + side * .416f, h * .58f, z - .16f, .018f, .19f, .16f, .22f, .67f, .78f);
    }

    private static void DrawRock(float x, float z, uint seed)
    {
        float size = .24f + (seed % 9) * .035f;
        DrawEllipsoid(x, size * .55f, z, size, size * .75f, size, .34f, .37f, .40f);
    }

    private static void DrawWater(float x, float z, float t, uint seed)
    {
        float shimmer = .04f * (float)Math.Sin(t * 2 + seed % 17);
        glDisable(Lighting); glColor3f(.10f + shimmer, .38f + shimmer, .60f + shimmer);
        glBegin(Quads); glVertex3f(x - .5f, -.035f, z - .5f); glVertex3f(x + .5f, -.035f, z - .5f); glVertex3f(x + .5f, -.035f, z + .5f); glVertex3f(x - .5f, -.035f, z + .5f); glEnd(); glEnable(Lighting);
    }

    private void DrawQuestGiver(float x, float z, int world, float t)
    {
        DrawCharacter(x, z, .96f, 1, false, t, true, 180f);
        DrawEllipsoid(x, 2.25f + .08f * (float)Math.Sin(t * 2), z, .10f, .14f, .10f, 1f, .78f, .27f);
    }

    private static void DrawChest(float x, float z, float t)
    {
        DrawBox(x, .22f, z, .56f, .42f, .43f, .51f, .29f, .10f);
        DrawBox(x, .45f, z, .58f, .09f, .45f, .82f, .59f, .21f);
        DrawBox(x, .28f, z - .225f, .08f, .20f, .025f, .91f, .76f, .35f);
        DrawEllipsoid(x, .67f + .04f * (float)Math.Sin(t * 2), z, .12f, .10f, .12f, 1f, .81f, .38f);
    }

    private static void DrawDungeonGate(float x, float z, float t)
    {
        DrawBox(x, .8f, z, .94f, 1.6f, .30f, .21f, .23f, .29f);
        DrawEllipsoid(x, .84f, z - .18f, .48f, .70f, .12f, .10f, .66f + .10f * (float)Math.Sin(t * 2), .92f);
        DrawBox(x, 1.66f, z, 1.0f, .18f, .36f, .41f, .45f, .53f);
    }

    private static void DrawPortal(float x, float z, float t)
    {
        glPushMatrix(); glTranslatef(x, 1.22f, z); glRotatef(t * 24f, 0, 0, 1);
        glDisable(Lighting); glColor3f(.20f, .86f, 1f); glBegin(Lines);
        for (int i = 0; i < 36; i++) { double a = i * Math.PI * 2 / 36, b = (i + 1) * Math.PI * 2 / 36; glVertex3f(.52f * (float)Math.Cos(a), .82f * (float)Math.Sin(a), 0); glVertex3f(.52f * (float)Math.Cos(b), .82f * (float)Math.Sin(b), 0); }
        glEnd(); glEnable(Lighting); glPopMatrix();
        DrawEllipsoid(x, .92f, z, .30f, .57f, .10f, .13f, .55f, .92f);
    }

    private void DrawCharacter(float x, float z, float scale, int character, bool hero, float t, bool walking, float facing)
    {
        float swing = walking ? (float)Math.Sin(t * 10) * .55f : (float)Math.Sin(t * 1.7f) * .025f;
        bool robot = character is 2 or 4 || (!hero && character == 4);
        (float r, float g, float b) suit = hero ? character switch { 2 => (.16f, .48f, .43f), 3 => (.61f, .20f, .25f), 4 => (.34f, .38f, .47f), _ => (.15f, .40f, .61f) }
            : character == 4 ? (.47f, .17f, .22f) : (.39f, .43f, .36f);
        glPushMatrix(); glTranslatef(x, 0, z); glRotatef(facing, 0, 1, 0); glScalef(scale, scale, scale);
        DrawEllipsoid(0, 1.02f, 0, .31f, .45f, .22f, suit.r, suit.g, suit.b);
        DrawEllipsoid(0, .56f, 0, .25f, .17f, .21f, .18f, .21f, .28f);
        if (robot)
        {
            DrawEllipsoid(0, 1.70f, 0, .25f, .26f, .24f, .40f, .52f, .57f);
            DrawBox(0, 1.70f, .214f, .29f, .09f, .04f, .15f, .91f, 1f);
            DrawEllipsoid(0, 1.35f, 0, .15f, .13f, .15f, .31f, .41f, .47f);
        }
        else
        {
            DrawEllipsoid(0, 1.72f, 0, .23f, .26f, .22f, .75f, .52f, .37f);
            DrawEllipsoid(0, 1.82f, -.02f, .24f, .15f, .23f, .18f, .14f, .14f);
        }
        DrawEllipsoid(-.38f, 1.32f, 0, .15f, .16f, .15f, suit.r, suit.g, suit.b);
        DrawEllipsoid(.38f, 1.32f, 0, .15f, .16f, .15f, suit.r, suit.g, suit.b);
        DrawLimb(-.40f, 1.29f, -.10f - swing, robot, suit.r, suit.g, suit.b);
        DrawLimb(.40f, 1.29f, .10f + swing, robot, suit.r, suit.g, suit.b);
        DrawLeg(-.14f, swing, suit.r, suit.g, suit.b);
        DrawLeg(.14f, -swing, suit.r, suit.g, suit.b);
        if (hero && character == 4) DrawEllipsoid(-.36f, 1.06f, -.13f, .22f, .28f, .12f, .71f, .42f, .12f);
        glPopMatrix();
    }

    private static void DrawLimb(float x, float y, float swing, bool robot, float r, float g, float b)
    {
        glPushMatrix(); glTranslatef(x, y, 0); glRotatef(swing * 57f, 1, 0, 0);
        DrawEllipsoid(0, -.18f, 0, robot ? .105f : .11f, .28f, .11f, r * .86f, g * .86f, b * .86f);
        DrawEllipsoid(0, -.43f, 0, .105f, .10f, .105f, robot ? .60f : .75f, robot ? .68f : .52f, robot ? .70f : .37f);
        glPopMatrix();
    }

    private static void DrawLeg(float x, float swing, float r, float g, float b)
    {
        glPushMatrix(); glTranslatef(x, .52f, 0); glRotatef(swing * 48f, 1, 0, 0);
        DrawEllipsoid(0, -.20f, 0, .13f, .30f, .14f, .18f, .22f, .30f);
        DrawEllipsoid(0, -.47f, -.05f, .14f, .12f, .22f, .12f, .15f, .20f);
        glPopMatrix();
    }

    private static void DrawIris(float x, float z, float t)
    {
        float y = 1.0f + .13f * (float)Math.Sin(t * 2.4f);
        DrawEllipsoid(x, y, z, .22f, .25f, .22f, .17f, .79f, 1f);
        DrawEllipsoid(x, y, z + .13f, .08f, .08f, .06f, .86f, 1f, 1f);
    }

    private static void DrawHealthBar3D(float x, float fraction, float t)
    {
        glDisable(Lighting); DrawBox(x, 2.62f, -1.0f, .95f, .11f, .05f, .19f, .08f, .09f);
        DrawBox(x - .48f + .48f * Math.Clamp(fraction, 0f, 1f), 2.63f, -1.04f, .94f * Math.Clamp(fraction, 0f, 1f), .06f, .025f, .95f, .18f, .20f);
        glEnable(Lighting);
    }

    private static void DrawEnergyStrike(float t)
    {
        glDisable(Lighting); glColor3f(.44f, .92f, 1f); glBegin(Lines);
        glVertex3f(-1.4f, 1.1f, -.2f); glVertex3f(1.65f, 1.35f, -1.0f);
        glVertex3f(-1.25f, 1.2f, -.12f); glVertex3f(1.55f, 1.45f, -.9f);
        glEnd(); glEnable(Lighting);
    }

    private void DrawGuideBeacons(float t)
    {
        // Subtle distant light columns lead the eye toward the story landmarks.
        foreach (var beacon in new[] { (8, 10, .12f, .75f, .9f), (28, 10, .43f, .85f, 1f) })
        {
            glPushMatrix(); glTranslatef((beacon.Item1 - owner.SceneX) * .025f, .45f, (beacon.Item2 - owner.SceneY) * .025f);
            glDisable(Lighting); glColor3f(beacon.Item3, beacon.Item4, beacon.Item5); glBegin(Lines); glVertex3f(0, 0, 0); glVertex3f(0, .58f + .1f * (float)Math.Sin(t * 2), 0); glEnd(); glEnable(Lighting); glPopMatrix();
        }
    }

    private static void DrawCylinder(float x, float y, float z, float radius, float bottom, float top, float r, float g, float b)
    {
        glColor3f(r, g, b); glBegin(QuadStrip);
        for (int i = 0; i <= 10; i++) { double a = i * Math.PI / 5; float nx = (float)Math.Cos(a), nz = (float)Math.Sin(a); glNormal3f(nx, 0, nz); glVertex3f(x + nx * radius, y + bottom, z + nz * radius); glVertex3f(x + nx * radius * .72f, y + top, z + nz * radius * .72f); }
        glEnd();
    }

    private static void DrawEllipsoid(float x, float y, float z, float rx, float ry, float rz, float r, float g, float b)
    {
        glColor3f(r, g, b);
        const int latitudes = 8, longitudes = 12;
        for (int i = 0; i < latitudes; i++)
        {
            double lat0 = -Math.PI / 2 + Math.PI * i / latitudes, lat1 = -Math.PI / 2 + Math.PI * (i + 1) / latitudes;
            glBegin(QuadStrip);
            for (int j = 0; j <= longitudes; j++)
            {
                double lon = Math.PI * 2 * j / longitudes;
                VertexOnEllipsoid(x, y, z, rx, ry, rz, lat0, lon); VertexOnEllipsoid(x, y, z, rx, ry, rz, lat1, lon);
            }
            glEnd();
        }
    }

    private static void VertexOnEllipsoid(float x, float y, float z, float rx, float ry, float rz, double latitude, double longitude)
    {
        float nx = (float)(Math.Cos(latitude) * Math.Cos(longitude)), ny = (float)Math.Sin(latitude), nz = (float)(Math.Cos(latitude) * Math.Sin(longitude));
        glNormal3f(nx, ny, nz); glVertex3f(x + nx * rx, y + ny * ry, z + nz * rz);
    }

    private static void DrawBox(float x, float y, float z, float sx, float sy, float sz, float r, float g, float b)
    {
        glPushMatrix(); glTranslatef(x, y, z); glScalef(sx, sy, sz); glColor3f(r, g, b);
        glBegin(Quads);
        glNormal3f(0, 0, 1); glVertex3f(-.5f, -.5f, .5f); glVertex3f(.5f, -.5f, .5f); glVertex3f(.5f, .5f, .5f); glVertex3f(-.5f, .5f, .5f);
        glNormal3f(0, 0, -1); glVertex3f(.5f, -.5f, -.5f); glVertex3f(-.5f, -.5f, -.5f); glVertex3f(-.5f, .5f, -.5f); glVertex3f(.5f, .5f, -.5f);
        glNormal3f(1, 0, 0); glVertex3f(.5f, -.5f, .5f); glVertex3f(.5f, -.5f, -.5f); glVertex3f(.5f, .5f, -.5f); glVertex3f(.5f, .5f, .5f);
        glNormal3f(-1, 0, 0); glVertex3f(-.5f, -.5f, -.5f); glVertex3f(-.5f, -.5f, .5f); glVertex3f(-.5f, .5f, .5f); glVertex3f(-.5f, .5f, -.5f);
        glNormal3f(0, 1, 0); glVertex3f(-.5f, .5f, .5f); glVertex3f(.5f, .5f, .5f); glVertex3f(.5f, .5f, -.5f); glVertex3f(-.5f, .5f, -.5f);
        glNormal3f(0, -1, 0); glVertex3f(-.5f, -.5f, -.5f); glVertex3f(.5f, -.5f, -.5f); glVertex3f(.5f, -.5f, .5f); glVertex3f(-.5f, -.5f, .5f);
        glEnd(); glPopMatrix();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        owner.ProcessGameKey(e);
        base.OnKeyDown(e);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        frameTimer.Stop();
        if (context != IntPtr.Zero) { wglMakeCurrent(IntPtr.Zero, IntPtr.Zero); wglDeleteContext(context); context = IntPtr.Zero; }
        if (dc != IntPtr.Zero) { ReleaseDC(Handle, dc); dc = IntPtr.Zero; }
        ready = false;
        base.OnHandleDestroyed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) frameTimer.Dispose();
        base.Dispose(disposing);
    }
}

