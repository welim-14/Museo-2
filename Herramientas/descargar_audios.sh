#!/usr/bin/env bash
# Descarga los audios de una lista y los deja listos para las EsferaAudio:
#   - Ogg Vorbis (.ogg), estéreo, 44.1 kHz: Unity lo importa igual en todos los sistemas
#   - Volumen normalizado (-16 LUFS) para que todas las esferas suenen parecido
#   - Se verifica que la duración sea la esperada y que no haya errores antes de copiarlo
#
# Uso:
#   Herramientas/descargar_audios.sh lista.txt [carpeta destino]
#
# La carpeta destino es relativa a la raíz del proyecto (por defecto Assets/Audio/Descargas).
# Formato de cada línea de la lista:
#   URL | nombre opcional | inicio-fin opcional
# Ejemplos:
#   https://www.youtube.com/watch?v=XXXX
#   https://www.youtube.com/watch?v=XXXX | Going Home
#   https://www.youtube.com/watch?v=XXXX | Going Home | 0:05-2:30

set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/comun.sh"

if [ $# -lt 1 ] || [ ! -f "$1" ]; then
    sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'
    exit 1
fi

comprobar_herramientas
LISTA="$(realpath "$1")"
DESTINO="$RAIZ_PROYECTO/${2:-Assets/Audio/Descargas}"
mkdir -p "$DESTINO"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

ok=(); fallos=(); omitidos=()

procesar() {
    local url="$1" nombre="$2" inicio="$3" fin="$4"
    local trabajo="$TMP/actual"
    rm -rf "$trabajo"; mkdir -p "$trabajo"

    if [ -n "$nombre" ] && [ -f "$DESTINO/$(limpiar_nombre "$nombre").ogg" ]; then
        echo "  ↷ Ya existe '$(limpiar_nombre "$nombre").ogg', se omite."
        omitidos+=("$nombre"); return
    fi

    echo "  ↓ Descargando..."
    if ! yt-dlp --no-playlist --no-progress -q -f "ba/b" \
            --print-to-file "%(title)s" "$trabajo/titulo.txt" \
            -o "$trabajo/origen.%(ext)s" "$url"; then
        echo "  ✗ No se pudo descargar." >&2
        fallos+=("$url (descarga)"); return
    fi
    local origen
    origen="$(find "$trabajo" -maxdepth 1 -name 'origen.*' | head -1)"
    [ -z "$nombre" ] && nombre="$(head -1 "$trabajo/titulo.txt" 2>/dev/null)"
    nombre="$(limpiar_nombre "${nombre:-audio}")"
    local salida="$DESTINO/$nombre.ogg"
    if [ -f "$salida" ]; then
        echo "  ↷ Ya existe '$nombre.ogg', se omite."
        omitidos+=("$nombre"); return
    fi

    local recorte=() esperado
    esperado="$(duracion "$origen")"
    if [ -n "$inicio" ]; then
        recorte+=(-ss "$(a_segundos "$inicio")")
        recorte+=(-to "$(a_segundos "$fin")")
        esperado="$(awk -v a="$(a_segundos "$inicio")" -v b="$(a_segundos "$fin")" 'BEGIN { print b - a }')"
    fi

    echo "  ⚙ Convirtiendo a Ogg Vorbis ($nombre)..."
    if ! ffmpeg -nostdin -v error -y -i "$origen" "${recorte[@]}" \
            -map 0:a:0 -vn -af "loudnorm=I=-16:TP=-1.5:LRA=11" \
            -c:a libvorbis -q:a 5 -ac 2 -ar 44100 \
            "$trabajo/salida.ogg"; then
        echo "  ✗ Falló la conversión." >&2
        fallos+=("$url (conversión)"); return
    fi

    if ! verificar "$trabajo/salida.ogg" "$esperado"; then
        fallos+=("$url (verificación)"); return
    fi

    mv "$trabajo/salida.ogg" "$salida"
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
