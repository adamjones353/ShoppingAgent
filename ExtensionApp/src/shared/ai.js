import { getAll, getOne, put } from "./db.js";

const MEAL_SCHEMA = {
  type: "object",
  additionalProperties: false,
  required: ["meals"],
  properties: {
    meals: {
      type: "array",
      minItems: 1,
      maxItems: 10,
      items: {
        type: "object",
        additionalProperties: false,
        required: ["name", "description", "prepEffort", "cookingTimeMinutes", "portions", "tags", "ingredients", "cookingSteps"],
        properties: {
          name: { type: "string" },
          description: { type: "string" },
          prepEffort: { type: "string", enum: ["Low", "Medium", "High"] },
          cookingTimeMinutes: { type: "integer", minimum: 1, maximum: 240 },
          portions: { type: "integer", minimum: 1, maximum: 12 },
          tags: { type: "array", items: { type: "string" } },
          ingredients: {
            type: "array",
            items: {
              type: "object",
              additionalProperties: false,
              required: ["name", "quantity", "unit", "category"],
              properties: {
                name: { type: "string" },
                quantity: { type: "number" },
                unit: { type: "string" },
                category: { type: "string" }
              }
            }
          },
          cookingSteps: { type: "array", items: { type: "string" } }
        }
      }
    }
  }
};

export async function suggestMealsWithAi(prompt) {
  const settings = await getOne("settings", "app");
  if (!settings?.enableAi) throw new Error("Enable AI in Settings first.");
  if (!settings?.openAiApiKey) throw new Error("Add your OpenAI API key in Settings first.");

  const recentMealNames = (await getAll("mealHistory"))
    .slice(-20)
    .map(entry => entry.mealName)
    .filter(Boolean);
  const existingMealNames = (await getAll("meals"))
    .map(meal => meal.name)
    .slice(0, 80);

  const startedAt = new Date().toISOString();
  const response = await fetch("https://api.openai.com/v1/responses", {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${settings.openAiApiKey}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      model: settings.openAiModel || "gpt-4.1-mini",
      input: [
        {
          role: "system",
          content: [
            {
              type: "input_text",
              text: "You suggest practical household meals. Return only meals that can be saved as structured recipe data. Do not include unsafe food advice."
            }
          ]
        },
        {
          role: "user",
          content: [
            {
              type: "input_text",
              text: JSON.stringify({
                request: prompt,
                avoidRecentlyEaten: recentMealNames,
                existingMeals: existingMealNames
              })
            }
          ]
        }
      ],
      text: {
        format: {
          type: "json_schema",
          name: "meal_suggestions",
          strict: true,
          schema: MEAL_SCHEMA
        }
      },
      max_output_tokens: Number(settings.maxOutputTokens || 3500)
    })
  });

  const body = await response.json();
  const usage = body.usage || {};
  await put("aiLogs", {
    purpose: "MealSuggestions",
    prompt,
    model: settings.openAiModel || "gpt-4.1-mini",
    inputTokens: usage.input_tokens || 0,
    outputTokens: usage.output_tokens || 0,
    succeeded: response.ok,
    error: response.ok ? "" : body.error?.message || `HTTP ${response.status}`,
    createdAt: startedAt
  });

  if (!response.ok) {
    throw new Error(body.error?.message || `OpenAI request failed with HTTP ${response.status}.`);
  }

  const parsed = JSON.parse(extractOutputText(body));
  return validateMealSuggestions(parsed.meals || []);
}

function extractOutputText(body) {
  if (body.output_text) return body.output_text;
  for (const output of body.output || []) {
    for (const content of output.content || []) {
      if (content.type === "output_text" && content.text) return content.text;
      if (content.text) return content.text;
    }
  }
  throw new Error("OpenAI returned no structured meal text.");
}

function validateMealSuggestions(meals) {
  return meals
    .filter(meal => meal?.name && Array.isArray(meal.ingredients))
    .map(meal => ({
      name: String(meal.name).trim(),
      description: String(meal.description || "").trim(),
      prepEffort: ["Low", "Medium", "High"].includes(meal.prepEffort) ? meal.prepEffort : "Medium",
      cookingTimeMinutes: clampInt(meal.cookingTimeMinutes, 1, 240, 30),
      portions: clampInt(meal.portions, 1, 12, 4),
      tags: Array.isArray(meal.tags) ? meal.tags.map(String).filter(Boolean) : [],
      ingredients: meal.ingredients.map(ingredient => ({
        name: String(ingredient.name || "").trim(),
        quantity: Number(ingredient.quantity || 1),
        unit: String(ingredient.unit || "").trim(),
        category: String(ingredient.category || "Other").trim() || "Other"
      })).filter(ingredient => ingredient.name),
      cookingSteps: Array.isArray(meal.cookingSteps) ? meal.cookingSteps.map(String).filter(Boolean) : []
    }))
    .filter(meal => meal.name && meal.ingredients.length);
}

function clampInt(value, min, max, fallback) {
  const number = Number.parseInt(value, 10);
  if (!Number.isFinite(number)) return fallback;
  return Math.min(max, Math.max(min, number));
}
