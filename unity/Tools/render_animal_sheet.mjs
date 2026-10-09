import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const sourcePath = String.raw`E:\unity\project\Unity_AnimalRace\Assets\Art\Sources\animal_run_sheet.svg`;
const outputPath = String.raw`E:\unity\project\Unity_AnimalRace\Assets\Art\animal_run_sheet.png`;

const browser = await chromium.launch({
  headless: true,
  executablePath: String.raw`C:\Program Files\Google\Chrome\Application\chrome.exe`,
});
const page = await browser.newPage({ viewport: { width: 1024, height: 1152 } });
await page.goto(pathToFileURL(sourcePath).href);
await page.screenshot({
  path: outputPath,
  omitBackground: true,
  clip: { x: 0, y: 0, width: 1024, height: 1152 },
});
await browser.close();
