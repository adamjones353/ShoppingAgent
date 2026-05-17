const BRIDGE = "http://127.0.0.1:51234";

let currentItem = null;

function ensurePanel() {
  let root = document.getElementById("shopping-agent-root");
  if (root) return root;

  root = document.createElement("div");
  root.id = "shopping-agent-root";
  root.innerHTML = `
    <div class="sa-title">ShoppingAgent</div>
    <div id="sa-status" class="sa-line sa-muted">Connecting to desktop app...</div>
    <div id="sa-item" class="sa-line"></div>
    <div class="sa-line">
      <button id="sa-refresh">Current item</button>
      <button id="sa-open">Open/search</button>
      <button id="sa-add">Add open product</button>
      <button id="sa-next" class="secondary">Next</button>
    </div>
  `;
  document.body.appendChild(root);
  document.getElementById("sa-refresh").addEventListener("click", loadCurrentItem);
  document.getElementById("sa-open").addEventListener("click", openCurrentItem);
  document.getElementById("sa-add").addEventListener("click", addOpenProduct);
  document.getElementById("sa-next").addEventListener("click", nextItem);
  return root;
}

function setStatus(message) {
  ensurePanel().querySelector("#sa-status").textContent = message;
}

function setItem(item) {
  currentItem = item;
  const element = ensurePanel().querySelector("#sa-item");
  if (!item) {
    element.textContent = "No active item. Select a shopping list in the app.";
    return;
  }

  element.textContent = `${item.name} (${item.quantity} ${item.unit || ""})`;
}

async function bridge(path, options = {}) {
  const response = await fetch(`${BRIDGE}/${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {})
    }
  });
  if (!response.ok) throw new Error(await response.text());
  return response.json();
}

async function loadCurrentItem() {
  try {
    setStatus("Loading current item...");
    const data = await bridge("current-item");
    setItem(data.item);
    setStatus(data.item ? "Ready." : "No current item.");
  } catch (error) {
    setStatus(`Desktop app bridge unavailable: ${error.message}`);
  }
}

function openCurrentItem() {
  if (!currentItem) {
    setStatus("No current item loaded.");
    return;
  }

  const url = currentItem.productUrl && currentItem.productUrl.length > 0
    ? currentItem.productUrl
    : `https://www.tesco.com/groceries/en-GB/search?query=${encodeURIComponent(currentItem.searchTerm || currentItem.name)}`;
  setStatus(`Opening ${currentItem.name}...`);
  window.location.href = url;
}

function productNameFromPage() {
  const h1 = document.querySelector("h1");
  if (h1 && h1.innerText.trim()) return h1.innerText.trim();
  const title = document.title.replace("| Tesco", "").replace("- Tesco Groceries", "").trim();
  return title || (currentItem ? currentItem.name : "Tesco product");
}

function findAddButton() {
  const buttons = [...document.querySelectorAll("button")];
  return buttons.find(button => {
    const text = `${button.innerText || ""} ${button.getAttribute("aria-label") || ""}`.toLowerCase();
    return text.includes("add") && !button.disabled && button.offsetParent !== null;
  });
}

async function addOpenProduct() {
  if (!currentItem) {
    setStatus("No current item loaded.");
    return;
  }

  if (!location.href.includes("/products/")) {
    setStatus("Open a Tesco product page first.");
    return;
  }

  const productName = productNameFromPage();
  if (!confirm(`Add this product for ${currentItem.name}?\n\n${productName}`)) {
    setStatus("Product not added.");
    return;
  }

  const addButton = findAddButton();
  if (!addButton) {
    setStatus("Could not find an Add button. Add it manually, then confirm in the app.");
    return;
  }

  addButton.click();
  await new Promise(resolve => setTimeout(resolve, 800));

  const saveAsPreferred = confirm(`Save this as preferred for ${currentItem.name}?`);
  const data = await bridge("item-added", {
    method: "POST",
    body: JSON.stringify({
      productName,
      productUrl: location.href,
      saveAsPreferred
    })
  });

  setItem(data.item);
  setStatus(data.item ? "Added. Next item loaded." : "Shopping list complete.");
}

async function nextItem() {
  try {
    const data = await bridge("next-item", { method: "POST" });
    setItem(data.item);
    setStatus(data.item ? "Next item loaded." : "Shopping list complete.");
  } catch (error) {
    setStatus(`Could not move next: ${error.message}`);
  }
}

ensurePanel();
loadCurrentItem();
