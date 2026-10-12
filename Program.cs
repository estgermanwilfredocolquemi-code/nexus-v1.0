using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    static string nombre = "";
    static string genero = "";
    static int edad;
    static int estadoFisico;

    static string realidad = "";

    static int energia;
    static int conexion = 100;
    static int peligro;

    static int investigacionesIRIS = 0;
    static int recursos = 0;

    static bool conectado = true;

    static string[] realidades =
    {
        "TIERRA-01",
        "TIERRA-02",
        "TIERRA-07",
        "NEXUS-09"
    };

    // SISTEMAS POO

    static Personaje personaje;
    static Inventario inventario = new Inventario();
    static Mision mision = new Mision("LA ANOMALÍA PERDIDA");
    static List<EventoNexus> eventos = new List<EventoNexus>();
    static Random aleatorio = new Random();

    static int filaJugador = -1;
    static int columnaJugador = -1;
    static bool posicionIniciada = false;

    static void Main()
    {
        Console.Title = "NEXUS SYSTEM v1.0";

        ConfigurarConsola();

        InicioNexus();

        RegistrarExplorador();

        SeleccionarPersonaje();

        SeleccionarRealidad();

        InicializarMision();

        EvaluacionInicial();

        if (!conectado)
        {
            SalidaControlada();
            return;
        }

        IntroduccionNarrativa();

        Pausa();

        while (conectado)
        {
            MostrarInterfazPrincipal();

            string opcion = Console.ReadLine() ?? "";

            Console.Clear();

            switch (opcion)
            {
                case "1":
                    Explorar();
                    break;

                case "2":
                    AnalizarIRIS();
                    break;

                case "3":
                    MostrarEstado();
                    break;

                case "4":
                    RecuperarEnergia();
                    break;

                case "5":
                    EscanearRealidad();
                    break;

                case "6":
                    MostrarMapa();
                    break;

                case "7":
                    ComunicacionIRIS();
                    break;

                case "8":
                    MostrarNarrativa();
                    break;

                case "9":
                    Desconectar();
                    break;

                default:
                    Error("COMANDO NO RECONOCIDO");

                    Console.WriteLine();
                    Console.WriteLine(
                        "  Selecciona una opción del 1 al 9.");
                    break;
            }

            LimitarValores();

            if (conectado)
                EvaluarSituacion();

            if (conectado)
                Pausa();
        }

        SalidaControlada();
    }

    // CONFIGURACIÓN

    static void ConfigurarConsola()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.Clear();

        Console.CursorVisible = false;

        try
        {
            Console.SetWindowSize(120, 42);
            Console.SetBufferSize(120, 42);
        }
        catch
        {
        }
    }

    // INICIO

    static void InicioNexus()
    {
        Console.Clear();

        DibujarTituloGrande();

        Console.WriteLine();

        MostrarCerebro();

        Console.WriteLine();

        BarraCargaEntrada();

        Progreso("Inicializando núcleo NEXUS");
        Progreso("Conectando red neuronal");
        Progreso("Cargando base de realidades");
        Progreso("Activando sistema IRIS");
        Progreso("Preparando matriz de mapas");

        Console.WriteLine();

        Exito("NÚCLEO NEXUS EN LÍNEA");
        Exito("SISTEMA IRIS PREPARADO");
        Exito("CONEXIÓN MULTIVERSAL ACTIVA");
        Exito("MATRICES DE MAPA CARGADAS");

        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine(
            CentrarTexto("[ ENTER ]", 116));

        Console.WriteLine(
            CentrarTexto("ACCEDER AL SISTEMA NEXUS", 116));

        Console.ResetColor();

        Pausa();
    }

    // BARRA DE CARGA DE ENTRADA

    static void BarraCargaEntrada()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.WriteLine(
            CentrarTexto(
                "SINCRONIZACIÓN DE ENTRADA MULTIVERSAL",
                116));

        Console.ResetColor();

        for (int i = 0; i <= 100; i += 5)
        {
            Console.Write("\r  [");

            int llenos = i / 5;

            Console.ForegroundColor =
                i >= 60
                    ? ConsoleColor.Green
                    : ConsoleColor.Cyan;

            for (int j = 0; j < 20; j++)
            {
                Console.Write(
                    j < llenos
                        ? "█"
                        : "░");
            }

            Console.ResetColor();

            Console.Write(
                "] " +
                i.ToString().PadLeft(3) +
                "%");

            Thread.Sleep(80);
        }

        Console.WriteLine();
        Console.WriteLine();
    }

    // TÍTULO

    static void DibujarTituloGrande()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.WriteLine(CentrarTexto(
            "███╗   ██╗███████╗██╗  ██╗██╗   ██╗███████╗",
            116));

        Console.WriteLine(CentrarTexto(
            "████╗  ██║██╔════╝╚██╗██╔╝██║   ██║██╔════╝",
            116));

        Console.WriteLine(CentrarTexto(
            "██╔██╗ ██║█████╗   ╚███╔╝ ██║   ██║███████╗",
            116));

        Console.WriteLine(CentrarTexto(
            "██║╚██╗██║██╔══╝   ██╔██╗ ╚██╗ ██╔╝╚════██║",
            116));

        Console.WriteLine(CentrarTexto(
            "██║ ╚████║███████╗██╔╝ ██╗ ╚████╔╝ ███████║",
            116));

        Console.WriteLine(CentrarTexto(
            "╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝  ╚═══╝  ╚══════╝",
            116));

        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine();

        Console.WriteLine(
            CentrarTexto(
                "N E X U S   S Y S T E M   v1.0",
                116));

        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine(
            CentrarTexto(
                "CONTROL DEL MULTIVERSO",
                116));

        Console.ResetColor();
    }

    // CEREBRO

    static void MostrarCerebro()
    {
        string[] cerebro =
        {
            "             ╭───────────╮",
            "          ╭──╯     ◉     ╰──╮",
            "        ╭─╯   ╲   │   ╱     ╰─╮",
            "       │       ╔═══════╗       │",
            "       │   ╭───║ NEXUS ║───╮   │",
            "       │   ╰───║       ║───╯   │",
            "        ╰─╮    ╚═══════╝    ╭─╯",
            "          ╰───────╥──────────╯",
            "                  ║",
            "                ╔═╩═╗"
        };

        Console.ForegroundColor = ConsoleColor.DarkCyan;

        foreach (string linea in cerebro)
        {
            Console.WriteLine(
                CentrarTexto(linea, 116));
        }

        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine(
            CentrarTexto(
                "◉ NÚCLEO NEURAL ◉",
                116));

        Console.ResetColor();
    }

    // REGISTRO

    static void RegistrarExplorador()
    {
        Console.Clear();

        Encabezado("REGISTRO DEL EXPLORADOR");

        Console.WriteLine();

        Console.Write(
            "  NOMBRE DEL EXPLORADOR : ");

        nombre = Console.ReadLine() ?? "";

        while (string.IsNullOrWhiteSpace(nombre))
        {
            Advertencia(
                "El nombre no puede estar vacío.");

            Console.Write(
                "  NOMBRE DEL EXPLORADOR : ");

            nombre = Console.ReadLine() ?? "";
        }

        Console.WriteLine();

        Console.WriteLine(
            "  ¿CÓMO TE IDENTIFICAS?");

        Console.WriteLine(
            "  [1] Masculino (hombre)");

        Console.WriteLine(
            "  [2] Femenino (mujer)");

        int opcionGenero =
            LeerRango(
                "  SELECCIONA [1-2]       : ",
                1,
                2);

        genero =
            opcionGenero == 1
                ? "Masculino / Hombre"
                : "Femenino / Mujer";

        edad = LeerRango(
            "  EDAD                  : ",
            1,
            100);

        estadoFisico = LeerRango(
            "  ESTADO FÍSICO [0-100] : ",
            0,
            100);

        energia = estadoFisico;

        Console.WriteLine();

        Exito("IDENTIDAD REGISTRADA");

        Console.WriteLine();

        Console.WriteLine(
            "  EXPLORADOR     : " + nombre);

        Console.WriteLine(
            "  GÉNERO         : " + genero);

        Console.WriteLine(
            "  EDAD           : " + edad);

        Console.WriteLine(
            "  ESTADO FÍSICO   : " +
            estadoFisico +
            "%");

        Console.WriteLine(
            "  ENERGÍA INICIAL : " +
            energia +
            "%");

        Pausa();
    }

    // SELECCIÓN DE PERSONAJE

    static void SeleccionarPersonaje()
    {
        Console.Clear();

        Encabezado(
            "SELECCIÓN DE CLASE DE PERSONAJE");

        Console.WriteLine(
            "  Selecciona la entidad que controlarás durante la misión.");

        Console.WriteLine();

        Console.WriteLine(
            "  [1] HUMANO        - Explorador NEXUS");

        Console.WriteLine(
            "  [2] ROBOT         - Unidad tecnológica");

        Console.WriteLine(
            "  [3] ENEMIGO       - Entidad hostil");

        Console.WriteLine(
            "  [4] ROBOT ENEMIGO - Unidad de combate");

        Console.WriteLine();

        int opcion =
            LeerRango(
                "  SELECCIONAR CLASE [1-4] : ",
                1,
                4);

        switch (opcion)
        {
            case 1:
                personaje =
                    new Humano(nombre);
                break;

            case 2:
                personaje =
                    new Robot(nombre, false);
                break;

            case 3:
                personaje =
                    new Enemigo(nombre, false);
                break;

            case 4:
                personaje =
                    new EnemigoRobot(nombre, false);
                break;
        }

        personaje.AplicarEstadisticas();

        Console.WriteLine();

        Exito(
            "CLASE ASIGNADA: " +
            personaje.Tipo);

        Console.WriteLine();

        Console.WriteLine(
            "  SALUD   : " +
            personaje.Salud +
            "%");

        Console.WriteLine(
            "  ATAQUE  : " +
            personaje.Ataque);

        Console.WriteLine(
            "  DEFENSA : " +
            personaje.Defensa);

        Console.WriteLine(
            "  ESCUDO  : " +
            personaje.Escudo +
            "%");

        Pausa();
    }

    // SELECCIÓN DE REALIDAD

    static void SeleccionarRealidad()
    {
        Console.Clear();

        Encabezado(
            "SELECCIÓN DE REALIDAD");

        Console.WriteLine();

        for (int i = 0;
             i < realidades.Length;
             i++)
        {
            Console.ForegroundColor =
                i == 1
                    ? ConsoleColor.Red
                    : ConsoleColor.Cyan;

            string descripcion = "";

            if (realidades[i] == "TIERRA-01")
                descripcion = "Mundo Natural";

            if (realidades[i] == "TIERRA-02")
                descripcion = "Zona Zombi";

            if (realidades[i] == "TIERRA-07")
                descripcion = "Mundo Criogénico";

            if (realidades[i] == "NEXUS-09")
                descripcion = "Mundo Digital";

            Console.WriteLine(
                "                 [" +
                (i + 1) +
                "]  " +
                realidades[i] +
                "  —  " +
                descripcion);
        }

        Console.ResetColor();

        Console.WriteLine();

        int opcion =
            LeerRango(
                "  SELECCIONAR REALIDAD : ",
                1,
                realidades.Length);

        realidad =
            realidades[opcion - 1];

        peligro =
            ObtenerPeligroInicial();

        Console.WriteLine();

        Exito("REALIDAD ASIGNADA");

        Console.WriteLine();

        Console.WriteLine(
            "  REALIDAD : " +
            realidad);

        Console.WriteLine(
            "  PELIGRO INICIAL : " +
            peligro +
            "%");
    }

    static int ObtenerPeligroInicial()
    {
        if (realidad == "TIERRA-02")
            return 65;

        if (realidad == "TIERRA-07")
            return 40;

        if (realidad == "NEXUS-09")
            return 30;

        return 15;
    }

    // INICIO DE MISIÓN

    static void InicializarMision()
    {
        mision =
            new Mision(
                "LA ANOMALÍA PERDIDA");

        inventario =
            new Inventario();

        eventos.Clear();

        posicionIniciada = false;

        inventario.Agregar(
            new Objeto(
                "Célula de Energía",
                TipoObjeto.Energia,
                1));

        inventario.Agregar(
            new Objeto(
                "Analizador",
                TipoObjeto.Analizador,
                1));

        RegistrarEvento(
            "MISIÓN",
            "La anomalía perdida ha sido activada en " +
            realidad);
    }

    // EVALUACIÓN

    static bool EvaluacionInicial()
    {
        if (edad < 18)
        {
            Error(
                "EDAD NO PERMITIDA PARA LA MISIÓN");

            conectado = false;

            return false;
        }

        if (energia < 20 ||
            estadoFisico < 20)
        {
            Error(
                "CONDICIONES FÍSICAS INSUFICIENTES");

            conectado = false;

            return false;
        }

        return true;
    }

    // NARRATIVA

    static void IntroduccionNarrativa()
    {
        Console.Clear();

        Encabezado(
            "MISIÓN // LA ANOMALÍA PERDIDA");

        Console.WriteLine(
            "  Año 2297.");

        Console.WriteLine(
            "  Una anomalía desconocida ha alterado una realidad.");

        Console.WriteLine(
            "  Tu objetivo es investigar la zona, recuperar el núcleo energético,");

        Console.WriteLine(
            "  sobrevivir a las entidades y regresar con vida al punto NEXUS.");

        Console.WriteLine();

        Console.WriteLine(
            "  OBJETIVOS:");

        Console.WriteLine(
            "  1. Llegar a la zona de anomalía.");

        Console.WriteLine(
            "  2. Recuperar el núcleo energético.");

        Console.WriteLine(
            "  3. Sobrevivir a un encuentro con una entidad.");

        Console.WriteLine(
            "  4. Regresar al punto de extracción NEXUS.");

        Console.WriteLine();

        Console.WriteLine(
            "  RECOMPENSA: +100 XP + 1 MÓDULO DESCONOCIDO");
    }

    // INTERFAZ PRINCIPAL
    // EL ÍNDICE ORIGINAL SE MANTIENE

    static void MostrarInterfazPrincipal()
    {
        Console.Clear();

        Encabezado(
            "NEXUS SYSTEM v1.0 // CONTROL DEL MULTIVERSO");

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "╔══════════════════════╗  ╔══════════════════════════════════════╗  ╔══════════════════════╗");

        Console.WriteLine(
            "║      NEXUS CORE      ║  ║          INFORMACIÓN DEL JUGADOR     ║  ║      REALIDAD        ║");

        Console.WriteLine(
            "╠══════════════════════╣  ╠══════════════════════════════════════╣  ╠══════════════════════╣");

        Console.ResetColor();

        Console.WriteLine(
            "║  ● NÚCLEO ACTIVO     ║  ║  EXPLORADOR : " +
            AjustarTexto(nombre, 19) +
            " ║  ║  " +
            AjustarTexto(realidad, 18) +
            " ║");

        Console.WriteLine(
            "║  ● IRIS ACTIVA       ║  ║  EDAD       : " +
            edad.ToString().PadRight(19) +
            " ║  ║  PELIGRO             ║");

        Console.WriteLine(
            "║  ● ENLACE ACTIVO     ║  ║  ESTADO FÍSICO : " +
            estadoFisico.ToString().PadRight(15) +
            "% ║  ║  " +
            peligro.ToString().PadLeft(3) +
            "%                ║");

        Console.WriteLine(
            "╚══════════════════════╝  ╚══════════════════════════════════════╝  ╚══════════════════════╝");

        Console.WriteLine();

        MostrarPanelEstadisticas();

        Console.WriteLine();

        MostrarResumenMision();

        Console.WriteLine();

        MostrarMenu();
    }

    // ESTADÍSTICAS

    static void MostrarPanelEstadisticas()
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "╔════════════════════════════════════════════════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║                                      ESTADO DEL EXPLORADOR                                             ║");

        Console.WriteLine(
            "╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

        Console.ResetColor();

        Console.Write("║  ");

        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.Write("ENERGÍA");

        Console.ResetColor();

        Console.Write("       ");

        MostrarBarraHorizontal(
            energia,
            12);

        Console.WriteLine(
            "                                                            ║");

        Console.Write("║  ");

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.Write("CONEXIÓN");

        Console.ResetColor();

        Console.Write("      ");

        MostrarBarraHorizontal(
            conexion,
            12);

        Console.WriteLine(
            "                                                            ║");

        Console.Write("║  ");

        Console.ForegroundColor =
            ConsoleColor.Red;

        Console.Write("PELIGRO");

        Console.ResetColor();

        Console.Write("       ");

        MostrarBarraHorizontal(
            peligro,
            12);

        Console.WriteLine(
            "                                                            ║");

        Console.Write("║  ");

        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.Write("SALUD");

        Console.ResetColor();

        Console.Write("         ");

        MostrarBarraHorizontal(
            personaje.Salud,
            12);

        Console.WriteLine(
            "                                                            ║");

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "╚════════════════════════════════════════════════════════════════════════════════════════════════════════╝");

        Console.ResetColor();
    }

    // RESUMEN DE MISIÓN

    static void MostrarResumenMision()
    {
        Console.ForegroundColor =
            ConsoleColor.Magenta;

        Console.WriteLine(
            "  MISIÓN: " +
            mision.Nombre +
            "   |   PROGRESO: " +
            mision.Progreso +
            "/4   |   XP: " +
            mision.Experiencia);

        Console.ResetColor();

        Console.WriteLine(
            "  [" +
            (mision.AnomaliaEncontrada ? "X" : " ") +
            "] Anomalía   [" +
            (mision.NucleoEncontrado ? "X" : " ") +
            "] Núcleo   [" +
            (mision.EntidadSuperada ? "X" : " ") +
            "] Supervivencia   [" +
            (mision.Extraccion ? "X" : " ") +
            "] Extracción");

        Console.WriteLine(
            "  CLASE: " +
            personaje.Tipo +
            "   |   INVENTARIO: " +
            inventario.CantidadTotal +
            " objetos   |   EVENTOS: " +
            eventos.Count);
    }

    // MENÚ ORIGINAL

    static void MostrarMenu()
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "╔════════════════════════════════════════════════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║                                      OPERACIONES NEXUS                                                ║");

        Console.WriteLine(
            "╠════════════════════════════════════════════════════════════════════════════════════════════════════════╣");

        Console.ResetColor();

        Console.WriteLine(
            "║                                                                                                        ║");

        Console.WriteLine(
            "║     [1] EXPLORAR             [2] ANALIZAR IRIS             [3] VER ESTADO                              ║");

        Console.WriteLine(
            "║                                                                                                        ║");

        Console.WriteLine(
            "║     [4] RECUPERAR ENERGÍA    [5] ESCANEAR REALIDAD         [6] ABRIR MAPA                              ║");

        Console.WriteLine(
            "║                                                                                                        ║");

        Console.WriteLine(
            "║     [7] ENLACE IRIS          [8] HISTORIA                  [9] DESCONECTAR                             ║");

        Console.WriteLine(
            "║                                                                                                        ║");

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "╚════════════════════════════════════════════════════════════════════════════════════════════════════════╝");

        Console.ResetColor();

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.White;

        Console.Write(
            "  NEXUS > ");

        Console.ResetColor();
    }

    // EXPLORACIÓN

    static void Explorar()
    {
        Encabezado(
            "PROTOCOLO DE EXPLORACIÓN");

        Console.WriteLine();

        if (energia < 5)
        {
            Error(
                "ENERGÍA INSUFICIENTE PARA EXPLORAR");

            return;
        }

        int energiaAntes = energia;
        int conexionAntes = conexion;
        int peligroAntes = peligro;

        ExploracionLibre();

        if (conectado &&
            energia > 0)
        {
            Progreso(
                "Analizando terreno");

            Progreso(
                "Buscando recursos");

            Progreso(
                "Detectando anomalías");

            if (energia >= 60 &&
                conexion >= 60 &&
                peligro <= 60)
            {
                energia =
                    Math.Max(
                        0,
                        energia - 15);

                conexion =
                    Math.Max(
                        0,
                        conexion - 5);

                peligro =
                    Math.Max(
                        0,
                        peligro - 5);

                recursos += 10;

                Exito(
                    "EXPLORACIÓN COMPLETADA");

                Console.WriteLine(
                    "  RECURSOS ENCONTRADOS : +10");
            }
            else if (
                energia >= 40 &&
                conexion >= 40)
            {
                energia =
                    Math.Max(
                        0,
                        energia - 10);

                conexion =
                    Math.Max(
                        0,
                        conexion - 10);

                peligro =
                    Math.Min(
                        100,
                        peligro + 10);

                recursos += 3;

                Advertencia(
                    "EXPLORACIÓN PARCIAL");

                Console.WriteLine(
                    "  RECURSOS ENCONTRADOS : +3");
            }
            else
            {
                peligro =
                    Math.Min(
                        100,
                        peligro + 10);

                Error(
                    "EXPLORACIÓN PELIGROSA");
            }

            MostrarCambios(
                energiaAntes,
                energia,
                conexionAntes,
                conexion,
                peligroAntes,
                peligro);
        }

        LimitarValores();
    }

    // EXPLORACIÓN EN TIEMPO REAL

    static void ExploracionLibre()
    {
        char[,] mapa =
            CrearMapaSegunRealidad();

        PrepararMisionEnMapa(mapa);

        if (!posicionIniciada)
        {
            for (int f = 0;
                 f < mapa.GetLength(0);
                 f++)
            {
                for (int c = 0;
                     c < mapa.GetLength(1);
                     c++)
                {
                    if (mapa[f, c] == 'E')
                    {
                        filaJugador = f;
                        columnaJugador = c;

                        mapa[f, c] = '.';

                        posicionIniciada = true;

                        break;
                    }
                }

                if (posicionIniciada)
                    break;
            }
        }

        bool salir = false;

        while (
            !salir &&
            energia > 0 &&
            conectado &&
            personaje.EstaVivo)
        {
            Console.Clear();

            Encabezado(
                "EXPLORACIÓN EN TIEMPO REAL // " +
                realidad);

            Console.WriteLine(
                "  WASD: moverse   I: inventario   M: misión   Q: salir al menú");

            Console.WriteLine(
                "  W = arriba | A = izquierda | S = abajo | D = derecha");

            Console.WriteLine(
                "  Explorador: " +
                nombre +
                " | Clase: " +
                personaje.Tipo +
                " | Energía: " +
                energia +
                "% | Conexión: " +
                conexion +
                "%");

            Console.WriteLine();

            char[,] vista =
                (char[,])mapa.Clone();

            vista[
                filaJugador,
                columnaJugador] = 'E';

            DibujarMapa(vista);

            Console.WriteLine();

            MostrarLeyenda();

            Console.WriteLine();

            Console.WriteLine(
                "  Pulsa una tecla WASD para caminar.");

            ConsoleKey tecla =
                Console.ReadKey(true).Key;

            if (tecla == ConsoleKey.Q)
            {
                salir = true;
                continue;
            }

            if (tecla == ConsoleKey.I)
            {
                MostrarInventario();
                continue;
            }

            if (tecla == ConsoleKey.M)
            {
                MostrarMision();
                continue;
            }

            int nuevaFila = filaJugador;
            int nuevaColumna = columnaJugador;

            if (tecla == ConsoleKey.W)
                nuevaFila--;

            else if (tecla == ConsoleKey.S)
                nuevaFila++;

            else if (tecla == ConsoleKey.A)
                nuevaColumna--;

            else if (tecla == ConsoleKey.D)
                nuevaColumna++;

            else
                continue;

            if (
                nuevaFila < 0 ||
                nuevaFila >= mapa.GetLength(0) ||
                nuevaColumna < 0 ||
                nuevaColumna >= mapa.GetLength(1))
            {
                Advertencia(
                    "LÍMITE DEL MAPA: no puedes avanzar más.");

                Thread.Sleep(500);

                continue;
            }

            char destino =
                mapa[
                    nuevaFila,
                    nuevaColumna];

            if (
                destino == '~' ||
                destino == '▲' ||
                destino == '#')
            {
                Advertencia(
                    "OBSTÁCULO: no puedes pasar por aquí.");

                Thread.Sleep(500);

                continue;
            }

            filaJugador = nuevaFila;
            columnaJugador = nuevaColumna;

            energia =
                Math.Max(
                    0,
                    energia - 1);

            RegistrarEvento(
                "EXPLORACIÓN",
                "Movimiento realizado en " +
                filaJugador +
                "," +
                columnaJugador);

            ProcesarDestino(
                destino,
                mapa,
                nuevaFila,
                nuevaColumna);

            if (
                destino != 'E' &&
                destino != '.' &&
                destino != '░' &&
                destino != '·' &&
                destino != '─' &&
                destino != '│' &&
                destino != 'O' &&
                destino != 'C' &&
                destino != 'K')
            {
                mapa[
                    nuevaFila,
                    nuevaColumna] = '.';
            }
        }
    }

    // UBICACIÓN DEL NÚCLEO

    static void PrepararMisionEnMapa(char[,] mapa)
    {
        int fila = 14;
        int columna = 46;

        if (realidad == "TIERRA-02")
        {
            fila = 13;
            columna = 48;
        }
        else if (realidad == "TIERRA-07")
        {
            fila = 9;
            columna = 48;
        }
        else if (realidad == "NEXUS-09")
        {
            fila = 9;
            columna = 45;
        }

        if (
            !mision.NucleoEncontrado &&
            fila < mapa.GetLength(0) &&
            columna < mapa.GetLength(1))
        {
            mapa[fila, columna] = 'K';
        }
    }

    // PROCESAR EVENTOS DEL MAPA

    static void ProcesarDestino(
        char destino,
        char[,] mapa,
        int fila,
        int columna)
    {
        switch (destino)
        {
            case 'X':

                RegistrarEvento(
                    "ANOMALÍA",
                    "Distorsión multiversal detectada.");

                peligro =
                    Math.Min(
                        100,
                        peligro + 15);

                conexion =
                    Math.Max(
                        0,
                        conexion - 4);

                mision.AnomaliaEncontrada = true;

                Exito(
                    "⚡ ANOMALÍA DETECTADA");

                Console.WriteLine(
                    "  [IRIS] El núcleo energético está cerca.");

                break;

            case 'K':

                if (!mision.AnomaliaEncontrada)
                {
                    Advertencia(
                        "El núcleo está bloqueado. Investiga primero la anomalía.");

                    return;
                }

                if (!mision.NucleoEncontrado)
                {
                    mision.NucleoEncontrado = true;

                    inventario.Agregar(
                        new Objeto(
                            "Núcleo Energético",
                            TipoObjeto.Nucleo,
                            1));

                    mision.Experiencia += 50;

                    recursos += 5;

                    RegistrarEvento(
                        "MISIÓN",
                        "Núcleo energético recuperado.");

                    Exito(
                        "NÚCLEO ENERGÉTICO RECUPERADO");

                    Console.WriteLine(
                        "  [IRIS] Objetivo actualizado: regresar a NEXUS.");
                }

                break;

            case 'C':

                IntentarExtraccion();

                break;

            case 'R':

                EncontrarObjeto();

                break;

            case 'B':

                Encuentro(
                    new Robot(
                        "Unidad R-07",
                        false));

                break;

            case 'Z':

                Encuentro(
                    new Enemigo(
                        "Entidad Hostil",
                        false));

                break;

            case 'Y':

                Encuentro(
                    new EnemigoRobot(
                        "Robot Enemigo RX",
                        false));

                break;

            case 'I':

                AnalizarIRIS();

                break;

            case 'P':

                RegistrarEvento(
                    "EVENTO",
                    "Portal dimensional inestable.");

                peligro =
                    Math.Min(
                        100,
                        peligro + 5);

                Advertencia(
                    "PORTAL DETECTADO: la conexión dimensional se ha alterado.");

                break;

            default:

                EventoAleatorio();

                break;
        }
    }

    // OBJETOS

    static void EncontrarObjeto()
    {
        TipoObjeto tipo;

        string nombreObjeto;

        int n =
            aleatorio.Next(4);

        if (n == 0)
        {
            tipo = TipoObjeto.Energia;
            nombreObjeto = "Célula de Energía";
        }
        else if (n == 1)
        {
            tipo = TipoObjeto.Escudo;
            nombreObjeto = "Escudo de Emergencia";
        }
        else if (n == 2)
        {
            tipo = TipoObjeto.Ataque;
            nombreObjeto = "Módulo de Ataque";
        }
        else
        {
            tipo = TipoObjeto.Analizador;
            nombreObjeto = "Analizador";
        }

        inventario.Agregar(
            new Objeto(
                nombreObjeto,
                tipo,
                1));

        recursos++;

        RegistrarEvento(
            "OBJETO",
            "Encontrado: " +
            nombreObjeto);

        Exito(
            "🎁 OBJETO ENCONTRADO: " +
            nombreObjeto);
    }

    // ENCUENTROS

    static void Encuentro(
        Personaje entidad)
    {
        entidad.AplicarEstadisticas();

        RegistrarEvento(
            "ENTIDAD",
            entidad.Tipo +
            " detectado: " +
            entidad.Nombre);

        Console.Clear();

        Encabezado(
            "ENCUENTRO // " +
            entidad.Tipo.ToUpper());

        Console.WriteLine(
            "  ENTIDAD : " +
            entidad.Nombre);

        Console.WriteLine(
            "  SALUD   : " +
            entidad.Salud +
            "%");

        Console.WriteLine(
            "  ATAQUE  : " +
            entidad.Ataque);

        Console.WriteLine(
            "  DEFENSA : " +
            entidad.Defensa);

        Console.WriteLine();

        Console.WriteLine(
            "  [1] ATACAR");

        Console.WriteLine(
            "  [2] ESCAPAR");

        Console.WriteLine(
            "  [3] USAR OBJETO");

        Console.WriteLine(
            "  [4] ANALIZAR");

        Console.WriteLine();

        while (
            entidad.EstaVivo &&
            personaje.EstaVivo &&
            conectado)
        {
            Console.Write(
                "  DECISIÓN > ");

            string opcion =
                (Console.ReadLine() ?? "")
                .Trim();

            switch (opcion)
            {
                case "1":

                    if (personaje.Ataque <= 0)
                    {
                        Error(
                            "NO PUEDES ATACAR CON ATAQUE 0");

                        continue;
                    }

                    int dano =
                        personaje.Ataque +
                        aleatorio.Next(0, 6);

                    entidad.RecibirDanio(
                        dano);

                    Exito(
                        "ATAQUE REALIZADO: -" +
                        dano +
                        " HP");

                    if (entidad.EstaVivo)
                        entidad.Atacar(personaje);

                    break;

                case "2":

                    int probabilidad =
                        entidad is Robot
                            ? 65
                            : 45;

                    if (
                        aleatorio.Next(100) <
                        probabilidad)
                    {
                        Exito(
                            "ESCAPE EXITOSO");

                        RegistrarEvento(
                            "COMBATE",
                            "El jugador escapó del encuentro.");

                        return;
                    }

                    Advertencia(
                        "ESCAPE FALLIDO");

                    entidad.Atacar(
                        personaje);

                    break;

                case "3":

                    if (!UsarObjetoInventario())
                    {
                        Advertencia(
                            "No se utilizó ningún objeto.");

                        continue;
                    }

                    if (entidad.EstaVivo)
                        entidad.Atacar(personaje);

                    break;

                case "4":

                    Console.WriteLine();

                    Console.WriteLine(
                        "  [IRIS] Tipo: " +
                        entidad.Tipo);

                    Console.WriteLine(
                        "  [IRIS] Defensa: " +
                        entidad.Defensa);

                    Console.WriteLine(
                        "  [IRIS] Nivel de amenaza: " +
                        entidad.NivelAmenaza);

                    investigacionesIRIS++;

                    mision.Experiencia += 10;

                    RegistrarEvento(
                        "ANÁLISIS",
                        "Entidad analizada por IRIS.");

                    entidad.Reaccionar(
                        personaje);

                    break;

                default:

                    Error(
                        "OPCIÓN INVÁLIDA. Debes elegir 1, 2, 3 o 4.");

                    break;
            }

            LimitarValores();
        }

        if (
            !entidad.EstaVivo &&
            personaje.EstaVivo)
        {
            Exito(
                "ENTIDAD SUPERADA");

            mision.EntidadSuperada = true;

            mision.Experiencia += 30;

            RegistrarEvento(
                "COMBATE",
                "Entidad neutralizada.");
        }

        if (!personaje.EstaVivo)
        {
            Error(
                "EL PERSONAJE HA CAÍDO.");

            conectado = false;
        }
    }

    // USAR OBJETO

    static bool UsarObjetoInventario()
    {
        if (inventario.CantidadTotal == 0)
        {
            Error(
                "INVENTARIO VACÍO");

            return false;
        }

        MostrarInventario(false);

        Console.Write(
            "  NÚMERO DEL OBJETO > ");

        if (!int.TryParse(
            Console.ReadLine(),
            out int indice))
        {
            Error(
                "OPCIÓN INVÁLIDA");

            return false;
        }

        Objeto objeto =
            inventario.ObtenerPorIndice(
                indice);

        if (objeto == null)
        {
            Error(
                "OBJETO INEXISTENTE O NO POSEÍDO");

            return false;
        }

        switch (objeto.Tipo)
        {
            case TipoObjeto.Energia:

                int antes =
                    energia;

                energia =
                    Math.Min(
                        100,
                        energia + 25);

                Exito(
                    "ENERGÍA: " +
                    antes +
                    " → " +
                    energia);

                break;

            case TipoObjeto.Escudo:

                personaje.Escudo =
                    Math.Min(
                        100,
                        personaje.Escudo + 40);

                Exito(
                    "ESCUDO RESTAURADO +40");

                break;

            case TipoObjeto.Ataque:

                personaje.Ataque += 10;

                Exito(
                    "MÓDULO DE ATAQUE: +10 ATAQUE");

                break;

            case TipoObjeto.Analizador:

                investigacionesIRIS++;

                mision.Experiencia += 15;

                Exito(
                    "ANALIZADOR UTILIZADO: IRIS AMPLIFICADA");

                break;

            case TipoObjeto.ModuloDesconocido:

                mision.Experiencia += 50;

                Exito(
                    "MÓDULO DESCONOCIDO INSTALADO");

                break;

            case TipoObjeto.Nucleo:

                Advertencia(
                    "El núcleo es un objetivo de misión y no puede consumirse.");

                return false;
        }

        inventario.Eliminar(
            objeto.Nombre);

        RegistrarEvento(
            "INVENTARIO",
            "Utilizado: " +
            objeto.Nombre);

        return true;
    }

    // INVENTARIO

    static void MostrarInventario(
        bool pausa = true)
    {
        if (pausa)
        {
            Console.Clear();

            Encabezado(
                "INVENTARIO NEXUS");
        }
        else
        {
            Console.WriteLine(
                "  INVENTARIO:");
        }

        if (inventario.CantidadTotal == 0)
        {
            Console.WriteLine(
                "  INVENTARIO VACÍO");
        }
        else
        {
            for (
                int i = 0;
                i < inventario.Items.Count;
                i++)
            {
                Objeto objeto =
                    inventario.Items[i];

                Console.WriteLine(
                    "  [" +
                    (i + 1) +
                    "] " +
                    objeto.Nombre +
                    " x" +
                    objeto.Cantidad +
                    " | " +
                    objeto.Tipo);
            }
        }

        Console.WriteLine();

        Console.WriteLine(
            "  Agregar, consultar, utilizar y eliminar objetos está habilitado.");

        if (pausa)
            Pausa();
    }

    // MISIÓN

    static void MostrarMision()
    {
        Console.Clear();

        Encabezado(
            "MISIÓN // " +
            mision.Nombre);

        Console.WriteLine(
            "  [" +
            (mision.AnomaliaEncontrada ? "X" : " ") +
            "] Llegar a la zona de anomalía");

        Console.WriteLine(
            "  [" +
            (mision.NucleoEncontrado ? "X" : " ") +
            "] Encontrar el núcleo energético");

        Console.WriteLine(
            "  [" +
            (mision.EntidadSuperada ? "X" : " ") +
            "] Sobrevivir al encuentro con una entidad");

        Console.WriteLine(
            "  [" +
            (mision.Extraccion ? "X" : " ") +
            "] Regresar al punto de extracción");

        Console.WriteLine();

        Console.WriteLine(
            "  PROGRESO : " +
            mision.Progreso +
            "/4");

        Console.WriteLine(
            "  XP       : " +
            mision.Experiencia);

        Console.WriteLine();

        Console.WriteLine(
            "  RECOMPENSA:");

        Console.WriteLine(
            "  +100 XP");

        Console.WriteLine(
            "  +1 Módulo Desconocido");

        Pausa();
    }

    // EVENTOS ALEATORIOS

    static void EventoAleatorio()
    {
        int evento =
            aleatorio.Next(100);

        if (evento < 15)
        {
            energia =
                Math.Max(
                    0,
                    energia - 8);

            RegistrarEvento(
                "EVENTO",
                "PÉRDIDA DE ENERGÍA");

            Advertencia(
                "⚡ PÉRDIDA DE ENERGÍA: -8");
        }
        else if (evento < 28)
        {
            conexion =
                Math.Max(
                    0,
                    conexion - 8);

            RegistrarEvento(
                "EVENTO",
                "SISTEMA DAÑADO");

            Error(
                "🚨 SISTEMA DAÑADO: conexión -8");
        }
        else if (evento < 40)
        {
            mision.Experiencia += 10;

            RegistrarEvento(
                "EVENTO",
                "INFORMACIÓN DESCUBIERTA");

            Exito(
                "INFORMACIÓN DESCUBIERTA: +10 XP");
        }
        else if (evento < 48)
        {
            EncontrarObjeto();
        }
    }

    // REGISTRO DE EVENTOS

    static void RegistrarEvento(
        string tipo,
        string descripcion)
    {
        eventos.Add(
            new EventoNexus(
                tipo,
                descripcion));

        if (eventos.Count > 50)
            eventos.RemoveAt(0);
    }

    // IRIS

    static void AnalizarIRIS()
    {
        Console.Clear();

        Encabezado(
            "IRIS // ANÁLISIS MULTIVERSAL");

        Console.WriteLine(
            "  REALIDAD        : " +
            realidad);

        Console.WriteLine(
            "  CLASE           : " +
            personaje.Tipo);

        Console.WriteLine(
            "  NIVEL PELIGRO   : " +
            peligro +
            "%");

        Console.WriteLine(
            "  CONEXIÓN        : " +
            conexion +
            "%");

        Console.WriteLine(
            "  ANOMALÍA        : " +
            (mision.AnomaliaEncontrada
                ? "DETECTADA"
                : "NO CONFIRMADA"));

        Console.WriteLine(
            "  NÚCLEO          : " +
            (mision.NucleoEncontrado
                ? "RECUPERADO"
                : "NO RECUPERADO"));

        Console.WriteLine(
            "  ENTIDAD         : " +
            (mision.EntidadSuperada
                ? "SUPERADA"
                : "PENDIENTE"));

        investigacionesIRIS++;

        mision.Experiencia += 5;

        RegistrarEvento(
            "IRIS",
            "Análisis general ejecutado.");
    }

    // ESTADO

    static void MostrarEstado()
    {
        Encabezado(
            "ESTADO DEL EXPLORADOR");

        Console.WriteLine();

        Console.WriteLine(
            "  EXPLORADOR    : " +
            nombre);

        Console.WriteLine(
            "  EDAD          : " +
            edad);

        Console.WriteLine(
            "  REALIDAD      : " +
            realidad);

        Console.WriteLine(
            "  CLASE         : " +
            personaje.Tipo);

        Console.WriteLine();

        Console.WriteLine(
            "  ESTADO FÍSICO");

        MostrarBarraCompleta(
            estadoFisico);

        Console.WriteLine();

        Console.WriteLine(
            "  SALUD");

        MostrarBarraCompleta(
            personaje.Salud);

        Console.WriteLine();

        Console.WriteLine(
            "  ESCUDO");

        MostrarBarraCompleta(
            personaje.Escudo);

        Console.WriteLine();

        Console.WriteLine(
            "  ENERGÍA");

        MostrarBarraCompleta(
            energia);

        Console.WriteLine();

        Console.WriteLine(
            "  CONEXIÓN");

        MostrarBarraCompleta(
            conexion);

        Console.WriteLine();

        Console.WriteLine(
            "  PELIGRO");

        MostrarBarraCompleta(
            peligro);

        Console.WriteLine();

        Console.WriteLine(
            "  ATAQUE              : " +
            personaje.Ataque);

        Console.WriteLine(
            "  DEFENSA             : " +
            personaje.Defensa);

        Console.WriteLine(
            "  INVESTIGACIONES IRIS : " +
            investigacionesIRIS);

        Console.WriteLine(
            "  RECURSOS             : " +
            recursos);

        Console.WriteLine(
            "  EXPERIENCIA          : " +
            mision.Experiencia);

        Console.WriteLine();

        MostrarInventario(false);

        Console.WriteLine();

        if (energia <= 30)
            Advertencia("ENERGÍA BAJA");

        if (conexion <= 30)
            Advertencia("CONEXIÓN CRÍTICA");

        if (peligro >= 70)
            Error("PELIGRO EXTREMO");

        if (
            energia > 30 &&
            conexion > 30 &&
            peligro < 70)
        {
            Exito("SISTEMA ESTABLE");
        }
    }

    // RECUPERACIÓN

    static void RecuperarEnergia()
    {
        Encabezado(
            "RECUPERACIÓN DE ENERGÍA");

        Console.WriteLine();

        if (energia >= 100)
        {
            Advertencia(
                "LA ENERGÍA YA ESTÁ AL MÁXIMO");

            return;
        }

        if (
            inventario.Buscar(
                "Célula de Energía") != null)
        {
            UsarObjetoPorNombre(
                "Célula de Energía");

            return;
        }

        int energiaAntes = energia;
        int conexionAntes = conexion;
        int peligroAntes = peligro;

        if (
            conexion >= 60 &&
            peligro <= 50)
        {
            energia += 25;
            conexion += 5;

            Exito(
                "RECUPERACIÓN SEGURA");
        }
        else if (conexion >= 40)
        {
            energia += 15;
            peligro += 5;

            Advertencia(
                "RECUPERACIÓN LIMITADA");
        }
        else
        {
            energia += 10;
            peligro += 10;

            Error(
                "RECUPERACIÓN DE EMERGENCIA");
        }

        LimitarValores();

        Console.WriteLine();

        MostrarCambios(
            energiaAntes,
            energia,
            conexionAntes,
            conexion,
            peligroAntes,
            peligro);
    }

    static void UsarObjetoPorNombre(
        string nombreObjeto)
    {
        Objeto objeto =
            inventario.Buscar(
                nombreObjeto);

        if (objeto == null)
        {
            Error(
                "OBJETO NO POSEÍDO");

            return;
        }

        if (
            objeto.Tipo ==
            TipoObjeto.Energia)
        {
            int antes =
                energia;

            energia =
                Math.Min(
                    100,
                    energia + 25);

            inventario.Eliminar(
                nombreObjeto);

            Exito(
                "ENERGÍA RECUPERADA: " +
                antes +
                " → " +
                energia);

            RegistrarEvento(
                "INVENTARIO",
                "Célula de energía utilizada.");
        }
    }

    // ESCÁNER

    static void EscanearRealidad()
    {
        Console.Clear();

        Encabezado(
            "ESCÁNER DE REALIDAD");

        char[,] mapa =
            CrearMapaSegunRealidad();

        int recursosMapa = 0;
        int entidades = 0;
        int anomalias = 0;

        for (
            int f = 0;
            f < mapa.GetLength(0);
            f++)
        {
            for (
                int c = 0;
                c < mapa.GetLength(1);
                c++)
            {
                if (mapa[f, c] == 'R')
                    recursosMapa++;

                if (
                    mapa[f, c] == 'Z' ||
                    mapa[f, c] == 'B' ||
                    mapa[f, c] == 'Y')
                {
                    entidades++;
                }

                if (mapa[f, c] == 'X')
                    anomalias++;
            }
        }

        Console.WriteLine(
            "  RECURSOS DETECTADOS : " +
            recursosMapa);

        Console.WriteLine(
            "  ENTIDADES           : " +
            entidades);

        Console.WriteLine(
            "  ANOMALÍAS           : " +
            anomalias);

        Console.WriteLine(
            "  EXTRACCIÓN          : símbolo C");
    }

    // MAPA

    static void MostrarMapa()
    {
        Console.Clear();

        Encabezado(
            "MAPA DEL MULTIVERSO");

        Console.WriteLine(
            "  REALIDAD ACTUAL : " +
            realidad);

        Console.WriteLine();

        char[,] mapa =
            CrearMapaSegunRealidad();

        PrepararMisionEnMapa(
            mapa);

        if (posicionIniciada)
        {
            mapa[
                filaJugador,
                columnaJugador] = 'E';
        }

        DibujarMapa(mapa);

        Console.WriteLine();

        MostrarLeyenda();

        MostrarAnalisisMapa(mapa);
    }

    // MAPA SEGÚN REALIDAD

    static char[,] CrearMapaSegunRealidad()
    {
        if (realidad == "TIERRA-01")
            return CrearTierra01();

        if (realidad == "TIERRA-02")
            return CrearTierra02();

        if (realidad == "TIERRA-07")
            return CrearTierra07();

        return CrearNexus09();
    }

    // TIERRA-01

    static char[,] CrearTierra01()
    {
        char[,] mapa =
            CrearMatriz(
                20,
                60,
                '~');

        DibujarZona(
            mapa,
            2,
            5,
            15,
            28,
            '░');

        DibujarZona(
            mapa,
            5,
            29,
            11,
            20,
            '░');

        DibujarZona(
            mapa,
            11,
            3,
            6,
            20,
            '░');

        DibujarZona(
            mapa,
            3,
            7,
            4,
            6,
            '♣');

        DibujarZona(
            mapa,
            10,
            10,
            5,
            6,
            '♣');

        DibujarZona(
            mapa,
            4,
            34,
            5,
            6,
            '♣');

        DibujarZona(
            mapa,
            12,
            28,
            5,
            7,
            '♣');

        mapa[2, 24] = '▲';
        mapa[3, 25] = '▲';
        mapa[4, 26] = '▲';
        mapa[3, 27] = '▲';

        mapa[4, 43] = '▲';
        mapa[5, 44] = '▲';
        mapa[6, 45] = '▲';

        for (int i = 5; i < 15; i++)
        {
            mapa[i, 22] = '~';
            mapa[i, 23] = '~';
        }

        mapa[8, 35] = '#';
        mapa[8, 36] = '#';
        mapa[8, 37] = '#';

        mapa[9, 35] = '#';
        mapa[9, 36] = '#';
        mapa[9, 37] = '#';

        mapa[10, 35] = '#';
        mapa[10, 36] = '#';

        for (int i = 6; i < 17; i++)
            mapa[i, 30] = '.';

        for (int i = 30; i < 38; i++)
            mapa[12, i] = '.';

        mapa[12, 33] = 'C';

        mapa[4, 14] = 'I';

        mapa[14, 18] = 'E';

        mapa[15, 46] = 'X';
        mapa[16, 46] = 'X';
        mapa[16, 47] = 'X';
        mapa[17, 46] = 'X';

        mapa[14, 42] = 'Y';
        mapa[13, 40] = 'B';
        mapa[7, 48] = 'R';

        return mapa;
    }

    // TIERRA-02

    static char[,] CrearTierra02()
    {
        char[,] mapa =
            CrearMatriz(
                20,
                60,
                '░');

        DibujarZona(
            mapa,
            2,
            1,
            3,
            15,
            '~');

        DibujarZona(
            mapa,
            16,
            35,
            3,
            23,
            '~');

        DibujarZona(
            mapa,
            2,
            4,
            7,
            10,
            '♣');

        DibujarZona(
            mapa,
            12,
            2,
            5,
            11,
            '♣');

        DibujarZona(
            mapa,
            4,
            20,
            8,
            25,
            '#');

        for (
            int columna = 20;
            columna < 45;
            columna++)
        {
            mapa[8, columna] = '.';
        }

        for (
            int fila = 4;
            fila < 13;
            fila++)
        {
            mapa[fila, 31] = '.';
        }

        mapa[5, 22] = '#';
        mapa[5, 23] = '#';
        mapa[6, 22] = '#';

        mapa[5, 37] = '#';
        mapa[6, 37] = '#';
        mapa[6, 38] = '#';

        mapa[10, 25] = '#';
        mapa[10, 26] = '#';
        mapa[11, 26] = '#';

        mapa[13, 18] = '†';
        mapa[14, 20] = '†';
        mapa[15, 22] = '†';

        mapa[6, 27] = 'Z';
        mapa[7, 29] = 'Z';
        mapa[10, 34] = 'Z';
        mapa[12, 39] = 'Z';
        mapa[14, 44] = 'Z';

        mapa[15, 12] = 'R';

        mapa[3, 48] = 'I';

        mapa[9, 32] = 'C';

        mapa[17, 17] = 'E';

        mapa[14, 48] = 'X';
        mapa[14, 49] = 'X';
        mapa[15, 48] = 'X';
        mapa[15, 49] = 'X';
        mapa[16, 48] = 'X';

        mapa[11, 46] = 'Y';
        mapa[6, 45] = 'B';

        return mapa;
    }

    // TIERRA-07

    static char[,] CrearTierra07()
    {
        char[,] mapa =
            CrearMatriz(
                20,
                60,
                '░');

        DibujarZona(
            mapa,
            3,
            4,
            5,
            16,
            '~');

        DibujarZona(
            mapa,
            13,
            37,
            5,
            18,
            '~');

        for (int i = 0; i < 10; i++)
        {
            mapa[
                4 + (i % 5),
                20 + i * 2] = '▲';
        }

        mapa[2, 39] = '▲';
        mapa[3, 40] = '▲';
        mapa[4, 41] = '▲';

        mapa[8, 8] = '▲';
        mapa[9, 9] = '▲';
        mapa[10, 10] = '▲';

        mapa[14, 15] = '#';
        mapa[15, 16] = '#';
        mapa[16, 17] = '#';

        mapa[10, 30] = 'C';

        mapa[3, 52] = 'I';

        mapa[17, 25] = 'E';

        mapa[12, 22] = '/';
        mapa[13, 23] = '/';
        mapa[14, 24] = '/';

        mapa[10, 48] = 'X';
        mapa[11, 48] = 'X';
        mapa[11, 49] = 'X';
        mapa[12, 48] = 'X';

        mapa[9, 48] = 'K';
        mapa[8, 46] = 'Y';
        mapa[6, 50] = 'B';
        mapa[15, 30] = 'R';

        return mapa;
    }

    // NEXUS-09

    static char[,] CrearNexus09()
    {
        char[,] mapa =
            CrearMatriz(
                20,
                60,
                '·');

        for (
            int fila = 3;
            fila < 18;
            fila += 4)
        {
            for (
                int columna = 4;
                columna < 56;
                columna++)
            {
                mapa[fila, columna] = '─';
            }
        }

        for (
            int columna = 8;
            columna < 56;
            columna += 8)
        {
            for (
                int fila = 2;
                fila < 19;
                fila++)
            {
                mapa[fila, columna] = '│';
            }
        }

        for (
            int fila = 3;
            fila < 18;
            fila += 4)
        {
            for (
                int columna = 8;
                columna < 56;
                columna += 8)
            {
                mapa[fila, columna] = 'O';
            }
        }

        mapa[11, 32] = 'C';

        mapa[3, 48] = 'I';

        mapa[17, 8] = 'E';

        mapa[7, 24] = 'P';
        mapa[15, 40] = 'P';

        mapa[10, 45] = 'X';
        mapa[11, 45] = 'X';
        mapa[12, 45] = 'X';

        mapa[9, 45] = 'K';

        mapa[8, 42] = 'Y';
        mapa[6, 50] = 'B';
        mapa[13, 30] = 'R';

        return mapa;
    }

    // MATRIZ

    static char[,] CrearMatriz(
        int filas,
        int columnas,
        char caracter)
    {
        char[,] matriz =
            new char[
                filas,
                columnas];

        for (
            int fila = 0;
            fila < filas;
            fila++)
        {
            for (
                int columna = 0;
                columna < columnas;
                columna++)
            {
                matriz[
                    fila,
                    columna] = caracter;
            }
        }

        return matriz;
    }

    // ZONAS

    static void DibujarZona(
        char[,] mapa,
        int filaInicio,
        int columnaInicio,
        int alto,
        int ancho,
        char caracter)
    {
        for (
            int fila = filaInicio;
            fila < filaInicio + alto;
            fila++)
        {
            for (
                int columna = columnaInicio;
                columna < columnaInicio + ancho;
                columna++)
            {
                if (
                    fila >= 0 &&
                    fila < mapa.GetLength(0) &&
                    columna >= 0 &&
                    columna < mapa.GetLength(1))
                {
                    mapa[
                        fila,
                        columna] =
                        caracter;
                }
            }
        }
    }

    // DIBUJAR MAPA

    static void DibujarMapa(
        char[,] mapa)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "  ╔══════════════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "  ║                         MAPA DE REALIDAD                            ║");

        Console.WriteLine(
            "  ╠══════════════════════════════════════════════════════════════════════╣");

        Console.ResetColor();

        for (
            int fila = 0;
            fila < mapa.GetLength(0);
            fila++)
        {
            Console.ForegroundColor =
                ConsoleColor.DarkCyan;

            Console.Write(
                "  ║ ");

            for (
                int columna = 0;
                columna < mapa.GetLength(1);
                columna++)
            {
                char elemento =
                    mapa[
                        fila,
                        columna];

                AplicarColorMapa(
                    elemento);

                Console.Write(
                    elemento);

                Console.ResetColor();

                Console.ForegroundColor =
                    ConsoleColor.DarkCyan;

                Console.Write(' ');
            }

            Console.WriteLine(
                "║");
        }

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "  ╚══════════════════════════════════════════════════════════════════════╝");

        Console.ResetColor();
    }

    // COLORES

    static void AplicarColorMapa(
        char elemento)
    {
        switch (elemento)
        {
            case '~':
                Console.ForegroundColor =
                    ConsoleColor.Blue;
                break;

            case '░':
                Console.ForegroundColor =
                    ConsoleColor.Green;
                break;

            case '♣':
                Console.ForegroundColor =
                    ConsoleColor.DarkGreen;
                break;

            case '▲':
                Console.ForegroundColor =
                    ConsoleColor.Gray;
                break;

            case '#':
                Console.ForegroundColor =
                    ConsoleColor.DarkYellow;
                break;

            case '.':
                Console.ForegroundColor =
                    ConsoleColor.Yellow;
                break;

            case 'E':
                Console.ForegroundColor =
                    ConsoleColor.White;
                break;

            case 'C':
                Console.ForegroundColor =
                    ConsoleColor.Yellow;
                break;

            case 'I':
                Console.ForegroundColor =
                    ConsoleColor.Magenta;
                break;

            case 'X':
                Console.ForegroundColor =
                    ConsoleColor.Red;
                break;

            case 'Z':
                Console.ForegroundColor =
                    ConsoleColor.DarkRed;
                break;

            case 'B':
                Console.ForegroundColor =
                    ConsoleColor.Cyan;
                break;

            case 'Y':
                Console.ForegroundColor =
                    ConsoleColor.Red;
                break;

            case 'K':
                Console.ForegroundColor =
                    ConsoleColor.Green;
                break;

            case 'R':
                Console.ForegroundColor =
                    ConsoleColor.Green;
                break;

            case 'P':
                Console.ForegroundColor =
                    ConsoleColor.Magenta;
                break;

            case 'O':
                Console.ForegroundColor =
                    ConsoleColor.Cyan;
                break;

            case '†':
                Console.ForegroundColor =
                    ConsoleColor.DarkGray;
                break;

            case '/':
                Console.ForegroundColor =
                    ConsoleColor.Red;
                break;

            case '─':
            case '│':
            case '·':
                Console.ForegroundColor =
                    ConsoleColor.DarkCyan;
                break;

            default:
                Console.ForegroundColor =
                    ConsoleColor.Green;
                break;
        }
    }

    // LEYENDA

    static void MostrarLeyenda()
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "  ───────────────────────── LEYENDA DEL MAPA ─────────────────────────");

        Console.ResetColor();

        MostrarLeyendaElemento(
            '░',
            "Pasto / terreno");

        MostrarLeyendaElemento(
            '~',
            "Agua");

        MostrarLeyendaElemento(
            '♣',
            "Bosque");

        MostrarLeyendaElemento(
            '▲',
            "Montaña");

        MostrarLeyendaElemento(
            '#',
            "Edificio / estructura");

        MostrarLeyendaElemento(
            '.',
            "Camino");

        MostrarLeyendaElemento(
            'E',
            "Explorador");

        MostrarLeyendaElemento(
            'C',
            "Punto NEXUS / extracción");

        MostrarLeyendaElemento(
            'I',
            "IRIS");

        MostrarLeyendaElemento(
            'X',
            "Anomalía");

        MostrarLeyendaElemento(
            'K',
            "Núcleo energético");

        MostrarLeyendaElemento(
            'B',
            "Robot");

        MostrarLeyendaElemento(
            'Y',
            "Robot enemigo");

        MostrarLeyendaElemento(
            'Z',
            "Enemigo");

        MostrarLeyendaElemento(
            'R',
            "Recurso");

        MostrarLeyendaElemento(
            'P',
            "Portal");
    }

    static void MostrarLeyendaElemento(
        char simbolo,
        string descripcion)
    {
        AplicarColorMapa(
            simbolo);

        Console.Write(
            "  " +
            simbolo +
            " ");

        Console.ResetColor();

        Console.WriteLine(
            "= " +
            descripcion);
    }

    // ANÁLISIS DEL MAPA

    static void MostrarAnalisisMapa(
        char[,] mapa)
    {
        int anomalias = 0;
        int nodos = 0;
        int enemigos = 0;
        int robots = 0;

        for (
            int fila = 0;
            fila < mapa.GetLength(0);
            fila++)
        {
            for (
                int columna = 0;
                columna < mapa.GetLength(1);
                columna++)
            {
                if (mapa[fila, columna] == 'X')
                    anomalias++;

                if (mapa[fila, columna] == 'O')
                    nodos++;

                if (
                    mapa[fila, columna] == 'Z' ||
                    mapa[fila, columna] == 'Y')
                {
                    enemigos++;
                }

                if (mapa[fila, columna] == 'B')
                    robots++;
            }
        }

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "  ╔══════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "  ║                    ANÁLISIS DEL MAPA                ║");

        Console.WriteLine(
            "  ╠══════════════════════════════════════════════════════╣");

        Console.ResetColor();

        Console.WriteLine(
            "  ║  ANOMALÍAS DETECTADAS : " +
            anomalias.ToString().PadRight(25) +
            "║");

        Console.WriteLine(
            "  ║  NODOS DE ENERGÍA     : " +
            nodos.ToString().PadRight(25) +
            "║");

        Console.WriteLine(
            "  ║  ENTIDADES HOSTILES   : " +
            enemigos.ToString().PadRight(25) +
            "║");

        Console.WriteLine(
            "  ║  ROBOTS               : " +
            robots.ToString().PadRight(25) +
            "║");

        Console.WriteLine(
            "  ║  MATRIZ               : " +
            (
                mapa.GetLength(0) +
                " x " +
                mapa.GetLength(1)
            ).PadRight(25) +
            "║");

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "  ╚══════════════════════════════════════════════════════╝");

        Console.ResetColor();

        if (anomalias >= 5)
        {
            Error(
                "CONCENTRACIÓN EXTREMA DE ANOMALÍAS");
        }
        else if (anomalias >= 2)
        {
            Advertencia(
                "CONCENTRACIÓN MODERADA DE ANOMALÍAS");
        }
        else
        {
            Exito(
                "NIVEL DE ANOMALÍAS CONTROLADO");
        }
    }

    // IRIS

    static void ComunicacionIRIS()
    {
        Console.Clear();

        Encabezado(
            "COMUNICACIÓN IRIS");

        Console.WriteLine(
            "  [IRIS] Conexión estable.");

        Console.WriteLine(
            "  [IRIS] Realidad: " +
            realidad);

        Console.WriteLine(
            "  [IRIS] Clase: " +
            personaje.Tipo);

        if (energia < 30)
        {
            Console.WriteLine(
                "  [IRIS] ADVERTENCIA: energía crítica.");
        }

        if (conexion < 30)
        {
            Console.WriteLine(
                "  [IRIS] ADVERTENCIA: conexión inestable.");
        }

        if (peligro > 70)
        {
            Console.WriteLine(
                "  [IRIS] ADVERTENCIA: peligro extremo.");
        }

        if (mision.NucleoEncontrado)
        {
            Console.WriteLine(
                "  [IRIS] Núcleo asegurado. Regresa a C.");
        }

        Console.WriteLine(
            "  [IRIS] Eventos registrados: " +
            eventos.Count);
    }

    // HISTORIA

    static void MostrarNarrativa()
    {
        Console.Clear();

        Encabezado(
            "HISTORIA NEXUS");

        Console.WriteLine(
            "  Año 2297. NEXUS detecta una anomalía fuera de los patrones conocidos.");

        Console.WriteLine(
            "  El explorador entra en una realidad desconocida y debe recuperar un núcleo energético.");

        Console.WriteLine(
            "  Cada decisión puede producir una consecuencia diferente.");

        Console.WriteLine(
            "  La misión solo termina cuando el personaje regresa vivo al punto NEXUS.");

        Console.WriteLine();

        Console.WriteLine(
            "  EVENTOS REGISTRADOS:");

        int inicio =
            Math.Max(
                0,
                eventos.Count - 10);

        for (
            int i = inicio;
            i < eventos.Count;
            i++)
        {
            Console.WriteLine(
                "  - " +
                eventos[i].Tipo +
                ": " +
                eventos[i].Descripcion);
        }
    }

    // EXTRACCIÓN

    static void IntentarExtraccion()
    {
        if (!mision.NucleoEncontrado)
        {
            Advertencia(
                "EXTRACCIÓN BLOQUEADA: falta recuperar el núcleo.");

            return;
        }

        if (!mision.EntidadSuperada)
        {
            Advertencia(
                "EXTRACCIÓN BLOQUEADA: debes sobrevivir a una entidad.");

            return;
        }

        CompletarMision();
    }

    static void CompletarMision()
    {
        if (mision.Extraccion)
            return;

        mision.Extraccion = true;

        mision.Completada = true;

        mision.Experiencia += 100;

        inventario.Agregar(
            new Objeto(
                "Módulo Desconocido",
                TipoObjeto.ModuloDesconocido,
                1));

        RegistrarEvento(
            "MISIÓN",
            "Extracción completada. Regreso a NEXUS.");

        Console.Clear();

        Encabezado(
            "EXTRACCIÓN NEXUS");

        Console.WriteLine(
            "  El punto de extracción ha sido alcanzado.");

        Console.WriteLine(
            "  Preparando regreso a la realidad de origen...");

        BarraCargaRegreso();

        Exito(
            "MISIÓN COMPLETADA");

        Console.WriteLine(
            "  +100 XP");

        Console.WriteLine(
            "  +1 MÓDULO DESCONOCIDO");

        conectado = false;
    }

    // BARRA DE REGRESO

    static void BarraCargaRegreso()
    {
        for (int i = 0;
             i <= 100;
             i += 10)
        {
            Console.Write(
                "\r  [");

            for (int j = 0;
                 j < 30;
                 j++)
            {
                Console.Write(
                    j < i / 4
                        ? "█"
                        : "░");
            }

            Console.Write(
                "] " +
                i +
                "%");

            Thread.Sleep(100);
        }

        Console.WriteLine();
    }

    // EVALUACIÓN AUTOMÁTICA

    static void EvaluarSituacion()
    {
        LimitarValores();

        if (energia <= 0)
        {
            Error(
                "ENERGÍA AGOTADA");

            personaje.RecibirDanio(20);
        }

        if (conexion <= 0)
        {
            Error(
                "CONEXIÓN PERDIDA");

            conectado = false;
        }

        if (!personaje.EstaVivo)
        {
            Error(
                "EL PERSONAJE HA CAÍDO");

            conectado = false;
        }

        if (peligro >= 95)
        {
            Advertencia(
                "PELIGRO EXTREMO DETECTADO");
        }
    }

    // DESCONECTAR

    static void Desconectar()
    {
        Encabezado(
            "DESCONEXIÓN DEL SISTEMA");

        if (!mision.Completada)
        {
            Advertencia(
                "DESCONEXIÓN BLOQUEADA");

            Console.WriteLine(
                "  Debes completar la misión y regresar al punto NEXUS.");

            return;
        }

        conectado = false;

        Progreso(
            "Guardando datos del explorador");

        Progreso(
            "Cerrando canal IRIS");

        Progreso(
            "Cerrando conexión multiversal");

        Exito(
            "DESCONEXIÓN COMPLETADA");
    }

    // SALIDA

    static void SalidaControlada()
    {
        Console.Clear();

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║                         SESIÓN NEXUS FINALIZADA                              ║");

        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════════════════════╣");

        Console.ResetColor();

        Console.WriteLine();

        Console.WriteLine(
            "  EXPLORADOR       : " +
            nombre);

        Console.WriteLine(
            "  CLASE            : " +
            (personaje == null
                ? "N/D"
                : personaje.Tipo));

        Console.WriteLine(
            "  REALIDAD         : " +
            realidad);

        Console.WriteLine(
            "  INVESTIGACIONES  : " +
            investigacionesIRIS);

        Console.WriteLine(
            "  RECURSOS         : " +
            recursos);

        Console.WriteLine(
            "  XP               : " +
            mision.Experiencia);

        Console.WriteLine(
            "  MISIÓN           : " +
            (
                mision.Completada
                    ? "COMPLETADA"
                    : "INCOMPLETA"
            ));

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.DarkCyan;

        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════════════════════╝");

        Console.ResetColor();

        Console.CursorVisible = true;

        Console.WriteLine();

        Console.WriteLine(
            "  Presiona ENTER para cerrar...");

        Console.ReadLine();
    }

    // CAMBIOS

    static void MostrarCambios(
        int energiaAntes,
        int energiaAhora,
        int conexionAntes,
        int conexionAhora,
        int peligroAntes,
        int peligroAhora)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "  ╔══════════════════════════════════════════════════╗");

        Console.WriteLine(
            "  ║                 CAMBIOS DEL SISTEMA             ║");

        Console.WriteLine(
            "  ╠══════════════════════════════════════════════════╣");

        Console.ResetColor();

        MostrarCambio(
            "ENERGÍA",
            energiaAntes,
            energiaAhora);

        MostrarCambio(
            "CONEXIÓN",
            conexionAntes,
            conexionAhora);

        MostrarCambio(
            "PELIGRO",
            peligroAntes,
            peligroAhora);

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "  ╚══════════════════════════════════════════════════╝");

        Console.ResetColor();
    }

    static void MostrarCambio(
        string campo,
        int antes,
        int ahora)
    {
        int diferencia =
            ahora - antes;

        string signo =
            diferencia >= 0
                ? "+"
                : "";

        Console.WriteLine(
            "  " +
            campo.PadRight(10) +
            ": " +
            antes.ToString().PadLeft(3) +
            "%  →  " +
            signo +
            diferencia.ToString().PadLeft(3) +
            "%  →  " +
            ahora.ToString().PadLeft(3) +
            "%");
    }

    // BARRAS

    static void MostrarBarraCompleta(
        int valor)
    {
        int bloques =
            (int)Math.Round(
                valor / 100.0 * 12);

        Console.Write(
            "  [");

        if (valor >= 60)
            Console.ForegroundColor =
                ConsoleColor.Green;

        else if (valor >= 30)
            Console.ForegroundColor =
                ConsoleColor.Yellow;

        else
            Console.ForegroundColor =
                ConsoleColor.Red;

        for (
            int i = 0;
            i < 12;
            i++)
        {
            Console.Write(
                i < bloques
                    ? "█"
                    : "░");
        }

        Console.ResetColor();

        Console.WriteLine(
            "] " +
            valor +
            "%");
    }

    static void MostrarBarraHorizontal(
        int valor,
        int longitud)
    {
        int bloques =
            (int)Math.Round(
                valor / 100.0 * longitud);

        Console.Write(
            "[");

        if (valor >= 60)
            Console.ForegroundColor =
                ConsoleColor.Green;

        else if (valor >= 30)
            Console.ForegroundColor =
                ConsoleColor.Yellow;

        else
            Console.ForegroundColor =
                ConsoleColor.Red;

        for (
            int i = 0;
            i < longitud;
            i++)
        {
            Console.Write(
                i < bloques
                    ? "█"
                    : "░");
        }

        Console.ResetColor();

        Console.Write(
            "] " +
            valor.ToString().PadLeft(3) +
            "%");
    }

    // ENCABEZADO

    static void Encabezado(
        string titulo)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine();

        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════════════════════════════════════════════╗");

        Console.WriteLine(
            "║" +
            CentrarTexto(
                " NEXUS SYSTEM // " +
                titulo,
                100) +
            "║");

        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════════════════════════════════════════════╝");

        Console.ResetColor();

        Console.WriteLine();
    }

    // PROGRESO

    static void Progreso(
        string mensaje)
    {
        Console.Write(
            "  " +
            mensaje);

        int puntos =
            Math.Max(
                1,
                42 - mensaje.Length);

        Console.Write(
            new string(
                '.',
                puntos));

        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.Write(
            " COMPLETADO");

        Console.ResetColor();

        Console.WriteLine();
    }

    // MENSAJES

    static void Exito(
        string mensaje)
    {
        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.WriteLine(
            "  [✓] " +
            mensaje);

        Console.ResetColor();
    }

    static void Advertencia(
        string mensaje)
    {
        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.WriteLine(
            "  [!] " +
            mensaje);

        Console.ResetColor();
    }

    static void Error(
        string mensaje)
    {
        Console.ForegroundColor =
            ConsoleColor.Red;

        Console.WriteLine(
            "  [X] " +
            mensaje);

        Console.ResetColor();
    }

    // LIMITAR VALORES

    static void LimitarValores()
    {
        energia =
            Math.Max(
                0,
                Math.Min(
                    100,
                    energia));

        conexion =
            Math.Max(
                0,
                Math.Min(
                    100,
                    conexion));

        peligro =
            Math.Max(
                0,
                Math.Min(
                    100,
                    peligro));
    }

    // LEER RANGO

    static int LeerRango(
        string mensaje,
        int minimo,
        int maximo)
    {
        while (true)
        {
            Console.Write(
                mensaje);

            if (
                int.TryParse(
                    Console.ReadLine(),
                    out int numero))
            {
                if (
                    numero >= minimo &&
                    numero <= maximo)
                {
                    return numero;
                }
            }

            Error(
                "Valor inválido. Rango permitido: " +
                minimo +
                " - " +
                maximo);
        }
    }

    // AJUSTAR TEXTO

    static string AjustarTexto(
        string texto,
        int ancho)
    {
        if (texto.Length > ancho)
            return texto.Substring(
                0,
                ancho);

        int total =
            ancho -
            texto.Length;

        return new string(
                   ' ',
                   total / 2) +
               texto +
               new string(
                   ' ',
                   total -
                   total / 2);
    }

    // CENTRAR

    static string CentrarTexto(
        string texto,
        int ancho)
    {
        if (texto.Length >= ancho)
            return texto;

        int espaciosTotales =
            ancho -
            texto.Length;

        int izquierda =
            espaciosTotales / 2;

        int derecha =
            espaciosTotales -
            izquierda;

        return new string(
                   ' ',
                   izquierda) +
               texto +
               new string(
                   ' ',
                   derecha);
    }

    // PAUSA

    static void Pausa()
    {
        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.DarkCyan;

        Console.WriteLine(
            "  ─────────────────────────────────────────────────────────────────────────────");

        Console.ResetColor();

        Console.WriteLine();

        Console.WriteLine(
            "  Presiona ENTER para continuar...");

        Console.ReadLine();
    }
}


// ============================================================
// CLASE BASE: PERSONAJE
// ============================================================

abstract class Personaje
{
    public string Nombre { get; protected set; }

    public string Tipo { get; protected set; }

    public int Salud { get; protected set; }

    public int Ataque { get; set; }

    public int Defensa { get; protected set; }

    public int Escudo { get; set; }

    public int NivelAmenaza { get; protected set; }

    public bool Hostil { get; protected set; }

    public bool EstaVivo
    {
        get
        {
            return Salud > 0;
        }
    }

    protected Personaje(
        string nombre,
        string tipo,
        int salud,
        int ataque,
        int defensa,
        bool hostil,
        int amenaza)
    {
        Nombre = nombre;
        Tipo = tipo;
        Salud = salud;
        Ataque = ataque;
        Defensa = defensa;
        Escudo = 50;
        Hostil = hostil;
        NivelAmenaza = amenaza;
    }

    public virtual void AplicarEstadisticas()
    {
    }

    public virtual void RecibirDanio(
        int dano)
    {
        int danoEfectivo =
            Math.Max(
                0,
                dano - Defensa);

        if (Escudo > 0)
        {
            int absorbido =
                Math.Min(
                    Escudo,
                    danoEfectivo);

            Escudo -= absorbido;

            danoEfectivo -= absorbido;
        }

        Salud =
            Math.Max(
                0,
                Salud - danoEfectivo);
    }

    public virtual void Atacar(
        Personaje objetivo)
    {
        Random random =
            new Random();

        int dano =
            Ataque +
            random.Next(0, 4);

        objetivo.RecibirDanio(
            dano);

        Console.WriteLine(
            "  " +
            Nombre +
            " causa " +
            dano +
            " de daño.");
    }

    public virtual void Reaccionar(
        Personaje objetivo)
    {
        Console.WriteLine(
            "  " +
            Nombre +
            " analiza al personaje y mantiene distancia.");
    }
}


// ============================================================
// HUMANO
// ============================================================

class Humano : Personaje
{
    public Humano(
        string nombre)
        : base(
            nombre,
            "HUMANO / CADETE",
            100,
            20,
            10,
            false,
            15)
    {
    }

    public override void AplicarEstadisticas()
    {
        Escudo = 70;
    }
}


// ============================================================
// ROBOT
// ============================================================

class Robot : Personaje
{
    public Robot(
        string nombre,
        bool hostil = true)
        : base(
            nombre,
            hostil
                ? "ROBOT ENEMIGO"
                : "ROBOT",
            100,
            18,
            18,
            hostil,
            hostil
                ? 70
                : 30)
    {
    }

    public override void AplicarEstadisticas()
    {
        Escudo = 100;
    }

    public override void Reaccionar(
        Personaje objetivo)
    {
        Console.WriteLine(
            "  " +
            Nombre +
            " activa protocolo defensivo.");

        objetivo.RecibirDanio(8);
    }
}


// ============================================================
// ENEMIGO
// ============================================================

class Enemigo : Personaje
{
    public Enemigo(
        string nombre,
        bool hostil = true)
        : base(
            nombre,
            "ENEMIGO",
            85,
            22,
            8,
            hostil,
            85)
    {
    }

    public override void AplicarEstadisticas()
    {
        Escudo = 30;
    }
}


// ============================================================
// ROBOT ENEMIGO
// ============================================================

class EnemigoRobot : Personaje
{
    public EnemigoRobot(
        string nombre,
        bool hostil = true)
        : base(
            nombre,
            "ROBOT ENEMIGO",
            120,
            25,
            20,
            hostil,
            95)
    {
    }

    public override void AplicarEstadisticas()
    {
        Escudo = 100;
    }

    public override void Reaccionar(
        Personaje objetivo)
    {
        Console.WriteLine(
            "  " +
            Nombre +
            " activa blindaje y contraataca.");

        Atacar(objetivo);
    }
}


// ============================================================
// OBJETOS
// ============================================================

enum TipoObjeto
{
    Energia,
    Escudo,
    Ataque,
    Analizador,
    Nucleo,
    ModuloDesconocido
}

class Objeto
{
    public string Nombre { get; private set; }

    public TipoObjeto Tipo { get; private set; }

    public int Cantidad { get; set; }

    public Objeto(
        string nombre,
        TipoObjeto tipo,
        int cantidad)
    {
        Nombre = nombre;
        Tipo = tipo;
        Cantidad = cantidad;
    }
}


// ============================================================
// INVENTARIO
// ============================================================

class Inventario
{
    private List<Objeto> objetos =
        new List<Objeto>();

    public List<Objeto> Items
    {
        get
        {
            return objetos;
        }
    }

    public int CantidadTotal
    {
        get
        {
            int total = 0;

            foreach (Objeto objeto in objetos)
            {
                total += objeto.Cantidad;
            }

            return total;
        }
    }

    public void Agregar(
        Objeto nuevo)
    {
        Objeto existente =
            Buscar(nuevo.Nombre);

        if (existente != null)
        {
            existente.Cantidad +=
                nuevo.Cantidad;
        }
        else
        {
            objetos.Add(nuevo);
        }
    }

    public Objeto Buscar(
        string nombre)
    {
        foreach (
            Objeto objeto in objetos)
        {
            if (
                objeto.Nombre.Equals(
                    nombre,
                    StringComparison.OrdinalIgnoreCase))
            {
                return objeto;
            }
        }

        return null;
    }

    public Objeto ObtenerPorIndice(
        int indice)
    {
        if (
            indice < 1 ||
            indice > objetos.Count)
        {
            return null;
        }

        return objetos[indice - 1];
    }

    public bool Eliminar(
        string nombre)
    {
        Objeto objeto =
            Buscar(nombre);

        if (objeto == null)
            return false;

        objeto.Cantidad--;

        if (objeto.Cantidad <= 0)
            objetos.Remove(objeto);

        return true;
    }
}


// ============================================================
// MISIÓN
// ============================================================

class Mision
{
    public string Nombre { get; private set; }

    public bool AnomaliaEncontrada { get; set; }

    public bool NucleoEncontrado { get; set; }

    public bool EntidadSuperada { get; set; }

    public bool Extraccion { get; set; }

    public bool Completada { get; set; }

    public int Experiencia { get; set; }

    public int Progreso
    {
        get
        {
            int total = 0;

            if (AnomaliaEncontrada)
                total++;

            if (NucleoEncontrado)
                total++;

            if (EntidadSuperada)
                total++;

            if (Extraccion)
                total++;

            return total;
        }
    }

    public Mision(
        string nombre)
    {
        Nombre = nombre;
    }
}


class EventoNexus
{
    public string Tipo { get; private set; }

    public string Descripcion { get; private set; }

    public DateTime Momento { get; private set; }

    public EventoNexus(
        string tipo,
        string descripcion)
    {
        Tipo = tipo;
        Descripcion = descripcion;
        Momento = DateTime.Now;
    }
}