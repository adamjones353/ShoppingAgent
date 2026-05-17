const STORES = [
  "settings",
  "meals",
  "ingredients",
  "mealHistory",
  "mealPlans",
  "shoppingLists",
  "productMappings",
  "aiLogs"
];

export async function getAll(storeName) {
  assertStore(storeName);
  await migrateOldIndexedDbOnce();
  const data = await chrome.storage.local.get(storeName);
  return data[storeName] || [];
}

export async function getOne(storeName, id) {
  const items = await getAll(storeName);
  return items.find(item => item.id === id);
}

export async function put(storeName, value) {
  assertStore(storeName);
  const items = await getAll(storeName);
  const copy = { ...value };

  if (copy.id === undefined || copy.id === null || copy.id === "") {
    copy.id = await nextId(storeName);
  }

  const index = items.findIndex(item => item.id === copy.id);
  if (index >= 0) {
    items[index] = copy;
  } else {
    items.push(copy);
  }

  await chrome.storage.local.set({ [storeName]: items });
  return copy.id;
}

export async function remove(storeName, id) {
  assertStore(storeName);
  const items = (await getAll(storeName)).filter(item => item.id !== id);
  await chrome.storage.local.set({ [storeName]: items });
}

export async function clear(storeName) {
  assertStore(storeName);
  await chrome.storage.local.set({ [storeName]: [] });
}

async function nextId(storeName) {
  const key = `${storeName}NextId`;
  const data = await chrome.storage.local.get(key);
  const next = Number(data[key] || 1);
  await chrome.storage.local.set({ [key]: next + 1 });
  return next;
}

function assertStore(storeName) {
  if (!STORES.includes(storeName)) {
    throw new Error(`Unknown store: ${storeName}`);
  }
}

async function migrateOldIndexedDbOnce() {
  const flag = "migratedFromIndexedDbExtension";
  const migration = await chrome.storage.local.get(flag);
  if (migration[flag]) return;

  try {
    const db = await openOldIndexedDb();
    for (const store of STORES) {
      if (!db.objectStoreNames.contains(store)) continue;
      const existing = (await chrome.storage.local.get(store))[store] || [];
      if (existing.length > 0) continue;
      const oldItems = await readOldStore(db, store);
      if (oldItems.length > 0) {
        await chrome.storage.local.set({ [store]: oldItems });
      }
    }
    db.close();
  } catch {
    // No old database to migrate.
  }

  await chrome.storage.local.set({ [flag]: true });
}

function openOldIndexedDb() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open("shopping-agent-extension");
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
    request.onblocked = () => reject(new Error("Old IndexedDB migration blocked."));
  });
}

function readOldStore(db, storeName) {
  return new Promise((resolve, reject) => {
    const transaction = db.transaction(storeName, "readonly");
    const request = transaction.objectStore(storeName).getAll();
    request.onsuccess = () => resolve(request.result || []);
    request.onerror = () => reject(request.error);
  });
}
