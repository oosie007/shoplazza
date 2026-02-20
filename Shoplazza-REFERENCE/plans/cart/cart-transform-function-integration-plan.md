# Cart-Transform Function Integration Plan

## Overview
This plan outlines the integration of the cart-transform WASM function registration into the merchant app installation process. The function will be automatically registered when a merchant installs the app, ensuring seamless add-on pricing functionality.

## Current State
- ✅ Cart-transform WASM function is implemented and tested
- ✅ Function reads `cdh_shoplazza_addon` metafields correctly
- ✅ Function automatically calculates and applies add-on pricing
- ❌ Function registration is not integrated into merchant installation
- ❌ No automatic WASM building and upload process

## Target State
- ✅ WASM function automatically built during app installation
- ✅ Function automatically uploaded to Shoplazza via Function API
- ✅ Function configured with correct triggers for merchant's shop
- ✅ Function ID stored in merchant configuration
- ✅ Function active immediately after app installation

## Technical Architecture

### Function Registration Flow
```
Merchant Installs App
        ↓
Create Merchant Record
        ↓
Configure Add-on Settings
        ↓
Build WASM File (cart-transform.wasm)
        ↓
Upload to Shoplazza Function API
        ↓
Store Function ID in Merchant Config
        ↓
Activate Function for Merchant's Shop
        ↓
App Installation Complete
```

### Data Flow
1. **App Installation Trigger** → MerchantController.InstallAppAsync()
2. **WASM Building** → CartTransformFunctionService.BuildWasmAsync()
3. **Function Upload** → ShoplazzaFunctionApiService.CreateFunctionAsync()
4. **Configuration Storage** → MerchantService.UpdateFunctionConfigAsync()
5. **Function Activation** → ShoplazzaFunctionApiService.ActivateFunctionAsync()

## Required Changes

### 1. New Services
- **ICartTransformFunctionService** - WASM building and management
- **IShoplazzaFunctionApiService** - Shoplazza Function API integration

### 2. Extended Services
- **IMerchantService** - Add function registration methods
- **MerchantService** - Implement function registration logic

### 3. New Models
- **FunctionConfiguration** - Store function metadata
- **FunctionRegistrationRequest** - API request models

### 4. Configuration Updates
- **appsettings.json** - Add Shoplazza Function API settings
- **Database** - Add function configuration tables

## Implementation Phases

### Phase 1: Core Infrastructure
- Create new service interfaces and implementations
- Add Shoplazza Function API integration
- Implement WASM building process

### Phase 2: Merchant Integration
- Extend MerchantService with function registration
- Integrate into merchant installation flow
- Add function configuration storage

### Phase 3: Testing & Validation
- Test complete installation flow
- Validate function registration and activation
- Test add-on pricing functionality

## Success Criteria
- [ ] WASM function builds successfully during installation
- [ ] Function uploads to Shoplazza without errors
- [ ] Function ID is stored in merchant configuration
- [ ] Function activates immediately after installation
- [ ] Add-on pricing works correctly in merchant's shop
- [ ] No manual configuration required from merchant

## Risk Mitigation
- **Function Build Failures** - Fallback to pre-built WASM
- **API Upload Failures** - Retry logic with exponential backoff
- **Function Activation Delays** - Async activation with status checking
- **Configuration Storage Issues** - Rollback to safe state

## Dependencies
- Shoplazza Function API access and credentials
- Node.js environment for WASM building
- Javy compiler for WASM generation
- Database schema updates for function configuration
