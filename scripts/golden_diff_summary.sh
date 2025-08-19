#!/usr/bin/env bash
set -euo pipefail

# Generates a JSON + Markdown summary for golden baseline diffs.
# Assumes baseline already regenerated. Compares current working tree vs HEAD.

GOLDEN_DIR="ClubDoorman.Baseline/golden"
OUT_JSON="golden-diff-summary.json"
OUT_MD="golden-diff-summary.md"

changed_files=$(git diff --name-status HEAD -- "$GOLDEN_DIR" || true)
if [ -z "$changed_files" ]; then
  echo "{}" > "$OUT_JSON"
  echo "No golden diffs" > "$OUT_MD"
  exit 0
fi

echo "Detecting changes in $GOLDEN_DIR" 1>&2

tmpdir=$(mktemp -d)
files_json=$tmpdir/files.json
echo '[]' > "$files_json"

while IFS=$'\t' read -r status path; do
  [ -z "$path" ] && continue
  jq --arg st "$status" --arg p "$path" '. += [{status:$st,path:$p}]' "$files_json" > "$files_json.tmp" && mv "$files_json.tmp" "$files_json"
done <<< "$changed_files"

manifest_path="$GOLDEN_DIR/manifest.json"
manifest_diff_json='null'
if echo "$changed_files" | grep -q "manifest.json" && [ -f "$manifest_path" ]; then
  if git cat-file -e "HEAD:$manifest_path" 2>/dev/null; then
    git show "HEAD:$manifest_path" > "$tmpdir/old_manifest.json" || true
  else
    echo '[]' > "$tmpdir/old_manifest.json"
  fi
  cp "$manifest_path" "$tmpdir/new_manifest.json"

  old_ids=$(jq '.[].Id' "$tmpdir/old_manifest.json" 2>/dev/null | sort -n | uniq || true)
  new_ids=$(jq '.[].Id' "$tmpdir/new_manifest.json" 2>/dev/null | sort -n | uniq || true)

  added_ids=$(comm -13 <(echo "$old_ids") <(echo "$new_ids") | tr '\n' ' ' | sed 's/ $//')
  removed_ids=$(comm -23 <(echo "$old_ids") <(echo "$new_ids") | tr '\n' ' ' | sed 's/ $//')

  overlap=$(comm -12 <(echo "$old_ids") <(echo "$new_ids") | tr '\n' ' ' | sed 's/ $//')
  diffs='[]'
  for id in $overlap; do
    old_obj=$(jq -c --argjson id "$id" 'map(select(.Id==$id)) | .[0]' "$tmpdir/old_manifest.json") || old_obj='null'
    new_obj=$(jq -c --argjson id "$id" 'map(select(.Id==$id)) | .[0]' "$tmpdir/new_manifest.json") || new_obj='null'
    [ "$old_obj" = "null" ] && continue
    [ "$new_obj" = "null" ] && continue
    fields=$(jq -r 'keys[]' <<< "$new_obj" | sort -u)
    field_changes='[]'
    while IFS= read -r f; do
      oval=$(jq -r --arg f "$f" '.[$f] // empty' <<< "$old_obj")
      nval=$(jq -r --arg f "$f" '.[$f] // empty' <<< "$new_obj")
      if [ "$oval" != "$nval" ]; then
        field_changes=$(jq --arg f "$f" --arg old "$oval" --arg new "$nval" '. += [{field:$f,old:$old,new:$new}]' <<< "$field_changes")
      fi
    done <<< "$fields"
    if [ "$(jq 'length' <<< "$field_changes")" -gt 0 ]; then
      diffs=$(jq --argjson id "$id" --argjson changes "$field_changes" '. += [{Id:$id, fieldChanges:$changes}]' <<< "$diffs")
    fi
  done

  manifest_diff_json=$(jq -n \
    --argjson added "[$(echo $added_ids | sed 's/ /,/g')]" \
    --argjson removed "[$(echo $removed_ids | sed 's/ /,/g')]" \
    --argjson diffs "$diffs" '{added: $added, removed: $removed, changed: $diffs}')
fi

jq -n \
  --argjson files "$(cat $files_json)" \
  --argjson manifestDiff "$manifest_diff_json" \
  '{files:$files, manifest: $manifestDiff}' > "$OUT_JSON"

{
  echo "### Golden Diff Summary"
  echo
  echo "Changed files:"; echo '```'; echo "$changed_files"; echo '```'
  if [ "$manifest_diff_json" != "null" ]; then
    echo
    echo "Manifest changes:"
    added_list=$(jq -r '.added | map(tostring) | join(", ")' <<< "$manifest_diff_json")
    removed_list=$(jq -r '.removed | map(tostring) | join(", ")' <<< "$manifest_diff_json")
    echo "- Added Ids: ${added_list:-none}"
    echo "- Removed Ids: ${removed_list:-none}"
    echo "- Field changes:"
    jq -r '.changed[]? | "  * Id " + (.Id|tostring) + ": " + ( .fieldChanges | map(.field + "=" + .old + "→" + .new) | join(", ") )' <<< "$manifest_diff_json"
  fi
} > "$OUT_MD"

echo "Created $OUT_JSON and $OUT_MD" 1>&2
