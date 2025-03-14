// build-tailwind.js
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// Input and output file paths
const inputFile = './wwwroot/css/tailwind-input.css';
const outputFile = './wwwroot/css/tailwind.css';

// Content from tailwind-input.css
const inputContent = fs.readFileSync(inputFile, 'utf8');

// Create a temporary config file
const configContent = `
module.exports = {
  content: ["./Views/**/*.cshtml", "./wwwroot/js/**/*.js"],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#f0f9ff',
          100: '#e0f2fe',
          200: '#bae6fd',
          300: '#7dd3fc',
          400: '#38bdf8',
          500: '#0ea5e9',
          600: '#0284c7',
          700: '#0369a1',
          800: '#075985',
          900: '#0c4a6e',
        },
      },
    },
  },
  plugins: [],
}`;

// Write temporary config
fs.writeFileSync('temp-tailwind.config.js', configContent);

try {
  // Static CSS that will be included
  const staticCSS = fs.readFileSync('./wwwroot/css/tailwind-input.css', 'utf8');
  
  // Basic Tailwind CSS
  const baseTailwindCSS = `
/* Tailwind Base */
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Custom components from tailwind-input.css */
${staticCSS}
  `;
  
  // Write to output file
  fs.writeFileSync(outputFile, baseTailwindCSS);
  
  console.log('✅ CSS file generated successfully!');
} catch (error) {
  console.error('❌ Error generating CSS:', error);
} finally {
  // Clean up
  if (fs.existsSync('temp-tailwind.config.js')) {
    fs.unlinkSync('temp-tailwind.config.js');
  }
}