import React from 'react';

export function TechnologyCard({
  index, title, description, mathLabel, children
}: {
  index: string, title: string, description: string, mathLabel: string, children: React.ReactNode
}) {
  return (
    <div className="border border-border p-6 flex flex-col gap-6 bg-card relative group hover:border-muted-foreground/30 transition-colors h-full">
      <div className="flex justify-between items-start shrink-0">
        <span className="font-mono text-xs text-muted-foreground">{index}</span>
        <div className="text-[10px] font-mono text-primary/80 border border-primary/20 bg-primary/5 px-2 py-0.5">
          {mathLabel}
        </div>
      </div>
      
      <div className="h-32 shrink-0 flex items-center justify-center border border-border bg-[#0A0B0D] overflow-hidden relative">
        {children}
      </div>
      
      <div className="flex-1 flex flex-col">
        <h3 className="text-base font-semibold text-foreground mb-2">{title}</h3>
        <p className="text-sm text-muted-foreground leading-relaxed">{description}</p>
      </div>
    </div>
  );
}
