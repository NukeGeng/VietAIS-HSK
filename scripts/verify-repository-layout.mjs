import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const failures = [];

function requireDirectory(relativePath) {
  const absolutePath = path.join(repositoryRoot, relativePath);
  if (!fs.existsSync(absolutePath) || !fs.statSync(absolutePath).isDirectory()) {
    failures.push(`missing directory: ${relativePath}`);
  }
}

function requireFile(relativePath) {
  const absolutePath = path.join(repositoryRoot, relativePath);
  if (!fs.existsSync(absolutePath) || !fs.statSync(absolutePath).isFile()) {
    failures.push(`missing file: ${relativePath}`);
  }
}

function walk(directory) {
  if (!fs.existsSync(directory)) return [];

  const files = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...walk(entryPath));
    } else if (entry.isFile()) {
      files.push(entryPath);
    }
  }
  return files;
}

requireDirectory("frontend");
requireDirectory("frontend/src");
requireDirectory("backend");
requireDirectory("backend/VietAisHsk.Api");
requireFile("frontend/package.json");
requireFile("backend/VietAisHsk.Api/VietAisHsk.Api.csproj");
requireFile("VietAisHsk.slnx");

if (fs.existsSync(path.join(repositoryRoot, "src"))) {
  failures.push("root src/ exists: backend code must remain under backend/");
}

const forbiddenProductionReferences = [
  "design-template",
  "localhost:8766",
  "serve-preview.sh",
];
const productionFiles = walk(path.join(repositoryRoot, "frontend", "src"))
  .filter((filePath) => /\.(css|scss|ts|tsx|vue)$/.test(filePath));

for (const filePath of productionFiles) {
  const source = fs.readFileSync(filePath, "utf8");
  for (const forbiddenReference of forbiddenProductionReferences) {
    if (source.includes(forbiddenReference)) {
      const relativePath = path.relative(repositoryRoot, filePath);
      failures.push(`production frontend references ${forbiddenReference}: ${relativePath}`);
    }
  }
}

if (failures.length > 0) {
  console.error("Repository layout check failed:");
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log("Repository layout check passed:");
console.log("- production UI: frontend/");
console.log("- backend processes: backend/");
console.log("- design-template is reference-only and is not imported into frontend/");
