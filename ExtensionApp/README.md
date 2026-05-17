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

- Shared `chrome.storage.local` storage between the side panel and supported grocery pages.
- Seed meals.
- Meals list with ingredient entry.
- AI meal ideas with review before saving.
- Weekly auto-plan from approved meals.
- Manual meal add/remove in the weekly planner.
- Shopping list generation from planned meals.
- Dedicated Lists page for creating, renaming, and editing lists.
- Dedicated Shopping page for running grocery shopping flows.
- Manual shopping items for non-food products.
- Product preferences.
- Preference refresh and removal in the side panel.
- Retailer selector for Tesco, Sainsbury's, Asda, Morrisons, Waitrose, Ocado, Iceland, Co-op, and Amazon Fresh.
- Grocery site content overlay.
- Save the current grocery product as a retailer-specific preference.
- Click a visible add-to-basket button from the overlay.
- Start a shopping run from the side panel and continue item-by-item in one retailer tab.
- Export local data as JSON.

## Planned Next Steps

- Edit full recipes and ingredients in the side panel.
- AI meal suggestions from the extension settings.
- Better product candidate ranking from page HTML.
- Import/export restore.
- Richer shopping run state shared between side panel and grocery pages.
- Optional migration/import from the existing WPF SQLite database export.

## Architecture

- `manifest.json` - Chrome extension manifest.
- `src/sidepanel` - main app UI.
- `src/content` - grocery page overlay.
- `src/shared` - shared storage and domain helpers.
- `src/background` - extension service worker.
