// Test script to verify template stitching logic
const fs = require('fs');
const path = require('path');

// Mock the template loading
function loadTemplate(templateName) {
    const templatePath = path.join(__dirname, 'wwwroot', 'widget-templates', templateName);
    try {
        return fs.readFileSync(templatePath, 'utf8');
    } catch (error) {
        console.error(`Error loading template ${templateName}:`, error.message);
        return '';
    }
}

// Mock the template stitching logic
function generateWidgetJavaScript(merchant) {
    try {
        // Load all template pieces
        const widgetScriptTemplate = loadTemplate('widget-script.js');
        const widgetStylesTemplate = loadTemplate('widget-styles.html');
        const addonSelectionTemplate = loadTemplate('addon-selection.html');
        
        // Mock add-on configuration
        const addonConfig = {
            AddOnTitle: 'Premium Protection',
            AddOnPriceCents: 199,
            AddOnDescription: 'Protect your purchase with comprehensive coverage',
            AddOnSku: 'PROTECTION-001',
            AddOnVariantId: 'test-variant-123'
        };
        
        // Create the complete widget HTML by combining templates
        const widgetHtml = addonSelectionTemplate
            .replace(/{{ADDON_TITLE}}/g, addonConfig.AddOnTitle)
            .replace(/{{ADDON_PRICE}}/g, (addonConfig.AddOnPriceCents / 100.0).toFixed(2))
            .replace(/{{ADDON_DESCRIPTION}}/g, addonConfig.AddOnDescription)
            .replace(/{{ADDON_SKU}}/g, addonConfig.AddOnSku)
            .replace(/{{ADDON_VARIANT_ID}}/g, addonConfig.AddOnVariantId);
        
        // Replace template placeholders in the JavaScript
        const widgetScript = widgetScriptTemplate
            .replace(/{{SHOP_DOMAIN}}/g, merchant.shop)
            .replace(/{{API_ENDPOINT}}/g, 'http://localhost:5128')
            .replace(/{{WIDGET_STYLES}}/g, widgetStylesTemplate)
            .replace(/{{WIDGET_HTML}}/g, widgetHtml);
        
        return widgetScript;
    } catch (error) {
        console.error('Error generating widget JavaScript:', error.message);
        return 'console.error("Widget script generation failed.");';
    }
}

// Test the stitching
console.log('🧪 Testing template stitching...\n');

const mockMerchant = { shop: 'test-shop.myshoplaza.com' };
const result = generateWidgetJavaScript(mockMerchant);

// Check if all placeholders were replaced
const placeholders = [
    '{{SHOP_DOMAIN}}',
    '{{API_ENDPOINT}}', 
    '{{WIDGET_STYLES}}',
    '{{WIDGET_HTML}}',
    '{{ADDON_TITLE}}',
    '{{ADDON_PRICE}}',
    '{{ADDON_DESCRIPTION}}',
    '{{ADDON_SKU}}',
    '{{ADDON_VARIANT_ID}}'
];

console.log('📋 Placeholder replacement check:');
placeholders.forEach(placeholder => {
    if (result.includes(placeholder)) {
        console.log(`❌ ${placeholder} - NOT replaced`);
    } else {
        console.log(`✅ ${placeholder} - replaced`);
    }
});

console.log('\n📏 Generated script length:', result.length, 'characters');
console.log('\n🔍 Sample of generated script:');
console.log('---');
console.log(result.substring(0, 500) + '...');
console.log('---');

// Check for key functionality
console.log('\n🔍 Functionality check:');
if (result.includes('ShoplazzaAddonWidget')) {
    console.log('✅ ShoplazzaAddonWidget object found');
} else {
    console.log('❌ ShoplazzaAddonWidget object missing');
}

if (result.includes('Intercept fetch calls to /api/cart')) {
    console.log('✅ Fetch interception found');
} else {
    console.log('❌ Fetch interception missing');
}

if (result.includes('Premium Protection')) {
    console.log('✅ Add-on title injected');
} else {
    console.log('❌ Add-on title not injected');
}

if (result.includes('$1.99')) {
    console.log('✅ Add-on price injected');
} else {
    console.log('❌ Add-on price not injected');
}

console.log('\n🎯 Test completed!');
