import React from 'react';

export function DiagramCircularKinematics() {
  return (
    <svg width="120" height="80" viewBox="0 0 120 80" className="opacity-90">
      {/* Grid */}
      <path d="M0,40 L120,40 M60,0 L60,80" stroke="#2C3138" strokeWidth="1" strokeDasharray="2 2" />
      
      {/* Normal dead reckoning (linear) */}
      <path d="M30,60 L90,20" stroke="#8E949B" strokeWidth="1" strokeDasharray="4 4" fill="none" />
      <text x="90" y="16" fill="#8E949B" fontSize="8" fontFamily="monospace" textAnchor="middle">linear</text>

      {/* Circular Kinematics */}
      <path d="M30,60 Q60,60 90,40" stroke="#E7A34B" strokeWidth="2.5" fill="none" />
      <circle cx="90" cy="40" r="3.5" fill="#E7A34B" />
      <circle cx="30" cy="60" r="3.5" fill="#F1F0EB" />
      
      <text x="100" y="50" fill="#E7A34B" fontSize="8" fontFamily="monospace">inertial</text>
    </svg>
  );
}

export function DiagramQuinticReconciliation() {
  return (
    <svg width="120" height="80" viewBox="0 0 120 80" className="opacity-90">
      {/* Axes */}
      <path d="M10,70 L110,70 M10,70 L10,10" stroke="#3A4048" strokeWidth="1" />
      
      {/* Target value */}
      <path d="M10,60 L110,60" stroke="#2C3138" strokeWidth="1" strokeDasharray="2 2" />
      <text x="115" y="63" fill="#8E949B" fontSize="8" fontFamily="monospace">0</text>
      
      {/* Quintic curve: approaches 0 smoothly */}
      <path d="M10,20 C 40,20 60,60 100,60" stroke="#E7A34B" strokeWidth="2.5" fill="none" />
      
      {/* Error label */}
      <text x="15" y="15" fill="#E7A34B" fontSize="8" fontFamily="monospace">error</text>
    </svg>
  );
}

export function DiagramUncertaintyEnvelope() {
  return (
    <svg width="120" height="80" viewBox="0 0 120 80" className="opacity-90">
      {/* Crosshair */}
      <path d="M50,40 L70,40 M60,30 L60,50" stroke="#3A4048" strokeWidth="1" />
      
      {/* Rings */}
      <circle cx="60" cy="40" r="10" stroke="#6FAF87" strokeWidth="1.5" fill="rgba(111,175,135,0.15)" />
      <circle cx="60" cy="40" r="20" stroke="#E7A34B" strokeWidth="1.5" strokeDasharray="2 2" fill="rgba(231,163,75,0.1)" />
      <circle cx="60" cy="40" r="32" stroke="#C96868" strokeWidth="1.5" strokeDasharray="1 3" fill="none" />
      
      <circle cx="60" cy="40" r="2" fill="#F1F0EB" />
    </svg>
  );
}
