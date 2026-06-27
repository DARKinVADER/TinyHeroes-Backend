# Missing PRIZE_CLAIM Translations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the missing `PRIZE_CLAIM` translation block to all four non-English locale files (Hungarian, German, French, Spanish) so prize claim UI renders correctly in all languages.

**Architecture:** Pure content fix — add a `PRIZE_CLAIM` JSON object with 8 keys to each of `hu.json`, `de.json`, `fr.json`, and `es.json`. No code changes required. The English source (`en.json`) is the authoritative reference.

**Tech Stack:** JSON i18n files at `frontend/public/assets/i18n/`; Angular `@ngx-translate` pipe reads these at runtime.

---

## Context

The `PRIZE_CLAIM` block was added to `en.json` but never back-filled into the other four locales. All four are missing the exact same 8 keys:

| Key | English value |
|---|---|
| `PRIZE_CLAIM.SECTION_TITLE` | `🎁 Prizes` |
| `PRIZE_CLAIM.MARK_USED` | `Mark used` |
| `PRIZE_CLAIM.USED` | `Used ✓` |
| `PRIZE_CLAIM.PENDING` | `Pending` |
| `PRIZE_CLAIM.COMMENTS` | `Comments` |
| `PRIZE_CLAIM.ADD_COMMENT_PLACEHOLDER` | `Add a comment…` |
| `PRIZE_CLAIM.POST` | `Post` |
| `PRIZE_CLAIM.DELETE_COMMENT` | `Delete` |

These keys are used in `frontend/src/app/features/podium/pages/history.component.ts`.

## File Structure

**Modified files only (no new files):**
- `frontend/public/assets/i18n/hu.json` — add `PRIZE_CLAIM` block with Hungarian translations
- `frontend/public/assets/i18n/de.json` — add `PRIZE_CLAIM` block with German translations
- `frontend/public/assets/i18n/fr.json` — add `PRIZE_CLAIM` block with French translations
- `frontend/public/assets/i18n/es.json` — add `PRIZE_CLAIM` block with Spanish translations

---

### Task 1: Verify the gap (sanity check)

- [ ] **Step 1: Run the key-diff script**

```bash
python3 -c "
import json

def flatten(d, prefix=''):
    result = {}
    for k, v in d.items():
        full = f'{prefix}.{k}' if prefix else k
        if isinstance(v, dict):
            result.update(flatten(v, full))
        else:
            result[full] = v
    return result

with open('frontend/public/assets/i18n/en.json') as f:
    en = flatten(json.load(f))

for lang in ['hu', 'de', 'fr', 'es']:
    with open(f'frontend/public/assets/i18n/{lang}.json') as f:
        other = flatten(json.load(f))
    missing = [k for k in en if k not in other]
    print(f'{lang}: missing {len(missing)} keys: {missing}')
"
```

Expected output (all four languages, exactly these 8 keys):
```
hu: missing 8 keys: ['PRIZE_CLAIM.SECTION_TITLE', 'PRIZE_CLAIM.MARK_USED', 'PRIZE_CLAIM.USED', 'PRIZE_CLAIM.PENDING', 'PRIZE_CLAIM.COMMENTS', 'PRIZE_CLAIM.ADD_COMMENT_PLACEHOLDER', 'PRIZE_CLAIM.POST', 'PRIZE_CLAIM.DELETE_COMMENT']
de: missing 8 keys: [...]
fr: missing 8 keys: [...]
es: missing 8 keys: [...]
```

---

### Task 2: Add PRIZE_CLAIM to Hungarian (hu.json)

**Files:**
- Modify: `frontend/public/assets/i18n/hu.json`

- [ ] **Step 1: Open `frontend/public/assets/i18n/hu.json` and add the `PRIZE_CLAIM` block**

Insert the following at the top level of the JSON object (e.g. after the `PRIZES` block):

```json
"PRIZE_CLAIM": {
  "SECTION_TITLE": "🎁 Jutalmak",
  "MARK_USED": "Megjelölés felhasználtként",
  "USED": "Felhasználva ✓",
  "PENDING": "Függőben",
  "COMMENTS": "Megjegyzések",
  "ADD_COMMENT_PLACEHOLDER": "Megjegyzés hozzáadása…",
  "POST": "Küldés",
  "DELETE_COMMENT": "Törlés"
}
```

- [ ] **Step 2: Validate JSON syntax**

```bash
python3 -c "import json; json.load(open('frontend/public/assets/i18n/hu.json')); print('hu.json: valid')"
```

Expected: `hu.json: valid`

- [ ] **Step 3: Commit**

```bash
git add frontend/public/assets/i18n/hu.json
git commit -m "fix: add missing PRIZE_CLAIM translations to Hungarian"
```

---

### Task 3: Add PRIZE_CLAIM to German (de.json)

**Files:**
- Modify: `frontend/public/assets/i18n/de.json`

- [ ] **Step 1: Open `frontend/public/assets/i18n/de.json` and add the `PRIZE_CLAIM` block**

Insert at the top level (after the `PRIZES` block):

```json
"PRIZE_CLAIM": {
  "SECTION_TITLE": "🎁 Preise",
  "MARK_USED": "Als verwendet markieren",
  "USED": "Verwendet ✓",
  "PENDING": "Ausstehend",
  "COMMENTS": "Kommentare",
  "ADD_COMMENT_PLACEHOLDER": "Kommentar hinzufügen…",
  "POST": "Senden",
  "DELETE_COMMENT": "Löschen"
}
```

- [ ] **Step 2: Validate JSON syntax**

```bash
python3 -c "import json; json.load(open('frontend/public/assets/i18n/de.json')); print('de.json: valid')"
```

Expected: `de.json: valid`

- [ ] **Step 3: Commit**

```bash
git add frontend/public/assets/i18n/de.json
git commit -m "fix: add missing PRIZE_CLAIM translations to German"
```

---

### Task 4: Add PRIZE_CLAIM to French (fr.json)

**Files:**
- Modify: `frontend/public/assets/i18n/fr.json`

- [ ] **Step 1: Open `frontend/public/assets/i18n/fr.json` and add the `PRIZE_CLAIM` block**

Insert at the top level (after the `PRIZES` block):

```json
"PRIZE_CLAIM": {
  "SECTION_TITLE": "🎁 Récompenses",
  "MARK_USED": "Marquer comme utilisé",
  "USED": "Utilisé ✓",
  "PENDING": "En attente",
  "COMMENTS": "Commentaires",
  "ADD_COMMENT_PLACEHOLDER": "Ajouter un commentaire…",
  "POST": "Envoyer",
  "DELETE_COMMENT": "Supprimer"
}
```

- [ ] **Step 2: Validate JSON syntax**

```bash
python3 -c "import json; json.load(open('frontend/public/assets/i18n/fr.json')); print('fr.json: valid')"
```

Expected: `fr.json: valid`

- [ ] **Step 3: Commit**

```bash
git add frontend/public/assets/i18n/fr.json
git commit -m "fix: add missing PRIZE_CLAIM translations to French"
```

---

### Task 5: Add PRIZE_CLAIM to Spanish (es.json)

**Files:**
- Modify: `frontend/public/assets/i18n/es.json`

- [ ] **Step 1: Open `frontend/public/assets/i18n/es.json` and add the `PRIZE_CLAIM` block**

Insert at the top level (after the `PRIZES` block):

```json
"PRIZE_CLAIM": {
  "SECTION_TITLE": "🎁 Premios",
  "MARK_USED": "Marcar como usado",
  "USED": "Usado ✓",
  "PENDING": "Pendiente",
  "COMMENTS": "Comentarios",
  "ADD_COMMENT_PLACEHOLDER": "Añadir un comentario…",
  "POST": "Publicar",
  "DELETE_COMMENT": "Eliminar"
}
```

- [ ] **Step 2: Validate JSON syntax**

```bash
python3 -c "import json; json.load(open('frontend/public/assets/i18n/es.json')); print('es.json: valid')"
```

Expected: `es.json: valid`

- [ ] **Step 3: Commit**

```bash
git add frontend/public/assets/i18n/es.json
git commit -m "fix: add missing PRIZE_CLAIM translations to Spanish"
```

---

### Task 6: Final verification — no missing keys remain

- [ ] **Step 1: Run the key-diff script again**

```bash
python3 -c "
import json

def flatten(d, prefix=''):
    result = {}
    for k, v in d.items():
        full = f'{prefix}.{k}' if prefix else k
        if isinstance(v, dict):
            result.update(flatten(v, full))
        else:
            result[full] = v
    return result

with open('frontend/public/assets/i18n/en.json') as f:
    en = flatten(json.load(f))

all_ok = True
for lang in ['hu', 'de', 'fr', 'es']:
    with open(f'frontend/public/assets/i18n/{lang}.json') as f:
        other = flatten(json.load(f))
    missing = [k for k in en if k not in other]
    if missing:
        print(f'FAIL {lang}: still missing {missing}')
        all_ok = False
    else:
        print(f'OK   {lang}: no missing keys')

if all_ok:
    print('All locales complete.')
"
```

Expected output:
```
OK   hu: no missing keys
OK   de: no missing keys
OK   fr: no missing keys
OK   es: no missing keys
All locales complete.
```

- [ ] **Step 2: Update CHANGELOG.md**

Add under `## [Unreleased]` → `### Fixed`:

```markdown
### Fixed
- Prize claim UI (section title, status labels, comments) now displays correctly in Hungarian, German, French, and Spanish.
```

- [ ] **Step 3: Commit docs**

```bash
git add CHANGELOG.md
git commit -m "docs: update changelog for missing PRIZE_CLAIM translation fix"
```
