import type { FoodItem, DiaryEntry, Recipe, PlannedMeal, ShoppingItem } from '../types/nutrition'

export const INITIAL_FOOD_ITEMS: FoodItem[] = [
  {
    id: 'food-1',
    name: 'Piept de pui la grătar',
    brand: 'Generic',
    calories: 165,
    proteins: 31.0,
    carbohydrates: 0.0,
    fats: 3.6,
    fiber: 0.0,
    category: 'Carne & Pește'
  },
  {
    id: 'food-2',
    name: 'Fulgi de ovăz integrali',
    brand: 'Sanovita',
    calories: 389,
    proteins: 16.9,
    carbohydrates: 66.3,
    fats: 6.9,
    fiber: 10.6,
    category: 'Cereale'
  },
  {
    id: 'food-3',
    name: 'Orez alb basmati fiert',
    brand: 'Generic',
    calories: 130,
    proteins: 2.7,
    carbohydrates: 28.2,
    fats: 0.3,
    fiber: 0.4,
    category: 'Cereale'
  },
  {
    id: 'food-4',
    name: 'Ou de găină întreg',
    brand: 'Generic',
    calories: 143,
    proteins: 12.6,
    carbohydrates: 0.7,
    fats: 9.5,
    fiber: 0.0,
    category: 'Lactate'
  },
  {
    id: 'food-5',
    name: 'Banană proaspătă',
    brand: 'Generic',
    calories: 89,
    proteins: 1.1,
    carbohydrates: 22.8,
    fats: 0.3,
    fiber: 2.6,
    category: 'Fructe & Legume'
  },
  {
    id: 'food-6',
    name: 'File de somon la cuptor',
    brand: 'Generic',
    calories: 208,
    proteins: 20.4,
    carbohydrates: 0.0,
    fats: 13.4,
    fiber: 0.0,
    category: 'Carne & Pește'
  },
  {
    id: 'food-7',
    name: 'Broccoli la abur',
    brand: 'Generic',
    calories: 34,
    proteins: 2.8,
    carbohydrates: 6.6,
    fats: 0.4,
    fiber: 2.6,
    category: 'Fructe & Legume'
  },
  {
    id: 'food-8',
    name: 'Iaurt grecesc 2%',
    brand: 'Olympus',
    calories: 73,
    proteins: 10.0,
    carbohydrates: 3.6,
    fats: 2.0,
    fiber: 0.0,
    category: 'Lactate'
  },
  {
    id: 'food-9',
    name: 'Migdale crude',
    brand: 'Generic',
    calories: 579,
    proteins: 21.2,
    carbohydrates: 21.6,
    fats: 49.9,
    fiber: 12.5,
    category: 'Băcănie'
  },
  {
    id: 'food-10',
    name: 'Ulei de măsline extravirgin',
    brand: 'Monini',
    calories: 884,
    proteins: 0.0,
    carbohydrates: 0.0,
    fats: 100.0,
    fiber: 0.0,
    category: 'Băcănie'
  },
  {
    id: 'food-11',
    name: 'Avocado copt',
    brand: 'Generic',
    calories: 160,
    proteins: 2.0,
    carbohydrates: 8.5,
    fats: 14.7,
    fiber: 6.7,
    category: 'Fructe & Legume'
  },
  {
    id: 'food-12',
    name: 'Brânză proaspătă de vaci (Cottage)',
    brand: 'Făgăraș',
    calories: 98,
    proteins: 11.0,
    carbohydrates: 3.4,
    fats: 4.3,
    fiber: 0.0,
    category: 'Lactate'
  }
]

export const INITIAL_DIARY_ENTRIES: DiaryEntry[] = [
  {
    id: 'entry-1',
    mealType: 'breakfast',
    foodItem: INITIAL_FOOD_ITEMS[1], // Ovaz
    quantityGrams: 80,
    calculatedCalories: 311,
    calculatedProteins: 13.5,
    calculatedCarbs: 53.0,
    calculatedFats: 5.5
  },
  {
    id: 'entry-2',
    mealType: 'breakfast',
    foodItem: INITIAL_FOOD_ITEMS[4], // Banana
    quantityGrams: 120,
    calculatedCalories: 107,
    calculatedProteins: 1.3,
    calculatedCarbs: 27.4,
    calculatedFats: 0.4
  },
  {
    id: 'entry-3',
    mealType: 'lunch',
    foodItem: INITIAL_FOOD_ITEMS[0], // Piept pui
    quantityGrams: 200,
    calculatedCalories: 330,
    calculatedProteins: 62.0,
    calculatedCarbs: 0.0,
    calculatedFats: 7.2
  },
  {
    id: 'entry-4',
    mealType: 'lunch',
    foodItem: INITIAL_FOOD_ITEMS[2], // Orez
    quantityGrams: 180,
    calculatedCalories: 234,
    calculatedProteins: 4.9,
    calculatedCarbs: 50.8,
    calculatedFats: 0.5
  },
  {
    id: 'entry-5',
    mealType: 'dinner',
    foodItem: INITIAL_FOOD_ITEMS[5], // Somon
    quantityGrams: 150,
    calculatedCalories: 312,
    calculatedProteins: 30.6,
    calculatedCarbs: 0.0,
    calculatedFats: 20.1
  },
  {
    id: 'entry-6',
    mealType: 'dinner',
    foodItem: INITIAL_FOOD_ITEMS[6], // Broccoli
    quantityGrams: 150,
    calculatedCalories: 51,
    calculatedProteins: 4.2,
    calculatedCarbs: 9.9,
    calculatedFats: 0.6
  }
]

export const INITIAL_RECIPES: Recipe[] = [
  {
    id: 'rec-1',
    name: 'Bol cu Somon la Cuptor și Orez Basmati',
    description: 'Masă echilibrată bogată în Omega-3 și proteine calitative.',
    servingsCount: 2,
    prepTimeMinutes: 25,
    ingredients: [
      { foodItem: INITIAL_FOOD_ITEMS[5], quantityGrams: 300 }, // 300g somon
      { foodItem: INITIAL_FOOD_ITEMS[2], quantityGrams: 250 }, // 250g orez
      { foodItem: INITIAL_FOOD_ITEMS[6], quantityGrams: 200 }, // 200g broccoli
      { foodItem: INITIAL_FOOD_ITEMS[9], quantityGrams: 15 }   // 15g ulei masline
    ]
  },
  {
    id: 'rec-2',
    name: 'Terci Proteic de Ovăz cu Banană și Migdale',
    description: 'Mic dejun energetic cu eliberare lentă a carbohidraților.',
    servingsCount: 1,
    prepTimeMinutes: 10,
    ingredients: [
      { foodItem: INITIAL_FOOD_ITEMS[1], quantityGrams: 80 },  // 80g ovaz
      { foodItem: INITIAL_FOOD_ITEMS[7], quantityGrams: 150 }, // 150g iaurt grecesc
      { foodItem: INITIAL_FOOD_ITEMS[4], quantityGrams: 100 }, // 100g banana
      { foodItem: INITIAL_FOOD_ITEMS[8], quantityGrams: 20 }   // 20g migdale
    ]
  },
  {
    id: 'rec-3',
    name: 'Omletă Nutritivă cu Avocado și Brânză Cottage',
    description: 'Sățioasă și ușor de preparat pentru diminețile active.',
    servingsCount: 1,
    prepTimeMinutes: 12,
    ingredients: [
      { foodItem: INITIAL_FOOD_ITEMS[3], quantityGrams: 120 }, // 2 oua (~120g)
      { foodItem: INITIAL_FOOD_ITEMS[11], quantityGrams: 100 }, // 100g cottage
      { foodItem: INITIAL_FOOD_ITEMS[10], quantityGrams: 50 }   // 50g avocado
    ]
  }
]

export const INITIAL_PLANNED_MEALS: PlannedMeal[] = [
  // Luni
  {
    id: 'plan-1',
    dayOfWeek: 'Luni',
    mealType: 'breakfast',
    name: 'Terci de Ovăz cu Iaurt și Banană',
    calories: 450,
    proteins: 24,
    carbs: 65,
    fats: 11,
    ingredientsSummary: 'Ovăz 80g, Iaurt 150g, Banană 100g'
  },
  {
    id: 'plan-2',
    dayOfWeek: 'Luni',
    mealType: 'lunch',
    name: 'Piept de Pui cu Orez și Broccoli',
    calories: 620,
    proteins: 65,
    carbs: 55,
    fats: 9,
    ingredientsSummary: 'Pui 200g, Orez 180g, Broccoli 150g'
  },
  {
    id: 'plan-3',
    dayOfWeek: 'Luni',
    mealType: 'dinner',
    name: 'Somon la Cuptor cu Salată Verde',
    calories: 480,
    proteins: 35,
    carbs: 10,
    fats: 24,
    ingredientsSummary: 'Somon 180g, Ulei măsline 10g'
  },
  // Marți
  {
    id: 'plan-4',
    dayOfWeek: 'Marți',
    mealType: 'breakfast',
    name: 'Omletă cu Brânză Cottage și Avocado',
    calories: 420,
    proteins: 30,
    carbs: 8,
    fats: 28,
    ingredientsSummary: 'Ouă 2 buc, Cottage 100g, Avocado 50g'
  },
  {
    id: 'plan-5',
    dayOfWeek: 'Marți',
    mealType: 'lunch',
    name: 'Bol cu Somon și Quinoa',
    calories: 590,
    proteins: 40,
    carbs: 48,
    fats: 20,
    ingredientsSummary: 'Somon 160g, Orez 150g, Legume'
  },
  {
    id: 'plan-6',
    dayOfWeek: 'Marți',
    mealType: 'dinner',
    name: 'Piept de Curcan la Grătar cu Fasole Verde',
    calories: 410,
    proteins: 48,
    carbs: 15,
    fats: 7,
    ingredientsSummary: 'Curcan 180g, Fasole verde 200g'
  },
  // Miercuri
  {
    id: 'plan-7',
    dayOfWeek: 'Miercuri',
    mealType: 'breakfast',
    name: 'Pancake Proteic cu Afine și Banană',
    calories: 460,
    proteins: 32,
    carbs: 60,
    fats: 8,
    ingredientsSummary: 'Ovăz 60g, Albuș ou 100g, Banană 1 buc'
  },
  {
    id: 'plan-8',
    dayOfWeek: 'Miercuri',
    mealType: 'lunch',
    name: 'Vită Fragedă la Tigaie cu Cartof Copt',
    calories: 680,
    proteins: 55,
    carbs: 60,
    fats: 16,
    ingredientsSummary: 'Mușchi vită 180g, Cartof 250g'
  },
  {
    id: 'plan-9',
    dayOfWeek: 'Miercuri',
    mealType: 'dinner',
    name: 'Salată Mare cu Ouă Fierte și Ton',
    calories: 390,
    proteins: 38,
    carbs: 12,
    fats: 18,
    ingredientsSummary: 'Ton 120g, Ouă 2 buc, Măsline 30g'
  }
]

export const INITIAL_SHOPPING_ITEMS: ShoppingItem[] = [
  { id: 'shop-1', name: 'Piept de pui dezosat', quantity: '800 g', category: 'Carne & Pește', isPurchased: true },
  { id: 'shop-2', name: 'File de somon proaspăt', quantity: '500 g', category: 'Carne & Pește', isPurchased: false },
  { id: 'shop-3', name: 'Fulgi de ovăz fini', quantity: '1 kg', category: 'Cereale', isPurchased: true },
  { id: 'shop-4', name: 'Orez basmati calitatea I', quantity: '1 kg', category: 'Cereale', isPurchased: false },
  { id: 'shop-5', name: 'Iaurt grecesc 2%', quantity: '4 buc x 150g', category: 'Lactate', isPurchased: false },
  { id: 'shop-6', name: 'Ouă proaspete mărimea L', quantity: '10 buc', category: 'Lactate', isPurchased: true },
  { id: 'shop-7', name: 'Banane coapte', quantity: '1 kg', category: 'Fructe & Legume', isPurchased: false },
  { id: 'shop-8', name: 'Broccoli proaspăt', quantity: '500 g', category: 'Fructe & Legume', isPurchased: false },
  { id: 'shop-9', name: 'Avocado Hass', quantity: '3 buc', category: 'Fructe & Legume', isPurchased: false },
  { id: 'shop-10', name: 'Migdale crude', quantity: '200 g', category: 'Băcănie', isPurchased: false },
  { id: 'shop-11', name: 'Ulei de măsline extravirgin', quantity: '750 ml', category: 'Băcănie', isPurchased: true }
]
