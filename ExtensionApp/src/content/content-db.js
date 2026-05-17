window.ShoppingAgentDb = (() => {
  const STORES = ["settings", "meals", "ingredients", "mealHistory", "mealPlans", "shoppingLists", "productMappings", "aiLogs"];

  async function getAll(storeName) {
    assertStore(storeName);
    const data = await storageGet(storeName);
    return data[storeName] || [];
  }

  async function getOne(storeName, id) {
    const items = await getAll(storeName);
    return items.find(item => item.id === id);
  }

  async function put(storeName, value) {
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

    await storageSet({ [storeName]: items });
    return copy.id;
  }

  async function nextId(storeName) {
    const key = `${storeName}NextId`;
    const data = await storageGet(key);
    const next = Number(data[key] || 1);
    await storageSet({ [key]: next + 1 });
    return next;
  }

  async function storageGet(key) {
    if (!isExtensionContextLive()) return {};
    try {
      return await chrome.storage.local.get(key);
    } catch (error) {
      if (isContextInvalidatedError(error)) return {};
      throw error;
    }
  }

  async function storageSet(value) {
    if (!isExtensionContextLive()) return;
    try {
      await chrome.storage.local.set(value);
    } catch (error) {
      if (!isContextInvalidatedError(error)) throw error;
    }
  }

  function isExtensionContextLive() {
    return Boolean(globalThis.chrome?.runtime?.id && chrome.storage?.local);
  }

  function isContextInvalidatedError(error) {
    return String(error?.message || error).includes("Extension context invalidated");
  }

  function assertStore(storeName) {
    if (!STORES.includes(storeName)) {
      throw new Error(`Unknown store: ${storeName}`);
    }
  }

  return { getAll, getOne, put };
})();
