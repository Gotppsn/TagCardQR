// simplified-build.js
const { spawnSync } = require('child_process');
const path = require('path');

// Get the path to node_modules/.bin directory
const binPath = path.resolve('./node_modules/.bin');
const tailwindBin = path.join(binPath, 'tailwindcss');

console.log('Building Tailwind CSS...');
console.log(`Using tailwindcss at: ${tailwindBin}`);

// Execute tailwindcss directly from node_modules/.bin
const result = spawnSync(tailwindBin, [
  '-i', './wwwroot/css/tailwind-input.css',
  '-o', './wwwroot/css/tailwind.css'
], { 
  stdio: 'inherit',
  shell: true // Important for Windows
});

if (result.error) {
  console.error('Failed to execute tailwindcss:', result.error);
  process.exit(1);
}

if (result.status !== 0) {
  console.error('Tailwind CSS build failed with status:', result.status);
  process.exit(1);
}

console.log('✅ Tailwind CSS built successfully!');