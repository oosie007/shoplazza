# SHOPLAZZA ADD-ON APP - CONTEXT LOG

## PROJECT OVERVIEW
**Goal**: Implement cart-transform function integration to complete the cart-transform refactor plan
**Current Phase**: Phase 2 - Service Implementations
**Last Updated**: [Current Date]

## 🚨 CRITICAL WORKING FOLDER INSTRUCTION
**WORKING FOLDER**: `shoplazza-addon-system/shoplazza-addon-app/`
**DO NOT WORK OUTSIDE THIS FOLDER** - All implementation files must be created within this directory structure
**ONLY EXCEPTION**: `CONTEXT_LOG.md` is located at the root level for easy access
**FOLDER STRUCTURE**:
```
shoplazza-addon-system/
├── shoplazza-addon-app/          ← WORK HERE (Services/, Models/, etc.)
├── cart-transform-function/       ← WASM source code
└── CONTEXT_LOG.md                ← Context documentation
```

### 🚨 EXPLICIT WORKING FOLDER RULES:
1. **ALL NEW FILES** must be created in `shoplazza-addon-system/shoplazza-addon-app/` subdirectories
2. **NEVER create files** in the root `shoplazza-addon-system/` folder
3. **NEVER create files** outside the `shoplazza-addon-app/` folder
4. **Services go in**: `shoplazza-addon-app/Services/`
5. **Models go in**: `shoplazza-addon-app/Models/Configuration/` or `shoplazza-addon-app/Models/Api/`
6. **Controllers go in**: `shoplazza-addon-app/Controllers/`
7. **Database migrations go in**: `shoplazza-addon-app/Migrations/`
8. **ONLY CONTEXT_LOG.md** is allowed at the root level

## CURRENT SYSTEM STATE

### ✅ COMPLETED COMPONENTS

#### 1. Metadata Backend Implementation
- **Namespace**: `cdh_shoplazza_addon`
- **Metadata Fields**:
  - `addon_title` - Add-on title from configuration
  - `addon_description` - Add-on description from configuration  
  - `addon_price` - Add-on price (formatted as decimal string)
  - `addon_selected` - Boolean indicating if add-on is selected (starts as "false")
- **Storage**: Metafields stored on Shoplazza products, NOT in local database
- **Integration**: Automatically added/updated/removed when add-ons are configured

#### 2. Widget Simplification
- **Status**: COMPLETED
- **Change**: Refactored from hardcoded JavaScript to HTML template streaming
- **Location**: `wwwroot/widget-templates/` directory
- **Files**: `widget-base.html`, `addon-selection.html`, `widget-script.html`, `widget-styles.html`

#### 3. Cart-Transform WASM Function
- **Status**: IMPLEMENTED AND TESTED
- **Location**: `cart-transform-function/` directory
- **Functionality**: Reads `cdh_shoplazza_addon` metafields, calculates pricing, adds line items
- **Build**: Uses Javy compiler to convert JavaScript to WASM
- **Deployment**: `deploy.sh` script handles building and testing

### 🔄 CURRENT DATABASE SCHEMA

#### Entities
1. **Merchant** - Store information and configuration
2. **ProductAddOn** - Add-on configurations for products
3. **Configuration** - Global merchant settings

#### Key Relationships
- `Merchant` → `ProductAddOn` (One-to-Many)
- `Merchant` → `Configuration` (One-to-One)

#### No Database Changes for Metadata
- Metafields are stored on Shoplazza platform, not locally
- No EF migrations needed for metadata functionality

## IMPLEMENTATION DECISIONS

### Metadata Strategy
- **Scope**: Product level only (not variant level)
- **Namespace**: `cdh_shoplazza_addon` (agreed with user)
- **Field Names**: `addon_title`, `addon_description`, `addon_price`, `addon_selected`
- **Error Handling**: Add-on operations MUST fail if metadata operations fail

### WASM Function Integration
- **Registration**: Automatic during merchant app installation
- **Build Process**: Automated using existing `deploy.sh` script
- **Upload Format**: Base64 encoded WASM file to Shoplazza Function API
- **Triggers**: `cart.add`, `cart.update`, `checkout.begin`

### Function Registration Flow
```
Merchant Installs App → Create Merchant Record → Configure Add-ons → 
Build WASM → Upload to Shoplazza → Store Function ID → Activate Function
```

## PHASE 1 IMPLEMENTATION PLAN

### Step 1.1: Create New Service Interfaces
- `IShoplazzaFunctionApiService` - Shoplazza Function API integration
- `ICartTransformFunctionService` - WASM building and management

### Step 1.2: Create New Models
- `FunctionConfiguration` - Store function metadata in database
- `FunctionRegistrationRequest` - API request models for function creation

### Step 1.3: Update Configuration
- Add Shoplazza Function API settings to `appsettings.json`

## TECHNICAL CONSTRAINTS

### Dependencies
- Node.js 18+ for WASM building
- Javy compiler for JavaScript to WASM conversion
- Shoplazza Function API access and credentials

### Error Handling Requirements
- Build failures → Fallback to pre-built WASM
- Upload failures → Retry logic with exponential backoff
- Function activation delays → Async activation with status checking

### Performance Requirements
- Build time: <30 seconds
- Deployment time: <60 seconds
- Function activation: Immediate after successful upload

## SUCCESS CRITERIA

### Phase 1 Success
- [ ] New service interfaces created
- [ ] New models created
- [ ] Configuration updated
- [ ] App builds without errors
- [ ] All new types compile correctly

### Overall Success
- [ ] WASM function builds successfully during installation
- [ ] Function uploads to Shoplazza without errors
- [ ] Function ID stored in merchant configuration
- [ ] Function activates immediately after installation
- [ ] Add-on pricing works correctly in merchant's shop

## NEXT PHASES

### Phase 2: Service Implementations
- Implement concrete service classes
- Add WASM building logic
- Add Shoplazza Function API integration

### Phase 3: Database & Integration
- Create EF migration for FunctionConfiguration table
- Extend MerchantService with function registration
- Integrate into merchant installation flow

## IMPORTANT NOTES

### File Locations
- **Services**: `Services/` directory
- **Models**: `Models/Configuration/` and `Models/Api/` directories
- **Configuration**: `appsettings.json`

### Naming Conventions
- Follow existing C# naming conventions
- Use async/await pattern consistently
- Implement proper error handling and logging

### Testing Strategy
- Test each component individually before integration
- Validate against success criteria at each phase
- Use existing test patterns and infrastructure

## CONTEXT PRESERVATION

### For Agent Continuity
- This file contains all implementation decisions and current state
- Each phase should update this file with progress and new decisions
- All technical details and constraints are documented here
- File should be committed to git after each phase completion

### Key Decisions Made
1. **Metadata namespace**: `cdh_shoplazza_addon` (agreed with user)
2. **Field names**: `addon_title`, `addon_description`, `addon_price`, `addon_selected`
3. **Integration point**: Merchant app installation flow
4. **WASM building**: Use existing `deploy.sh` script
5. **Error handling**: Fail-fast approach for critical operations

## CURRENT IMPLEMENTATION STATUS

### Phase 1: Core Infrastructure
- **Status**: ✅ COMPLETED
- **Completed Steps**: 
  - ✅ Created context log file
  - ✅ Created new service interfaces (IShoplazzaFunctionApiService, ICartTransformFunctionService)
  - ✅ Created new models (FunctionConfiguration, FunctionRegistrationRequest)
  - ✅ Updated appsettings.json with Shoplazza Function API configuration
- **Time Taken**: ~15 minutes

### Phase 2: Service Implementations
- **Status**: ✅ COMPLETED
- **Completed Steps**:
  - ✅ Implemented ShoplazzaFunctionApiService (Shoplazza Function API integration)
  - ✅ Implemented CartTransformFunctionService (WASM building and management)
  - ✅ Fixed compilation errors (added missing using statements)
  - ✅ App builds successfully with all Phase 2 services
- **Time Taken**: ~25 minutes total

### Phase 3: Database & Integration
- **Status**: ✅ COMPLETED
- **Completed Steps**:
  - ✅ Created EF migration for FunctionConfiguration table
  - ✅ Added FunctionConfiguration DbSet to ApplicationDbContext
  - ✅ Extended IMerchantService interface with function registration methods
  - ✅ Extended MerchantService implementation with function registration logic
  - ✅ Integrated function registration into merchant installation flow in AuthController
  - ✅ App builds successfully with all Phase 3 changes
- **Next Phase**: Testing & Validation
- **Time Taken**: ~30 minutes total

### Overall Progress
- **Completed**: 95% (Metadata backend, Widget simplification, WASM function, Phase 1 infrastructure, Phase 2 services, Phase 3 database integration)
- **Remaining**: 5% (Testing & validation, final deployment)
- **Target Completion**: End of current session
