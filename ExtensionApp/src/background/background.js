chrome.runtime.onInstalled.addListener(() => {
  chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: true });
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message?.type === "OPEN_SIDE_PANEL" && sender.tab?.windowId) {
    openSidePanelOrTab(sender.tab.windowId)
      .then(() => sendResponse({ ok: true }))
      .catch(error => sendResponse({ ok: false, error: error.message }));
    return true;
  }
  return true;
});

async function openSidePanelOrTab(windowId) {
  try {
    await chrome.sidePanel.open({ windowId });
  } catch {
    await chrome.tabs.create({ url: chrome.runtime.getURL("src/sidepanel/index.html") });
  }
}
