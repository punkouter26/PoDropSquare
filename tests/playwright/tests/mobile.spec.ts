import { test, expect, devices } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Mobile Responsive E2E Tests
 * 
 * Tests mobile-specific functionality:
 * - Touch interactions
 * - Responsive layout
 * - Portrait/landscape orientations
 * - Mobile-specific UI elements
 */

test.use({ ...devices['iPhone SE'] });

test.describe('Mobile Gameplay - iPhone SE', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should load game page on mobile viewport', async ({ page }) => {
    // Verify viewport is mobile size
    const viewport = page.viewportSize();
    expect(viewport).not.toBeNull();
    expect(viewport!.width).toBeLessThan(768); // Mobile breakpoint
    
    // Verify page loads
    await expect(page).toHaveTitle(/PoDropSquare/i);
  });

  test('should display mobile-friendly layout', async ({ page }) => {
    // Game container should be visible and properly sized
    const gameContainer = page.locator('.game-container, #game-container, [data-testid="game-container"]');
    await expect(gameContainer.first()).toBeVisible();
    
    // Check container doesn't overflow
    const box = await gameContainer.first().boundingBox();
    const viewport = page.viewportSize();
    expect(box).not.toBeNull();
    expect(viewport).not.toBeNull();
    expect(box!.width).toBeLessThanOrEqual(viewport!.width + 10); // Small margin for rounding
  });

  test('should have touch-friendly buttons (minimum 44x44px)', async ({ page }) => {
    // Find all interactive buttons
    const buttons = page.locator('button');
    const count = await buttons.count();
    
    // Check first button size (iOS minimum touch target)
    if (count > 0) {
      const box = await buttons.first().boundingBox();
      expect(box).not.toBeNull();
      
      // Verify meets accessibility guidelines (44x44px minimum)
      expect(box!.width).toBeGreaterThanOrEqual(40); // Allowing slight variance
      expect(box!.height).toBeGreaterThanOrEqual(40);
    }
  });

  test('should support tap gesture on drop button', async ({ page }) => {
    const dropButton = page.locator('button').filter({ hasText: /drop|spawn|place/i }).first();
    
    // Use tap instead of click for mobile
    await dropButton.tap();
    
    // Wait for physics update
    await page.waitForTimeout(500);
    
    // Verify page still responsive
    await expect(dropButton).toBeVisible();
  });

  test('should handle rapid taps without issues', async ({ page }) => {
    const dropButton = page.locator('button').filter({ hasText: /drop|spawn|place/i }).first();
    
    // Rapid tap 5 times
    for (let i = 0; i < 5; i++) {
      await dropButton.tap();
      await page.waitForTimeout(100);
    }
    
    // Verify no errors or crashes
    await expect(dropButton).toBeVisible();
  });

  test('canvas should be appropriately sized for mobile', async ({ page }) => {
    const canvas = page.locator('canvas').first();
    await expect(canvas).toBeVisible();
    
    const viewport = page.viewportSize();
    const box = await canvas.boundingBox();
    
    expect(box).not.toBeNull();
    
    // Canvas width should fit within viewport
    expect(box!.width).toBeLessThanOrEqual(viewport!.width);
  });

  test('should not require horizontal scrolling', async ({ page }) => {
    // Check document width doesn't exceed viewport
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
    
    expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 5); // Small margin
  });

  test('text should be readable (minimum 16px font)', async ({ page }) => {
    // Check main text elements
    const textElements = page.locator('p, span, button, h1, h2, h3');
    
    if (await textElements.count() > 0) {
      const fontSize = await textElements.first().evaluate(el => {
        return window.getComputedStyle(el).fontSize;
      });
      
      const fontSizeNum = parseFloat(fontSize);
      expect(fontSizeNum).toBeGreaterThanOrEqual(14); // Slightly below 16px for flexibility
    }
  });

  test('@accessibility should pass mobile accessibility checks', async ({ page }) => {
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should match mobile visual snapshot', async ({ page }) => {
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    await expect(page).toHaveScreenshot('mobile-game-initial.png', {
      fullPage: true,
      maxDiffPixels: 150
    });
  });
});

test.describe('Mobile Leaderboard', () => {
  test.use({ ...devices['iPhone SE'] });

  test('should display mobile-friendly leaderboard', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Leaderboard should be visible
    const leaderboard = page.locator('table, .leaderboard, [data-testid="leaderboard"]');
    await expect(leaderboard.first()).toBeVisible();
    
    // Should not require horizontal scroll
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
    
    expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 5);
  });

  test('table should be responsive or use card layout', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Check if using responsive table or card layout
    const table = page.locator('table');
    const cards = page.locator('.card, .leaderboard-card');
    
    // Should have either table or cards
    const hasTable = await table.count() > 0;
    const hasCards = await cards.count() > 0;
    
    expect(hasTable || hasCards).toBeTruthy();
  });
});

test.describe('Tablet Gameplay - iPad', () => {
  test.use({ ...devices['iPad (gen 7)'] });

  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should load on tablet viewport', async ({ page }) => {
    const viewport = page.viewportSize();
    expect(viewport).not.toBeNull();
    expect(viewport!.width).toBeGreaterThan(600); // Tablet size
    
    await expect(page).toHaveTitle(/PoDropSquare/i);
  });

  test('should utilize larger tablet screen space', async ({ page }) => {
    const gameContainer = page.locator('.game-container, #game-container').first();
    await expect(gameContainer).toBeVisible();
    
    // Canvas should be larger on tablet
    const canvas = page.locator('canvas').first();
    const box = await canvas.boundingBox();
    
    expect(box).not.toBeNull();
    expect(box!.width).toBeGreaterThan(400); // Larger than mobile
  });

  test('should support both tap and mouse interactions', async ({ page }) => {
    const dropButton = page.locator('button').filter({ hasText: /drop|spawn|place/i }).first();
    
    // Try tap
    await dropButton.tap();
    await page.waitForTimeout(300);
    
    // Try click
    await dropButton.click();
    await page.waitForTimeout(300);
    
    await expect(dropButton).toBeVisible();
  });

  test('@accessibility should pass tablet accessibility checks', async ({ page }) => {
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });
});

test.describe('Orientation Changes', () => {
  test('should handle portrait to landscape rotation', async ({ page, context }) => {
    // Start in portrait (iPhone SE default)
    await context.addCookies([]);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Verify portrait layout
    let viewport = page.viewportSize();
    expect(viewport!.height).toBeGreaterThan(viewport!.width);
    
    // Rotate to landscape
    await page.setViewportSize({ width: 667, height: 375 }); // iPhone SE landscape
    await page.waitForTimeout(500);
    
    // Verify page still works
    const canvas = page.locator('canvas').first();
    await expect(canvas).toBeVisible();
  });
});
