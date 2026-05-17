# Chrome Web Store Listing Draft

## Short description

Local-first meal planning and grocery shopping assistant for major UK online grocery sites.

## Detailed description

Grocery Shopping Agent helps plan meals, build shopping lists, and work through those lists on supported online grocery sites.

Features:
- Save meals and ingredients.
- Generate weekly meal plans.
- Create editable grocery shopping lists.
- Choose a supported retailer before shopping.
- Open each shopping item on the selected retailer's search page.
- Save retailer-specific product preferences for future lists.
- Use an on-page overlay to add products and move through the list.
- Optional AI meal ideas using the user's own OpenAI API key.
- Export local extension data as JSON.

Supported retailers:
- Tesco
- Sainsbury's
- Asda
- Morrisons
- Waitrose
- Ocado
- Iceland
- Co-op
- Amazon Fresh

## Category

Productivity

## Single purpose

Help users plan meals, create grocery lists, and shop those lists on supported online grocery websites.

## Permission justifications

storage:
Stores meals, meal plans, shopping lists, product preferences, settings, and AI request logs locally in Chrome.

sidePanel:
Shows the main meal planning and shopping list interface in Chrome's side panel.

tabs:
Opens the current shopping item on the selected retailer website and updates the active tab during a shopping run.

Host permissions:
Required so the extension overlay can run on supported grocery websites and so the optional AI feature can call the OpenAI API when enabled by the user.

## Privacy disclosure draft

Grocery Shopping Agent stores meal plans, meals, shopping lists, product preferences, settings, and AI request logs locally using Chrome storage on the user's device.

The extension does not sell user data and does not use third-party analytics.

If AI meal ideas are disabled, the extension does not send meal planning prompts to an AI provider. If the user enables AI and enters an OpenAI API key, the extension sends the user's meal idea prompt, recent meal names, and existing meal names to OpenAI to generate meal suggestions. The user's OpenAI API key is stored locally in Chrome storage.

The extension runs an overlay on supported grocery websites so users can save product preferences and step through shopping lists. Product preference data is stored locally.

## Test instructions

1. Load the extension and open the side panel from the toolbar icon.
2. Create or use seeded meals.
3. Create a shopping list from the planner or manually add shopping items.
4. Go to the Shopping tab, select a retailer, and click Shop.
5. Confirm the selected retailer search page opens.
6. On a supported retailer product page, use the overlay to save a product preference or click Add product.
7. Optional AI test: enter an OpenAI API key in Settings, enable AI, and request meal ideas.
