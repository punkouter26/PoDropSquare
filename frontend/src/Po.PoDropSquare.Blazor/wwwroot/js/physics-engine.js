/**
 * PoDropSquare Physics Engine
 * Matter.js integration for Blazor WebAssembly
 */

// Global variables for the physics engine
let engine = null;
let world = null;
let render = null;
let runner = null;
let canvas = null;
let gameBlocks = [];
let boundaries = [];
let goalLine = null;
let isRunning = false;

// Danger countdown state
let dangerCountdownActive = false;
let dangerCountdownStartTime = null;
let lastDangerCheck = 0;
let dangerCountdownInterval = null;

let performanceStats = {
    fps: 60,
    frameTime: 0,
    lastFrameTime: 0,
    frameCount: 0
};

// Collision detection callbacks
let gameCallbacks = {
    onGoalLineCrossed: null,
    onBlockCollision: null,
    onScoreUpdate: null,
    onGameStateChange: null
};

// Physics constants
const PHYSICS_CONFIG = {
    worldWidth: 300,
    worldHeight: 200,
    goalLineY: 40,  // Proportionally adjusted (was 100 for 600px height, now 40 for 200px)
    gravity: { x: 0, y: 0.8 },
    blockSize: 20,  // Reduced from 40 to fit smaller canvas
    wallThickness: 10,  // Reduced from 20 to fit smaller canvas
    targetFPS: 60,
    dangerCountdownTime: 2000, // 2 seconds in milliseconds
    maxBlockCount: 100,
    performanceThreshold: 30 // Minimum FPS before performance warning
};

/**
 * Initialize the Matter.js physics engine with enhanced world setup
 * @param {string} canvasId - The ID of the HTML canvas element
 * @param {object} callbacks - Game callback functions
 */
window.initializePhysics = function(canvasId, callbacks) {
    try {
        // Store callbacks for later use
        if (callbacks) {
            gameCallbacks = { ...gameCallbacks, ...callbacks };
        }

        // Create engine with optimized settings
        engine = Matter.Engine.create({
            enableSleeping: true,
            positionIterations: 6,
            velocityIterations: 4,
            constraintIterations: 2
        });
        world = engine.world;
        
        // Set gravity
        engine.world.gravity.x = PHYSICS_CONFIG.gravity.x;
        engine.world.gravity.y = PHYSICS_CONFIG.gravity.y;
        
        // Get canvas element
        canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Canvas element not found:', canvasId);
            return false;
        }
        
        // Set canvas size
        canvas.width = PHYSICS_CONFIG.worldWidth;
        canvas.height = PHYSICS_CONFIG.worldHeight;
        
        // Create renderer with enhanced options
        render = Matter.Render.create({
            canvas: canvas,
            engine: engine,
            options: {
                width: PHYSICS_CONFIG.worldWidth,
                height: PHYSICS_CONFIG.worldHeight,
                wireframes: false,
                background: '#1a1a2e',
                showAngleIndicator: false,
                showVelocity: false,
                showDebug: false,
                showBroadphase: false,
                pixelRatio: window.devicePixelRatio || 1
            }
        });
        
        // Create boundaries (walls and floor)
        createBoundaries();
        
        // Create goal line
        createGoalLine();
        
        // Setup collision detection
        setupCollisionDetection();
        
        // Start the renderer
        Matter.Render.run(render);
        
        // Create custom runner for 60 FPS with performance monitoring
        startPhysicsLoop();
        
        isRunning = true;
        console.log('Physics engine initialized successfully with enhanced features');
        
        // Notify game state change
        if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
            window.PhysicsInteropService.OnGameStateChange('initialized');
        } else if (gameCallbacks.onGameStateChange) {
            gameCallbacks.onGameStateChange('initialized');
        }
        
        return true;
    } catch (error) {
        console.error('Failed to initialize physics engine:', error);
        return false;
    }
};

/**
 * Create world boundaries (walls and floor)
 */
function createBoundaries() {
    const walls = [
        // Floor
        Matter.Bodies.rectangle(
            PHYSICS_CONFIG.worldWidth / 2, 
            PHYSICS_CONFIG.worldHeight - PHYSICS_CONFIG.wallThickness / 2, 
            PHYSICS_CONFIG.worldWidth, 
            PHYSICS_CONFIG.wallThickness, 
            { isStatic: true, render: { fillStyle: '#2d2d54' } }
        ),
        // Left wall
        Matter.Bodies.rectangle(
            PHYSICS_CONFIG.wallThickness / 2, 
            PHYSICS_CONFIG.worldHeight / 2, 
            PHYSICS_CONFIG.wallThickness, 
            PHYSICS_CONFIG.worldHeight, 
            { isStatic: true, render: { fillStyle: '#2d2d54' } }
        ),
        // Right wall
        Matter.Bodies.rectangle(
            PHYSICS_CONFIG.worldWidth - PHYSICS_CONFIG.wallThickness / 2, 
            PHYSICS_CONFIG.worldHeight / 2, 
            PHYSICS_CONFIG.wallThickness, 
            PHYSICS_CONFIG.worldHeight, 
            { isStatic: true, render: { fillStyle: '#2d2d54' } }
        )
    ];
    
    boundaries = walls;
    Matter.World.add(world, walls);
}

/**
 * Setup collision detection system
 */
function setupCollisionDetection() {
    // Collision start event
    Matter.Events.on(engine, 'collisionStart', function(event) {
        const pairs = event.pairs;
        
        for (let i = 0; i < pairs.length; i++) {
            const { bodyA, bodyB } = pairs[i];
            
            // Check for goal line collision
            if ((bodyA === goalLine && bodyB.blockId) || (bodyB === goalLine && bodyA.blockId)) {
                const block = bodyA === goalLine ? bodyB : bodyA;
                console.log('Block crossed goal line:', block.blockId);
                
                // Use Blazor callback if available, otherwise use direct callback
                if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
                    window.PhysicsInteropService.OnGoalLineCrossed(block.blockId);
                } else if (gameCallbacks.onGoalLineCrossed) {
                    gameCallbacks.onGoalLineCrossed(block.blockId);
                }
            }
            
            // Check for block-to-block collision
            if (bodyA.blockId && bodyB.blockId) {
                // Use Blazor callback if available, otherwise use direct callback
                if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
                    window.PhysicsInteropService.OnBlockCollision(bodyA.blockId, bodyB.blockId);
                } else if (gameCallbacks.onBlockCollision) {
                    gameCallbacks.onBlockCollision(bodyA.blockId, bodyB.blockId);
                }
            }
        }
    });
    
    // Before update event for performance monitoring
    Matter.Events.on(engine, 'beforeUpdate', function() {
        performanceStats.lastFrameTime = performance.now();
    });
    
    // After update event for performance monitoring
    Matter.Events.on(engine, 'afterUpdate', function() {
        const currentTime = performance.now();
        performanceStats.frameTime = currentTime - performanceStats.lastFrameTime;
        performanceStats.frameCount++;
        
        // Check danger countdown every few frames to avoid performance issues
        if (performanceStats.frameCount % 30 === 0) { // Check every 30 frames (~0.5 seconds)
            checkDangerCountdown();
        }
        
        // Calculate FPS every 60 frames
        if (performanceStats.frameCount % 60 === 0) {
            performanceStats.fps = Math.round(1000 / performanceStats.frameTime);
            
            // Performance warning
            if (performanceStats.fps < PHYSICS_CONFIG.performanceThreshold) {
                console.warn('Physics performance warning: FPS dropped to', performanceStats.fps);
                
                const perfData = {
                    fps: performanceStats.fps,
                    blockCount: gameBlocks.length
                };
                
                // Use Blazor callback if available, otherwise use direct callback
                if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
                    window.PhysicsInteropService.OnGameStateChange('performance_warning', perfData);
                } else if (gameCallbacks.onGameStateChange) {
                    gameCallbacks.onGameStateChange('performance_warning', perfData);
                }
            }
        }
    });
}

/**
 * Start the physics loop with 60 FPS requestAnimationFrame
 */
function startPhysicsLoop() {
    let lastTime = 0;
    const targetDelta = 1000 / PHYSICS_CONFIG.targetFPS; // 16.67ms for 60 FPS
    
    function physicsStep(currentTime) {
        if (!isRunning) return;
        
        const deltaTime = currentTime - lastTime;
        
        if (deltaTime >= targetDelta) {
            // Update the engine
            Matter.Engine.update(engine, targetDelta);
            lastTime = currentTime - (deltaTime % targetDelta);
        }
        
        // Continue the loop
        requestAnimationFrame(physicsStep);
    }
    
    // Start the loop
    requestAnimationFrame(physicsStep);
    console.log('60 FPS physics loop started');
}

/**
 * Create goal line indicator
 */
function createGoalLine() {
    goalLine = Matter.Bodies.rectangle(
        PHYSICS_CONFIG.worldWidth / 2,
        PHYSICS_CONFIG.goalLineY,
        PHYSICS_CONFIG.worldWidth - (PHYSICS_CONFIG.wallThickness * 2),
        2,
        { 
            isStatic: true, 
            isSensor: true,
            render: { fillStyle: '#ff4757' },
            label: 'goalLine'
        }
    );
    
    Matter.World.add(world, goalLine);
}

/**
 * Create a new block at specified position with enhanced physics properties
 * @param {number} x - X coordinate
 * @param {number} y - Y coordinate
 * @param {string} color - Block color
 * @param {object} options - Additional block options
 * @returns {string} Block ID
 */
window.createBlock = function(x, y, color, options = {}) {
    try {
        // Validate input parameters
        if (typeof x !== 'number' || typeof y !== 'number') {
            throw new Error('Invalid coordinates provided');
        }
        
        if (gameBlocks.length >= PHYSICS_CONFIG.maxBlockCount) {
            console.warn('Maximum block count reached');
            return null;
        }
        
        const blockId = 'block_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        
        // Enhanced physics properties
        const blockOptions = {
            render: { fillStyle: color || '#4834d4' },
            restitution: options.restitution || 0.2,      // Bounciness
            friction: options.friction || 0.8,           // Surface friction
            frictionAir: options.frictionAir || 0.01,    // Air resistance
            density: options.density || 0.001,           // Mass density
            sleepThreshold: 60,                          // Sleep after being still
            ...options.physicsOptions
        };
        
        const block = Matter.Bodies.rectangle(
            x, 
            y, 
            options.width || PHYSICS_CONFIG.blockSize, 
            options.height || PHYSICS_CONFIG.blockSize, 
            blockOptions
        );
        
        // Add custom properties
        block.blockId = blockId;
        block.color = color || '#4834d4';
        block.createdAt = Date.now();
        block.label = 'gameBlock';
        
        gameBlocks.push(block);
        Matter.World.add(world, block);
        
        console.log('Created block:', blockId, 'at', x, y, 'Total blocks:', gameBlocks.length);
        
        // Notify score update if callback exists
        const scoreData = {
            action: 'block_created',
            blockId: blockId,
            totalBlocks: gameBlocks.length
        };
        
        // Use Blazor callback if available, otherwise use direct callback
        if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
            window.PhysicsInteropService.OnScoreUpdate(scoreData);
        } else if (gameCallbacks.onScoreUpdate) {
            gameCallbacks.onScoreUpdate(scoreData);
        }
        
        return blockId;
    } catch (error) {
        console.error('Failed to create block:', error);
        return null;
    }
};

/**
 * Remove a block from the world
 * @param {string} blockId - Block ID to remove
 */
window.removeBlock = function(blockId) {
    try {
        const blockIndex = gameBlocks.findIndex(b => b.blockId === blockId);
        if (blockIndex !== -1) {
            const block = gameBlocks[blockIndex];
            Matter.World.remove(world, block);
            gameBlocks.splice(blockIndex, 1);
            console.log('Removed block:', blockId);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Failed to remove block:', error);
        return false;
    }
};

/**
 * Get all block positions and states
 * @returns {Array} Array of block data
 */
window.getBlockStates = function() {
    try {
        return gameBlocks.map(block => ({
            id: block.blockId,
            x: block.position.x,
            y: block.position.y,
            angle: block.angle,
            color: block.color,
            velocity: {
                x: block.velocity.x,
                y: block.velocity.y
            }
        }));
    } catch (error) {
        console.error('Failed to get block states:', error);
        return [];
    }
};

/**
 * Check if any blocks have crossed the goal line
 * @returns {boolean} True if game over condition met
 */
window.checkGameOver = function() {
    try {
        return gameBlocks.some(block => 
            block.position.y <= PHYSICS_CONFIG.goalLineY
        );
    } catch (error) {
        console.error('Failed to check game over:', error);
        return false;
    }
};

/**
 * Check for blocks above danger line and manage countdown
 */
function checkDangerCountdown() {
    if (!isRunning) return;
    
    const now = Date.now();
    
    // Check if any block's TOP edge is at or above the goal line
    // Block position.y is the CENTER, so we subtract half the block size
    const halfBlockSize = PHYSICS_CONFIG.blockSize / 2;
    
    // Debug: Log block positions
    const blockPositions = gameBlocks.map(b => {
        const topY = b.position.y - halfBlockSize;
        return {
            id: b.blockId,
            centerY: Math.round(b.position.y),
            topY: Math.round(topY),
            aboveLine: topY <= PHYSICS_CONFIG.goalLineY
        };
    });
    
    const blocksAboveLine = gameBlocks.some(block => {
        const blockTopY = block.position.y - halfBlockSize;
        return blockTopY <= PHYSICS_CONFIG.goalLineY;
    });
    
    // Debug logging
    if (gameBlocks.length > 0 && gameBlocks.length % 30 === 0) { // Log periodically
        console.log('Danger check - Blocks:', blockPositions.length, 
                    'Above line:', blockPositions.filter(b => b.aboveLine).length,
                    'Goal line Y:', PHYSICS_CONFIG.goalLineY,
                    'Countdown active:', dangerCountdownActive,
                    'Sample block:', blockPositions[0]);
    }
    
    if (blocksAboveLine) {
        if (!dangerCountdownActive) {
            // Start danger countdown
            dangerCountdownActive = true;
            dangerCountdownStartTime = now;
            console.log('Danger countdown started!');
            
            // Notify Blazor about countdown start
            if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
                window.PhysicsInteropService.OnDangerCountdownStarted();
            }
            
            // Start countdown interval to update display
            dangerCountdownInterval = setInterval(() => {
                const elapsed = Date.now() - dangerCountdownStartTime;
                const elapsedSeconds = Math.min(elapsed / 1000, 2.0); // Cap at 2.0 seconds
                const displayTime = elapsedSeconds.toFixed(1);
                
                // Notify Blazor about countdown progress (now counting up)
                if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
                    window.PhysicsInteropService.OnDangerCountdownUpdate(parseFloat(displayTime));
                }
                
                if (elapsed >= PHYSICS_CONFIG.dangerCountdownTime) {
                    // Countdown complete - trigger victory
                    clearInterval(dangerCountdownInterval);
                    dangerCountdownInterval = null;
                    dangerCountdownActive = false;
                    console.log('Victory! Block held high for 2 seconds!');
                    
                    if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
                        window.PhysicsInteropService.OnVictoryAchieved();
                    }
                }
            }, 100); // Update every 100ms for smooth countdown
        }
    } else {
        if (dangerCountdownActive) {
            // All blocks are below line - cancel countdown
            dangerCountdownActive = false;
            dangerCountdownStartTime = null;
            if (dangerCountdownInterval) {
                clearInterval(dangerCountdownInterval);
                dangerCountdownInterval = null;
            }
            console.log('Danger countdown cancelled - blocks below line');
            
            // Notify Blazor about countdown cancellation
            if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
                window.PhysicsInteropService.OnDangerCountdownCancelled();
            }
        }
    }
}

/**
 * Get current danger countdown status
 * @returns {object} Countdown status
 */
window.getDangerCountdownStatus = function() {
    if (!dangerCountdownActive) {
        return { active: false, elapsed: 0 };
    }
    
    const elapsed = Date.now() - dangerCountdownStartTime;
    const elapsedSeconds = Math.min(elapsed / 1000, 2.0); // Cap at 2.0 seconds
    return {
        active: true,
        elapsed: elapsedSeconds,
        startTime: dangerCountdownStartTime
    };
};

/**
 * Clear all blocks from the world
 */
window.clearAllBlocks = function() {
    try {
        gameBlocks.forEach(block => {
            Matter.World.remove(world, block);
        });
        gameBlocks = [];
        console.log('Cleared all blocks');
        return true;
    } catch (error) {
        console.error('Failed to clear blocks:', error);
        return false;
    }
};

/**
 * Get comprehensive performance statistics
 * @returns {object} Performance data
 */
window.getPerformanceStats = function() {
    return {
        fps: performanceStats.fps,
        frameTime: performanceStats.frameTime,
        frameCount: performanceStats.frameCount,
        blockCount: gameBlocks.length,
        isRunning: isRunning,
        memoryUsage: performance.memory ? {
            used: Math.round(performance.memory.usedJSHeapSize / 1024 / 1024),
            total: Math.round(performance.memory.totalJSHeapSize / 1024 / 1024),
            limit: Math.round(performance.memory.jsHeapSizeLimit / 1024 / 1024)
        } : null
    };
};

/**
 * Set game callback functions for Blazor interop
 * @param {object} callbacks - Callback functions
 */
window.setGameCallbacks = function(callbacks) {
    gameCallbacks = { ...gameCallbacks, ...callbacks };
    console.log('Game callbacks updated:', Object.keys(callbacks));
};

/**
 * Pause/resume the physics simulation
 * @param {boolean} paused - Whether to pause the simulation
 */
window.pausePhysics = function(paused) {
    isRunning = !paused;
    
    if (paused) {
        console.log('Physics simulation paused');
        if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
            window.PhysicsInteropService.OnGameStateChange('paused');
        } else if (gameCallbacks.onGameStateChange) {
            gameCallbacks.onGameStateChange('paused');
        }
    } else {
        console.log('Physics simulation resumed');
        startPhysicsLoop(); // Restart the loop
        if (window.PhysicsInteropService && window.PhysicsInteropService.dotNetReference) {
            window.PhysicsInteropService.OnGameStateChange('resumed');
        } else if (gameCallbacks.onGameStateChange) {
            gameCallbacks.onGameStateChange('resumed');
        }
    }
    
    return !paused;
};
/**
 * Update physics simulation (legacy function for manual stepping)
 */
window.updatePhysics = function() {
    try {
        // Engine automatically updates via custom loop
        // This function maintained for backward compatibility
        return true;
    } catch (error) {
        console.error('Failed to update physics:', error);
        return false;
    }
};

/**
 * Dispose of the physics engine with proper cleanup
 */
window.disposePhysics = function() {
    try {
        // Stop the physics loop
        isRunning = false;
        
        // Clear all collision events
        if (engine) {
            Matter.Events.off(engine);
        }
        
        // Stop runner if it exists
        if (runner) {
            Matter.Runner.stop(runner);
        }
        
        // Stop renderer
        if (render) {
            Matter.Render.stop(render);
        }
        
        // Clear the engine
        if (engine) {
            Matter.Engine.clear(engine);
        }
        
        // Reset all global variables
        engine = null;
        world = null;
        render = null;
        runner = null;
        canvas = null;
        gameBlocks = [];
        boundaries = [];
        goalLine = null;
        isRunning = false;
        
        // Reset performance stats
        performanceStats = {
            fps: 60,
            frameTime: 0,
            lastFrameTime: 0,
            frameCount: 0
        };
        
        // Reset callbacks
        gameCallbacks = {
            onGoalLineCrossed: null,
            onBlockCollision: null,
            onScoreUpdate: null,
            onGameStateChange: null
        };
        
        console.log('Physics engine disposed completely');
        return true;
    } catch (error) {
        console.error('Failed to dispose physics engine:', error);
        return false;
    }
};

// Export for module systems if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        initializePhysics,
        createBlock,
        removeBlock,
        getBlockStates,
        checkGameOver,
        clearAllBlocks,
        updatePhysics,
        disposePhysics,
        getPerformanceStats,
        setGameCallbacks,
        pausePhysics
    };
}

// Setup for Blazor interop callbacks
window.PhysicsInteropService = {
    dotNetReference: null,
    
    setupCallbacks: function(dotNetRef) {
        this.dotNetReference = dotNetRef;
        console.log('Blazor callbacks setup completed');
    },
    
    OnGoalLineCrossed: function(blockId) {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnGoalLineCrossed', blockId);
        }
    },
    
    OnBlockCollision: function(blockIdA, blockIdB) {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnBlockCollision', blockIdA, blockIdB);
        }
    },
    
    OnScoreUpdate: function(data) {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnScoreUpdate', data);
        }
    },
    
    OnGameStateChange: function(state, data) {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnGameStateChange', state, data);
        }
    },
    
    OnDangerCountdownStarted: function() {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnDangerCountdownStarted');
        }
    },
    
    OnDangerCountdownUpdate: function(remainingSeconds) {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnDangerCountdownUpdate', remainingSeconds);
        }
    },
    
    OnDangerCountdownCancelled: function() {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnDangerCountdownCancelled');
        }
    },
    
    OnDangerGameOver: function() {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnDangerGameOver');
        }
    },
    
    OnVictoryAchieved: function() {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('OnVictoryAchieved');
        }
    }
};

// Game control functions for Blazor interop
window.resetGame = function() {
    console.log('Resetting game...');
    clearAllBlocks();
    isRunning = false;
    
    // Reset danger countdown state
    dangerCountdownActive = false;
    dangerCountdownStartTime = null;
    if (dangerCountdownInterval) {
        clearInterval(dangerCountdownInterval);
        dangerCountdownInterval = null;
    }
};

window.startGame = function() {
    console.log('Starting game...');
    isRunning = true;
    if (runner) {
        Matter.Runner.start(runner, engine);
    }
};

window.pauseGame = function() {
    console.log('Pausing game...');
    isRunning = false;
    if (runner) {
        Matter.Runner.stop(runner);
    }
};

window.resumeGame = function() {
    console.log('Resuming game...');
    isRunning = true;
    if (runner) {
        Matter.Runner.start(runner, engine);
    }
};

window.stopGame = function() {
    console.log('Stopping game...');
    isRunning = false;
    if (runner) {
        Matter.Runner.stop(runner);
    }
    
    // Stop danger countdown
    dangerCountdownActive = false;
    dangerCountdownStartTime = null;
    if (dangerCountdownInterval) {
        clearInterval(dangerCountdownInterval);
        dangerCountdownInterval = null;
    }
};

// Game input functions
window.moveBlockLeft = function() {
    console.log('Move block left (not implemented yet)');
    // TODO: Implement block movement logic
};

window.moveBlockRight = function() {
    console.log('Move block right (not implemented yet)');
    // TODO: Implement block movement logic
};

window.dropBlockFast = function() {
    console.log('Drop block fast (not implemented yet)');
    // TODO: Implement fast drop logic
};

window.dropBlockAtPosition = function(x, y) {
    console.log(`Drop block at position: (${x}, ${y})`);
    // Create a standardized square block at the specified position
    // Use consistent size and color for simplicity
    createBlock(x, y, '#ffffff', {
        width: PHYSICS_CONFIG.blockSize,
        height: PHYSICS_CONFIG.blockSize,
        restitution: 0.3,
        friction: 0.8,
        density: 0.002
    });
};

window.cleanup = function() {
    console.log('Cleaning up physics engine...');
    disposePhysics();
};

// Set callbacks using the Blazor naming convention
window.setPhysicsCallbacks = function(dotNetRef) {
    window.setGameCallbacks(dotNetRef);
};