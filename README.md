# RencodeMP4
Re-encoding MP4 files for HTML5 (x264-libaac) with FFMPEG

This small windows program is filtering mp4 files from that you showed folder and use ffmpeg codes to re-encode file for html5 players.

FFMPEG mp4 re-encoding command:
ffmpeg.exe -i input -c:v libx264 -crf 20 -preset medium -c:a -strict experimental -b:a 192k -ac 2 -movflags faststart output
input represent original file. (input.mp4)

output represent re-encoded file. (output.mp4)
