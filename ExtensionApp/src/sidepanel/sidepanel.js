import { ensureSeedData } from "../shared/seed.js";
import { getAll, getOne, put, remove } from "../shared/db.js";
import { combineIngredients, createShoppingListFromMealIds } from "../shared/shopping.js";
import { suggestMealsWithAi } from "../shared/ai.js";

const DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
const RETAILERS = window.ShoppingAgentRetailers;

let currentShoppingList = null;
let currentShoppingIndex = 0;
let draftIngredients = [];
let aiSuggestions = [];

await ensureSeedData();
await renderAll();

document.querySelectorAll("nav button").forEach(button => {
  button.addEventListener("click", () => switchView(button.dataset.view));
});

document.getElementById("add-ingredient").addEventListener("click", addDraftIngredient);
document.getElementById("add-meal").addEventListener("click", addMeal);
document.getElementById("ask-ai-meals").addEventListener("click", askAiMeals);
document.getElementById("auto-plan").addEventListener("click", autoPlan);
document.getElementById("add-plan-meal").addEventListener("click", addPlanMeal);
document.getElementById("make-shopping-list").addEventListener("click", makeShoppingList);
document.getElementById("shopping-list-select").addEventListener("change", selectShoppingList);
document.getElementById("shopping-run-list-select").addEventListener("change", selectShoppingList);
document.getElementById("create-shopping-list").addEventListener("click", createShoppingList);
document.getElementById("rename-shopping-list").addEventListener("click", renameShoppingList);
document.getElementById("start-shopping").addEventListener("click", startShopping);
document.getElementById("open-current-retailer").addEventListener("click", openCurrentRetailer);
document.getElementById("next-shopping-item").addEventListener("click", nextShoppingItem);
document.getElementById("stop-shopping").addEventListener("click", stopShopping);
document.getElementById("shopping-retailer-select").addEventListener("change", saveSelectedRetailer);
document.getElementById("add-meal-to-shopping").addEventListener("click", addMealToShoppingList);
document.getElementById("add-shopping-item").addEventListener("click", addManualShoppingItem);
document.getElementById("refresh-mappings").addEventListener("click", renderMappings);
document.getElementById("save-settings").addEventListener("click", saveSettings);
document.getElementById("export-data").addEventListener("click", exportData);

function switchView(view) {
  document.querySelectorAll("nav button").forEach(button => button.classList.toggle("active", button.dataset.view === view));
  document.querySelectorAll(".view").forEach(section => section.classList.toggle("active", section.id === `view-${view}`));
}

async function renderAll() {
  await renderMeals();
  renderAiSuggestions();
  await renderPlanner();
  await renderShopping();
  await renderMappings();
  await renderSettings();
}

async function askAiMeals() {
  const prompt = document.getElementById("ai-prompt").value.trim();
  const status = document.getElementById("ai-status");
  if (!prompt) return;

  status.textContent = "Asking AI for meal ideas...";
  try {
    aiSuggestions = await suggestMealsWithAi(prompt);
    status.textContent = `Got ${aiSuggestions.length} meal ideas. Review before saving.`;
    renderAiSuggestions();
  } catch (error) {
    status.textContent = error.message || "AI request failed.";
  }
}

function renderAiSuggestions() {
  const list = document.getElementById("ai-suggestion-list");
  if (!list) return;
  list.innerHTML = aiSuggestions.length ? "" : `<div class="card muted">No AI suggestions yet.</div>`;

  for (const [index, meal] of aiSuggestions.entries()) {
    const ingredients = meal.ingredients.map(ingredient => ingredient.name).join(", ");
    const card = document.createElement("div");
    card.className = "card";
    card.innerHTML = `
      <div class="card-title">${escapeHtml(meal.name)}</div>
      <div class="muted">${meal.prepEffort} - ${meal.cookingTimeMinutes} mins - ${meal.portions} portions</div>
      <div>${escapeHtml(meal.description)}</div>
      <div class="muted">${escapeHtml(ingredients)}</div>
      <div class="row-actions">
        <button class="primary" data-action="save">Save meal</button>
        <button class="danger" data-action="remove">Remove</button>
      </div>
    `;
    card.querySelector("[data-action='save']").addEventListener("click", async () => saveAiSuggestion(index));
    card.querySelector("[data-action='remove']").addEventListener("click", () => {
      aiSuggestions.splice(index, 1);
      renderAiSuggestions();
    });
    list.appendChild(card);
  }
}

async function saveAiSuggestion(index) {
  const meal = aiSuggestions[index];
  if (!meal) return;
  const now = new Date().toISOString();
  await put("meals", {
    ...meal,
    source: "AiSuggested",
    approved: true,
    createdAt: now,
    updatedAt: now
  });
  aiSuggestions.splice(index, 1);
  await renderAll();
  switchView("meals");
}

async function renderMeals() {
  await renderPlannerMealSelect();
  renderDraftIngredients();

  const meals = await getAll("meals");
  const list = document.getElementById("meal-list");
  list.innerHTML = "";
  for (const meal of meals.sort((a, b) => a.name.localeCompare(b.name))) {
    const ingredients = (meal.ingredients || []).map(ingredient => ingredient.name).join(", ");
    const card = document.createElement("div");
    card.className = "card";
    card.innerHTML = `
      <div class="row">
        <div>
          <div class="card-title">${escapeHtml(meal.name)}</div>
          <div class="muted">${meal.prepEffort} - ${meal.cookingTimeMinutes} mins - ${meal.source}</div>
          <div class="muted">${escapeHtml(ingredients || "No ingredients yet")}</div>
        </div>
        <button class="danger" data-id="${meal.id}">Remove</button>
      </div>
    `;
    card.querySelector("button").addEventListener("click", async () => {
      await remove("meals", meal.id);
      await renderAll();
    });
    list.appendChild(card);
  }
}

function addDraftIngredient() {
  const name = document.getElementById("ingredient-name").value.trim();
  if (!name) return;

  draftIngredients.push({
    name,
    quantity: Number(document.getElementById("ingredient-quantity").value || 1),
    unit: document.getElementById("ingredient-unit").value.trim(),
    category: document.getElementById("ingredient-category").value.trim() || "Other"
  });

  document.getElementById("ingredient-name").value = "";
  document.getElementById("ingredient-quantity").value = "";
  document.getElementById("ingredient-unit").value = "";
  document.getElementById("ingredient-category").value = "";
  renderDraftIngredients();
}

function renderDraftIngredients() {
  const list = document.getElementById("new-meal-ingredients");
  list.innerHTML = "";
  for (const [index, ingredient] of draftIngredients.entries()) {
    const row = document.createElement("div");
    row.className = "mini-item";
    row.innerHTML = `
      <span>${escapeHtml(ingredient.name)} <span class="muted">${ingredient.quantity} ${escapeHtml(ingredient.unit)}</span></span>
      <button class="danger" data-index="${index}">Remove</button>
    `;
    row.querySelector("button").addEventListener("click", () => {
      draftIngredients.splice(index, 1);
      renderDraftIngredients();
    });
    list.appendChild(row);
  }
}

async function addMeal() {
  const nameInput = document.getElementById("meal-name");
  const name = nameInput.value.trim();
  if (!name) return;

  const now = new Date().toISOString();
  await put("meals", {
    name,
    description: "User-created meal",
    prepEffort: document.getElementById("meal-effort").value,
    cookingTimeMinutes: Number(document.getElementById("meal-minutes").value || 30),
    portions: 4,
    tags: ["user"],
    source: "UserCreated",
    approved: true,
    ingredients: draftIngredients.map(ingredient => ({ ...ingredient })),
    cookingSteps: [],
    createdAt: now,
    updatedAt: now
  });

  nameInput.value = "";
  draftIngredients = [];
  await renderAll();
}

async function autoPlan() {
  const meals = (await getAll("meals")).filter(meal => meal.approved);
  const selected = meals
    .sort((a, b) => effortRank(a.prepEffort) - effortRank(b.prepEffort) || a.cookingTimeMinutes - b.cookingTimeMinutes)
    .slice(0, 7);
  await put("mealPlans", {
    id: "current-week",
    weekStartDate: startOfWeek(new Date()).toISOString().slice(0, 10),
    meals: selected.map((meal, index) => ({ dayOffset: index, mealId: meal.id, mealName: meal.name }))
  });
  await renderPlanner();
}

async function renderPlanner() {
  await renderPlannerMealSelect();

  const plan = await getCurrentPlan();
  const list = document.getElementById("plan-list");
  list.innerHTML = "";
  if (!plan.meals.length) {
    list.innerHTML = `<div class="card muted">No meals planned yet.</div>`;
    return;
  }

  for (const [index, item] of plan.meals.entries()) {
    const card = document.createElement("div");
    card.className = "card";
    card.innerHTML = `
      <div class="row">
        <span>${DAYS[item.dayOffset] || `Day ${item.dayOffset + 1}`}: ${escapeHtml(item.mealName)}</span>
        <button class="danger" data-index="${index}">Remove</button>
      </div>
    `;
    card.querySelector("button").addEventListener("click", async () => {
      plan.meals.splice(index, 1);
      await put("mealPlans", plan);
      await renderPlanner();
    });
    list.appendChild(card);
  }
}

async function renderPlannerMealSelect() {
  const select = document.getElementById("planner-meal-select");
  if (!select) return;

  const meals = (await getAll("meals")).filter(meal => meal.approved);
  renderMealOptions(select, meals);

  const shoppingMealSelect = document.getElementById("shopping-meal-select");
  if (shoppingMealSelect) {
    renderMealOptions(shoppingMealSelect, meals);
  }
}

function renderMealOptions(select, meals) {
  select.innerHTML = "";
  for (const meal of meals.sort((a, b) => a.name.localeCompare(b.name))) {
    const option = document.createElement("option");
    option.value = meal.id;
    option.textContent = meal.name;
    select.appendChild(option);
  }
}

async function addPlanMeal() {
  const mealId = Number(document.getElementById("planner-meal-select").value);
  const dayOffset = Number(document.getElementById("planner-day-select").value);
  if (!mealId && mealId !== 0) return;

  const meal = (await getAll("meals")).find(item => item.id === mealId);
  if (!meal) return;

  const plan = await getCurrentPlan();
  plan.meals.push({ dayOffset, mealId: meal.id, mealName: meal.name });
  plan.meals.sort((a, b) => a.dayOffset - b.dayOffset || a.mealName.localeCompare(b.mealName));
  await put("mealPlans", plan);
  await renderPlanner();
}

async function getCurrentPlan() {
  return await getOne("mealPlans", "current-week") || {
    id: "current-week",
    weekStartDate: startOfWeek(new Date()).toISOString().slice(0, 10),
    meals: []
  };
}

async function makeShoppingList() {
  const plan = await getCurrentPlan();
  if (!plan.meals.length) return;
  await createShoppingListFromMealIds(plan.meals.map(item => item.mealId));
  await renderShopping();
  switchView("lists");
}

async function renderShopping() {
  const lists = await getAll("shoppingLists");
  const run = await getOne("settings", "shoppingRun");
  await renderRetailerSelect(run);
  const listSelect = document.getElementById("shopping-list-select");
  const runSelect = document.getElementById("shopping-run-list-select");
  const previousId = currentShoppingList?.id || Number(listSelect.value) || Number(runSelect.value);

  listSelect.innerHTML = "";
  runSelect.innerHTML = "";
  for (const list of lists) {
    listSelect.appendChild(createOption(list));
    runSelect.appendChild(createOption(list));
  }

  currentShoppingList = lists.find(list => list.id === previousId) || lists[0] || null;
  const runIsActiveForSelectedList = Boolean(run?.running && currentShoppingList?.id === run.listId);
  currentShoppingIndex = runIsActiveForSelectedList ? run.currentIndex || 0 : 0;
  if (currentShoppingList) {
    listSelect.value = currentShoppingList.id;
    runSelect.value = currentShoppingList.id;
  }
  document.getElementById("shopping-list-name").value = currentShoppingList?.name || "";

  document.querySelector(".run-controls").hidden = !runIsActiveForSelectedList && !currentShoppingList;
  document.getElementById("open-current-retailer").hidden = !runIsActiveForSelectedList;
  document.getElementById("next-shopping-item").hidden = !runIsActiveForSelectedList;
  document.getElementById("stop-shopping").hidden = !runIsActiveForSelectedList;
  document.getElementById("start-shopping").hidden = !currentShoppingList || runIsActiveForSelectedList;

  document.getElementById("shopping-status").textContent = run?.running
    ? `Shopping run active: item ${run.currentIndex + 1} - ${run.phase || "running"}`
    : currentShoppingList ? "Select a list and click Shop when ready." : "Create a list on the Lists page first.";
  renderShoppingItems();
}

async function renderRetailerSelect(run) {
  const select = document.getElementById("shopping-retailer-select");
  const selectedRetailerId = run?.retailerId || await getPreferredRetailerId();
  select.innerHTML = "";
  for (const retailer of RETAILERS.RETAILERS) {
    const option = document.createElement("option");
    option.value = retailer.id;
    option.textContent = retailer.name;
    select.appendChild(option);
  }
  select.value = RETAILERS.retailerById(selectedRetailerId).id;
}

async function saveSelectedRetailer() {
  const retailerId = document.getElementById("shopping-retailer-select").value;
  const settings = await getOne("settings", "app") || { id: "app" };
  await put("settings", {
    ...settings,
    shoppingRetailerId: retailerId
  });
  const run = await getOne("settings", "shoppingRun");
  if (run?.running) {
    await put("settings", {
      ...run,
      retailerId,
      updatedAt: new Date().toISOString()
    });
  }
}

function createOption(list) {
  const option = document.createElement("option");
  option.value = list.id;
  option.textContent = list.name;
  return option;
}

async function selectShoppingList() {
  const source = document.activeElement?.id === "shopping-run-list-select"
    ? "shopping-run-list-select"
    : "shopping-list-select";
  const id = Number(document.getElementById(source).value);
  currentShoppingList = (await getAll("shoppingLists")).find(list => list.id === id) || null;
  currentShoppingIndex = 0;
  document.getElementById("shopping-list-name").value = currentShoppingList?.name || "";
  await renderShopping();
}

async function createShoppingList() {
  const name = uniqueShoppingListName(await getAll("shoppingLists"));
  const id = await put("shoppingLists", {
    name,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    items: []
  });
  const lists = await getAll("shoppingLists");
  currentShoppingList = lists.find(list => list.id === id) || null;
  await renderShopping();
  document.getElementById("shopping-list-select").value = id;
  document.getElementById("shopping-run-list-select").value = id;
  document.getElementById("shopping-status").textContent = `Created ${name}.`;
  document.getElementById("list-status").textContent = `Created ${name}.`;
}

function uniqueShoppingListName(existingLists) {
  const date = new Date().toLocaleDateString();
  const base = `Shopping list ${date}`;
  const existingNames = new Set(existingLists.map(list => list.name));
  if (!existingNames.has(base)) return base;

  let index = 2;
  while (existingNames.has(`${base} ${index}`)) {
    index += 1;
  }
  return `${base} ${index}`;
}

async function renameShoppingList() {
  if (!currentShoppingList) return;
  const name = document.getElementById("shopping-list-name").value.trim();
  if (!name) return;

  await put("shoppingLists", {
    ...currentShoppingList,
    name,
    updatedAt: new Date().toISOString()
  });
  currentShoppingList.name = name;
  await renderShopping();
  document.getElementById("list-status").textContent = `Renamed to ${name}.`;
}

async function addManualShoppingItem() {
  const nameInput = document.getElementById("manual-shopping-name");
  const name = nameInput.value.trim();
  if (!name) return;

  const list = await ensureCurrentShoppingList();

  list.items = [...(list.items || []), {
    name,
    category: document.getElementById("manual-shopping-category").value.trim() || "Other",
    quantity: Number(document.getElementById("manual-shopping-quantity").value || 1),
    unit: document.getElementById("manual-shopping-unit").value.trim(),
    checkedOff: false,
    alreadyOwned: false,
    source: "Manual"
  }];

  await put("shoppingLists", list);
  nameInput.value = "";
  document.getElementById("manual-shopping-quantity").value = "1";
  document.getElementById("manual-shopping-unit").value = "";
  document.getElementById("manual-shopping-category").value = "";
  await renderShopping();
  document.getElementById("list-status").textContent = `Added ${name}.`;
}

async function ensureCurrentShoppingList() {
  if (currentShoppingList) return currentShoppingList;
  const id = await put("shoppingLists", {
    name: `Shopping list ${new Date().toLocaleDateString()}`,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    items: []
  });
  const lists = await getAll("shoppingLists");
  currentShoppingList = lists.find(list => list.id === id) || null;
  return currentShoppingList;
}

async function addMealToShoppingList() {
  const mealId = Number(document.getElementById("shopping-meal-select").value);
  const meal = (await getAll("meals")).find(item => item.id === mealId);
  if (!meal) return;

  const list = await ensureCurrentShoppingList();
  list.items = mergeShoppingItems(list.items || [], combineIngredients([meal]));
  await put("shoppingLists", {
    ...list,
    updatedAt: new Date().toISOString()
  });
  await renderShopping();
  document.getElementById("list-status").textContent = `Added ${meal.name}.`;
}

function mergeShoppingItems(existingItems, newItems) {
  const grouped = new Map();
  for (const item of [...existingItems, ...newItems]) {
    const key = `${item.name.toLowerCase()}|${item.unit || ""}`;
    const existing = grouped.get(key);
    if (existing) {
      existing.quantity = Number(existing.quantity || 0) + Number(item.quantity || 0);
    } else {
      grouped.set(key, { ...item });
    }
  }
  return [...grouped.values()].sort((a, b) => `${a.category}${a.name}`.localeCompare(`${b.category}${b.name}`));
}

function renderShoppingItems() {
  const current = getCurrentShoppingItem();
  document.getElementById("current-shopping-item").innerHTML = current
    ? `<div class="card-title">Current: ${escapeHtml(current.name)}</div><div class="muted">${current.quantity} ${current.unit || ""}</div>`
    : `<span class="muted">No current shopping item.</span>`;

  const editorList = document.getElementById("shopping-items");
  const runList = document.getElementById("shopping-run-items");
  editorList.innerHTML = "";
  runList.innerHTML = "";
  for (const [index, item] of (currentShoppingList?.items || []).entries()) {
    editorList.appendChild(renderShoppingItemCard(item, index, true));
    runList.appendChild(renderShoppingItemCard(item, index, false));
  }
}

function renderShoppingItemCard(item, index, canRemove) {
  const card = document.createElement("div");
  card.className = "card";
  card.innerHTML = `
    <div class="row">
      <div>
        <span>${index === currentShoppingIndex ? "<strong>Current</strong> " : ""}${escapeHtml(item.name)}</span>
        <div class="muted">${escapeHtml(item.category || "Other")} - ${item.source || "Meal"}</div>
      </div>
      <div class="row-actions">
        <span class="muted">${item.quantity} ${escapeHtml(item.unit || "")}</span>
        ${canRemove ? `<button class="danger" data-index="${index}">Remove</button>` : ""}
      </div>
    </div>
  `;
  if (canRemove) {
    card.querySelector("button").addEventListener("click", async () => removeShoppingItem(index));
  }
  return card;
}

async function removeShoppingItem(index) {
  if (!currentShoppingList?.items) return;
  currentShoppingList.items.splice(index, 1);
  await put("shoppingLists", currentShoppingList);
  if (currentShoppingIndex >= currentShoppingList.items.length) {
    currentShoppingIndex = Math.max(0, currentShoppingList.items.length - 1);
  }
  await renderShopping();
}

async function startShopping() {
  if (!currentShoppingList?.items?.length) return;
  const retailerId = await getPreferredRetailerId();

  const run = {
    id: "shoppingRun",
    listId: currentShoppingList.id,
    currentIndex: 0,
    retailerId,
    running: true,
    phase: "starting",
    autoAddAttempted: false,
    updatedAt: new Date().toISOString()
  };
  await put("settings", run);
  currentShoppingIndex = 0;
  await openShoppingTarget(currentShoppingList.items[0], run);
  await renderShopping();
}

async function openCurrentRetailer() {
  const item = getCurrentShoppingItem();
  if (!item) return;
  await openShoppingTarget(item);
}

async function nextShoppingItem() {
  if (!currentShoppingList) return;
  const nextIndex = Math.min(currentShoppingIndex + 1, currentShoppingList.items.length);
  currentShoppingIndex = nextIndex;
  const run = {
    id: "shoppingRun",
    listId: currentShoppingList.id,
    currentIndex: nextIndex,
    retailerId: await getPreferredRetailerId(),
    running: nextIndex < currentShoppingList.items.length,
    phase: nextIndex < currentShoppingList.items.length ? "movingNext" : "complete",
    autoAddAttempted: false,
    updatedAt: new Date().toISOString()
  };
  await put("settings", run);
  renderShoppingItems();
  if (nextIndex < currentShoppingList.items.length) {
    await openShoppingTarget(currentShoppingList.items[nextIndex], run);
  }
}

async function stopShopping() {
  const run = await getOne("settings", "shoppingRun");
  if (!run) return;
  await put("settings", {
    ...run,
    running: false,
    phase: "stopped",
    updatedAt: new Date().toISOString()
  });
  await renderShopping();
}

async function openShoppingTarget(item, existingRun = null) {
  const mapping = await findMappingForItem(item);
  const run = existingRun || await getOne("settings", "shoppingRun");
  const retailerId = run?.retailerId || await getPreferredRetailerId();
  const url = RETAILERS.searchUrlForItem(item, mapping, retailerId);
  if (run?.id === "shoppingRun") {
    await put("settings", {
      ...run,
      retailerId,
      targetUrl: url,
      phase: mapping?.productUrl && RETAILERS.isProductUrl(mapping.productUrl) ? "preferredProductOpened" : "awaitingManualSelection",
      autoAddAttempted: false,
      updatedAt: new Date().toISOString()
    });
  }
  await openUrlInCurrentTab(url);
}

async function findMappingForItem(item) {
  const mappings = await getAll("productMappings");
  const retailerId = await getPreferredRetailerId();
  const retailerMatch = mappings.find(mapping =>
    mapping.ingredientName?.toLowerCase() === item.name.toLowerCase()
    && mapping.retailerId === retailerId
  );
  if (retailerMatch) return retailerMatch;
  if (retailerId === "tesco") {
    return mappings.find(mapping => mapping.ingredientName?.toLowerCase() === item.name.toLowerCase() && !mapping.retailerId);
  }
  return null;
}

async function getPreferredRetailerId() {
  const selected = document.getElementById("shopping-retailer-select")?.value;
  if (selected) return selected;
  const settings = await getOne("settings", "app");
  return RETAILERS.retailerById(settings?.shoppingRetailerId).id;
}

async function openUrlInCurrentTab(url) {
  const [tab] = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  if (tab?.id && !tab.url?.startsWith("chrome://") && !tab.url?.startsWith("chrome-extension://")) {
    await chrome.tabs.update(tab.id, { url });
    return;
  }
  await chrome.tabs.create({ url });
}

async function renderMappings() {
  const mappings = await getAll("productMappings");
  const list = document.getElementById("mapping-list");
  list.innerHTML = mappings.length ? "" : `<div class="card muted">No preferences saved yet.</div>`;
  for (const mapping of mappings.sort((a, b) => (a.ingredientName || "").localeCompare(b.ingredientName || ""))) {
    const card = document.createElement("div");
    card.className = "card";
    card.innerHTML = `
      <div class="row">
        <div>
          <div class="card-title">${escapeHtml(mapping.ingredientName || mapping.productName)}</div>
          <div>${escapeHtml(mapping.productName || "")}</div>
          <div class="muted">${escapeHtml(mapping.supermarketName || RETAILERS.retailerById(mapping.retailerId).name)}</div>
          <div class="muted">${escapeHtml(mapping.productUrl || "")}</div>
        </div>
        <button class="danger" data-id="${mapping.id}">Remove</button>
      </div>
    `;
    card.querySelector("button").addEventListener("click", async () => {
      await remove("productMappings", mapping.id);
      await renderMappings();
    });
    list.appendChild(card);
  }
}

async function renderSettings() {
  const settings = await getOne("settings", "app");
  document.getElementById("openai-key").value = settings?.openAiApiKey || "";
  document.getElementById("openai-model").value = settings?.openAiModel || "gpt-4.1-mini";
  document.getElementById("openai-max-output").value = settings?.maxOutputTokens || 3500;
  document.getElementById("enable-ai").checked = Boolean(settings?.enableAi);
}

async function saveSettings() {
  const existing = await getOne("settings", "app") || { id: "app" };
  await put("settings", {
    ...existing,
    id: "app",
    openAiApiKey: document.getElementById("openai-key").value,
    openAiModel: document.getElementById("openai-model").value || "gpt-4.1-mini",
    maxOutputTokens: Number(document.getElementById("openai-max-output").value || 3500),
    enableAi: document.getElementById("enable-ai").checked
  });
}

async function exportData() {
  const data = {};
  for (const store of ["settings", "meals", "mealPlans", "shoppingLists", "productMappings", "aiLogs"]) {
    data[store] = await getAll(store);
  }
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  chrome.tabs.create({ url });
}

function getCurrentShoppingItem() {
  return currentShoppingList?.items?.[currentShoppingIndex] || null;
}

function effortRank(effort) {
  return effort === "Low" ? 0 : effort === "Medium" ? 1 : 2;
}

function startOfWeek(date) {
  const copy = new Date(date);
  const day = (copy.getDay() + 6) % 7;
  copy.setDate(copy.getDate() - day);
  return copy;
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, char => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#039;"
  }[char]));
}
