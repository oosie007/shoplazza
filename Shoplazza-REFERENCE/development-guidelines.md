# Development Guidelines

## Code Quality Standards

### .NET 8.0 Guidelines

#### Coding Standards
- Follow Microsoft C# coding conventions
- Use PascalCase for public members, camelCase for private
- Include XML documentation for public APIs
- Maximum method length: 50 lines
- Maximum class length: 500 lines

#### Architecture Patterns
- **Repository Pattern** for data access
- **Dependency Injection** for service management
- **CQRS** for complex operations (if needed)
- **Factory Pattern** for widget generation
- **Strategy Pattern** for different theme integrations

#### Error Handling
- Use structured logging (Serilog)
- Implement global exception handling
- Return appropriate HTTP status codes
- Log security-related events

#### Performance Guidelines
- Use async/await for I/O operations
- Implement caching where appropriate
- Use connection pooling
- Optimize database queries

### JavaScript Guidelines

#### Code Style
- Use ES6+ features consistently
- Follow Airbnb JavaScript style guide
- Maximum function length: 30 lines
- Use meaningful variable names

#### Architecture Patterns
- **Module Pattern** for code organization
- **Observer Pattern** for event handling
- **Factory Pattern** for component creation
- **Singleton Pattern** for global state (sparingly)

#### Performance Guidelines
- Minimize DOM manipulation
- Use event delegation
- Implement lazy loading
- Optimize bundle size

## Git Workflow

### Branch Strategy
```
main
├── develop
│   ├── feature/auth-implementation
│   ├── feature/product-configuration
│   └── feature/widget-development
├── release/v1.0.0
└── hotfix/security-patch
```

### Commit Guidelines

#### Commit Message Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

#### Types
- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style changes (formatting)
- **refactor**: Code refactoring
- **test**: Adding or updating tests
- **chore**: Maintenance tasks

#### Examples
```
feat(auth): implement OAuth 2.0 flow

- Add ShoplazzaAuthService with OAuth implementation
- Include HMAC verification for webhook security
- Add unit tests for authentication flow

Closes #12
```

```
fix(widget): resolve cart integration issue

- Fix add-on not being added to cart on product pages
- Improve error handling for cart API failures
- Add retry logic for failed cart operations

Fixes #25
```

### Code Review Process

#### Before Creating PR
- [ ] All tests pass locally
- [ ] Code compiles without warnings
- [ ] Documentation updated
- [ ] Self-review completed
- [ ] Feature branch is up-to-date with develop

#### PR Requirements
- [ ] Clear title and description
- [ ] Link to related issues
- [ ] Screenshots/demos for UI changes
- [ ] Test coverage maintained/improved
- [ ] No sensitive data exposed

#### Review Checklist
- [ ] Code follows style guidelines
- [ ] Logic is sound and efficient
- [ ] Security considerations addressed
- [ ] Error handling is appropriate
- [ ] Tests are comprehensive

## Testing Strategy

### Unit Testing

#### .NET Testing
- Use xUnit for unit tests
- Moq for mocking dependencies
- Arrange-Act-Assert pattern
- Minimum 80% code coverage

#### JavaScript Testing
- Use Jest for unit tests
- Test DOM manipulation separately
- Mock external dependencies
- Test error scenarios

### Integration Testing
- Test API endpoints end-to-end
- Test Shoplazza API integration
- Test webhook processing
- Test database operations

### Widget Testing
- Test across different themes
- Test responsive behavior
- Test cart integration scenarios
- Test error handling

## Security Guidelines

### Authentication & Authorization
- Never store credentials in code
- Use environment variables for secrets
- Implement proper session management
- Validate all input parameters

### Data Protection
- Encrypt sensitive data at rest
- Use HTTPS for all communications
- Implement proper CORS policies
- Sanitize user input

### Webhook Security
- Verify HMAC signatures
- Implement replay attack protection
- Use HTTPS endpoints only
- Log security events

## Performance Guidelines

### Backend Performance
- Use async/await consistently
- Implement response caching
- Optimize database queries
- Monitor memory usage

### Frontend Performance
- Minimize JavaScript bundle size
- Use CSS minification
- Implement lazy loading
- Optimize image assets

### Monitoring
- Set up Application Insights
- Monitor API response times
- Track error rates
- Monitor resource usage

## Deployment Guidelines

### Environment Management
- Use separate configs per environment
- Never commit secrets to Git
- Use Azure Key Vault for production secrets
- Implement proper logging levels

### CI/CD Pipeline
```
Developer Push → Build → Test → Security Scan → Deploy to Staging → Manual Approval → Deploy to Production
```

### Pre-deployment Checklist
- [ ] All tests pass
- [ ] Security scan clean
- [ ] Performance testing completed
- [ ] Documentation updated
- [ ] Rollback plan prepared

### Post-deployment Verification
- [ ] Health checks pass
- [ ] Key functionality verified
- [ ] Monitoring alerts configured
- [ ] Performance metrics baseline

## Documentation Standards

### Code Documentation
- XML documentation for public APIs
- Inline comments for complex logic
- README files for each component
- API documentation with examples

### Architecture Documentation
- Keep diagrams updated
- Document design decisions
- Maintain deployment guides
- Update troubleshooting docs

## Troubleshooting Guide

### Common Issues

#### Authentication Problems
1. **Invalid HMAC signature**
   - Check webhook endpoint URL
   - Verify HMAC secret configuration
   - Ensure timestamp tolerance

2. **OAuth flow failures**
   - Verify redirect URL configuration
   - Check client ID/secret
   - Review Shoplazza app settings

#### Widget Integration Issues
1. **Widget not appearing**
   - Check script tag injection
   - Verify product page detection
   - Review console for errors

2. **Cart integration failures**
   - Verify Shoplazza cart API endpoints
   - Check product/variant IDs
   - Review network requests

#### Performance Issues
1. **Slow API responses**
   - Review database query performance
   - Check caching implementation
   - Monitor external API calls

2. **High memory usage**
   - Review object disposal
   - Check for memory leaks
   - Monitor garbage collection

### Debugging Tools
- Application Insights for monitoring
- Local debugging with breakpoints
- Network tab for API issues
- Console logging for widget issues

### Support Escalation
1. Check logs and monitoring
2. Reproduce issue locally
3. Document steps to reproduce
4. Create detailed bug report
5. Escalate to senior developer

## Quality Gates

### Definition of Done
- [ ] Feature implemented per requirements
- [ ] Unit tests written and passing
- [ ] Integration tests passing
- [ ] Code reviewed and approved
- [ ] Documentation updated
- [ ] Security review completed
- [ ] Performance testing passed
- [ ] Deployed to staging successfully

### Release Criteria
- [ ] All features tested end-to-end
- [ ] Performance benchmarks met
- [ ] Security scan completed
- [ ] Documentation finalized
- [ ] Rollback plan prepared
- [ ] Monitoring configured
- [ ] Support team trained

These guidelines ensure consistent, high-quality development across the entire project.