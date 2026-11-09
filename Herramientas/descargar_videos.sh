#!/usr/bin/env bash
# Descarga los videos de una lista y los deja listos para Unity en cualquier sistema:
#   - VP8 + Vorbis en .webm (el único formato que Unity reproduce en Windows, macOS y Linux)
#   - Máximo 720p y 30 fps (VP8 se decodifica por CPU; más resolución pesa mucho)
#   - Se verifica que la duración sea la esperada y que no haya errores antes de copiarlo
#
# Uso:
#   Herramientas/descargar_videos.sh lista.txt [carpeta destino]
#
# La carpeta destino es relativa a la raíz del proyecto (por defecto Assets/Video/Descargas).
# Formato de cada línea de la lista:
#   URL | nombre opcional | inicio-fin opcional
# Ejemplos:
#   https://www.youtube.com/watch?v=XXXX
#   https://www.youtube.com/watch?v=XXXX | Bleach Opening 1
#   https://www.youtube.com/watch?v=XXXX | Bleach Opening 1 | 0:10-1:40

set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/comun.sh"

ALTURA_MAX=720
FPS_MAX=30
BITRATE=2500k

if [ $# -lt 1 ] || [ ! -f "$1" ]; then
    sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'
    exit 1
fi

comprobar_herramientas
LISTA="$(realpath "$1")"
DESTINO="$RAIZ_PROYECTO/${2:-Assets/Video/Descargas}"
mkdir -p "$DESTINO"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
HILOS="$(nproc 2>/dev/null || echo 4)"

ok=(); fallos=(); omitidos=()

procesar() {
    local url="$1" nombre="$2" inicio="$3" fin="$4"
    local trabajo="$TMP/actual"
    rm -rf "$trabajo"; mkdir -p "$trabajo"

    if [ -n "$nombre" ] && [ -f "$DESTINO/$(limpiar_nombre "$nombre").webm" ]; then
        echo "  ↷ Ya existe '$(limpiar_nombre "$nombre").webm', se omite."
        omitidos+=("$nombre"); return
    fi

    echo "  ↓ Descargando..."
    if ! yt-dlp --no-playlist --no-progress -q \
            -f "bv*[height<=$ALTURA_MAX]+ba/b[height<=$ALTURA_MAX]/bv*+ba/b" \
            --print-to-file "%(title)s" "$trabajo/titulo.txt" \
            -o "$trabajo/origen.%(ext)s" "$url"; then
        echo "  ✗ No se pudo descargar." >&2
        fallos+=("$url (descarga)"); return
    fi
    local origen
    origen="$(find "$trabajo" -maxdepth 1 -name 'origen.*' | head -1)"
    [ -z "$nombre" ] && nombre="$(head -1 "$trabajo/titulo.txt" 2>/dev/null)"
    nombre="$(limpiar_nombre "${nombre:-video}")"
    local salida="$DESTINO/$nombre.webm"
    if [ -f "$salida" ]; then
        echo "  ↷ Ya existe '$nombre.webm', se omite."
        omitidos+=("$nombre"); return
    fi

    # Recorte opcional y duración esperada
    local recorte=() esperado
    esperado="$(duracion "$origen")"
    if [ -n "$inicio" ]; then
        recorte+=(-ss "$(a_segundos "$inicio")")
        recorte+=(-to "$(a_segundos "$fin")")
        esperado="$(awk -v a="$(a_segundos "$inicio")" -v b="$(a_segundos "$fin")" 'BEGIN { print b - a }')"
    fi

    # Bajar resolución y fps solo si el original los supera
    local filtros="scale=-2:'min($ALTURA_MAX,ih)'"
    local fps
    fps="$(ffprobe -v error -select_streams v:0 -show_entries stream=r_frame_rate -of csv=p=0 "$origen" | awk -F/ '{ if ($2 > 0) print $1 / $2; else print $1 }')"
    if awk -v f="$fps" -v m="$FPS_MAX" 'BEGIN { exit !(f > m + 0.5) }'; then
        filtros="$filtros,fps=$FPS_MAX"
    fi

    echo "  ⚙ Convirtiendo a VP8 ($nombre)..."
    if ! ffmpeg -nostdin -v error -y -i "$origen" "${recorte[@]}" \
            -map 0:v:0 -map '0:a:0?' -vf "$filtros" \
            -c:v libvpx -b:v "$BITRATE" -crf 10 -deadline good -cpu-used 4 -threads "$HILOS" \
            -c:a libvorbis -q:a 4 -ac 2 -ar 44100 \
            "$trabajo/salida.webm"; then
        echo "  ✗ Falló la conversión." >&2
        fallos+=("$url (conversión)"); return
    fi

    if ! verificar "$trabajo/salida.webm" "$esperado"; then
        fallos+=("$url (verificación)"); return
    fi

    # Se mueve al proyecto solo cuando está completo, para que Unity no importe un archivo a medias
    mv "$trabajo/salida.webm" "$salida"
    echo "  → ${salida#$RAIZ_PROYECTO/}"
    ok+=("$nombre")
}

recorrer_lista "$LISTA"

echo
echo "===== Resumen ====="
echo "Listos:    ${#ok[@]}"
echo "Omitidos:  ${#omitidos[@]} (ya existían)"
echo "Fallidos:  ${#fallos[@]}"
for f in "${fallos[@]}"; do echo "  - $f"; done
echo "Destino: ${DESTINO#$RAIZ_PROYECTO/}"
[ ${#fallos[@]} -eq 0 ]
