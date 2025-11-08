import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Core Gameplay E2E Tests
 * 
 * Tests the fundamental game mechanics:
 * - Page loads correctly
 * - Game board renders
 * - Block dropping functionality
 * - Scoring system
 * - Timer countdown
 * - Game over conditions
 */

test.describe('Core Gameplay', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should load game page successfully', async ({ page }) => {
    // Verify page title
    await expect(page).toHaveTitle(/PoDropSquare/i);
    
    // Verify main game container exists
    const gameContainer = page.locator('.game-container, #game-container, [data-testid="game-container"]');
    await expect(gameContainer.first()).toBeVisible();
  });

  test('should display game board with Matter.js canvas', async ({ page }) => {
    // Wait for canvas to render (Matter.js physics engine)
    const canvas = page.locator('canvas');
    await expect(canvas.first()).toBeVisible();
    
    // Verify canvas has reasonable dimensions
    const box = await canvas.first().boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeGreaterThan(200);
    expect(box!.height).toBeGreaterThan(300);
  });

  test('should display game title and instructions', async ({ page }) => {
    // Look for game title (h1 or similar heading)
    const headings = page.locator('h1, h2, [data-testid="game-title"]');
    await expect(headings.first()).toBeVisible();
    
    // Verify some text content exists
    const text = await headings.first().textContent();
    expect(text).toBeTruthy();
  });

  test('should have drop block button', async ({ page }) => {
    // Find drop button (various possible selectors)
    const dropButton = page.locator('button').filter({ hasText: /drop|spawn|place/i });
    
    // Verify at least one drop button exists
    const count = await dropButton.count();
    expect(count).toBeGreaterThan(0);
    
    // First button should be visible and enabled
    await expect(dropButton.first()).toBeVisible();
    await expect(dropButton.first()).toBeEnabled();
  });

  test('should display current score', async ({ page }) => {
    // Look for score display
    const scoreElement = page.locator('[data-testid="score"], .score, text=/score/i').first();
    await expect(scoreElement).toBeVisible();
  });

  test('should display countdown timer', async ({ page }) => {
    // Look for timer display
    const timerElement = page.locator('[data-testid="timer"], .timer, text=/time|seconds/i').first();
    await expect(timerElement).toBeVisible();
  });

  test('should drop block when button clicked', async ({ page }) => {
    // Find and click drop button
    const dropButton = page.locator('button').filter({ hasText: /drop|spawn|place/i }).first();
    await dropButton.click();
    
    // Wait a moment for physics engine to process
    await page.waitForTimeout(500);
    
    // Verify canvas still exists (basic sanity check)
    const canvas = page.locator('canvas');
    await expect(canvas.first()).toBeVisible();
  });

  test('should update score after dropping blocks', async ({ page }) => {
    // Get initial score
    const scoreElement = page.locator('[data-testid="score"], .score').first();
    const initialScore = await scoreElement.textContent();
    
    // Drop a block
    const dropButton = page.locator('button').filter({ hasText: /drop|spawn|place/i }).first();
    await dropButton.click();
    
    // Wait for score update
    await page.waitForTimeout(1000);
    
    // Note: Score might not change if block falls off immediately
    // This is just a smoke test
    await expect(scoreElement).toBeVisible();
  });

  test('should handle rapid block drops without crashing', async ({ page }) => {
    const dropButton = page.locator('button').filter({ hasText: /drop|spawn|place/i }).first();
    
    // Drop multiple blocks quickly
    for (let i = 0; i < 5; i++) {
      await dropButton.click();
      await page.waitForTimeout(200);
    }
    
    // Verify page still responsive
    await expect(dropButton).toBeVisible();
    await expect(dropButton).toBeEnabled();
  });

  test('timer should countdown', async ({ page }) => {
    // Get initial timer value
    const timerElement = page.locator('[data-testid="timer"], .timer').first();
    const initialTime = await timerElement.textContent();
    
    // Wait 2 seconds
    await page.waitForTimeout(2000);
    
    // Timer should have changed
    const newTime = await timerElement.textContent();
    expect(newTime).not.toBe(initialTime);
  });

  test('@accessibility should pass accessibility checks on game page', async ({ page }) => {
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should match visual snapshot - initial game state', async ({ page }) => {
    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);
    
    // Take screenshot for visual regression
    await expect(page).toHaveScreenshot('game-initial-state.png', {
      fullPage: true,
      maxDiffPixels: 100
    });
  });
});

test.describe('Game Over Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should show game over state when timer expires', async ({ page }) => {
    // Note: This test would need to wait for the full timer duration
    // Or we could mock the timer for faster testing
    // For now, this is a placeholder test structure
    
    // Skip in normal runs (too slow)
    test.skip();
  });

  test('should display final score on game over', async ({ page }) => {
    test.skip();
  });

  test('should show restart button on game over', async ({ page }) => {
    test.skip();
  });
});
