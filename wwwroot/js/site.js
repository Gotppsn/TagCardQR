// Print functionality
function printCard() {
    window.print();
    return false;
}

// Add event listeners after DOM loads
document.addEventListener('DOMContentLoaded', function() {
    // Auto-submit color form fields when color picker changes
    const colorInputs = document.querySelectorAll('input[type="color"]');
    colorInputs.forEach(input => {
        input.addEventListener('change', function() {
            const textInput = this.nextElementSibling;
            if (textInput) {
                textInput.value = this.value;
            }
        });
    });
    
    // Card preview functionality (if on edit/create page)
    const previewButton = document.getElementById('preview-card');
    if (previewButton) {
        previewButton.addEventListener('click', updateCardPreview);
    }
    
    // Initialize any QR code elements
    const qrCodeContainers = document.querySelectorAll('.qr-code-container');
    qrCodeContainers.forEach(container => {
        const data = container.getAttribute('data-qr');
        if (data && typeof QRCode !== 'undefined') {
            new QRCode(container, {
                text: data,
                width: 128,
                height: 128
            });
        }
    });
});

// Card preview function
function updateCardPreview() {
    const name = document.getElementById('Name').value;
    const title = document.getElementById('Title').value;
    const company = document.getElementById('Company').value;
    const email = document.getElementById('Email').value;
    const phone = document.getElementById('Phone').value;
    const website = document.getElementById('Website').value;
    const bgColor = document.getElementById('BackgroundColor').value;
    const textColor = document.getElementById('TextColor').value;
    const accentColor = document.getElementById('AccentColor').value;
    
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
        document.getElementById('preview-website').textContent = website;
        
        const icons = preview.querySelectorAll('i');
        icons.forEach(icon => {
            icon.style.color = accentColor;
        });
    }
}