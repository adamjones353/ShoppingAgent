init(window.ShoppingAgentDb);

function init(db) {
  if (!currentRetailer()) return;
  const root = ensureOverlay();
  root.querySelector("#sa-toggle").addEventListener("click", toggleOverlay);
  root.querySelector("#sa-use-product").addEventListener("click", () => runSafely(() => useCurrentProduct(db)));
  root.querySelector("#sa-save-map").addEventListener("click", () => runSafely(() => saveMapping(db)));
  root.querySelector("#sa-add").addEventListener("click", addCurrentProduct);
  root.querySelector("#sa-open-panel").addEventListener("click", () => runSafely(openPanel));
  root.querySelector("#sa-next").addEventListener("click", () => runSafely(() => advanceShoppingRun(db, true)));
  root.querySelector("#sa-stop").addEventListener("click", () => runSafely(() => stopShoppingRun(db)));
  runSafely(() => updateOverlay(db));
  setInterval(() => runSafely(() => updateOverlay(db)), 1500);
  setTimeout(() => runSafely(() => autoAddPreferredProduct(db)), 1200);
}

function ensureOverlay() {
  let root = document.getElementById("shopping-agent-grocery");
  if (root) return root;
  root = document.createElement("div");
  root.id = "shopping-agent-grocery";
  root.innerHTML = `
    <div class="sa-header">
      <strong>Grocery Shopping Agent</strong>
      <button id="sa-toggle" title="Minimize Grocery Shopping Agent">Minimize</button>
    </div>
    <div class="sa-body">
      <div id="sa-state" class="sa-state idle">Idle</div>
      <div id="sa-page" class="muted"></div>
      <div id="sa-run" class="muted"></div>
      <button id="sa-open-panel">Open panel</button>
      <button id="sa-use-product">Use this product</button>
      <button id="sa-add">Add product</button>
      <button id="sa-save-map">Save preference</button>
      <button id="sa-next">Next item</button>
      <button id="sa-stop">Stop</button>
      <div id="sa-status" class="muted"></div>
    </div>
  `;
  document.body.appendChild(root);
  if (sessionStorage.getItem("shoppingAgentOverlayMinimized") === "true") {
    root.classList.add("minimized");
    root.querySelector("#sa-toggle").textContent = "Show";
  }
  return root;
}

function toggleOverlay() {
  const root = document.getElementById("shopping-agent-grocery");
  const minimized = !root.classList.contains("minimized");
  root.classList.toggle("minimized", minimized);
  root.querySelector("#sa-toggle").textContent = minimized ? "Show" : "Minimize";
  sessionStorage.setItem("shoppingAgentOverlayMinimized", minimized ? "true" : "false");
}

async function updateOverlay(db) {
  const page = document.getElementById("sa-page");
  const runText = document.getElementById("sa-run");
  const state = document.getElementById("sa-state");
  if (!page || !runText || !state) return;

  const retailer = currentRetailer();
  page.textContent = isProductPage()
    ? `Product: ${productNameFromPage()}`
    : `Open a ${retailer?.name || "grocery"} product page to use or save it.`;

  const runState = await getRunState(db);
  updateRunControls(runState);

  state.className = `sa-state ${runState.status}`;
  state.textContent = runState.label;
  runText.textContent = runState.item
    ? `Shopping: ${runState.item.name} (${runState.run.currentIndex + 1} of ${runState.list.items.length})`
    : runState.detail;
}

function productNameFromPage() {
  return window.ShoppingAgentRetailers.productNameFromDocument(document, currentRetailer());
}

function findAddButton() {
  return [...document.querySelectorAll("button")].find(button => {
    const text = `${button.innerText || ""} ${button.getAttribute("aria-label") || ""}`.toLowerCase();
    return text.includes("add") && !button.disabled && button.offsetParent !== null;
  });
}

function addCurrentProduct() {
  const status = document.getElementById("sa-status");
  if (!isProductPage()) {
    status.textContent = `Open a ${currentRetailer()?.name || "grocery"} product page first.`;
    return false;
  }
  const button = findAddButton();
  if (!button) {
    status.textContent = "Could not find an Add button.";
    return false;
  }
  button.click();
  status.textContent = `Clicked Add. Check your ${currentRetailer()?.name || "retailer"} basket before checkout.`;
  return true;
}

async function autoAddPreferredProduct(db) {
  const context = await getRunContext(db);
  if (!context?.item || !isProductPage()) return;
  if (context.run.phase !== "preferredProductOpened" || context.run.autoAddAttempted) return;

  const mapping = await findMapping(db, context.item);
  if (!mapping?.productUrl || normalizeUrl(mapping.productUrl) !== normalizeUrl(location.href)) return;

  await db.put("settings", {
    ...context.run,
    autoAddAttempted: true,
    phase: "addingPreferredProduct",
    updatedAt: new Date().toISOString()
  });

  const status = document.getElementById("sa-status");
  status.textContent = "Preferred product found. Adding to basket.";
  if (addCurrentProduct()) {
    setTimeout(() => advanceShoppingRun(db, true, context.run.currentIndex), 1200);
  }
}

async function useCurrentProduct(db) {
  const status = document.getElementById("sa-status");
  const context = await getRunContext(db);
  if (!isProductPage()) {
    status.textContent = `Open the chosen ${currentRetailer()?.name || "grocery"} product page first.`;
    return;
  }
  if (!context?.item) {
    status.textContent = "No active shopping item.";
    return;
  }

  const savePreferred = confirm(`Save "${productNameFromPage()}" as the preferred product for "${context.item.name}"?`);
  if (savePreferred) {
    await saveMappingForIngredient(db, context.item.name);
  }

  if (addCurrentProduct()) {
    setTimeout(() => advanceShoppingRun(db, true, context.run.currentIndex), 1200);
  }
}

async function saveMapping(db) {
  const status = document.getElementById("sa-status");
  if (!isProductPage()) {
    status.textContent = `Open a ${currentRetailer()?.name || "grocery"} product page first.`;
    return;
  }
  const context = await getRunContext(db);
  const defaultName = context?.item?.name || productNameFromPage();
  const ingredientName = prompt("Map this product to which ingredient?", defaultName);
  if (!ingredientName) return;
  await saveMappingForIngredient(db, ingredientName);
}

async function saveMappingForIngredient(db, ingredientName) {
  const mappings = await db.getAll("productMappings");
  const retailerId = currentRetailer()?.id || "unknown";
  const existing = mappings.find(mapping =>
    mapping.ingredientName?.toLowerCase() === ingredientName.toLowerCase()
    && (mapping.retailerId || mapping.supermarketName?.toLowerCase()) === retailerId
  );
  await db.put("productMappings", {
    id: existing?.id,
    ingredientName,
    supermarketName: currentRetailer()?.name || "Unknown",
    retailerId,
    productName: productNameFromPage(),
    searchTerm: ingredientName,
    productUrl: location.href,
    lastUsedAt: new Date().toISOString()
  });
  document.getElementById("sa-status").textContent = "Saved preference.";
}

async function advanceShoppingRun(db, openNext, expectedIndex = null) {
  const context = await getRunContext(db);
  const status = document.getElementById("sa-status");
  if (!context) {
    status.textContent = "No active shopping run.";
    return;
  }
  if (expectedIndex !== null && context.run.currentIndex !== expectedIndex) {
    status.textContent = "Shopping run changed. Not moving automatically.";
    return;
  }

  const nextIndex = context.run.currentIndex + 1;
  const running = nextIndex < context.list.items.length;
  const nextUrl = running ? await targetUrlForItem(db, context.list.items[nextIndex]) : "";
  const hasPreferred = running && window.ShoppingAgentRetailers.isProductUrl(nextUrl);
  await db.put("settings", {
    ...context.run,
    currentIndex: nextIndex,
    running,
    phase: running ? (hasPreferred ? "preferredProductOpened" : "awaitingManualSelection") : "complete",
    targetUrl: nextUrl,
    autoAddAttempted: false,
    updatedAt: new Date().toISOString()
  });

  if (!running) {
    status.textContent = "Shopping list complete. Review your basket before checkout.";
    return;
  }

  status.textContent = `Moving to ${context.list.items[nextIndex].name}.`;
  if (openNext) {
    location.href = nextUrl;
  }
}

async function stopShoppingRun(db) {
  const run = await db.getOne("settings", "shoppingRun");
  const status = document.getElementById("sa-status");
  if (!run) {
    status.textContent = "No active shopping run.";
    return;
  }
  await db.put("settings", {
    ...run,
    running: false,
    phase: "stopped",
    updatedAt: new Date().toISOString()
  });
  status.textContent = "Shopping run stopped.";
  await updateOverlay(db);
}

async function getRunState(db) {
  const run = await db.getOne("settings", "shoppingRun");
  if (!run) {
    return { status: "idle", label: "Idle", detail: "No shopping run started." };
  }

  const list = await db.getOne("shoppingLists", run.listId);
  const item = list?.items?.[run.currentIndex];
  if (run.running && list && item) {
    return { status: "active", label: "Shopping run active", detail: "", run, list, item };
  }
  if (run.phase === "complete") {
    return { status: "complete", label: "Shopping complete", detail: "Review your basket before checkout.", run, list };
  }
  if (run.phase === "stopped") {
    return { status: "stopped", label: "Shopping stopped", detail: "Start shopping from the side panel to continue.", run, list };
  }
  return { status: "idle", label: "Idle", detail: "No active shopping run.", run, list };
}

function updateRunControls(runState) {
  const isActive = runState.status === "active";
  setVisible("sa-use-product", isActive);
  setVisible("sa-next", isActive);
  setVisible("sa-stop", isActive);
}

function setVisible(id, visible) {
  const element = document.getElementById(id);
  if (element) element.hidden = !visible;
}

async function getRunContext(db) {
  const state = await getRunState(db);
  return state.status === "active" ? state : null;
}

async function targetUrlForItem(db, item) {
  const mapping = await findMapping(db, item);
  const run = await db.getOne("settings", "shoppingRun");
  return window.ShoppingAgentRetailers.searchUrlForItem(item, mapping, run?.retailerId);
}

async function findMapping(db, item) {
  const mappings = await db.getAll("productMappings");
  const run = await db.getOne("settings", "shoppingRun");
  const retailerMatch = mappings.find(mapping =>
    mapping.ingredientName?.toLowerCase() === item.name.toLowerCase()
    && mapping.retailerId === run?.retailerId
  );
  if (retailerMatch) return retailerMatch;
  if (run?.retailerId === "tesco") {
    return mappings.find(mapping => mapping.ingredientName?.toLowerCase() === item.name.toLowerCase() && !mapping.retailerId);
  }
  return null;
}

function normalizeUrl(url) {
  return window.ShoppingAgentRetailers.urlOriginAndPath(url);
}

function currentRetailer() {
  return window.ShoppingAgentRetailers.retailerForUrl(location.href);
}

function isProductPage() {
  return window.ShoppingAgentRetailers.isProductUrl(location.href, currentRetailer());
}

async function runSafely(action) {
  try {
    await action();
  } catch (error) {
    if (String(error?.message || error).includes("Extension context invalidated")) {
      return;
    }
    const status = document.getElementById("sa-status");
    if (status) status.textContent = error.message || "Grocery Shopping Agent action failed.";
    throw error;
  }
}

async function openPanel() {
  const response = await chrome.runtime.sendMessage({ type: "OPEN_SIDE_PANEL" });
  const status = document.getElementById("sa-status");
  if (status) {
    status.textContent = response?.ok ? "Opened Grocery Shopping Agent." : "Could not open panel.";
  }
}
