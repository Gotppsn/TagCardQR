const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// Check if input file exists
const inputFile = path.resolve('./wwwroot/css/tailwind-input.css');
const outputFile = path.resolve('./wwwroot/css/tailwind.css');

if (!fs.existsSync(inputFile)) {
  console.error(`Input file not found: ${inputFile}`);
  process.exit(1);
}

// Ensure output directory exists
const outputDir = path.dirname(outputFile);
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

try {
  console.log('Building Tailwind CSS...');
  
  // Run the tailwind CLI directly using npx
  execSync(`npx tailwindcss -i "${inputFile}" -o "${outputFile}"`, {
    stdio: 'inherit'
  });
  
  console.log('✅ CSS file generated successfully!');
} catch (error) {
  console.error('❌ Error generating CSS:', error.message);
  process.exit(1);
}