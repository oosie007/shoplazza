# Agent Execution Prompt

## Task: Implement Cart-Transform Function Integration

### What to Do
Read the following plan files and implement the cart-transform function integration into the merchant installation process:

1. **`cart-transform-function-integration-plan.md`** - High-level overview
2. **`cart-transform-function-implementation-steps.md`** - Detailed implementation steps
3. **`wasm-building-and-deployment-guide.md`** - WASM building and deployment details

### Key Requirements
- **WASM function builds automatically** during merchant app installation
- **Function uploads to Shoplazza** via Function API
- **Function ID stored** in merchant configuration
- **Function activates immediately** after installation
- **No manual configuration** required from merchant

### Implementation Order
1. **Phase 1**: Create new service interfaces and models
2. **Phase 2**: Implement the services
3. **Phase 3**: Extend MerchantService with function registration
4. **Phase 4**: Update database schema
5. **Phase 5**: Integrate into merchant installation flow
6. **Phase 6**: Test and validate

### Important Notes
- **WASM files are base64 encoded** for Shoplazza API upload
- **Function registration happens automatically** during app installation
- **Error handling must be robust** with fallback mechanisms
- **All metadata field names must match** our existing backend implementation
- **Follow the development guidelines** in `development-guidelines.md`

### Success Criteria
- [ ] App builds without errors
- [ ] WASM function builds successfully
- [ ] Function uploads to Shoplazza
- [ ] Function ID stored in database
- [ ] Function activates automatically
- [ ] Add-on pricing works correctly

### Questions to Answer
- Does the WASM building process work correctly?
- Does the function upload to Shoplazza successfully?
- Is the function ID properly stored and retrieved?
- Does the function activate immediately after installation?
- Does add-on pricing work in the merchant's shop?

**Execute this implementation step by step, testing each phase before proceeding to the next.**
