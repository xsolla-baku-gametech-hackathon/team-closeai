import React, { useEffect, useRef, useState, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { Play, Pause, SkipForward, RotateCcw } from 'lucide-react';
import { evaluateActual, predictCircular, evaluateQuinticError, SimState } from '@/lib/simulation';

type SimNetworkState = 'NORMAL' | 'PACKET_LOSS' | 'RECONCILING';

class SimulatorState {
  networkState: SimNetworkState = 'NORMAL';
  t_loss: number = 0;
  t_restore: number = 0;
  simTime: number = 0;
  last_t: number = 0;
  isPaused: boolean = false;
  
  stateAtLoss: SimState = { x: 0, y: 0, vx: 0, vy: 0, ax: 0, ay: 0 };
  
  errX: number = 0; errVx: number = 0; errAx: number = 0;
  errY: number = 0; errVy: number = 0; errAy: number = 0;

  radiusAtLoss: number = 0;

  packets: { x: number; y: number; time: number; alpha: number }[] = [];
  last_packet_time: number = 0;

  scale: number = 1.0;
  
  A_x = 240;
  A_y = 120;
  omega_x = 0.7;
  omega_y = 1.4;
  baseRadius = 4;
}

export function HeroSimulator() {
  const containerRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [uiState, setUiState] = useState({
    networkState: 'NORMAL' as SimNetworkState,
    predictionAge: 0,
    uncertaintyRadius: 0,
    isPaused: false,
    isReducedMotion: false
  });
  
  const stateRef = useRef(new SimulatorState());

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    const updateMotion = (e: MediaQueryList | MediaQueryListEvent) => {
      setUiState(prev => {
        if (prev.isReducedMotion === e.matches) return prev;
        const st = stateRef.current;
        st.isPaused = e.matches; 
        return { ...prev, isReducedMotion: e.matches, isPaused: st.isPaused };
      });
    };
    updateMotion(mediaQuery);
    const handler = (e: MediaQueryListEvent) => updateMotion(e);
    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, []);

  const togglePause = useCallback(() => {
    const st = stateRef.current;
    st.isPaused = !st.isPaused;
    setUiState(prev => ({ ...prev, isPaused: st.isPaused }));
  }, []);

  const handleStep = useCallback(() => {
    const st = stateRef.current;
    if (st.isPaused) {
      st.simTime += 0.1;
    }
  }, []);

  const handleReset = useCallback(() => {
    const st = stateRef.current;
    st.simTime = 0;
    st.last_t = 0;
    st.networkState = 'NORMAL';
    st.packets = [];
    st.last_packet_time = 0;
    st.scale = 1.0;
    st.radiusAtLoss = 0;
    setUiState(prev => ({ ...prev, networkState: 'NORMAL', predictionAge: 0, uncertaintyRadius: (st.baseRadius * 0.1) }));
  }, []);

  const togglePacketLoss = useCallback(() => {
    const st = stateRef.current;
    if (st.networkState === 'RECONCILING') return;

    if (st.networkState === 'NORMAL') {
      st.networkState = 'PACKET_LOSS';
      st.t_loss = st.simTime;
      st.stateAtLoss = evaluateActual(st.t_loss, st.A_x, st.A_y, st.omega_x, st.omega_y);
      setUiState(prev => ({ ...prev, networkState: 'PACKET_LOSS' }));
    } else {
      st.networkState = 'RECONCILING';
      st.t_restore = st.simTime;
      
      const loss_dt = st.t_restore - st.t_loss;
      const predicted = predictCircular(st.stateAtLoss, loss_dt);
      const actual = evaluateActual(st.t_restore, st.A_x, st.A_y, st.omega_x, st.omega_y);
      
      st.errX = predicted.x - actual.x;
      st.errVx = predicted.vx - actual.vx;
      st.errAx = predicted.ax - actual.ax;
      
      st.errY = predicted.y - actual.y;
      st.errVy = predicted.vy - actual.vy;
      st.errAy = predicted.ay - actual.ay;
      
      setUiState(prev => ({ ...prev, networkState: 'RECONCILING' }));
    }
  }, []);

  useEffect(() => {
    const canvas = canvasRef.current;
    const container = containerRef.current;
    if (!canvas || !container) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const resizeObserver = new ResizeObserver(() => {
      const rect = container.getBoundingClientRect();
      const dpr = window.devicePixelRatio || 1;
      canvas.width = rect.width * dpr;
      canvas.height = rect.height * dpr;
      canvas.style.width = `${rect.width}px`;
      canvas.style.height = `${rect.height}px`;
      
      // Force an immediate render tick to fix scale without waiting for unpaused dt
      if (stateRef.current.isPaused) {
         renderTick(0.016);
      }
    });
    resizeObserver.observe(container);

    let animationFrameId: number;
    let lastRealTime = performance.now() / 1000;

    const renderTick = (forcedRealDt?: number) => {
      const currentRealTime = performance.now() / 1000;
      let realDt = forcedRealDt ?? (currentRealTime - lastRealTime);
      if (realDt > 0.1) realDt = 0.1;
      if (!forcedRealDt) lastRealTime = currentRealTime;
      
      const st = stateRef.current;
      
      if (!st.isPaused) {
        st.simTime += realDt;
      }

      const t = st.simTime;
      const logicalDt = t - st.last_t;
      st.last_t = t;

      const act = evaluateActual(t, st.A_x, st.A_y, st.omega_x, st.omega_y);
      
      let visual_x = act.x;
      let visual_y = act.y;
      let r_u = st.baseRadius;
      
      if (st.networkState === 'PACKET_LOSS') {
        const loss_dt = t - st.t_loss;
        const predicted = predictCircular(st.stateAtLoss, loss_dt);
        visual_x = predicted.x;
        visual_y = predicted.y;
        r_u = st.baseRadius + loss_dt * 15;
        st.radiusAtLoss = r_u;
      } else if (st.networkState === 'RECONCILING') {
        const rec_dt = t - st.t_restore;
        const T_rec = 0.6;
        let s = rec_dt / T_rec;
        if (s >= 1) {
          s = 1;
          st.networkState = 'NORMAL';
          queueMicrotask(() => {
            setUiState(prev => ({ ...prev, networkState: 'NORMAL' }));
          });
        }
        
        const errorX = evaluateQuinticError(st.errX, st.errVx, st.errAx, s, T_rec);
        const errorY = evaluateQuinticError(st.errY, st.errVy, st.errAy, s, T_rec);
        
        visual_x = act.x + errorX;
        visual_y = act.y + errorY;
        
        const easeWeight = evaluateQuinticError(1, 0, 0, s, 1);
        r_u = st.baseRadius + (st.radiusAtLoss - st.baseRadius) * easeWeight;
      }

      if (st.networkState === 'NORMAL' || st.networkState === 'RECONCILING') {
        if (t - st.last_packet_time > 0.15) {
          st.packets.push({ x: act.x, y: act.y, time: t, alpha: 1 });
          st.last_packet_time = t;
        }
      }

      st.packets = st.packets.filter(p => {
        p.alpha -= logicalDt * 0.5;
        return p.alpha > 0;
      });

      const dpr = window.devicePixelRatio || 1;
      const width = canvas.width / dpr;
      const height = canvas.height / dpr;
      
      ctx.resetTransform();
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.scale(dpr, dpr);
      
      const cx = width / 2;
      const cy = height / 2;

      const pad = 60;
      const extentX = Math.max(st.A_x + pad, Math.abs(visual_x) + r_u + pad, Math.abs(act.x) + r_u + pad);
      const extentY = Math.max(st.A_y + pad, Math.abs(visual_y) + r_u + pad, Math.abs(act.y) + r_u + pad);
      const scaleX = cx / extentX;
      const scaleY = cy / extentY;
      const targetScale = Math.min(1.0, scaleX, scaleY);
      
      st.scale += (targetScale - st.scale) * (realDt * 3.0);
      
      // Draw Grid relative to screen space, completely detached from logical scale
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.03)';
      ctx.lineWidth = 1;
      ctx.beginPath();
      const gridSize = 40;
      for (let x = (cx % gridSize) - gridSize; x < width; x += gridSize) { ctx.moveTo(x, 0); ctx.lineTo(x, height); }
      for (let y = (cy % gridSize) - gridSize; y < height; y += gridSize) { ctx.moveTo(0, y); ctx.lineTo(width, y); }
      ctx.stroke();

      ctx.translate(cx, cy);
      ctx.scale(st.scale, st.scale);

      // Draw actual path trace (History -> Solid)
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.4)';
      ctx.lineWidth = 2 / st.scale;
      ctx.beginPath();
      for (let i = 0; i <= 100; i++) {
        const pt = t - 2 + i * 0.02;
        const p = evaluateActual(pt, st.A_x, st.A_y, st.omega_x, st.omega_y);
        if (i === 0) ctx.moveTo(p.x, p.y);
        else ctx.lineTo(p.x, p.y);
      }
      ctx.stroke();
      
      // Draw actual path trace (Future -> Dashed/Faint)
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.15)';
      ctx.lineWidth = 2 / st.scale;
      ctx.setLineDash([4 / st.scale, 4 / st.scale]);
      ctx.beginPath();
      for (let i = 0; i <= 100; i++) {
        const pt = t + i * 0.02;
        const p = evaluateActual(pt, st.A_x, st.A_y, st.omega_x, st.omega_y);
        if (i === 0) ctx.moveTo(p.x, p.y);
        else ctx.lineTo(p.x, p.y);
      }
      ctx.stroke();
      ctx.setLineDash([]);
      
      // Draw prediction path if diverging
      if (st.networkState === 'PACKET_LOSS' || st.networkState === 'RECONCILING') {
        ctx.strokeStyle = '#E7A34B';
        ctx.lineWidth = 1.5 / st.scale;
        ctx.globalAlpha = 0.5;
        ctx.beginPath();
        const startState = st.stateAtLoss;
        ctx.moveTo(startState.x, startState.y);
        for(let i = 0; i < 60; i++) {
          const pt_dt = i * 0.05;
          const pp = predictCircular(startState, pt_dt);
          ctx.lineTo(pp.x, pp.y);
        }
        ctx.stroke();
        ctx.globalAlpha = 1.0;
      }

      // Draw packets
      st.packets.forEach(p => {
        ctx.fillStyle = `rgba(111, 175, 135, ${p.alpha})`;
        ctx.beginPath();
        ctx.arc(p.x, p.y, 4 / st.scale, 0, Math.PI * 2);
        ctx.fill();
      });

      // Draw actual state marker
      ctx.fillStyle = 'rgba(111, 175, 135, 0.4)';
      ctx.beginPath();
      ctx.arc(act.x, act.y, 6 / st.scale, 0, Math.PI * 2);
      ctx.fill();

      // Draw uncertainty envelope
      let envColor = 'rgba(111, 175, 135, 0.1)';
      let envStroke = '#6FAF87';
      if (st.networkState === 'PACKET_LOSS') {
        const loss_dt = t - st.t_loss;
        if (loss_dt > 1.5) {
          envColor = 'rgba(201, 104, 104, 0.1)';
          envStroke = '#C96868';
        } else {
          envColor = 'rgba(231, 163, 75, 0.1)';
          envStroke = '#E7A34B';
        }
      }
      
      ctx.fillStyle = envColor;
      ctx.strokeStyle = envStroke;
      ctx.lineWidth = 1 / st.scale;
      ctx.beginPath();
      ctx.arc(visual_x, visual_y, r_u, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // Draw client visual state
      ctx.fillStyle = '#F1F0EB';
      ctx.beginPath();
      ctx.arc(visual_x, visual_y, 4 / st.scale, 0, Math.PI * 2);
      ctx.fill();
      
      // Draw direction vector
      let dir_angle = 0;
      if (st.networkState === 'PACKET_LOSS') {
        const loss_dt = t - st.t_loss;
        const predicted = predictCircular(st.stateAtLoss, loss_dt);
        dir_angle = Math.atan2(predicted.vy, predicted.vx);
      } else {
        dir_angle = Math.atan2(act.vy, act.vx);
      }
      
      ctx.strokeStyle = '#F1F0EB';
      ctx.lineWidth = 1.5 / st.scale;
      ctx.beginPath();
      ctx.moveTo(visual_x, visual_y);
      ctx.lineTo(visual_x + Math.cos(dir_angle) * (16 / st.scale), visual_y + Math.sin(dir_angle) * (16 / st.scale));
      ctx.stroke();

      if (Math.random() < 0.1) {
        queueMicrotask(() => {
          let predAge = 0;
          if (st.networkState === 'PACKET_LOSS') predAge = Math.floor((t - st.t_loss) * 1000);
          setUiState(prev => ({
            ...prev,
            predictionAge: predAge,
            uncertaintyRadius: Number((r_u * 0.1).toFixed(2))
          }));
        });
      }
    };

    const runLoop = () => {
      renderTick();
      animationFrameId = requestAnimationFrame(runLoop);
    };
    runLoop();

    return () => {
      cancelAnimationFrame(animationFrameId);
      resizeObserver.disconnect();
    };
  }, []);

  const stateColor = uiState.networkState === 'NORMAL' ? 'text-green-500' : 
                     uiState.networkState === 'PACKET_LOSS' ? 'text-primary' : 'text-primary';

  return (
    <div className="flex flex-col border border-border bg-[#0A0B0D] shadow-2xl overflow-hidden font-mono">
      {/* Telemetry Header */}
      <div className="flex flex-wrap items-center justify-between p-3 md:px-5 md:py-4 border-b border-border bg-card/50 text-[10px] md:text-xs gap-4 relative z-10">
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-muted-foreground">
          <div className="flex items-center gap-2">
            <div className={`w-2 h-2 rounded-full ${uiState.networkState === 'NORMAL' ? 'bg-green-500' : 'bg-primary motion-safe:animate-pulse'}`} />
            <span className="text-secondary-foreground font-semibold">STATE:</span> <span className={stateColor}>{uiState.networkState}</span>
          </div>
          <div>AGE: <span className="text-foreground">{uiState.predictionAge}ms</span></div>
          <div>UNCERTAINTY: <span className="text-foreground">{uiState.uncertaintyRadius}m</span></div>
        </div>
        
        <div className="flex items-center gap-2">
          <Button onClick={handleReset} aria-label="Reset simulation" title="Reset simulation" variant="outline" size="icon" className="h-8 w-8 border-border text-foreground hover:bg-secondary">
            <RotateCcw size={14} />
          </Button>
          <Button onClick={togglePause} aria-label={uiState.isPaused ? 'Resume simulation' : 'Pause simulation'} title={uiState.isPaused ? 'Resume simulation' : 'Pause simulation'} variant="outline" size="icon" className="h-8 w-8 border-border text-foreground hover:bg-secondary">
            {uiState.isPaused ? <Play size={14}/> : <Pause size={14}/>}
          </Button>
          <Button onClick={handleStep} aria-label="Advance simulation by 100 milliseconds" title="Advance 100 ms" disabled={!uiState.isPaused} variant="outline" size="icon" className="h-8 w-8 border-border text-foreground hover:bg-secondary disabled:opacity-30">
            <SkipForward size={14}/>
          </Button>
          
          <Button 
            onClick={togglePacketLoss} 
            disabled={uiState.networkState === 'RECONCILING'}
            variant={uiState.networkState === 'NORMAL' ? "outline" : "default"}
            className={`h-8 px-4 text-xs ${
              uiState.networkState === 'NORMAL' 
                ? 'border-border text-foreground hover:bg-secondary' 
                : 'bg-primary text-primary-foreground hover:bg-primary/90'
            }`}
          >
            {uiState.networkState === 'NORMAL' ? 'Simulate Loss' : uiState.networkState === 'RECONCILING' ? 'Reconciling…' : 'Restore Connection'}
          </Button>
        </div>
      </div>
      {uiState.isPaused && (
        <p className="border-b border-border px-4 py-2 text-[11px] text-muted-foreground" role="status">
          Paused · Each step advances 100 ms.
          {uiState.networkState === 'RECONCILING' && ' Step through recovery or resume to finish.'}
        </p>
      )}
      
      {/* Canvas Area */}
      <div ref={containerRef} className="relative w-full h-[350px] md:h-[550px]">
        <canvas 
          ref={canvasRef}
          role="img"
          aria-label="Live comparison of authoritative motion, client prediction, and uncertainty during network loss"
          className="absolute inset-0 block touch-none"
        />
        
        <div className="absolute bottom-4 left-4 text-[10px] md:text-xs text-muted-foreground/80 space-y-1.5 pointer-events-none select-none bg-[#0A0B0D]/50 p-2 rounded backdrop-blur-sm">
          <div className="flex items-center gap-2"><div className="w-2 h-2 rounded-full bg-[#6FAF87]/40" /> PACKET HISTORY</div>
          <div className="flex items-center gap-2"><div className="w-4 h-0.5 bg-white/40" /> SERVER ACTUAL</div>
          <div className="flex items-center gap-2"><div className="w-4 h-0.5 bg-[#E7A34B]/50" /> CLIENT PREDICTION</div>
        </div>
      </div>
    </div>
  );
}
