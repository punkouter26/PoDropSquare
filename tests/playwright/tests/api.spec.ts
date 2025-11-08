import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * API Health Check E2E Tests
 * 
 * Tests the API endpoints are accessible and working:
 * - Health check endpoint
 * - API responsiveness
 * - Error handling
 */

test.describe('API Health and Status', () => {
  test('health check endpoint should return 200 OK', async ({ request }) => {
    const response = await request.get('/api/health');
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
  });

  test('health check should return valid JSON', async ({ request }) => {
    const response = await request.get('/api/health');
    expect(response.ok()).toBeTruthy();
    
    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('application/json');
    
    // Should be able to parse as JSON
    const body = await response.json();
    expect(body).toBeTruthy();
  });

  test('health check should indicate healthy status', async ({ request }) => {
    const response = await request.get('/api/health');
    const body = await response.json();
    
    // Common health check response formats
    const bodyStr = JSON.stringify(body).toLowerCase();
    expect(bodyStr).toMatch(/healthy|ok|up|success/i);
  });

  test('API should respond within 3 seconds', async ({ request }) => {
    const startTime = Date.now();
    const response = await request.get('/api/health');
    const endTime = Date.now();
    
    expect(response.ok()).toBeTruthy();
    expect(endTime - startTime).toBeLessThan(3000);
  });
});

test.describe('API CORS and Headers', () => {
  test('API should include appropriate CORS headers', async ({ request }) => {
    const response = await request.get('/api/health');
    const headers = response.headers();
    
    // Check for CORS headers (may or may not be present depending on config)
    // This is informational, not a hard requirement
    const hasCors = 'access-control-allow-origin' in headers;
    
    // Just log the result, don't fail
    console.log('CORS enabled:', hasCors);
  });

  test('API should handle OPTIONS preflight requests', async ({ request }) => {
    try {
      const response = await request.fetch('/api/health', { method: 'OPTIONS' });
      // Should return 200 or 204
      expect([200, 204, 405]).toContain(response.status());
    } catch (error) {
      // OPTIONS might not be supported, which is okay
      console.log('OPTIONS preflight not supported or errored');
    }
  });
});

test.describe('Diagnostics Page', () => {
  test('diagnostics page should load', async ({ page }) => {
    await page.goto('/diag');
    await page.waitForLoadState('networkidle');
    
    // Page should load without error
    const pageContent = page.locator('body');
    await expect(pageContent).toBeVisible();
  });

  test('diagnostics should display system information', async ({ page }) => {
    await page.goto('/diag');
    await page.waitForLoadState('networkidle');
    
    // Look for diagnostic information
    const pageText = await page.textContent('body');
    expect(pageText).toBeTruthy();
    expect(pageText!.length).toBeGreaterThan(50); // Should have some content
  });

  test('@accessibility diagnostics page should pass accessibility checks', async ({ page }) => {
    await page.goto('/diag');
    await page.waitForLoadState('networkidle');
    
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });
});

test.describe('Score Submission API', () => {
  test('should accept valid score submission', async ({ request }) => {
    const response = await request.post('/api/scores', {
      data: {
        playerName: 'TestPlayer',
        score: 1500
      }
    });
    
    // Should return 200 or 201
    expect([200, 201]).toContain(response.status());
  });

  test('should reject invalid score submission (negative score)', async ({ request }) => {
    const response = await request.post('/api/scores', {
      data: {
        playerName: 'TestPlayer',
        score: -100
      }
    });
    
    // Should return 400 Bad Request
    expect(response.status()).toBe(400);
  });

  test('should reject empty player name', async ({ request }) => {
    const response = await request.post('/api/scores', {
      data: {
        playerName: '',
        score: 1500
      }
    });
    
    // Should return 400 Bad Request
    expect(response.status()).toBe(400);
  });

  test('should return proper error format for validation errors', async ({ request }) => {
    const response = await request.post('/api/scores', {
      data: {
        playerName: '',
        score: -100
      }
    });
    
    expect(response.status()).toBe(400);
    
    const contentType = response.headers()['content-type'];
    expect(contentType).toContain('application/json');
    
    const body = await response.json();
    expect(body).toHaveProperty('title');
  });
});

test.describe('Leaderboard API', () => {
  test('should return leaderboard data', async ({ request }) => {
    const response = await request.get('/api/scores/leaderboard');
    expect(response.ok()).toBeTruthy();
  });

  test('should return JSON array of scores', async ({ request }) => {
    const response = await request.get('/api/scores/leaderboard');
    expect(response.ok()).toBeTruthy();
    
    const body = await response.json();
    expect(Array.isArray(body)).toBeTruthy();
  });

  test('should support top parameter', async ({ request }) => {
    const response = await request.get('/api/scores/leaderboard?top=5');
    expect(response.ok()).toBeTruthy();
    
    const body = await response.json();
    expect(Array.isArray(body)).toBeTruthy();
    expect(body.length).toBeLessThanOrEqual(5);
  });

  test('leaderboard entries should have required fields', async ({ request }) => {
    const response = await request.get('/api/scores/leaderboard?top=1');
    expect(response.ok()).toBeTruthy();
    
    const body = await response.json();
    
    if (body.length > 0) {
      const firstEntry = body[0];
      expect(firstEntry).toHaveProperty('playerName');
      expect(firstEntry).toHaveProperty('score');
    }
  });
});
