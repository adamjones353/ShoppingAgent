# Grocery Shopping Agent Chrome Extension

This is the browser-first version of Grocery Shopping Agent. It is designed to become the primary app so meal planning and grocery shopping live in Chrome.

## Install During Development

1. Open Chrome.
2. Go to `chrome://extensions`.
3. Enable `Developer mode`.
4. Click `Load unpacked`.
5. Select this folder:

```text
ExtensionApp
```

Click the Grocery Shopping Agent toolbar icon to open the side panel.

## What Works Now

- Shared `chrome.storage.local` storage between the side panel and Tesco pages.
- Seed meals.
- Meals list with ingredient entry.
- AI meal ideas with review before saving.
- Weekly auto-plan from approved meals.
- Manual meal add/remove in the weekly planner.
- Shopping list generation from planned meals.
- Dedicated Lists page for creating, renaming, and editing lists.
- Dedicated Shopping page for running the Tesco shopping flow.
- Manual shopping items for non-food products.
- Product preferences.
- Preference refresh and removal in the side panel.
- Tesco content overlay.
- Save current Tesco product as a preference.
- Click a visible Tesco add button from the overlay.
- Start a shopping run from the side panel and continue item-by-item in one Tesco tab.
- Export local data as JSON.

## Planned Next Steps

- Edit full recipes and ingredients in the side panel.
- AI meal suggestions from the extension settings.
- Better Tesco product candidate ranking from page HTML.
- Import/export restore.
- Richer shopping run state shared between side panel and Tesco pages.
- Optional migration/import from the existing WPF SQLite database export.

## Architecture

- `manifest.json` - Chrome extension manifest.
- `src/sidepanel` - main app UI.
- `src/content` - Tesco page overlay.
- `src/shared` - shared storage and domain helpers.
- `src/background` - extension service worker.
