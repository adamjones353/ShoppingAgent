import { getAll, put } from "./db.js";

export async function ensureSeedData() {
  const meals = await getAll("meals");
  if (meals.length > 0) return;

  const now = new Date().toISOString();
  const seedMeals = [
    {
      name: "Chicken Fried Rice",
      description: "Fast pan rice with chicken, egg and vegetables.",
      prepEffort: "Low",
      cookingTimeMinutes: 25,
      portions: 4,
      tags: ["weekday", "quick", "leftovers"],
      source: "Preloaded",
      approved: true,
      ingredients: [
        { name: "chicken breast", quantity: 450, unit: "g", category: "Meat" },
        { name: "rice", quantity: 300, unit: "g", category: "Dry goods" },
        { name: "eggs", quantity: 2, unit: "each", category: "Dairy" },
        { name: "broccoli", quantity: 250, unit: "g", category: "Vegetables" }
      ],
      cookingSteps: ["Cook rice.", "Fry chicken.", "Add veg and egg.", "Combine."]
    },
    {
      name: "Bacon Tomato Pasta",
      description: "Simple pasta with smoky bacon and tomato sauce.",
      prepEffort: "Low",
      cookingTimeMinutes: 30,
      portions: 4,
      tags: ["weekday", "quick"],
      source: "Preloaded",
      approved: true,
      ingredients: [
        { name: "bacon", quantity: 200, unit: "g", category: "Meat" },
        { name: "pasta", quantity: 350, unit: "g", category: "Dry goods" },
        { name: "tinned tomatoes", quantity: 2, unit: "tin", category: "Tins" }
      ],
      cookingSteps: ["Boil pasta.", "Fry bacon.", "Simmer sauce.", "Combine."]
    }
  ];

  for (const meal of seedMeals) {
    await put("meals", { ...meal, createdAt: now, updatedAt: now });
  }

  await put("settings", {
    id: "app",
    openAiApiKey: "",
    openAiModel: "gpt-4.1-mini",
    dailyBudgetUsd: 1,
    monthlyBudgetUsd: 10,
    enableAi: false
  });
}
