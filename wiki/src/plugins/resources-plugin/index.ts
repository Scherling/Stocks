import path from 'path';
import fs from 'fs';
import type {LoadContext, Plugin} from '@docusaurus/types';
import sharp from 'sharp';

type Resource = {
  id: string;
  Name: string;
  Category: string;
  Stage: string;
  Description?: string;
};

type RecipeSlot = {id: string; count: number};
type Recipe = {id: string; Method?: string; input: RecipeSlot[]; output: RecipeSlot[]};

type PluginContent = {
  resources: Resource[];
  recipesByOutput: Record<string, Recipe[]>;
  recipesByInput: Record<string, Recipe[]>;
  availableImages: string[]; // resource ids that have a dedicated image
};

function readJson<T>(filePath: string): T {
  const raw = fs.readFileSync(filePath, 'utf-8');
  // Strip single-line // comments (used as file headers in the data files)
  const cleaned = raw.replace(/^\s*\/\/.*$/gm, '');
  return JSON.parse(cleaned) as T;
}

async function syncImages(assetsDir: string, destDir: string): Promise<string[]> {
  const thumbDir = path.join(destDir, 'thumb');
  fs.mkdirSync(destDir, {recursive: true});
  fs.mkdirSync(thumbDir, {recursive: true});

  const pngs = fs.readdirSync(assetsDir).filter((f) => f.endsWith('.png'));
  await Promise.all(pngs.map(async (file) => {
    const src = path.join(assetsDir, file);
    // Hero: 256×256 (2× for HiDPI, displayed at 128×128)
    await sharp(src).resize(256, 256, {fit: 'contain', background: {r: 0, g: 0, b: 0, alpha: 0}}).toFile(path.join(destDir, file));
    // Icon: 64×64 (2× for HiDPI, displayed at 32×32)
    await sharp(src).resize(64, 64, {fit: 'contain', background: {r: 0, g: 0, b: 0, alpha: 0}}).toFile(path.join(thumbDir, file));
  }));

  // Return IDs (filenames without extension) that have a dedicated image,
  // excluding 'generic' which is the fallback
  return pngs.filter((f) => f !== 'generic.png').map((f) => f.replace(/\.png$/, ''));
}

export default function resourcesPlugin(context: LoadContext, _options?: unknown): Plugin<PluginContent> {
  return {
    name: 'resources-plugin',

    async loadContent(): Promise<PluginContent> {
      const dataDir = path.resolve(context.siteDir, '..', 'Data');
      const resources = readJson<Resource[]>(path.join(dataDir, 'Resources.json'));
      const recipes = readJson<Recipe[]>(path.join(dataDir, 'Recipes.json'));

      // Copy images from Data/assets → wiki/static/img/resources so Docusaurus serves them
      const assetsDir = path.join(dataDir, 'assets');
      const destDir = path.join(context.siteDir, 'static', 'img', 'resources');
      const availableImages = await syncImages(assetsDir, destDir);

      const recipesByOutput: Record<string, Recipe[]> = {};
      const recipesByInput: Record<string, Recipe[]> = {};

      for (const recipe of recipes) {
        for (const slot of recipe.output) {
          (recipesByOutput[slot.id] ??= []).push(recipe);
        }
        for (const slot of recipe.input) {
          (recipesByInput[slot.id] ??= []).push(recipe);
        }
      }

      return {resources, recipesByOutput, recipesByInput, availableImages};
    },

    async contentLoaded({content, actions}): Promise<void> {
      const {resources, recipesByOutput, recipesByInput, availableImages} = content;
      const {addRoute, createData} = actions;

      const imageSet = new Set(availableImages);

      // Lightweight map: id → {id, Name, imageFile} for resolving names/icons in recipe rows
      const resourceMap: Record<string, {id: string; Name: string; imageFile: string}> = {};
      for (const r of resources) {
        resourceMap[r.id] = {id: r.id, Name: r.Name, imageFile: imageSet.has(r.id) ? `${r.id}.png` : 'generic.png'};
      }

      // Index page
      const allResourcesPath = await createData(
        'resources-all.json',
        JSON.stringify(resources),
      );
      addRoute({
        path: '/resources',
        component: '@site/src/components/ResourceIndex',
        modules: {resources: allResourcesPath},
        exact: true,
      });

      // Per-resource pages
      for (const resource of resources) {
        const producedBy = recipesByOutput[resource.id] ?? [];
        const usedIn = recipesByInput[resource.id] ?? [];
        const imageFile = imageSet.has(resource.id) ? `${resource.id}.png` : 'generic.png';
        const pageData = {resource, producedBy, usedIn, resourceMap, imageFile};
        const dataPath = await createData(
          `resource-${resource.id}.json`,
          JSON.stringify(pageData),
        );
        addRoute({
          path: `/resources/${resource.id}`,
          component: '@site/src/components/ResourcePage',
          modules: {pageData: dataPath},
          exact: true,
        });
      }
    },
  };
}
