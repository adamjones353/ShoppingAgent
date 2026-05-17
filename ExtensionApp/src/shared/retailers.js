(function () {
  const RETAILERS = [
    {
      id: "tesco",
      name: "Tesco",
      domains: ["tesco.com", "www.tesco.com"],
      searchUrl: "https://www.tesco.com/groceries/en-GB/search?query={query}",
      productPathHints: ["/products/"],
      titleSuffixes: ["| Tesco", "- Tesco Groceries"]
    },
    {
      id: "sainsburys",
      name: "Sainsbury's",
      domains: ["sainsburys.co.uk", "www.sainsburys.co.uk"],
      searchUrl: "https://www.sainsburys.co.uk/gol-ui/SearchResults/{query}",
      productPathHints: ["/product/"],
      titleSuffixes: ["| Sainsbury's", "| Sainsbury's Groceries"]
    },
    {
      id: "asda",
      name: "Asda",
      domains: ["asda.com", "groceries.asda.com", "www.asda.com"],
      searchUrl: "https://groceries.asda.com/search/{query}",
      productPathHints: ["/product/"],
      titleSuffixes: ["| Asda", "| ASDA Groceries"]
    },
    {
      id: "morrisons",
      name: "Morrisons",
      domains: ["morrisons.com", "groceries.morrisons.com", "www.morrisons.com"],
      searchUrl: "https://groceries.morrisons.com/search?entry={query}",
      productPathHints: ["/products/"],
      titleSuffixes: ["| Morrisons", "| Morrisons Groceries"]
    },
    {
      id: "waitrose",
      name: "Waitrose",
      domains: ["waitrose.com", "www.waitrose.com"],
      searchUrl: "https://www.waitrose.com/ecom/shop/search?searchTerm={query}",
      productPathHints: ["/products/"],
      titleSuffixes: ["| Waitrose & Partners", "| Waitrose"]
    },
    {
      id: "ocado",
      name: "Ocado",
      domains: ["ocado.com", "www.ocado.com"],
      searchUrl: "https://www.ocado.com/search?entry={query}",
      productPathHints: ["/products/"],
      titleSuffixes: ["| Ocado"]
    },
    {
      id: "iceland",
      name: "Iceland",
      domains: ["iceland.co.uk", "www.iceland.co.uk"],
      searchUrl: "https://www.iceland.co.uk/search?q={query}",
      productPathHints: ["/p/"],
      titleSuffixes: ["| Iceland Foods", "| Iceland"]
    },
    {
      id: "coop",
      name: "Co-op",
      domains: ["coop.co.uk", "www.coop.co.uk"],
      searchUrl: "https://www.coop.co.uk/products/search?query={query}",
      productPathHints: ["/products/"],
      titleSuffixes: ["| Co-op"]
    },
    {
      id: "amazon",
      name: "Amazon Fresh",
      domains: ["amazon.co.uk", "www.amazon.co.uk"],
      searchUrl: "https://www.amazon.co.uk/s?k={query}&i=amazonfresh",
      productPathHints: ["/dp/", "/gp/product/"],
      titleSuffixes: ["| Amazon.co.uk", ": Amazon.co.uk"]
    }
  ];

  function retailerForUrl(url) {
    const parsed = parseUrl(url);
    if (!parsed) return null;
    const host = parsed.hostname.replace(/^www\./, "");
    return RETAILERS.find(retailer =>
      retailer.domains.some(domain => host === domain.replace(/^www\./, "") || host.endsWith(`.${domain.replace(/^www\./, "")}`))
    ) || null;
  }

  function defaultRetailer() {
    return RETAILERS[0];
  }

  function retailerById(id) {
    return RETAILERS.find(retailer => retailer.id === id) || defaultRetailer();
  }

  function isKnownGroceryUrl(url) {
    return Boolean(retailerForUrl(url));
  }

  function isProductUrl(url, retailer = retailerForUrl(url)) {
    const parsed = parseUrl(url);
    if (!parsed || !retailer) return false;
    const path = parsed.pathname.toLowerCase();
    return retailer.productPathHints.some(hint => path.includes(hint.toLowerCase()));
  }

  function productNameFromDocument(doc, retailer) {
    const candidates = [
      doc.querySelector("h1"),
      doc.querySelector("[data-testid*='product-title' i]"),
      doc.querySelector("[class*='product-title' i]"),
      doc.querySelector("[class*='product_name' i]"),
      doc.querySelector("[itemprop='name']")
    ];
    const element = candidates.find(node => node?.innerText?.trim() || node?.content?.trim());
    const raw = element?.innerText?.trim() || element?.content?.trim() || doc.title || "";
    return cleanProductName(raw, retailer);
  }

  function cleanProductName(name, retailer) {
    let result = String(name || "").trim();
    for (const suffix of retailer?.titleSuffixes || []) {
      result = result.replace(suffix, "").trim();
    }
    return result;
  }

  function searchUrlForItem(item, mapping = null, retailerId = null) {
    if (mapping?.productUrl) return mapping.productUrl;
    const retailer = retailerById(retailerId || mapping?.retailerId || mapping?.supermarketId || mapping?.supermarketName?.toLowerCase());
    const query = encodeURIComponent(mapping?.searchTerm || item?.name || "");
    return retailer.searchUrl.replace("{query}", query);
  }

  function urlOriginAndPath(url) {
    const parsed = parseUrl(url);
    return parsed ? `${parsed.origin}${parsed.pathname}` : url;
  }

  function parseUrl(url) {
    try {
      return new URL(url);
    } catch {
      return null;
    }
  }

  window.ShoppingAgentRetailers = {
    RETAILERS,
    defaultRetailer,
    retailerById,
    retailerForUrl,
    isKnownGroceryUrl,
    isProductUrl,
    productNameFromDocument,
    searchUrlForItem,
    urlOriginAndPath
  };
})();
