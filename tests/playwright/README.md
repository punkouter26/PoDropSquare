# PoDropSquare - Playwright TypeScript E2E Tests

> Comprehensive end-to-end testing suite for PoDropSquare using Playwright and TypeScript

## 📋 Overview

This test suite provides **TypeScript-based** Playwright E2E tests covering:

- ✅ **Core Gameplay** - Block dropping, physics, scoring, timers
- ✅ **Leaderboard** - Display, sorting, navigation, API integration
- ✅ **Mobile/Tablet** - Responsive design, touch interactions, multiple viewports
- ✅ **API Testing** - Health checks, score submission, validation
- ✅ **Accessibility** - WCAG 2.0 AA compliance using axe-core
- ✅ **Visual Regression** - Screenshot comparison across runs

## 🚀 Quick Start

### Prerequisites

- **Node.js** 18+ and npm
- **.NET 9.0 SDK** (for running the API)
- **Running API** at `http://localhost:5000` (auto-started by Playwright config)

### Installation

```powershell
# Navigate to test directory
cd tests/playwright

# Install dependencies
npm install

# Install Playwright browsers
npm run install-browsers
```

### Running Tests

```powershell
# Run all tests (headless)
npm test

# Run with visible browser
npm run test:headed

# Run specific project
npm run test:chromium
npm run test:mobile

# Debug mode (step through tests)
npm run test:debug

# Interactive UI mode
npm run test:ui

# Run only accessibility tests
npm run test:accessibility

# View last test report
npm run show-report
```

## 📁 Test Structure

```
tests/playwright/
├── tests/
│   ├── gameplay.spec.ts       # Core game mechanics (12 tests)
│   ├── leaderboard.spec.ts    # Leaderboard functionality (11 tests)
│   ├── mobile.spec.ts         # Mobile/tablet responsive (15 tests)
│   └── api.spec.ts            # API endpoint testing (15 tests)
├── playwright.config.ts       # Test configuration
├── tsconfig.json             # TypeScript configuration
├── package.json              # Dependencies and scripts
└── README.md                 # This file
```

## 🧪 Test Categories

### Core Gameplay Tests (12 tests)
- Page loading and rendering
- Canvas initialization (Matter.js)
- Block dropping functionality
- Score updates
- Timer countdown
- Rapid interactions
- Visual snapshots
- Accessibility compliance

### Leaderboard Tests (11 tests)
- Navigation to leaderboard
- Table/list display
- Column headers (Rank, Player, Score)
- Data formatting
- API error handling
- Loading states
- Visual snapshots

### Mobile/Tablet Tests (15 tests)
- iPhone SE viewport (375x667)
- iPad viewport (810x1080)
- Touch-friendly buttons (44x44px minimum)
- Tap gestures
- Responsive layouts
- No horizontal scrolling
- Font size readability
- Portrait/landscape orientation

### API Tests (15 tests)
- Health check endpoint
- Response times (<3s)
- CORS headers
- Score submission validation
- Error response formats
- Leaderboard API
- Query parameters

## 🎯 Test Projects

Playwright runs tests across multiple **projects** (browser/device configurations):

| Project | Device | Viewport | Use Case |
|---------|--------|----------|----------|
| **chromium** | Desktop Chrome | 1920x1080 | Primary desktop testing |
| **mobile-chrome** | iPhone SE | 375x667 | Mobile portrait |
| **tablet** | iPad (gen 7) | 810x1080 | Tablet testing |

Run specific project:
```powershell
npx playwright test --project=mobile-chrome
```

## 🔍 Test Filtering

### By File
```powershell
npx playwright test gameplay.spec.ts
npx playwright test leaderboard
```

### By Test Name
```powershell
npx playwright test -g "should load game page"
npx playwright test -g "accessibility"
```

### By Tag
```powershell
# Run only tests tagged with @accessibility
npx playwright test --grep @accessibility
```

## 📊 Test Reports

### HTML Report
After running tests, an HTML report is generated:

```powershell
npm run show-report
```

Opens `playwright-report/index.html` in your browser with:
- Test results summary
- Failed test screenshots
- Traces for debugging
- Video recordings (on failure)

### JSON Report
Machine-readable test results: `test-results.json`

## 🐛 Debugging

### Debug Mode
```powershell
npm run test:debug
```

Opens Playwright Inspector where you can:
- Step through each test action
- Inspect page elements
- View console logs
- Modify selectors on-the-fly

### Trace Viewer
```powershell
npx playwright show-trace trace.zip
```

View detailed trace of test execution with:
- DOM snapshots at each step
- Network requests
- Console logs
- Screenshots

## ♿ Accessibility Testing

All test files include `@accessibility` tagged tests using **axe-core**:

```typescript
test('@accessibility should pass accessibility checks', async ({ page }) => {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  expect(results.violations).toEqual([]);
});
```

**Standards Tested:**
- WCAG 2.0 Level A
- WCAG 2.0 Level AA
- WCAG 2.1 Level A
- WCAG 2.1 Level AA

Run only accessibility tests:
```powershell
npm run test:accessibility
```

## 📸 Visual Regression Testing

Tests include screenshot comparisons:

```typescript
test('should match visual snapshot', async ({ page }) => {
  await expect(page).toHaveScreenshot('game-initial-state.png', {
    fullPage: true,
    maxDiffPixels: 100
  });
});
```

**First Run**: Generates baseline screenshots  
**Subsequent Runs**: Compares against baseline  
**Failures**: Shows diff images in report

Update baselines:
```powershell
npx playwright test --update-snapshots
```

## 🌐 Environment Configuration

### Base URL
Default: `http://localhost:5000`

Override with environment variable:
```powershell
$env:BASE_URL = "https://podropsquare.azurewebsites.net"
npm test
```

### CI Mode
In CI environment, tests automatically:
- Retry failed tests (2 retries)
- Run serially (workers=1)
- Require all tests (forbidOnly=true)

Detected via `process.env.CI`

## 🔧 Configuration

### playwright.config.ts

Key settings:
- **timeout**: 30 seconds per test
- **actionTimeout**: 10 seconds per action
- **retries**: 0 locally, 2 in CI
- **workers**: Parallel locally, serial in CI
- **webServer**: Auto-starts API on `localhost:5000`

### Modifying Timeouts
```typescript
// In playwright.config.ts
export default defineConfig({
  timeout: 60000, // 60 seconds
  use: {
    actionTimeout: 15000, // 15 seconds
  },
});
```

## 📝 Writing New Tests

### Basic Test Template
```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should do something', async ({ page }) => {
    // Arrange
    const button = page.locator('button.my-button');
    
    // Act
    await button.click();
    
    // Assert
    await expect(button).toHaveClass(/active/);
  });
});
```

### Mobile-Specific Test
```typescript
import { test, expect, devices } from '@playwright/test';

test.use({ ...devices['iPhone SE'] });

test('mobile feature', async ({ page }) => {
  await page.goto('/');
  
  // Use tap instead of click
  await page.locator('button').tap();
});
```

### API Test
```typescript
test('API endpoint', async ({ request }) => {
  const response = await request.get('/api/health');
  expect(response.ok()).toBeTruthy();
  
  const body = await response.json();
  expect(body).toHaveProperty('status');
});
```

## 🎯 Best Practices

### ✅ Do's
- Use `data-testid` attributes for stable selectors
- Wait for `networkidle` before assertions
- Use `toBeVisible()` to verify element presence
- Add accessibility tests for all pages
- Take screenshots on failure (automatic)
- Use semantic locators (`getByRole`, `getByLabel`)

### ❌ Don'ts
- Don't use `page.waitForTimeout()` unless necessary
- Don't rely on exact pixel positions
- Don't hard-code delays
- Don't test external dependencies
- Don't skip `test.beforeEach` setup

### Selector Priority
1. `page.getByRole('button', { name: 'Submit' })` ✅ Best
2. `page.getByTestId('submit-button')` ✅ Good
3. `page.getByText('Submit')` ⚠️ Fragile
4. `page.locator('.btn-submit')` ⚠️ Implementation detail
5. `page.locator('div > button:nth-child(2)')` ❌ Avoid

## 🚨 Troubleshooting

### Tests Failing Locally

**Problem**: Tests pass in CI but fail locally

**Solutions**:
```powershell
# Clear Playwright cache
npx playwright install --force

# Delete test artifacts
Remove-Item -Recurse -Force test-results, playwright-report

# Re-run tests
npm test
```

### API Not Starting

**Problem**: `webServer` timeout error

**Solutions**:
```powershell
# Start API manually first
cd ../../backend/src/Po.PoDropSquare.Api
dotnet run

# Run tests with existing server
$env:BASE_URL = "http://localhost:5000"
npm test
```

### Screenshot Mismatches

**Problem**: Visual regression tests failing

**Solutions**:
```powershell
# Update all snapshots
npx playwright test --update-snapshots

# Update specific test snapshots
npx playwright test gameplay.spec.ts --update-snapshots

# Increase diff threshold in test file
maxDiffPixels: 200  # Allow more pixel differences
```

### Browser Install Issues

**Problem**: Playwright browsers not found

**Solutions**:
```powershell
# Install all browsers
npx playwright install

# Install only Chromium (faster)
npx playwright install chromium

# System dependencies (Linux/WSL)
npx playwright install-deps
```

### TypeScript Errors

**Problem**: `Cannot find name 'document'`

**Solution**: Check `tsconfig.json` includes DOM lib:
```json
{
  "compilerOptions": {
    "lib": ["ES2022", "DOM"]
  }
}
```

## 📚 Additional Resources

- **Playwright Docs**: https://playwright.dev
- **axe-core Playwright**: https://github.com/dequelabs/axe-core-npm/tree/develop/packages/playwright
- **TypeScript Handbook**: https://www.typescriptlang.org/docs/
- **WCAG Guidelines**: https://www.w3.org/WAI/WCAG21/quickref/

## 🔄 CI/CD Integration

### GitHub Actions Example
```yaml
- name: Install Playwright
  run: |
    cd tests/playwright
    npm ci
    npx playwright install chromium

- name: Run Playwright Tests
  run: |
    cd tests/playwright
    npm test
  env:
    CI: true

- name: Upload Test Report
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: playwright-report
    path: tests/playwright/playwright-report/
```

## 📊 Test Statistics

| Category | Test Count | Avg Duration | Coverage |
|----------|-----------|--------------|----------|
| **Gameplay** | 12 | ~15s | Core mechanics |
| **Leaderboard** | 11 | ~10s | Leaderboard UI/API |
| **Mobile** | 15 | ~20s | Responsive design |
| **API** | 15 | ~5s | Backend endpoints |
| **Total** | **53** | **~50s** | Full E2E coverage |

## 🎓 Learning Path

1. **Start here**: Read this README
2. **Run tests**: `npm test` to see them in action
3. **Explore**: Open test files in `tests/` directory
4. **Debug**: Use `npm run test:debug` to step through
5. **Write**: Add new tests following existing patterns
6. **Report**: View `npm run show-report` after runs

---

**Built with ❤️ using Playwright + TypeScript + axe-core**

**Questions?** Check the troubleshooting section or Playwright docs.
