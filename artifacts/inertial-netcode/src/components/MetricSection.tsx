import React from 'react';

export function MetricSection() {
  return (
    <div className="border-t border-b border-border w-full">
      <div className="container mx-auto px-6">
        <div className="grid grid-cols-1 md:grid-cols-4 divide-y md:divide-y-0 md:divide-x divide-border">
          
          <div className="py-12 md:py-16 md:px-8 flex flex-col justify-center items-start">
            <div className="text-3xl md:text-4xl font-light text-foreground mb-2 font-mono tracking-tight">{'< 0.05ms'}</div>
            <div className="text-[10px] text-muted-foreground font-mono tracking-widest">CPU EXECUTION</div>
          </div>
          
          <div className="py-12 md:py-16 md:px-8 flex flex-col justify-center items-start">
            <div className="text-3xl md:text-4xl font-light text-foreground mb-2 font-mono tracking-tight">0</div>
            <div className="text-[10px] text-muted-foreground font-mono tracking-widest">DYNAMIC ALLOCATIONS</div>
          </div>
          
          <div className="py-12 md:py-16 md:px-8 flex flex-col justify-center items-start">
            <div className="text-3xl md:text-4xl font-light text-foreground mb-2 font-mono tracking-tight">C²</div>
            <div className="text-[10px] text-muted-foreground font-mono tracking-widest">RECONCILIATION CONTINUITY</div>
          </div>
          
          <div className="py-12 md:py-16 md:px-8 flex flex-col justify-center items-start">
            <div className="text-3xl md:text-4xl font-light text-foreground mb-2 font-mono tracking-tight">ANY ENGINE</div>
            <div className="text-[10px] text-muted-foreground font-mono tracking-widest">INTEGRATION MODEL</div>
          </div>
          
        </div>
      </div>
    </div>
  );
}
