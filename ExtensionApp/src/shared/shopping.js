import { getAll, put } from "./db.js";

export function combineIngredients(meals) {
  const grouped = new Map();
  for (const meal of meals) {
    for (const ingredient of meal.ingredients || []) {
      const key = `${ingredient.name.toLowerCase()}|${ingredient.unit || ""}`;
      const existing = grouped.get(key) || {
        name: ingredient.name,
        category: ingredient.category || "Other",
        quantity: 0,
        unit: ingredient.unit || "",
        checkedOff: false,
        alreadyOwned: false,
        source: "Meal"
      };
      existing.quantity += Number(ingredient.quantity || 0);
      grouped.set(key, existing);
    }
  }
  return [...grouped.values()].sort((a, b) => `${a.category}${a.name}`.localeCompare(`${b.category}${b.name}`));
}

export async function createShoppingListFromMealIds(mealIds) {
  const meals = await getAll("meals");
  const selected = meals.filter(meal => mealIds.includes(meal.id));
  const list = {
    name: `Shopping list ${new Date().toLocaleDateString()}`,
    createdAt: new Date().toISOString(),
    items: combineIngredients(selected)
  };
  await put("shoppingLists", list);
  return list;
}
