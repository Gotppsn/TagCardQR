// Enhanced Card Tag Manager JavaScript

// Color theme presets
const colorPresets = [
    { name: 'Professional Blue', background: '#ffffff', text: '#333333', accent: '#0284c7' },
    { name: 'Modern Dark', background: '#1e293b', text: '#f8fafc', accent: '#38bdf8' },
    { name: 'Minimal Gray', background: '#f8f9fa', text: '#212529', accent: '#6c757d' },
    { name: 'Vibrant Purple', background: '#ffffff', text: '#4b5563', accent: '#8b5cf6' },
    { name: 'Earthy Green', background: '#f9fafb', text: '#1f2937', accent: '#059669' }
];

// Card preview functionality
function updateCardPreview() {
    const name = document.getElementById('Name')?.value || 'Your Name';
    const title = document.getElementById('Title')?.value || 'Your Title';
    const company = document.getElementById('Company')?.value || 'Your Company';
    const email = document.getElementById('Email')?.value || 'your.email@example.com';
    const phone = document.getElementById('Phone')?.value || '+1 (555) 123-4567';
    const website = document.getElementById('Website')?.value || 'www.example.com';
    const address = document.getElementById('Address')?.value || 'Your Address';
    const bgColor = document.getElementById('BackgroundColor')?.value;
    const textColor = document.getElementById('TextColor')?.value;
    const accentColor = document.getElementById('AccentColor')?.value;
    
    const preview = document.getElementById('card-preview');
    if (preview) {
        preview.style.backgroundColor = bgColor;
        preview.style.color = textColor;
        
        document.getElementById('preview-name').textContent = name;
        document.getElementById('preview-name').style.color = accentColor;
        document.getElementById('preview-title').textContent = title;
        document.getElementById('preview-company').textContent = company;
        document.getElementById('preview-email').textContent = email;
        document.getElementById('preview-phone').textContent = phone;
        
        if (document.getElementById('preview-website')) {
            document.getElementById('preview-website').textContent = website;
        }
        
        if (document.getElementById('preview-address')) {
            document.getElementById('preview-address').textContent = address;
        }
        
        const icons = preview.querySelectorAll('i');
        icons.forEach(icon => {
            icon.style.color = accentColor;
        });
        
        // Add animation to show update
        preview.classList.add('update-animation');
        setTimeout(() => {
            preview.classList.remove('update-animation');
        }, 1000);
    }
}

// Print functionality with animations
function printCard() {
    const printButton = document.querySelector('.print-button');
    if (printButton) {
        printButton.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i> Preparing...';
        
        setTimeout(() => {
            window.print();
            setTimeout(() => {
                printButton.innerHTML = '<i class="fas fa-print mr-2"></i> Print Card';
            }, 500);
        }, 800);
    } else {
        window.print();
    }
    return false;
}

// Card flip functionality
function initCardFlip() {
    const flipButtons = document.querySelectorAll('.flip-card-btn');
    flipButtons.forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            const container = this.closest('.card-container')?.querySelector('.card-flip-container');
            if (container) {
                container.classList.toggle('flipped');
            }
        });
    });
}

// Apply preset theme
function applyColorPreset(index) {
    const preset = colorPresets[index];
    if (preset) {
        const bgInput = document.getElementById('BackgroundColor');
        const textInput = document.getElementById('TextColor');
        const accentInput = document.getElementById('AccentColor');
        
        if (bgInput && textInput && accentInput) {
            bgInput.value = preset.background;
            textInput.value = preset.text;
            accentInput.value = preset.accent;
            
            // Update color picker inputs
            const colorInputs = document.querySelectorAll('input[type="color"]');
            colorInputs.forEach(input => {
                const nextInput = input.nextElementSibling;
                if (nextInput && input.id === 'BackgroundColor') nextInput.value = preset.background;
                if (nextInput && input.id === 'TextColor') nextInput.value = preset.text;
                if (nextInput && input.id === 'AccentColor') nextInput.value = preset.accent;
            });
            
            // Update preview
            updateCardPreview();
        }
    }
}

// QR code with animation
function initQrCodeHover() {
    const qrCodes = document.querySelectorAll('.card-qr');
    qrCodes.forEach(qr => {
        qr.addEventListener('mouseenter', function() {
            this.classList.add('qr-highlight');
            const scanText = this.querySelector('.scan-text');
            if (scanText) {
                scanText.classList.add('text-primary-600');
            }
        });
        qr.addEventListener('mouseleave', function() {
            this.classList.remove('qr-highlight');
            const scanText = this.querySelector('.scan-text');
            if (scanText) {
                scanText.classList.remove('text-primary-600');
            }
        });
    });
}

// Live preview on input change
function initLivePreview() {
    const formInputs = document.querySelectorAll('form input, form textarea');
    formInputs.forEach(input => {
        input.addEventListener('input', updateCardPreview);
    });
}

// Initialize all card functionality
document.addEventListener('DOMContentLoaded', function() {
    // Color inputs auto-update
    const colorInputs = document.querySelectorAll('input[type="color"]');
    colorInputs.forEach(input => {
        input.addEventListener('change', function() {
            const textInput = this.nextElementSibling;
            if (textInput) {
                textInput.value = this.value;
            }
            updateCardPreview();
        });
    });
    
    // Initialize card preview
    const previewButton = document.getElementById('preview-card');
    if (previewButton) {
        previewButton.addEventListener('click', updateCardPreview);
        // Initialize live preview
        initLivePreview();
    }
    
    // Initialize QR codes
    const qrCodeContainers = document.querySelectorAll('.qr-code-container');
    qrCodeContainers.forEach(container => {
        const data = container.getAttribute('data-qr');
        if (data && typeof QRCode !== 'undefined') {
            new QRCode(container, {
                text: data,
                width: 128,
                height: 128,
                colorDark: "#000000",
                colorLight: "#ffffff",
                correctLevel: QRCode.CorrectLevel.H
            });
        }
    });
    
    // Initialize card flip functionality
    initCardFlip();
    
    // Initialize QR code hover effects
    initQrCodeHover();
    
    // Add preset selectors if on create/edit page
    const colorSection = document.querySelector('form .mt-6');
    if (colorSection) {
        const presetContainer = document.createElement('div');
        presetContainer.className = 'mt-4 mb-6';
        presetContainer.innerHTML = `
            <label class="block text-sm font-medium text-gray-700 mb-2">Quick Color Themes</label>
            <div class="flex flex-wrap gap-2">
                ${colorPresets.map((preset, index) => `
                    <button type="button" class="px-3 py-1 text-xs rounded-md bg-gray-100 hover:bg-gray-200 transition-colors" 
                            onclick="applyColorPreset(${index})">${preset.name}</button>
                `).join('')}
            </div>
        `;
        colorSection.appendChild(presetContainer);
    }
});
document.addEventListener('DOMContentLoaded', function() {
    console.log('Forcing cache refresh of CSS');
    const link = document.querySelector('link[href*="tailwind.css"]');
    if (link) {
        const newHref = link.href.includes('?') 
            ? link.href.split('?')[0] + '?v=' + Date.now() 
            : link.href + '?v=' + Date.now();
        link.href = newHref;
    }
});