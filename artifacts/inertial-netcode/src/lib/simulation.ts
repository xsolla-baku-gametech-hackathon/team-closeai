export interface SimState {
  x: number;
  y: number;
  vx: number;
  vy: number;
  ax: number;
  ay: number;
}

export function evaluateActual(t: number, A_x: number, A_y: number, omega_x: number, omega_y: number): SimState {
  return {
    x: A_x * Math.sin(omega_x * t),
    y: A_y * Math.sin(omega_y * t),
    vx: A_x * omega_x * Math.cos(omega_x * t),
    vy: A_y * omega_y * Math.cos(omega_y * t),
    ax: -A_x * omega_x * omega_x * Math.sin(omega_x * t),
    ay: -A_y * omega_y * omega_y * Math.sin(omega_y * t),
  };
}

export function predictCircular(state0: SimState, dt: number): SimState {
  const speedSq = state0.vx * state0.vx + state0.vy * state0.vy;
  if (speedSq < 1e-4) {
    return {
      x: state0.x + state0.vx * dt,
      y: state0.y + state0.vy * dt,
      vx: state0.vx,
      vy: state0.vy,
      ax: 0,
      ay: 0,
    };
  }
  
  const speed = Math.sqrt(speedSq);
  const cross = state0.vx * state0.ay - state0.vy * state0.ax;
  const omega = cross / speedSq;
  
  if (Math.abs(omega) < 1e-4) {
     return {
      x: state0.x + state0.vx * dt + 0.5 * state0.ax * dt * dt,
      y: state0.y + state0.vy * dt + 0.5 * state0.ay * dt * dt,
      vx: state0.vx + state0.ax * dt,
      vy: state0.vy + state0.ay * dt,
      ax: state0.ax,
      ay: state0.ay,
    };
  }

  const angle0 = Math.atan2(state0.vy, state0.vx);
  const angleT = angle0 + omega * dt;
  
  const R = speed / omega;
  const cx = state0.x - (state0.vy / omega);
  const cy = state0.y + (state0.vx / omega);
  
  return {
    x: cx + R * Math.sin(angleT),
    y: cy - R * Math.cos(angleT),
    vx: speed * Math.cos(angleT),
    vy: speed * Math.sin(angleT),
    ax: -speed * omega * Math.sin(angleT),
    ay: speed * omega * Math.cos(angleT),
  };
}

export function evaluateQuinticError(e0: number, ev0: number, ea0: number, s: number, T: number): number {
  if (s >= 1) return 0;
  if (s <= 0) return e0;
  
  const c0 = e0;
  const c1 = ev0 * T;
  const c2 = 0.5 * ea0 * T * T;
  
  const c3 = -10 * c0 - 6 * c1 - 3 * c2;
  const c4 = 15 * c0 + 8 * c1 + 3 * c2;
  const c5 = -6 * c0 - 3 * c1 - c2;
  
  const s2 = s * s;
  const s3 = s2 * s;
  const s4 = s3 * s;
  const s5 = s4 * s;
  
  return c0 + c1 * s + c2 * s2 + c3 * s3 + c4 * s4 + c5 * s5;
}
