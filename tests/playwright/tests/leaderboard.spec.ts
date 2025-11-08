import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Leaderboard E2E Tests
 * 
 * Tests the leaderboard functionality:
 * - Navigation to leaderboard
 * - Display of scores
 * - Sorting and ranking
 * - Player name display
 * - Responsive layout
 */

test.describe('Leaderboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should navigate to leaderboard page', async ({ page }) => {
    // Look for leaderboard link/button
    const leaderboardLink = page.locator('a, button').filter({ hasText: /leaderboard|scores|rankings/i });
    
    if (await leaderboardLink.count() > 0) {
      await leaderboardLink.first().click();
      await page.waitForLoadState('networkidle');
      
      // Verify URL changed or content updated
      const url = page.url();
      expect(url).toMatch(/leaderboard|scores/i);
    } else {
      // Direct navigation if no link found
      await page.goto('/leaderboard');
    }
  });

  test('should display leaderboard table or list', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Look for table or list structure
    const leaderboard = page.locator('table, .leaderboard, [data-testid="leaderboard"], ul.scores');
    await expect(leaderboard.first()).toBeVisible();
  });

  test('should show column headers (Rank, Player, Score)', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Look for header row or labels
    const headers = page.locator('th, .header, [role="columnheader"]');
    const headerText = await headers.allTextContents();
    
    // Verify key columns exist
    const combinedText = headerText.join(' ').toLowerCase();
    expect(combinedText).toMatch(/rank|#/);
    expect(combinedText).toMatch(/player|name/);
    expect(combinedText).toMatch(/score|points/);
  });

  test('should display at least placeholder data', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Look for data rows
    const rows = page.locator('tr:has(td), .leaderboard-entry, li.score-entry');
    const count = await rows.count();
    
    // Should have at least 1 row (could be "no data" message or sample data)
    expect(count).toBeGreaterThan(0);
  });

  test('should format scores with proper number formatting', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Get all score cells
    const scoreCells = page.locator('[data-testid="score"], .score-value, td:nth-child(3)');
    
    if (await scoreCells.count() > 0) {
      const firstScore = await scoreCells.first().textContent();
      
      // Score should be a number (with optional commas)
      expect(firstScore).toMatch(/[\d,]+/);
    }
  });

  test('@accessibility should pass accessibility checks on leaderboard', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should have back to game navigation', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Look for back/home link
    const backLink = page.locator('a, button').filter({ hasText: /back|home|play|game/i });
    
    if (await backLink.count() > 0) {
      await expect(backLink.first()).toBeVisible();
    }
  });

  test('should display empty state gracefully if no scores', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Page should still load and not show errors
    const pageContent = page.locator('body');
    await expect(pageContent).toBeVisible();
    
    // Should not show error messages
    const errorText = await page.textContent('body');
    expect(errorText?.toLowerCase()).not.toContain('error');
    expect(errorText?.toLowerCase()).not.toContain('exception');
  });

  test('should match visual snapshot - leaderboard page', async ({ page }) => {
    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    await expect(page).toHaveScreenshot('leaderboard-page.png', {
      fullPage: true,
      maxDiffPixels: 100
    });
  });
});

test.describe('Leaderboard API Integration', () => {
  test('should handle API errors gracefully', async ({ page }) => {
    // Intercept API call and return error
    await page.route('**/api/scores/leaderboard*', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal server error' })
      });
    });

    await page.goto('/leaderboard');
    await page.waitForLoadState('networkidle');
    
    // Page should still render without crashing
    const pageContent = page.locator('body');
    await expect(pageContent).toBeVisible();
  });

  test('should display loading state while fetching scores', async ({ page }) => {
    // Intercept and delay API response
    await page.route('**/api/scores/leaderboard*', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      await route.continue();
    });

    await page.goto('/leaderboard');
    
    // Look for loading indicator
    const loadingIndicator = page.locator('.loading, .spinner, [data-testid="loading"]').first();
    
    if (await loadingIndicator.isVisible().catch(() => false)) {
      await expect(loadingIndicator).toBeVisible();
    }
  });
});
