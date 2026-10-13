#!/usr/bin/env bash
# Funciones compartidas por descargar_videos.sh y descargar_audios.sh.
# No se ejecuta solo: lo cargan los otros scripts con "source".

RAIZ_PROYECTO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

comprobar_herramientas() {
    local falta=0
    for h in yt-dlp ffmpeg ffprobe; do
        if ! command -v "$h" >/dev/null 2>&1; then
            echo "Falta '$h'. Instálalo antes de continuar." >&2
            falta=1
        fi
    done
    [ "$falta" -eq 0 ] || exit 1
}

# Quita caracteres que dan problemas en nombres de archivo y recorta la longitud
limpiar_nombre() {
    printf '%s' "$1" | tr -d '/\\:*?"<>|' | tr -s ' ' | sed 's/^ *//; s/ *$//' | cut -c1-80
}

# Convierte "mm:ss" o "hh:mm:ss" (o segundos) a segundos
a_segundos() {
    awk -F: '{ s = 0; for (i = 1; i <= NF; i++) s = s * 60 + $i; print s }' <<<"$1"
}

duracion() {
    ffprobe -v error -show_entries format=duration -of csv=p=0 "$1" 2>/dev/null
}

# Recorta espacios al inicio y al final
recortar() {
    sed 's/^[[:space:]]*//; s/[[:space:]]*$//' <<<"$1"
}

# Recorre la lista y llama a "procesar url nombre inicio fin" por cada línea válida.
# Formato de cada línea:   URL | nombre opcional | inicio-fin opcional
# Las líneas vacías y las que empiezan con # se ignoran.
recorrer_lista() {
    local lista="$1"
    local n=0 linea url nombre rango inicio fin
    while IFS= read -r linea || [ -n "$linea" ]; do
        linea="${linea%$'\r'}"
        linea="$(recortar "$linea")"
        [ -z "$linea" ] && continue
        [[ "$linea" == \#* ]] && continue

        IFS='|' read -r url nombre rango <<<"$linea"
        url="$(recortar "$url")"
        nombre="$(recortar "${nombre:-}")"
        rango="$(recortar "${rango:-}")"
        inicio=""; fin=""
        if [ -n "$rango" ]; then
            inicio="$(recortar "${rango%%-*}")"
            fin="$(recortar "${rango#*-}")"
        fi

        n=$((n + 1))
        echo
        echo "=== [$n] $url"
        procesar "$url" "$nombre" "$inicio" "$fin" </dev/null
    done <"$lista"
}

# Comprueba que el archivo final dure lo esperado y no tenga errores al decodificar
verificar() {
    local archivo="$1" esperado="$2"
    local real errores
    real="$(duracion "$archivo")"
    if [ -z "$real" ]; then
        echo "  ✗ El archivo resultante no se puede leer." >&2
        return 1
    fi
    if [ -n "$esperado" ] && awk -v a="$real" -v b="$esperado" 'BEGIN { d = a - b; if (d < 0) d = -d; exit !(d > 1.5) }'; then
        echo "  ✗ Duración incorrecta: ${real}s en lugar de ${esperado}s (¿conversión incompleta?)." >&2
        return 1
    fi
    errores="$(ffmpeg -nostdin -v error -i "$archivo" -f null - 2>&1 | head -3)"
    if [ -n "$errores" ]; then
        echo "  ✗ Errores al decodificar: $errores" >&2
        return 1
    fi
    printf '  ✓ Verificado (%.1f s)\n' "$real"
}
