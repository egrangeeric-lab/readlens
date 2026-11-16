#!/bin/bash
# Builds the ReadLens demo edit from the full headset take.
# Usage: edit.sh <take.mp4> <assets_dir> <work_dir> <out.mp4>
set -euo pipefail
V="$1"; A="$2"; W="$3"; OUT="$4"
mkdir -p "$W"; : > "$W/list.txt"
ENC=(-c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -r 30 -c:a aac -b:a 192k -ar 48000 -ac 2)

card() { # name image seconds
  ffmpeg -nostdin -v error -y -loop 1 -t "$3" -i "$2" -f lavfi -t "$3" -i anullsrc=r=48000:cl=stereo \
    -vf "scale=1920:1080,fps=30,fade=in:d=0.5,fade=out:st=$(echo "$3 - 0.5" | bc):d=0.5" "${ENC[@]}" -shortest "$W/$1.mp4"
  echo "file '$W/$1.mp4'" >> "$W/list.txt"
}

clip() { # name start end chapter(or -) speed zoom(0/1)
  local name=$1 s=$2 e=$3 ch=$4 sp=$5 zm=$6
  local dur; dur=$(echo "($e - $s) / $sp" | bc -l)
  local v="fps=30,scale=1920:1080,setpts=PTS/$sp"
  # slow push-in; upscale first so zoompan does not jitter
  [ "$zm" = 1 ] && v="$v,scale=3840:2160,zoompan=z='min(1+0.0008*on\,1.07)':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=1:s=1920x1080:fps=30"
  local a="volume=0.3,atempo=$sp,afade=t=in:d=0.15,afade=t=out:st=$(echo "$dur - 0.15" | bc -l):d=0.15"
  if [ "$ch" = "-" ]; then
    ffmpeg -nostdin -v error -y -ss "$s" -to "$e" -i "$V" -vf "$v" -af "$a" "${ENC[@]}" "$W/$name.mp4"
  else
    ffmpeg -nostdin -v error -y -ss "$s" -to "$e" -i "$V" -loop 1 -t "$dur" -i "$A/chapter_$ch.png" \
      -filter_complex "[0:v]$v[b];[1:v]format=rgba,fade=in:st=0.3:d=0.3:alpha=1,fade=out:st=3.4:d=0.4:alpha=1[o];[b][o]overlay=shortest=1[v];[0:a]$a[a]" \
      -map "[v]" -map "[a]" "${ENC[@]}" "$W/$name.mp4"
  fi
  echo "file '$W/$name.mp4'" >> "$W/list.txt"
  printf '%-14s %6.1fs\n' "$name" "$dur"
}

card  00_intro "$A/intro.png" 6
clip  01_capture      108  118   capture     1   1
clip  02_comfort_a     43   50.5 comfort     1.5 0
clip  03_comfort_b     54   62   -           1.5 0
clip  04_handwriting   84   90   handwriting 1   0
clip  05_focus         90  101   focus       1   1
clip  06_pages        118  126   pages       1   0
clip  07_settings     129  141   settings    1   0
clip  08_contrast     165  174   contrast    1   0
clip  09_thumb        191  196   thumb       1   0
clip  10_move         202  214   move        1   0
card  11_end "$A/endcard.png" 5

ffmpeg -nostdin -v error -y -f concat -safe 0 -i "$W/list.txt" -c copy -movflags +faststart "$OUT"
ffprobe -v error -show_entries format=duration -of csv=p=0 "$OUT"
