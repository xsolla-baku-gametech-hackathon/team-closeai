import React from 'react';

export function CodePanel() {
  return (
    <div className="border border-border bg-[#0A0B0D] flex flex-col font-mono shadow-2xl relative w-full overflow-hidden">
      {/* Top Bar */}
      <div className="h-8 border-b border-border flex items-center px-4 justify-between bg-card">
        <div className="flex gap-1.5 items-center">
          <div className="w-2 h-2 rounded-full bg-border" />
          <div className="w-2 h-2 rounded-full bg-border" />
          <div className="w-2 h-2 rounded-full bg-border" />
        </div>
        <div className="text-[10px] text-muted-foreground">prediction.cs</div>
        <div className="w-8"></div> {/* Spacer for center alignment of title */}
      </div>
      
      {/* Editor Body */}
      <div className="p-6 text-sm leading-relaxed overflow-x-auto">
        <pre className="text-muted-foreground">
          <code className="language-csharp">
<span className="text-[#6FAF87] italic">// Easy integration for existing simulation loops</span><br/>
<span className="text-secondary-foreground">var</span> netcode = <span className="text-primary">new</span> <span className="text-foreground">NetGhost</span>();<br/>
<br/>
<span className="text-[#6FAF87] italic">// Set the baseline uncertainty radius</span><br/>
netcode.<span className="text-foreground">SetBaseRadius</span>(<span className="text-secondary-foreground">10f</span>);<br/>
<br/>
<span className="text-[#6FAF87] italic">// Call when authoritative state returns</span><br/>
netcode.<span className="text-foreground">EvaluateQuinticCorrection</span>(gameObject);<br/>
          </code>
        </pre>
      </div>
      
      {/* Bottom Bar */}
      <div className="h-6 border-t border-border flex items-center px-4 bg-card/50 text-[9px] text-primary/80 uppercase tracking-widest">
        Prediction Layer Initialized
      </div>
    </div>
  );
}
