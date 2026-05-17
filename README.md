# Grocery Shopping Agent

Local-first Chrome extension for meal planning, shopping lists, grocery preferences, and assisted Tesco shopping.

## Current App

The active app lives in:

```text
ExtensionApp
```

Load it in Chrome with:

1. Open `chrome://extensions`.
2. Enable `Developer mode`.
3. Click `Load unpacked`.
4. Select `C:\Users\adam\dev\ShoppingAgent\ExtensionApp`.

## What Is Included

- Chrome extension side panel app.
- Local storage using `chrome.storage.local`.
- Meal list with ingredients.
- AI meal ideas using OpenAI, with review before saving.
- Weekly planner for users who want day-by-day planning.
- Dedicated Lists page for creating, renaming, and editing shopping lists.
- Dedicated Shopping page for running the Tesco shopping flow.
- Manual shopping items for non-food products.
- Product preferences for saved Tesco products.
- Tesco page overlay with minimize/show, active-run status, product selection, stop, and preference saving.

## Notes

The old WPF desktop app has been removed from this repository. The remaining implementation is the Chrome extension.
