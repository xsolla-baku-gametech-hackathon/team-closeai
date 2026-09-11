import assert from 'node:assert';
import { evaluateActual, predictCircular, evaluateQuinticError } from './simulation.js';

export function runTests() {
  console.log("Running Core Mathematical Integrity Tests...");
  
  // 1. Circular prediction midpoint check
  const s0 = { x: 0, y: 0, vx: 10, vy: 0, ax: 0, ay: 10 };
  const s1 = predictCircular(s0, Math.PI / 2);
  assert(Math.abs(s1.x - 10) < 1e-4, 'Circular prediction X failed');
  assert(Math.abs(s1.y - 10) < 1e-4, 'Circular prediction Y failed');
  
  // 2. C2 Continuity Derivative Tests for Quintic Reconciliation
  const T = 2.0;
  const e0 = 10, v0 = -5, a0 = 2;
  
  const dt = 1e-5;
  const ds = dt / T;
  
  // End point s=0
  const val0 = evaluateQuinticError(e0, v0, a0, 0, T);
  const val1 = evaluateQuinticError(e0, v0, a0, ds, T);
  const val2 = evaluateQuinticError(e0, v0, a0, 2*ds, T);
  
  const approx_v0 = (val1 - val0) / dt;
  const approx_a0 = (val2 - 2*val1 + val0) / (dt * dt);
  
  assert(Math.abs(val0 - e0) < 1e-4, 'Mismatch at s=0 for Position e0');
  assert(Math.abs(approx_v0 - v0) < 1e-2, `Mismatch at s=0 for Velocity v0: expected ${v0}, got ${approx_v0.toFixed(4)}`);
  assert(Math.abs(approx_a0 - a0) < 1e-2, `Mismatch at s=0 for Acceleration a0: expected ${a0}, got ${approx_a0.toFixed(4)}`);
  
  // End point s=1
  const valN0 = evaluateQuinticError(e0, v0, a0, 1 - 2*ds, T);
  const valN1 = evaluateQuinticError(e0, v0, a0, 1 - ds, T);
  const valN2 = evaluateQuinticError(e0, v0, a0, 1, T);
  
  const approx_v1 = (valN2 - valN1) / dt;
  const approx_a1 = (valN2 - 2*valN1 + valN0) / (dt * dt);
  
  assert(Math.abs(valN2 - 0) < 1e-4, 'Mismatch at s=1 for Position (target 0)');
  assert(Math.abs(approx_v1 - 0) < 1e-2, `Mismatch at s=1 for Velocity (target 0): got ${approx_v1.toFixed(4)}`);
  assert(Math.abs(approx_a1 - 0) < 1e-2, `Mismatch at s=1 for Acceleration (target 0): got ${approx_a1.toFixed(4)}`);
  
  console.log("SUCCESS: Quintic reconciliation rigorously maintains C2 continuity (Position, Velocity, Acceleration bounds).");
  console.log("SUCCESS: All mathematical core tests passed.");
}

runTests();
