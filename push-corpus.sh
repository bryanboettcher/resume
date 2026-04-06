#!/bin/bash
# Push all corpus markdown files to the resume-chat API
# Usage: ./push-corpus.sh [API_URL] [API_KEY]

API_URL="${1:-https://resume-chat.mallcop.dev}"
API_KEY="${2:-abc123}"

cd "$(dirname "$0")" || exit 1

count=0
skipped=0
errors=0

for dir in evidence projects links; do
    [ -d "$dir" ] || continue
    find "$dir" -name '*.md' -print0 | while IFS= read -r -d '' file; do
        result=$(curl -s -X POST "$API_URL/api/admin/corpus" \
            -H "X-Api-Key: $API_KEY" \
            -H "Content-Type: application/json" \
            -d "$(jq -n --arg path "$file" --rawfile content "$file" \
                '{sourcePath: $path, content: $content, embed: false}')")

        status=$(echo "$result" | jq -r '.status // "error"')
        chunks=$(echo "$result" | jq -r '.chunkCount // 0')

        if [ "$status" = "skipped" ]; then
            echo "  skip  $file"
            ((skipped++))
        elif [ "$status" = "synced" ]; then
            echo "  sync  $file ($chunks chunks)"
            ((count++))
        else
            echo "  ERROR $file: $result"
            ((errors++))
        fi
    done
done

echo ""
echo "Done. Synced: $count, Skipped: $skipped, Errors: $errors"
echo ""
echo "To re-ingest into Qdrant:"
echo "  curl -s -X POST $API_URL/api/admin/ingest -H 'X-Api-Key: $API_KEY'"
