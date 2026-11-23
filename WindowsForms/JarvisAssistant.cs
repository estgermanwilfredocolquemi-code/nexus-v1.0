using System;
using System.Linq;
using System.Text;

namespace NexusWinForms;

// Asistente táctico local: analiza el estado real de la expedición sin conexión externa.
internal static class JarvisAssistant
{
    internal static string BuildInventoryReport(GameSnapshot state)
    {
        if (state == null) return "No hay una expedición activa para analizar.";
        var items = state.Inventory ?? new();
        var sb = new StringBuilder();
        sb.AppendLine("J.A.R.V.I.S. // INFORME TÁCTICO");
        sb.AppendLine($"Explorador: {state.ExplorerName}  |  Realidad: {state.Reality}");
        sb.AppendLine($"Salud: {state.CharacterHealth}  |  Energía: {state.Energy}%  |  Conexión: {state.Connection}%  |  Peligro: {state.Danger}%");
        sb.AppendLine();
        sb.AppendLine("INVENTARIO DETECTADO");
        if (items.Count == 0)
        {
            sb.AppendLine("No se detectan objetos. Recomiendo explorar zonas seguras y buscar suministros.");
        }
        else
        {
            foreach (var item in items.OrderBy(i => i.Type).ThenBy(i => i.Name))
                sb.AppendLine($"• {item.Name} x{item.Quantity}  [{DescribeType(item.Type)}]");
        }
        sb.AppendLine();
        sb.AppendLine("RECOMENDACIÓN");
        sb.AppendLine(BuildRecommendation(state));
        return sb.ToString();
    }

    internal static string Answer(GameSnapshot state, string question)
    {
        if (state == null) return "No puedo leer los datos de la expedición. Inicia o recupera una partida.";
        string q = (question ?? "").Trim().ToLowerInvariant();
        var items = state.Inventory ?? new();
        if (q.Length == 0) return "Pregúntame qué tienes, qué objeto conviene usar, cómo está tu estado o qué estrategia recomiendo.";
        if (ContainsAny(q, "inventario", "objetos", "qué tengo", "que tengo", "llevo"))
            return items.Count == 0
                ? "Tu inventario está vacío. Busca recursos antes de entrar en una zona peligrosa."
                : "Inventario actual: " + string.Join("; ", items.Select(i => $"{i.Name} x{i.Quantity} ({DescribeType(i.Type)})")) + ".";
        if (ContainsAny(q, "recomienda", "recomiendas", "debería", "deberia", "conviene", "estrategia", "qué hago", "que hago"))
            return BuildRecommendation(state);
        if (ContainsAny(q, "salud", "vida", "herido", "herida"))
            return $"Tu salud actual es {state.CharacterHealth}. " + (state.CharacterHealth <= 30 ? "Estás en estado crítico: evita enfrentamientos y prioriza curarte." : state.CharacterHealth <= 60 ? "Conviene actuar con cautela y conservar objetos defensivos." : "Tu salud permite continuar, pero no descuides la defensa.");
        if (ContainsAny(q, "energía", "energia", "batería", "bateria"))
            return $"Energía: {state.Energy}%. " + (state.Energy <= 25 ? "Nivel crítico: busca una fuente de energía y evita explorar más de lo necesario." : state.Energy <= 55 ? "Reserva energía para regresar al punto de extracción." : "Nivel suficiente para continuar con prudencia.");
        if (ContainsAny(q, "peligro", "amenaza", "riesgo"))
            return $"Nivel de peligro: {state.Danger}%. " + (state.Danger >= 70 ? "Riesgo elevado: recomiendo retirarte o prepararte antes de avanzar." : state.Danger >= 40 ? "Riesgo moderado: mantén un objeto defensivo disponible." : "El entorno parece relativamente estable.");
        if (ContainsAny(q, "escudo", "defensa"))
        {
            var defensive = items.Where(i => ContainsAny(i.Type, "Escudo") || ContainsAny(i.Name, "escudo", "blindaje", "protección", "proteccion")).ToList();
            return defensive.Count == 0 ? "No detecto objetos defensivos en tu inventario." : "Objetos defensivos detectados: " + string.Join(", ", defensive.Select(i => $"{i.Name} x{i.Quantity}")) + ". Guárdalos para zonas de alto peligro.";
        }
        if (ContainsAny(q, "ataque", "arma", "combate"))
        {
            var offensive = items.Where(i => ContainsAny(i.Type, "Ataque") || ContainsAny(i.Name, "arma", "espada", "módulo", "modulo")).ToList();
            return offensive.Count == 0 ? "No detecto mejoras de ataque en tu inventario. Evita iniciar combates si tu equipo es insuficiente." : "Equipo ofensivo detectado: " + string.Join(", ", offensive.Select(i => $"{i.Name} x{i.Quantity}")) + ".";
        }
        return "Puedo analizar inventario, salud, energía, peligro, objetos defensivos, equipo de ataque y recomendar una estrategia. Prueba: «¿Qué me recomiendas?»";
    }

    private static string BuildRecommendation(GameSnapshot state)
    {
        var items = state.Inventory ?? new();
        var energyItems = items.Where(i => ContainsAny(i.Type, "Energia") || ContainsAny(i.Name, "energía", "energia", "célula", "celula")).ToList();
        var shieldItems = items.Where(i => ContainsAny(i.Type, "Escudo") || ContainsAny(i.Name, "escudo", "blindaje")).ToList();
        var attackItems = items.Where(i => ContainsAny(i.Type, "Ataque") || ContainsAny(i.Name, "arma", "espada", "ataque")).ToList();

        if (state.CharacterHealth <= 25)
            return "PRIORIDAD MÁXIMA: tu salud es crítica. Evita el combate y busca recuperación. " + (shieldItems.Count > 0 ? "Tienes protección disponible: resérvala para cubrir una retirada." : "No detecto protección en tu inventario.");
        if (state.Energy <= 25)
            return energyItems.Count > 0 ? "Tu energía es crítica. Tienes suministros energéticos: revisa sus efectos y úsalos si el juego permite consumirlos." : "Tu energía es crítica y no detecto suministros energéticos. Busca una zona de recuperación.";
        if (state.Danger >= 70)
            return shieldItems.Count > 0 ? "Peligro elevado. Mantén tu objeto defensivo preparado y considera retirarte." : "Peligro elevado y sin protección detectada. Evita iniciar un enfrentamiento.";
        if (state.CharacterHealth <= 55 && shieldItems.Count > 0)
            return "Tu salud está reducida. Conserva el escudo para el siguiente encuentro y busca una oportunidad de recuperación.";
        if (attackItems.Count > 0 && state.Danger < 60)
            return "Tienes equipo ofensivo disponible y el peligro no es extremo. Puedes avanzar, pero conserva energía para la extracción.";
        return "Estado relativamente estable. Explora por sectores, recoge suministros y conserva recursos para la misión principal.";
    }

    private static string DescribeType(string type) => (type ?? "").ToLowerInvariant() switch
    {
        "energia" => "recuperación de energía",
        "escudo" => "protección",
        "ataque" => "mejora ofensiva",
        "analizador" => "análisis",
        "nucleo" => "objetivo de misión",
        "modulodesconocido" => "objeto especial",
        _ => "objeto sin clasificar"
    };

    private static bool ContainsAny(string value, params string[] terms)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
