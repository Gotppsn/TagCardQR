const { execSync } = require('child_process');

console.log('Building Tailwind CSS...');

try {
  // Run the tailwind CLI directly 
  execSync('npx tailwindcss -i ./wwwroot/css/tailwind-input.css -o ./wwwroot/css/tailwind.css', {
    stdio: 'inherit'
  });
  
  console.log('✅ Tailwind CSS built successfully!');
} catch (error) {
  console.error('❌ Error generating CSS:', error.message);
  process.exit(1);
}