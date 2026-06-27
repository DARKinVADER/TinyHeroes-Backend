# Help Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a publicly accessible `/help` route — a fully translated, single-page user guide for parents covering all app features.

**Architecture:** A standalone `HelpComponent` added as a top-level lazy route (no `authGuard`). All text uses the existing `TranslateService` + ngx-translate JSON files via a new `HELP` namespace. A sticky sidebar + scroll-spy on desktop; a `<select>` anchor dropdown on mobile. Language picker in the header calls `translate.use()` without persisting to `localStorage`.

**Tech Stack:** Angular 21 standalone components, ngx-translate (`TranslateModule`, `TranslateService`, `translate` pipe), Tailwind 4, `IntersectionObserver` for scroll-spy.

---

## File Map

| Action | Path |
|--------|------|
| Create | `frontend/src/app/features/help/help.component.ts` |
| Modify | `frontend/src/app/app.routes.ts` |
| Modify | `frontend/public/assets/i18n/en.json` |
| Modify | `frontend/public/assets/i18n/hu.json` |
| Modify | `frontend/public/assets/i18n/de.json` |
| Modify | `frontend/public/assets/i18n/fr.json` |
| Modify | `frontend/public/assets/i18n/es.json` |
| Create dir | `frontend/public/assets/docs/screenshots/` |

---

## Task 1: Add English translations (`HELP` namespace)

**Files:**
- Modify: `frontend/public/assets/i18n/en.json`

- [ ] **Step 1: Add the `HELP` namespace to `en.json`**

Open `frontend/public/assets/i18n/en.json` and add the following top-level key alongside the existing ones (e.g. after `"PRIZE_CLAIM": {...}`):

```json
"HELP": {
  "PAGE_TITLE": "How to use TinyHeroes",
  "PAGE_SUBTITLE": "A guide for parents — track good deeds, celebrate your children, and manage prizes.",
  "NAV_LABEL": "Contents",
  "JUMP_TO": "Jump to section",
  "SCREENSHOT_NOTE": "Screenshots are shown in English.",
  "LANG_PICKER_LABEL": "Language",

  "SECTION_GETTING_STARTED": "Getting Started",
  "SECTION_HEROES": "Adding Heroes",
  "SECTION_DEEDS": "Logging Good Deeds",
  "SECTION_PODIUM": "Weekly Podium",
  "SECTION_MONTHLY": "Monthly Champion",
  "SECTION_PRIZES": "Prizes",
  "SECTION_INVITE": "Invite a Co-parent",
  "SECTION_SETTINGS": "Language & Settings",
  "SECTION_FAQ": "FAQ & Troubleshooting",

  "STEP_SIGNUP": "Create your account",
  "STEP_CREATE_FAMILY": "Set up your family",
  "STEP_ADD_CHILD": "Add a child hero",
  "STEP_LOG_DEED_TAP": "Tap a hero card on the dashboard",
  "STEP_LOG_DEED_PICK": "Choose or describe the deed",
  "STEP_VIEW_PODIUM": "View the weekly standings",
  "STEP_PODIUM_PRIZES": "Award weekly prizes",
  "STEP_MONTHLY_CHAMPION": "View the monthly champion",
  "STEP_MONTHLY_ELIGIBILITY": "Eligibility threshold",
  "STEP_PRIZES_BOARD": "Browse the prizes board",
  "STEP_PRIZES_EDIT": "Set prizes for each place",
  "STEP_PRIZES_CLAIM": "Mark a prize as used",
  "STEP_INVITE_SEND": "Send an invite",
  "STEP_INVITE_ROLE": "Co-parent role",
  "STEP_SETTINGS_LANG": "Change your language",
  "STEP_SETTINGS_PROFILE": "Edit your profile",
  "STEP_SETTINGS_FAMILY": "Family settings",

  "CAPTION_SIGNUP": "Sign up with email and password, or continue with Google, Apple, or Facebook. No credit card needed.",
  "CAPTION_CREATE_FAMILY": "Give your family a name and choose which day your week starts. This controls when the weekly podium resets.",
  "CAPTION_ADD_CHILD": "Tap + Add hero on the dashboard. Give the child a name and choose an emoji avatar or upload a photo.",
  "CAPTION_LOG_DEED_TAP": "The dashboard shows all your heroes and their deed count for the week. Tap any card to open the deed logger.",
  "CAPTION_LOG_DEED_PICK": "Pick from preset deeds or describe a custom one. An AI-generated image is created automatically.",
  "CAPTION_VIEW_PODIUM": "The Podium tab shows this week's live standings. The podium resets at the end of each week.",
  "CAPTION_PODIUM_PRIZES": "At the end of the week, 1st, 2nd, and 3rd place winners are announced and prizes can be awarded.",
  "CAPTION_MONTHLY_CHAMPION": "The Monthly tab shows the champion for the current month. The champion is declared on the last day of the month.",
  "CAPTION_MONTHLY_ELIGIBILITY": "If a minimum deed count is set, children must reach it to qualify for the monthly prize.",
  "CAPTION_PRIZES_BOARD": "The Prizes tab shows all active prizes for weekly places and the monthly champion.",
  "CAPTION_PRIZES_EDIT": "Tap Edit on any prize slot to choose from built-in suggestions, your saved prizes, or write a custom prize.",
  "CAPTION_PRIZES_CLAIM": "Once a prize is delivered, tap Mark used on the child's profile to record it.",
  "CAPTION_INVITE_SEND": "Go to Settings → Invite Co-Parent. Enter an email address or copy the shareable link.",
  "CAPTION_INVITE_ROLE": "Co-parents can add deeds. Only the admin can manage prizes, family settings, and prize rules.",
  "CAPTION_SETTINGS_LANG": "Go to Settings → My Profile → Language to change your display language.",
  "CAPTION_SETTINGS_PROFILE": "Edit your display name and notification preferences in Settings → My Profile.",
  "CAPTION_SETTINGS_FAMILY": "Change the family name or the week start day in Settings → Family Settings.",

  "FAQ_Q_1": "Can I add more than one co-parent?",
  "FAQ_A_1": "Yes. Go to Settings → Invite Co-Parent and send as many invites as you need. Each person who joins becomes a co-parent.",
  "FAQ_Q_2": "When does the weekly podium reset?",
  "FAQ_A_2": "At the end of the day you chose as your week start. You can change this day in Settings → Family Settings.",
  "FAQ_Q_3": "Why isn't the monthly champion showing?",
  "FAQ_A_3": "The monthly champion is declared on the last day of the month. If a minimum deed threshold is set, all children must meet it to qualify.",
  "FAQ_Q_4": "Can I delete a good deed after adding it?",
  "FAQ_A_4": "Yes. Open the child's profile, tap the deed, and use the delete option.",
  "FAQ_Q_5": "What is the AI image for deeds?",
  "FAQ_A_5": "When you log a deed, TinyHeroes automatically generates a fun illustration using AI. You can regenerate it or skip it.",
  "FAQ_Q_6": "How do I change the prize for 1st place?",
  "FAQ_A_6": "Go to the Prizes tab and tap Edit next to the prize slot. You must be the family admin to edit prizes.",
  "FAQ_Q_7": "Can children see the app?",
  "FAQ_A_7": "TinyHeroes is designed for parents to manage. You can show children the podium and their deed history on your device.",
  "FAQ_Q_8": "How do I delete the family?",
  "FAQ_A_8": "Go to Settings → Family Settings → Danger zone. Warning: this permanently deletes all children, deeds, and history."
}
```

- [ ] **Step 2: Verify JSON is valid**

```bash
cd frontend && node -e "JSON.parse(require('fs').readFileSync('public/assets/i18n/en.json','utf8')); console.log('valid')"
```

Expected: `valid`

- [ ] **Step 3: Commit**

```bash
git add frontend/public/assets/i18n/en.json
git commit -m "feat: add HELP namespace to English translations"
```

---

## Task 2: Add Hungarian, German, French, Spanish translations

**Files:**
- Modify: `frontend/public/assets/i18n/hu.json`
- Modify: `frontend/public/assets/i18n/de.json`
- Modify: `frontend/public/assets/i18n/fr.json`
- Modify: `frontend/public/assets/i18n/es.json`

- [ ] **Step 1: Add `HELP` namespace to `hu.json`**

Add after the last existing top-level key in `frontend/public/assets/i18n/hu.json`:

```json
"HELP": {
  "PAGE_TITLE": "Hogyan használd a TinyHeroest",
  "PAGE_SUBTITLE": "Útmutató szülőknek — kövesd a jó cselekedeteket, ünnepeld a gyerekeidet és kezeld a jutalmakat.",
  "NAV_LABEL": "Tartalom",
  "JUMP_TO": "Ugrás a részhez",
  "SCREENSHOT_NOTE": "A képernyőképek angol nyelvűek.",
  "LANG_PICKER_LABEL": "Nyelv",

  "SECTION_GETTING_STARTED": "Kezdő lépések",
  "SECTION_HEROES": "Hősök hozzáadása",
  "SECTION_DEEDS": "Jó cselekedetek naplózása",
  "SECTION_PODIUM": "Heti dobogó",
  "SECTION_MONTHLY": "Havi bajnok",
  "SECTION_PRIZES": "Jutalmak",
  "SECTION_INVITE": "Társ-szülő meghívása",
  "SECTION_SETTINGS": "Nyelv és beállítások",
  "SECTION_FAQ": "GYIK és hibaelhárítás",

  "STEP_SIGNUP": "Fiók létrehozása",
  "STEP_CREATE_FAMILY": "Család beállítása",
  "STEP_ADD_CHILD": "Gyermek hős hozzáadása",
  "STEP_LOG_DEED_TAP": "Koppints egy hős kártyára a főoldalon",
  "STEP_LOG_DEED_PICK": "Válassz vagy írj le egy cselekedetet",
  "STEP_VIEW_PODIUM": "Heti állás megtekintése",
  "STEP_PODIUM_PRIZES": "Heti jutalmak odaítélése",
  "STEP_MONTHLY_CHAMPION": "Havi bajnok megtekintése",
  "STEP_MONTHLY_ELIGIBILITY": "Jogosultsági küszöb",
  "STEP_PRIZES_BOARD": "Jutalomtábla böngészése",
  "STEP_PRIZES_EDIT": "Jutalmak beállítása helyezésenként",
  "STEP_PRIZES_CLAIM": "Jutalom felhasználtnak jelölése",
  "STEP_INVITE_SEND": "Meghívó küldése",
  "STEP_INVITE_ROLE": "Társ-szülő szerepköre",
  "STEP_SETTINGS_LANG": "Nyelv módosítása",
  "STEP_SETTINGS_PROFILE": "Profil szerkesztése",
  "STEP_SETTINGS_FAMILY": "Családi beállítások",

  "CAPTION_SIGNUP": "Regisztrálj e-mail és jelszó megadásával, vagy folytasd Google, Apple vagy Facebook fiókkal.",
  "CAPTION_CREATE_FAMILY": "Adj nevet a családnak, és válaszd ki, hogy melyik napon kezdődik a heted.",
  "CAPTION_ADD_CHILD": "Koppints a + Hős hozzáadása gombra. Add meg a gyermek nevét, és válassz emoji avatárt vagy tölts fel fotót.",
  "CAPTION_LOG_DEED_TAP": "A főoldal mutatja az összes hőst és a heti cselekedetszámot. Koppints bármelyik kártyára.",
  "CAPTION_LOG_DEED_PICK": "Válassz előre beállított cselekedetek közül, vagy írj egyénit. Az AI automatikusan képet generál.",
  "CAPTION_VIEW_PODIUM": "A Dobogó fül mutatja az aktuális heti állást. A dobogó a hét végén resetel.",
  "CAPTION_PODIUM_PRIZES": "A hét végén kihirdetik az 1., 2. és 3. helyezettet, és jutalmak oszthatók ki.",
  "CAPTION_MONTHLY_CHAMPION": "A Havi fül mutatja az aktuális hónap bajnokát. A bajnokot a hónap utolsó napján hirdetik ki.",
  "CAPTION_MONTHLY_ELIGIBILITY": "Ha minimális cselekedetszám van beállítva, a gyerekeknek el kell érniük azt a havi jutalomhoz.",
  "CAPTION_PRIZES_BOARD": "A Jutalmak fül mutatja az aktív jutalmakat a heti helyezésekhez és a havi bajnokhoz.",
  "CAPTION_PRIZES_EDIT": "Koppints a Szerkesztés gombra bármelyik jutalomhelyen, és válassz a javaslatokból vagy írj sajátot.",
  "CAPTION_PRIZES_CLAIM": "A jutalom átadása után koppints a Felhasználva gombra a gyermek profilján.",
  "CAPTION_INVITE_SEND": "Menj a Beállítások → Társ-szülő meghívása menübe. Add meg az e-mail címet, vagy másold a hivatkozást.",
  "CAPTION_INVITE_ROLE": "A társ-szülők cselekedeteket adhatnak hozzá. Csak az admin kezelheti a jutalmakat és a beállításokat.",
  "CAPTION_SETTINGS_LANG": "Menj a Beállítások → Profilom → Nyelv menübe a megjelenítési nyelv módosításához.",
  "CAPTION_SETTINGS_PROFILE": "Szerkeszd a megjelenítési nevedet és az értesítési beállításaidat a Beállítások → Profilom menüben.",
  "CAPTION_SETTINGS_FAMILY": "Módosítsd a családnevet vagy a hét kezdőnapját a Beállítások → Családi beállítások menüben.",

  "FAQ_Q_1": "Hozzáadhatok egynél több társ-szülőt?",
  "FAQ_A_1": "Igen. Menj a Beállítások → Társ-szülő meghívása menübe, és küldj annyi meghívót, amennyire szükséged van.",
  "FAQ_Q_2": "Mikor resetel a heti dobogó?",
  "FAQ_A_2": "Annak a napnak a végén, amelyet hétkezdetnek választottál. Ezt a Beállítások → Családi beállítások menüben módosíthatod.",
  "FAQ_Q_3": "Miért nem jelenik meg a havi bajnok?",
  "FAQ_A_3": "A havi bajnokot a hónap utolsó napján hirdetik ki. Ha minimális küszöb van beállítva, minden gyereknek el kell érnie azt.",
  "FAQ_Q_4": "Törölhetek egy jó cselekedetet hozzáadás után?",
  "FAQ_A_4": "Igen. Nyisd meg a gyermek profilját, koppints a cselekedetre, és használd a törlés lehetőséget.",
  "FAQ_Q_5": "Mire való az AI-kép a cselekedeteknél?",
  "FAQ_A_5": "Amikor rögzítesz egy cselekedetet, a TinyHeroes automatikusan egy vidám illusztrációt generál. Újragenerálhatod vagy kihagyhatod.",
  "FAQ_Q_6": "Hogyan módosíthatom az első helyezett jutalmát?",
  "FAQ_A_6": "Menj a Jutalmak fülre, és koppints a Szerkesztés gombra a jutalomhelyen. Jutalmakat csak az admin szerkeszthet.",
  "FAQ_Q_7": "Láthatják a gyerekek az appot?",
  "FAQ_A_7": "A TinyHeroes szülőknek való. A dobogót és a cselekedetlistát megmutathatod a gyerekeknek a saját eszközödön.",
  "FAQ_Q_8": "Hogyan törölhetem a családot?",
  "FAQ_A_8": "Menj a Beállítások → Családi beállítások → Veszélyes zóna menübe. Figyelem: ez véglegesen törli az összes adatot."
}
```

- [ ] **Step 2: Add `HELP` namespace to `de.json`**

Add after the last existing top-level key in `frontend/public/assets/i18n/de.json`:

```json
"HELP": {
  "PAGE_TITLE": "So verwendest du TinyHeroes",
  "PAGE_SUBTITLE": "Ein Leitfaden für Eltern — verfolge gute Taten, feiere deine Kinder und verwalte Belohnungen.",
  "NAV_LABEL": "Inhalt",
  "JUMP_TO": "Zum Abschnitt springen",
  "SCREENSHOT_NOTE": "Screenshots sind auf Englisch.",
  "LANG_PICKER_LABEL": "Sprache",

  "SECTION_GETTING_STARTED": "Erste Schritte",
  "SECTION_HEROES": "Helden hinzufügen",
  "SECTION_DEEDS": "Gute Taten protokollieren",
  "SECTION_PODIUM": "Wöchentliches Podium",
  "SECTION_MONTHLY": "Monatlicher Champion",
  "SECTION_PRIZES": "Belohnungen",
  "SECTION_INVITE": "Elternteil einladen",
  "SECTION_SETTINGS": "Sprache & Einstellungen",
  "SECTION_FAQ": "FAQ & Fehlerbehebung",

  "STEP_SIGNUP": "Konto erstellen",
  "STEP_CREATE_FAMILY": "Familie einrichten",
  "STEP_ADD_CHILD": "Ein Kind als Held hinzufügen",
  "STEP_LOG_DEED_TAP": "Heldenkarte auf dem Dashboard antippen",
  "STEP_LOG_DEED_PICK": "Tat auswählen oder beschreiben",
  "STEP_VIEW_PODIUM": "Wöchentliche Rangliste ansehen",
  "STEP_PODIUM_PRIZES": "Wöchentliche Belohnungen vergeben",
  "STEP_MONTHLY_CHAMPION": "Monatlichen Champion ansehen",
  "STEP_MONTHLY_ELIGIBILITY": "Teilnahmevoraussetzungen",
  "STEP_PRIZES_BOARD": "Belohnungstafel durchsuchen",
  "STEP_PRIZES_EDIT": "Belohnungen je Platz festlegen",
  "STEP_PRIZES_CLAIM": "Belohnung als eingelöst markieren",
  "STEP_INVITE_SEND": "Einladung senden",
  "STEP_INVITE_ROLE": "Rolle des Elternteils",
  "STEP_SETTINGS_LANG": "Sprache ändern",
  "STEP_SETTINGS_PROFILE": "Profil bearbeiten",
  "STEP_SETTINGS_FAMILY": "Familieneinstellungen",

  "CAPTION_SIGNUP": "Registriere dich mit E-Mail und Passwort oder fahre mit Google, Apple oder Facebook fort.",
  "CAPTION_CREATE_FAMILY": "Gib deiner Familie einen Namen und wähle den Wochenstartag. Dieser bestimmt, wann das Podium zurückgesetzt wird.",
  "CAPTION_ADD_CHILD": "Tippe auf + Held hinzufügen. Gib dem Kind einen Namen und wähle ein Emoji-Avatar oder lade ein Foto hoch.",
  "CAPTION_LOG_DEED_TAP": "Das Dashboard zeigt alle Helden und ihre wöchentliche Tatenzahl. Tippe auf eine Karte zum Protokollieren.",
  "CAPTION_LOG_DEED_PICK": "Wähle aus voreingestellten Taten oder beschreibe eine eigene. Ein KI-Bild wird automatisch erstellt.",
  "CAPTION_VIEW_PODIUM": "Der Podium-Tab zeigt den aktuellen Wochenstand. Das Podium wird am Ende jeder Woche zurückgesetzt.",
  "CAPTION_PODIUM_PRIZES": "Am Wochenende werden die Plätze 1, 2 und 3 bekanntgegeben und Belohnungen können vergeben werden.",
  "CAPTION_MONTHLY_CHAMPION": "Der Monatlich-Tab zeigt den Champion des aktuellen Monats. Der Champion wird am letzten Tag des Monats gekürt.",
  "CAPTION_MONTHLY_ELIGIBILITY": "Wenn ein Mindestmaß an Taten festgelegt ist, müssen Kinder dieses erreichen, um für den Monatsschampion zu qualifizieren.",
  "CAPTION_PRIZES_BOARD": "Der Belohnungs-Tab zeigt alle aktiven Belohnungen für wöchentliche Plätze und den monatlichen Champion.",
  "CAPTION_PRIZES_EDIT": "Tippe auf Bearbeiten bei einem Belohnungsplatz und wähle aus Vorschlägen oder schreibe eine eigene.",
  "CAPTION_PRIZES_CLAIM": "Nach der Übergabe tippe auf Als eingelöst markieren im Kinderprofil.",
  "CAPTION_INVITE_SEND": "Gehe zu Einstellungen → Elternteil einladen. Gib eine E-Mail-Adresse ein oder kopiere den Einladungslink.",
  "CAPTION_INVITE_ROLE": "Elternteile können Taten hinzufügen. Nur der Admin kann Belohnungen und Einstellungen verwalten.",
  "CAPTION_SETTINGS_LANG": "Gehe zu Einstellungen → Mein Profil → Sprache, um die Anzeigesprache zu ändern.",
  "CAPTION_SETTINGS_PROFILE": "Bearbeite deinen Anzeigenamen und Benachrichtigungseinstellungen unter Einstellungen → Mein Profil.",
  "CAPTION_SETTINGS_FAMILY": "Ändere den Familiennamen oder den Wochenstartag unter Einstellungen → Familieneinstellungen.",

  "FAQ_Q_1": "Kann ich mehr als ein Elternteil hinzufügen?",
  "FAQ_A_1": "Ja. Gehe zu Einstellungen → Elternteil einladen und sende so viele Einladungen wie nötig.",
  "FAQ_Q_2": "Wann wird das wöchentliche Podium zurückgesetzt?",
  "FAQ_A_2": "Am Ende des von dir gewählten Wochenstartags. Diesen kannst du unter Einstellungen → Familieneinstellungen ändern.",
  "FAQ_Q_3": "Warum wird der monatliche Champion nicht angezeigt?",
  "FAQ_A_3": "Der monatliche Champion wird am letzten Tag des Monats gekürt. Falls ein Mindestschwellenwert gesetzt ist, müssen alle Kinder ihn erreichen.",
  "FAQ_Q_4": "Kann ich eine gute Tat nach dem Hinzufügen löschen?",
  "FAQ_A_4": "Ja. Öffne das Kinderprofil, tippe auf die Tat und verwende die Löschoption.",
  "FAQ_Q_5": "Wozu dient das KI-Bild bei Taten?",
  "FAQ_A_5": "Beim Protokollieren einer Tat erstellt TinyHeroes automatisch eine lustige Illustration. Du kannst sie neu generieren oder überspringen.",
  "FAQ_Q_6": "Wie ändere ich die Belohnung für den ersten Platz?",
  "FAQ_A_6": "Gehe zum Belohnungs-Tab und tippe auf Bearbeiten. Du musst Familienadmin sein, um Belohnungen zu bearbeiten.",
  "FAQ_Q_7": "Können Kinder die App sehen?",
  "FAQ_A_7": "TinyHeroes ist für Eltern konzipiert. Du kannst das Podium und die Tatenliste den Kindern auf deinem Gerät zeigen.",
  "FAQ_Q_8": "Wie lösche ich die Familie?",
  "FAQ_A_8": "Gehe zu Einstellungen → Familieneinstellungen → Gefahrenzone. Warnung: Dies löscht dauerhaft alle Kinder, Taten und die Historie."
}
```

- [ ] **Step 3: Add `HELP` namespace to `fr.json`**

Add after the last existing top-level key in `frontend/public/assets/i18n/fr.json`:

```json
"HELP": {
  "PAGE_TITLE": "Comment utiliser TinyHeroes",
  "PAGE_SUBTITLE": "Un guide pour les parents — suivez les bonnes actions, célébrez vos enfants et gérez les récompenses.",
  "NAV_LABEL": "Sommaire",
  "JUMP_TO": "Aller à la section",
  "SCREENSHOT_NOTE": "Les captures d'écran sont en anglais.",
  "LANG_PICKER_LABEL": "Langue",

  "SECTION_GETTING_STARTED": "Pour commencer",
  "SECTION_HEROES": "Ajouter des héros",
  "SECTION_DEEDS": "Enregistrer des bonnes actions",
  "SECTION_PODIUM": "Podium hebdomadaire",
  "SECTION_MONTHLY": "Champion du mois",
  "SECTION_PRIZES": "Récompenses",
  "SECTION_INVITE": "Inviter un co-parent",
  "SECTION_SETTINGS": "Langue et paramètres",
  "SECTION_FAQ": "FAQ et dépannage",

  "STEP_SIGNUP": "Créer votre compte",
  "STEP_CREATE_FAMILY": "Configurer votre famille",
  "STEP_ADD_CHILD": "Ajouter un enfant héros",
  "STEP_LOG_DEED_TAP": "Appuyer sur une carte héros dans le tableau de bord",
  "STEP_LOG_DEED_PICK": "Choisir ou décrire l'action",
  "STEP_VIEW_PODIUM": "Voir le classement hebdomadaire",
  "STEP_PODIUM_PRIZES": "Attribuer les récompenses hebdomadaires",
  "STEP_MONTHLY_CHAMPION": "Voir le champion du mois",
  "STEP_MONTHLY_ELIGIBILITY": "Seuil d'éligibilité",
  "STEP_PRIZES_BOARD": "Consulter le tableau des récompenses",
  "STEP_PRIZES_EDIT": "Définir les récompenses par place",
  "STEP_PRIZES_CLAIM": "Marquer une récompense comme utilisée",
  "STEP_INVITE_SEND": "Envoyer une invitation",
  "STEP_INVITE_ROLE": "Rôle du co-parent",
  "STEP_SETTINGS_LANG": "Changer la langue",
  "STEP_SETTINGS_PROFILE": "Modifier le profil",
  "STEP_SETTINGS_FAMILY": "Paramètres de la famille",

  "CAPTION_SIGNUP": "Inscrivez-vous avec un e-mail et un mot de passe, ou continuez avec Google, Apple ou Facebook.",
  "CAPTION_CREATE_FAMILY": "Donnez un nom à votre famille et choisissez le jour de début de semaine. Cela détermine quand le podium se réinitialise.",
  "CAPTION_ADD_CHILD": "Appuyez sur + Ajouter un héros. Donnez un nom à l'enfant et choisissez un avatar emoji ou importez une photo.",
  "CAPTION_LOG_DEED_TAP": "Le tableau de bord affiche tous vos héros et leur nombre d'actions de la semaine. Appuyez sur une carte pour enregistrer.",
  "CAPTION_LOG_DEED_PICK": "Choisissez parmi les actions prédéfinies ou décrivez une action personnalisée. Une image IA est générée automatiquement.",
  "CAPTION_VIEW_PODIUM": "L'onglet Podium affiche le classement en direct de la semaine. Le podium se réinitialise en fin de semaine.",
  "CAPTION_PODIUM_PRIZES": "En fin de semaine, les places 1, 2 et 3 sont annoncées et les récompenses peuvent être attribuées.",
  "CAPTION_MONTHLY_CHAMPION": "L'onglet Mensuel affiche le champion du mois en cours. Le champion est désigné le dernier jour du mois.",
  "CAPTION_MONTHLY_ELIGIBILITY": "Si un seuil minimum d'actions est défini, les enfants doivent l'atteindre pour être éligibles au prix mensuel.",
  "CAPTION_PRIZES_BOARD": "L'onglet Récompenses affiche toutes les récompenses actives pour les places hebdomadaires et le champion mensuel.",
  "CAPTION_PRIZES_EDIT": "Appuyez sur Modifier sur un emplacement de récompense pour choisir parmi les suggestions ou écrire une récompense personnalisée.",
  "CAPTION_PRIZES_CLAIM": "Une fois la récompense remise, appuyez sur Marquer comme utilisée dans le profil de l'enfant.",
  "CAPTION_INVITE_SEND": "Allez dans Paramètres → Inviter un co-parent. Saisissez une adresse e-mail ou copiez le lien d'invitation.",
  "CAPTION_INVITE_ROLE": "Les co-parents peuvent ajouter des actions. Seul l'administrateur peut gérer les récompenses et les paramètres.",
  "CAPTION_SETTINGS_LANG": "Allez dans Paramètres → Mon profil → Langue pour changer la langue d'affichage.",
  "CAPTION_SETTINGS_PROFILE": "Modifiez votre nom d'affichage et vos préférences de notification dans Paramètres → Mon profil.",
  "CAPTION_SETTINGS_FAMILY": "Modifiez le nom de la famille ou le jour de début de semaine dans Paramètres → Paramètres de la famille.",

  "FAQ_Q_1": "Puis-je ajouter plus d'un co-parent ?",
  "FAQ_A_1": "Oui. Allez dans Paramètres → Inviter un co-parent et envoyez autant d'invitations que nécessaire.",
  "FAQ_Q_2": "Quand le podium hebdomadaire se réinitialise-t-il ?",
  "FAQ_A_2": "À la fin du jour que vous avez choisi comme début de semaine. Vous pouvez le modifier dans Paramètres → Paramètres de la famille.",
  "FAQ_Q_3": "Pourquoi le champion du mois n'apparaît-il pas ?",
  "FAQ_A_3": "Le champion du mois est désigné le dernier jour du mois. Si un seuil est défini, tous les enfants doivent l'atteindre.",
  "FAQ_Q_4": "Puis-je supprimer une bonne action après l'avoir ajoutée ?",
  "FAQ_A_4": "Oui. Ouvrez le profil de l'enfant, appuyez sur l'action et utilisez l'option de suppression.",
  "FAQ_Q_5": "À quoi sert l'image IA pour les actions ?",
  "FAQ_A_5": "Lors de l'enregistrement d'une action, TinyHeroes génère automatiquement une illustration amusante. Vous pouvez la régénérer ou l'ignorer.",
  "FAQ_Q_6": "Comment modifier la récompense de la première place ?",
  "FAQ_A_6": "Allez dans l'onglet Récompenses et appuyez sur Modifier. Vous devez être administrateur de la famille pour modifier les récompenses.",
  "FAQ_Q_7": "Les enfants peuvent-ils voir l'application ?",
  "FAQ_A_7": "TinyHeroes est conçu pour les parents. Vous pouvez montrer le podium et l'historique des actions aux enfants sur votre appareil.",
  "FAQ_Q_8": "Comment supprimer la famille ?",
  "FAQ_A_8": "Allez dans Paramètres → Paramètres de la famille → Zone dangereuse. Attention : cela supprime définitivement toutes les données."
}
```

- [ ] **Step 4: Add `HELP` namespace to `es.json`**

Add after the last existing top-level key in `frontend/public/assets/i18n/es.json`:

```json
"HELP": {
  "PAGE_TITLE": "Cómo usar TinyHeroes",
  "PAGE_SUBTITLE": "Una guía para padres — registra buenas acciones, celebra a tus hijos y gestiona los premios.",
  "NAV_LABEL": "Contenido",
  "JUMP_TO": "Ir a la sección",
  "SCREENSHOT_NOTE": "Las capturas de pantalla están en inglés.",
  "LANG_PICKER_LABEL": "Idioma",

  "SECTION_GETTING_STARTED": "Primeros pasos",
  "SECTION_HEROES": "Añadir héroes",
  "SECTION_DEEDS": "Registrar buenas acciones",
  "SECTION_PODIUM": "Podio semanal",
  "SECTION_MONTHLY": "Campeón del mes",
  "SECTION_PRIZES": "Premios",
  "SECTION_INVITE": "Invitar a un copadre/comadre",
  "SECTION_SETTINGS": "Idioma y ajustes",
  "SECTION_FAQ": "Preguntas frecuentes y solución de problemas",

  "STEP_SIGNUP": "Crear tu cuenta",
  "STEP_CREATE_FAMILY": "Configurar tu familia",
  "STEP_ADD_CHILD": "Añadir un hijo héroe",
  "STEP_LOG_DEED_TAP": "Pulsar una tarjeta de héroe en el panel",
  "STEP_LOG_DEED_PICK": "Elegir o describir la acción",
  "STEP_VIEW_PODIUM": "Ver la clasificación semanal",
  "STEP_PODIUM_PRIZES": "Asignar premios semanales",
  "STEP_MONTHLY_CHAMPION": "Ver el campeón del mes",
  "STEP_MONTHLY_ELIGIBILITY": "Umbral de elegibilidad",
  "STEP_PRIZES_BOARD": "Explorar el tablero de premios",
  "STEP_PRIZES_EDIT": "Establecer premios por puesto",
  "STEP_PRIZES_CLAIM": "Marcar un premio como usado",
  "STEP_INVITE_SEND": "Enviar una invitación",
  "STEP_INVITE_ROLE": "Rol del copadre/comadre",
  "STEP_SETTINGS_LANG": "Cambiar el idioma",
  "STEP_SETTINGS_PROFILE": "Editar el perfil",
  "STEP_SETTINGS_FAMILY": "Ajustes de la familia",

  "CAPTION_SIGNUP": "Regístrate con correo y contraseña, o continúa con Google, Apple o Facebook.",
  "CAPTION_CREATE_FAMILY": "Da un nombre a tu familia y elige el día en que empieza tu semana. Esto determina cuándo se reinicia el podio.",
  "CAPTION_ADD_CHILD": "Pulsa + Añadir héroe. Pon un nombre al niño y elige un avatar emoji o sube una foto.",
  "CAPTION_LOG_DEED_TAP": "El panel muestra todos tus héroes y su conteo de acciones de la semana. Pulsa cualquier tarjeta para registrar.",
  "CAPTION_LOG_DEED_PICK": "Elige entre acciones predefinidas o describe una personalizada. Se genera una imagen con IA automáticamente.",
  "CAPTION_VIEW_PODIUM": "La pestaña Podio muestra la clasificación en vivo de la semana. El podio se reinicia al final de cada semana.",
  "CAPTION_PODIUM_PRIZES": "Al final de la semana se anuncian los puestos 1, 2 y 3, y se pueden asignar premios.",
  "CAPTION_MONTHLY_CHAMPION": "La pestaña Mensual muestra el campeón del mes actual. El campeón se proclama el último día del mes.",
  "CAPTION_MONTHLY_ELIGIBILITY": "Si se establece un mínimo de acciones, los niños deben alcanzarlo para optar al premio mensual.",
  "CAPTION_PRIZES_BOARD": "La pestaña Premios muestra todos los premios activos para los puestos semanales y el campeón mensual.",
  "CAPTION_PRIZES_EDIT": "Pulsa Editar en cualquier puesto de premio y elige entre sugerencias o escribe uno personalizado.",
  "CAPTION_PRIZES_CLAIM": "Una vez entregado el premio, pulsa Marcar como usado en el perfil del niño.",
  "CAPTION_INVITE_SEND": "Ve a Ajustes → Invitar copadre/comadre. Introduce un correo o copia el enlace de invitación.",
  "CAPTION_INVITE_ROLE": "Los copadres pueden añadir acciones. Solo el administrador puede gestionar premios y ajustes.",
  "CAPTION_SETTINGS_LANG": "Ve a Ajustes → Mi perfil → Idioma para cambiar el idioma de visualización.",
  "CAPTION_SETTINGS_PROFILE": "Edita tu nombre y preferencias de notificación en Ajustes → Mi perfil.",
  "CAPTION_SETTINGS_FAMILY": "Cambia el nombre de la familia o el día de inicio de semana en Ajustes → Ajustes de la familia.",

  "FAQ_Q_1": "¿Puedo añadir más de un copadre/comadre?",
  "FAQ_A_1": "Sí. Ve a Ajustes → Invitar copadre/comadre y envía tantas invitaciones como necesites.",
  "FAQ_Q_2": "¿Cuándo se reinicia el podio semanal?",
  "FAQ_A_2": "Al final del día que elegiste como inicio de semana. Puedes cambiarlo en Ajustes → Ajustes de la familia.",
  "FAQ_Q_3": "¿Por qué no aparece el campeón del mes?",
  "FAQ_A_3": "El campeón del mes se proclama el último día del mes. Si hay un umbral mínimo, todos los niños deben alcanzarlo.",
  "FAQ_Q_4": "¿Puedo eliminar una buena acción después de añadirla?",
  "FAQ_A_4": "Sí. Abre el perfil del niño, pulsa la acción y usa la opción de eliminar.",
  "FAQ_Q_5": "¿Para qué sirve la imagen IA en las acciones?",
  "FAQ_A_5": "Al registrar una acción, TinyHeroes genera automáticamente una ilustración divertida. Puedes regenerarla u omitirla.",
  "FAQ_Q_6": "¿Cómo cambio el premio del primer puesto?",
  "FAQ_A_6": "Ve a la pestaña Premios y pulsa Editar. Debes ser el administrador de la familia para editar premios.",
  "FAQ_Q_7": "¿Pueden los niños ver la aplicación?",
  "FAQ_A_7": "TinyHeroes está diseñado para padres. Puedes mostrar el podio y el historial de acciones a los niños en tu dispositivo.",
  "FAQ_Q_8": "¿Cómo elimino la familia?",
  "FAQ_A_8": "Ve a Ajustes → Ajustes de la familia → Zona peligrosa. Atención: esto elimina permanentemente todos los datos."
}
```

- [ ] **Step 5: Verify all four JSON files are valid**

```bash
cd frontend
for lang in hu de fr es; do
  node -e "JSON.parse(require('fs').readFileSync('public/assets/i18n/$lang.json','utf8')); console.log('$lang: valid')"
done
```

Expected:
```
hu: valid
de: valid
fr: valid
es: valid
```

- [ ] **Step 6: Commit**

```bash
git add frontend/public/assets/i18n/hu.json frontend/public/assets/i18n/de.json frontend/public/assets/i18n/fr.json frontend/public/assets/i18n/es.json
git commit -m "feat: add HELP namespace to HU, DE, FR, ES translations"
```

---

## Task 3: Create screenshot placeholder assets

**Files:**
- Create dir: `frontend/public/assets/docs/screenshots/`

- [ ] **Step 1: Create directory and placeholder PNGs**

```bash
mkdir -p frontend/public/assets/docs/screenshots
```

Create a simple grey placeholder PNG for each screen using Node:

```bash
cd frontend
node -e "
const fs = require('fs');
const dir = 'public/assets/docs/screenshots';
// Minimal 1x1 grey PNG (base64)
const greyPng = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==', 'base64');
const screens = ['signup','create-family','dashboard','add-deed','podium','monthly','prizes','invite','settings'];
screens.forEach(s => fs.writeFileSync(\`\${dir}/\${s}.png\`, greyPng));
console.log('placeholder PNGs created:', screens.join(', '));
"
```

Expected output:
```
placeholder PNGs created: signup, create-family, dashboard, add-deed, podium, monthly, prizes, invite, settings
```

- [ ] **Step 2: Commit**

```bash
git add frontend/public/assets/docs/screenshots/
git commit -m "feat: add screenshot placeholder assets for help page"
```

---

## Task 4: Create `HelpComponent`

**Files:**
- Create: `frontend/src/app/features/help/help.component.ts`

- [ ] **Step 1: Create the help feature directory and component file**

Create `frontend/src/app/features/help/help.component.ts` with the following content:

```typescript
import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-help',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  template: `
    <div class="min-h-screen bg-brand-cream">

      <!-- Header -->
      <header class="bg-white border-b border-brand-border sticky top-0 z-50">
        <div class="max-w-5xl mx-auto px-4 h-14 flex items-center justify-between gap-4">
          <a routerLink="/" class="flex items-center gap-2 flex-shrink-0">
            <span class="text-xl">🌟</span>
            <span class="font-black text-brand-orange text-base hidden sm:inline">TinyHeroes</span>
          </a>
          <span class="font-bold text-brand-text text-sm sm:text-base truncate">
            {{ 'HELP.PAGE_TITLE' | translate }}
          </span>
          <div class="flex items-center gap-2 flex-shrink-0">
            <label class="sr-only" for="help-lang-picker">{{ 'HELP.LANG_PICKER_LABEL' | translate }}</label>
            <select id="help-lang-picker"
              [value]="currentLang()"
              (change)="onLangChange($event)"
              class="border border-brand-border rounded-lg px-2 py-1.5 text-sm bg-white text-brand-text focus:outline-none focus:ring-2 focus:ring-brand-orange">
              @for (lang of languages; track lang.code) {
                <option [value]="lang.code">{{ lang.flag }} {{ lang.name }}</option>
              }
            </select>
          </div>
        </div>
      </header>

      <div class="max-w-5xl mx-auto px-4 py-6 flex gap-8">

        <!-- Sidebar (desktop) -->
        <nav class="hidden md:block w-52 flex-shrink-0">
          <div class="sticky top-20 bg-white border border-brand-border rounded-2xl p-4">
            <p class="text-xs font-bold text-brand-muted uppercase tracking-wide mb-3">
              {{ 'HELP.NAV_LABEL' | translate }}
            </p>
            @for (section of sections; track section.id) {
              <a [href]="'#' + section.id"
                [class]="activeSection() === section.id
                  ? 'block text-sm px-2 py-1.5 rounded-lg mb-0.5 bg-brand-cream text-brand-orange font-semibold'
                  : 'block text-sm px-2 py-1.5 rounded-lg mb-0.5 text-brand-muted hover:bg-brand-cream transition-colors'"
                (click)="onNavClick($event, section.id)">
                {{ section.labelKey | translate }}
              </a>
            }
          </div>
        </nav>

        <!-- Main content -->
        <main class="flex-1 min-w-0">

          <!-- Mobile anchor nav -->
          <div class="md:hidden mb-6">
            <select (change)="onMobileNav($event)"
              class="w-full border border-brand-border rounded-xl px-3 py-2.5 text-sm bg-white text-brand-text focus:outline-none focus:ring-2 focus:ring-brand-orange">
              <option value="">{{ 'HELP.JUMP_TO' | translate }}</option>
              @for (section of sections; track section.id) {
                <option [value]="section.id">{{ section.labelKey | translate }}</option>
              }
            </select>
          </div>

          <p class="text-sm text-brand-muted mb-8">{{ 'HELP.PAGE_SUBTITLE' | translate }}</p>

          <!-- ─── Getting Started ─── -->
          <section [id]="sections[0].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_GETTING_STARTED' | translate }}
            </h2>
            <div class="space-y-8">
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_SIGNUP' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_SIGNUP' | translate }}</p>
                  <img src="/assets/docs/screenshots/signup.png" alt="{{ 'HELP.STEP_SIGNUP' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                  <p class="text-xs text-brand-muted mt-1 italic">{{ 'HELP.SCREENSHOT_NOTE' | translate }}</p>
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">2</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_CREATE_FAMILY' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_CREATE_FAMILY' | translate }}</p>
                  <img src="/assets/docs/screenshots/create-family.png" alt="{{ 'HELP.STEP_CREATE_FAMILY' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
            </div>
          </section>

          <!-- ─── Adding Heroes ─── -->
          <section [id]="sections[1].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_HEROES' | translate }}
            </h2>
            <div>
              <div class="flex items-center gap-3 mb-3">
                <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_ADD_CHILD' | translate }}</h3>
              </div>
              <div class="ml-10">
                <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_ADD_CHILD' | translate }}</p>
                <img src="/assets/docs/screenshots/dashboard.png" alt="{{ 'HELP.STEP_ADD_CHILD' | translate }}"
                  class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
              </div>
            </div>
          </section>

          <!-- ─── Logging Good Deeds ─── -->
          <section [id]="sections[2].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_DEEDS' | translate }}
            </h2>
            <div class="space-y-8">
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_LOG_DEED_TAP' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_LOG_DEED_TAP' | translate }}</p>
                  <img src="/assets/docs/screenshots/dashboard.png" alt="{{ 'HELP.STEP_LOG_DEED_TAP' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">2</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_LOG_DEED_PICK' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_LOG_DEED_PICK' | translate }}</p>
                  <img src="/assets/docs/screenshots/add-deed.png" alt="{{ 'HELP.STEP_LOG_DEED_PICK' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
            </div>
          </section>

          <!-- ─── Weekly Podium ─── -->
          <section [id]="sections[3].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_PODIUM' | translate }}
            </h2>
            <div class="space-y-8">
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_VIEW_PODIUM' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_VIEW_PODIUM' | translate }}</p>
                  <img src="/assets/docs/screenshots/podium.png" alt="{{ 'HELP.STEP_VIEW_PODIUM' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">2</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_PODIUM_PRIZES' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_PODIUM_PRIZES' | translate }}</p>
                </div>
              </div>
            </div>
          </section>

          <!-- ─── Monthly Champion ─── -->
          <section [id]="sections[4].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_MONTHLY' | translate }}
            </h2>
            <div class="space-y-8">
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_MONTHLY_CHAMPION' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_MONTHLY_CHAMPION' | translate }}</p>
                  <img src="/assets/docs/screenshots/monthly.png" alt="{{ 'HELP.STEP_MONTHLY_CHAMPION' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">2</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_MONTHLY_ELIGIBILITY' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_MONTHLY_ELIGIBILITY' | translate }}</p>
                </div>
              </div>
            </div>
          </section>

          <!-- ─── Prizes ─── -->
          <section [id]="sections[5].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_PRIZES' | translate }}
            </h2>
            <div class="space-y-8">
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_PRIZES_BOARD' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_PRIZES_BOARD' | translate }}</p>
                  <img src="/assets/docs/screenshots/prizes.png" alt="{{ 'HELP.STEP_PRIZES_BOARD' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">2</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_PRIZES_EDIT' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_PRIZES_EDIT' | translate }}</p>
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">3</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_PRIZES_CLAIM' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_PRIZES_CLAIM' | translate }}</p>
                </div>
              </div>
            </div>
          </section>

          <!-- ─── Invite Co-parent ─── -->
          <section [id]="sections[6].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_INVITE' | translate }}
            </h2>
            <div class="space-y-8">
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_INVITE_SEND' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_INVITE_SEND' | translate }}</p>
                  <img src="/assets/docs/screenshots/invite.png" alt="{{ 'HELP.STEP_INVITE_SEND' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">2</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_INVITE_ROLE' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_INVITE_ROLE' | translate }}</p>
                </div>
              </div>
            </div>
          </section>

          <!-- ─── Language & Settings ─── -->
          <section [id]="sections[7].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_SETTINGS' | translate }}
            </h2>
            <div class="space-y-8">
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">1</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_SETTINGS_LANG' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_SETTINGS_LANG' | translate }}</p>
                  <img src="/assets/docs/screenshots/settings.png" alt="{{ 'HELP.STEP_SETTINGS_LANG' | translate }}"
                    class="rounded-2xl border border-brand-border shadow-sm max-w-xs w-full" />
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">2</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_SETTINGS_PROFILE' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_SETTINGS_PROFILE' | translate }}</p>
                </div>
              </div>
              <div>
                <div class="flex items-center gap-3 mb-3">
                  <div class="w-7 h-7 rounded-full bg-brand-orange text-white text-xs font-bold flex items-center justify-center flex-shrink-0">3</div>
                  <h3 class="font-semibold text-brand-text">{{ 'HELP.STEP_SETTINGS_FAMILY' | translate }}</h3>
                </div>
                <div class="ml-10">
                  <p class="text-sm text-brand-muted mb-3">{{ 'HELP.CAPTION_SETTINGS_FAMILY' | translate }}</p>
                </div>
              </div>
            </div>
          </section>

          <!-- ─── FAQ ─── -->
          <section [id]="sections[8].id" class="mb-14">
            <h2 class="text-lg font-black text-brand-text mb-6 pb-2 border-b-2 border-brand-cream">
              {{ 'HELP.SECTION_FAQ' | translate }}
            </h2>
            <div class="space-y-4">
              @for (i of faqIndexes; track i) {
                <div class="bg-white rounded-2xl border border-brand-border p-4">
                  <p class="font-semibold text-brand-text text-sm mb-1">{{ 'HELP.FAQ_Q_' + i | translate }}</p>
                  <p class="text-sm text-brand-muted leading-relaxed">{{ 'HELP.FAQ_A_' + i | translate }}</p>
                </div>
              }
            </div>
          </section>

          <!-- Footer -->
          <div class="text-center text-xs text-brand-muted py-8 border-t border-brand-border">
            <a routerLink="/" class="text-brand-orange font-semibold hover:underline">← {{ 'APP_NAME' | translate }}</a>
          </div>

        </main>
      </div>
    </div>
  `
})
export class HelpComponent implements OnInit, OnDestroy {
  private translate = inject(TranslateService);

  currentLang = signal(this.translate.currentLang ?? 'en');

  languages = [
    { code: 'en', flag: '🇬🇧', name: 'English' },
    { code: 'hu', flag: '🇭🇺', name: 'Magyar' },
    { code: 'de', flag: '🇩🇪', name: 'Deutsch' },
    { code: 'fr', flag: '🇫🇷', name: 'Français' },
    { code: 'es', flag: '🇪🇸', name: 'Español' },
  ];

  sections = [
    { id: 'getting-started', labelKey: 'HELP.SECTION_GETTING_STARTED' },
    { id: 'heroes',          labelKey: 'HELP.SECTION_HEROES' },
    { id: 'deeds',           labelKey: 'HELP.SECTION_DEEDS' },
    { id: 'podium',          labelKey: 'HELP.SECTION_PODIUM' },
    { id: 'monthly',         labelKey: 'HELP.SECTION_MONTHLY' },
    { id: 'prizes',          labelKey: 'HELP.SECTION_PRIZES' },
    { id: 'invite',          labelKey: 'HELP.SECTION_INVITE' },
    { id: 'settings-help',   labelKey: 'HELP.SECTION_SETTINGS' },
    { id: 'faq',             labelKey: 'HELP.SECTION_FAQ' },
  ];

  faqIndexes = [1, 2, 3, 4, 5, 6, 7, 8];

  activeSection = signal('getting-started');

  private observer: IntersectionObserver | null = null;

  ngOnInit() {
    if (typeof IntersectionObserver !== 'undefined') {
      this.observer = new IntersectionObserver(
        (entries) => {
          const visible = entries.find(e => e.isIntersecting);
          if (visible) this.activeSection.set(visible.target.id);
        },
        { rootMargin: '-20% 0px -70% 0px' }
      );
      // Observe after render
      setTimeout(() => {
        this.sections.forEach(s => {
          const el = document.getElementById(s.id);
          if (el) this.observer!.observe(el);
        });
      }, 0);
    }
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }

  onLangChange(event: Event) {
    const code = (event.target as HTMLSelectElement).value;
    this.translate.use(code);
    this.currentLang.set(code);
    // Intentionally NOT writing to localStorage — don't override user's saved preference
  }

  onNavClick(event: Event, id: string) {
    event.preventDefault();
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
    this.activeSection.set(id);
  }

  onMobileNav(event: Event) {
    const id = (event.target as HTMLSelectElement).value;
    if (id) {
      document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
      (event.target as HTMLSelectElement).value = '';
    }
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/app/features/help/help.component.ts
git commit -m "feat: add HelpComponent for /help route"
```

---

## Task 5: Register the `/help` route

**Files:**
- Modify: `frontend/src/app/app.routes.ts`

- [ ] **Step 1: Add the `/help` route**

In `frontend/src/app/app.routes.ts`, add the following line after the `auth/callback` route (before the `create-family` route). The `**` wildcard route must always remain last:

```typescript
{ path: 'help', loadComponent: () => import('./features/help/help.component').then(m => m.HelpComponent) },
```

The routes array around that point should look like this after the edit:

```typescript
{ path: 'auth/callback', loadComponent: () => import('./features/auth/pages/callback.component').then(m => m.CallbackComponent) },
{ path: 'help', loadComponent: () => import('./features/help/help.component').then(m => m.HelpComponent) },
{ path: 'create-family', canActivate: [authGuard], loadComponent: () => import('./features/auth/pages/create-family.component').then(m => m.CreateFamilyComponent) },
```

- [ ] **Step 2: Verify the app builds without errors**

```bash
cd frontend && npx ng build --configuration production 2>&1 | tail -20
```

Expected: build completes with `Application bundle generation complete.` and no errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/app.routes.ts
git commit -m "feat: register /help route (public, no authGuard)"
```

---

## Task 6: Add "Help" link from the Settings page

**Files:**
- Modify: `frontend/src/app/features/settings/pages/settings.component.ts`

- [ ] **Step 1: Add HELP nav entry to en.json**

In `frontend/public/assets/i18n/en.json`, add to the `SETTINGS` object:

```json
"HELP": "Help & Guide"
```

Add the same key to the other four locale files:
- `hu.json`: `"HELP": "Súgó és útmutató"`
- `de.json`: `"HELP": "Hilfe & Anleitung"`
- `fr.json`: `"HELP": "Aide & Guide"`
- `es.json`: `"HELP": "Ayuda y guía"`

- [ ] **Step 2: Add the Help link card to SettingsComponent**

In `frontend/src/app/features/settings/pages/settings.component.ts`, add a help link card inside the `<div class="space-y-3">` block, after the last existing `<a>` card and before the closing `</div>`:

```html
<a routerLink="/help" class="block bg-white rounded-xl border border-brand-border p-4 hover:bg-brand-cream transition-colors">
  <div class="flex items-center justify-between">
    <div class="flex items-center gap-3">
      <span class="text-xl">❓</span>
      <span class="font-medium text-brand-text">{{ 'SETTINGS.HELP' | translate }}</span>
    </div>
    <span class="text-brand-muted">→</span>
  </div>
</a>
```

- [ ] **Step 3: Verify JSON files are valid**

```bash
cd frontend
for lang in en hu de fr es; do
  node -e "JSON.parse(require('fs').readFileSync('public/assets/i18n/$lang.json','utf8')); console.log('$lang: valid')"
done
```

Expected: all five print `valid`.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/settings/pages/settings.component.ts \
        frontend/public/assets/i18n/en.json \
        frontend/public/assets/i18n/hu.json \
        frontend/public/assets/i18n/de.json \
        frontend/public/assets/i18n/fr.json \
        frontend/public/assets/i18n/es.json
git commit -m "feat: add Help link to Settings page"
```

---

## Task 7: Verify in the browser

- [ ] **Step 1: Start the dev server**

```bash
cd frontend && npm start
```

Wait for `Application bundle generation complete` in the terminal, then open http://localhost:4200.

- [ ] **Step 2: Verify the route is publicly accessible**

Navigate to http://localhost:4200/help **without logging in**. The help page should load with the sticky header, sidebar, and all 9 sections visible.

- [ ] **Step 3: Verify language switching**

Change the language picker in the header. All text (section headings, step titles, captions, FAQ) should update immediately. Navigate away and back — the language should reflect the app's `th_preferred_lang` (not the docs page's last selection).

- [ ] **Step 4: Verify mobile layout**

Resize the browser window to below 768px (or use DevTools device emulation). The sidebar should disappear and the "Jump to section" dropdown should appear at the top.

- [ ] **Step 5: Verify scroll-spy**

On desktop, scroll slowly through the page. The active sidebar link should update as each section enters the viewport.

- [ ] **Step 6: Verify Settings link**

Log in, go to Settings. A "Help & Guide" entry with ❓ should appear. Tapping it navigates to `/help`.

- [ ] **Step 7: Final build check**

```bash
cd frontend && npx ng build --configuration production 2>&1 | tail -5
```

Expected: `Application bundle generation complete.` with no errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: user help page — public /help route with 9 sections, scroll-spy, 5-language support"
```
