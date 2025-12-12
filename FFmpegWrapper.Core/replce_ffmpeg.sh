#!/usr/bin/env sh

# Find all .cs files recursively
find . -type f -name "*.cs" | while IFS= read -r FILE; do
    echo "Processing $FILE"

    # Replace ffmpeg. except in lines starting with '///'
    awk '
        /^[[:space:]]*\/\/\// { print; next }
        { gsub(/ffmpeg\./, "", $0); print }
    ' "$FILE" > "$FILE.tmp"

    mv "$FILE.tmp" "$FILE"
done
