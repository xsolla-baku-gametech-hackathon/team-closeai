import React, { useState, useEffect } from 'react';
import { HeroSimulator } from '@/components/HeroSimulator';
import { TechnologyCard } from '@/components/TechnologyCard';
import { MetricSection } from '@/components/MetricSection';
import { CodePanel } from '@/components/CodePanel';
import { Button } from '@/components/ui/button';
import { ArrowRight, ChevronRight, Github, Menu, X } from 'lucide-react';
import { 
  DiagramCircularKinematics, 
  DiagramQuinticReconciliation, 
  DiagramUncertaintyEnvelope 
} from '@/components/Diagrams';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export default function Home() {
  const [dialogContent, setDialogContent] = useState<'SDK' | 'DOCS' | null>(null);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  useEffect(() => {
    const handleHashChange = () => {
      const hash = window.location.hash;
      if (hash) {
        const element = document.querySelector(hash);
        if (element) {
          const headerOffset = 80;
          const elementPosition = element.getBoundingClientRect().top;
          const offsetPosition = elementPosition + window.pageYOffset - headerOffset;
          window.scrollTo({
            top: offsetPosition,
            behavior: "smooth"
          });
        }
      }
    };
    
    window.addEventListener('hashchange', handleHashChange);
    if (window.location.hash) {
      setTimeout(handleHashChange, 100);
    }
    
    return () => window.removeEventListener('hashchange', handleHashChange);
  }, []);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && mobileMenuOpen) {
        setMobileMenuOpen(false);
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [mobileMenuOpen]);

  const handleOpenDialog = (type: 'SDK' | 'DOCS', e?: React.MouseEvent) => {
    e?.preventDefault();
    setDialogContent(type);
    setMobileMenuOpen(false);
  };

  const scrollToSection = (id: string, e: React.MouseEvent) => {
    e.preventDefault();
    setMobileMenuOpen(false);
    window.history.pushState(null, '', `#${id}`);
    const element = document.getElementById(id);
    if (element) {
      const headerOffset = 80;
      const elementPosition = element.getBoundingClientRect().top;
      const offsetPosition = elementPosition + window.pageYOffset - headerOffset;
      window.scrollTo({
        top: offsetPosition,
        behavior: "smooth"
      });
    }
  };

  return (
    <div className="min-h-screen bg-background text-foreground font-sans selection:bg-primary/20 selection:text-primary">
      
      {/* HEADER */}
      <header className="fixed top-0 left-0 right-0 z-50 bg-background/90 backdrop-blur-md border-b border-border h-16">
        <div className="container mx-auto px-6 h-full flex items-center justify-between">
          <div className="flex items-center gap-3 font-mono font-medium text-foreground tracking-tight text-sm">
            <div className="w-3 h-3 border border-primary relative overflow-hidden flex items-center justify-center">
              <div className="w-1.5 h-1.5 bg-primary/20 rounded-full" />
              <div className="absolute top-0 right-0 w-1 h-1 bg-primary" />
            </div>
            NetGhost
          </div>
          
          <nav className="hidden md:flex items-center gap-8 text-sm font-medium text-muted-foreground">
            <a href="#features" onClick={(e) => scrollToSection('features', e)} className="hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background rounded">Features</a>
            <a href="#performance" onClick={(e) => scrollToSection('performance', e)} className="hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background rounded">Performance</a>
            <a href="#integration" onClick={(e) => scrollToSection('integration', e)} className="hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background rounded">Integration</a>
            <a href="#" onClick={(e) => handleOpenDialog('DOCS', e)} className="hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background rounded">Docs</a>
          </nav>
          
          <div className="hidden md:block">
            <Button onClick={(e) => handleOpenDialog('SDK', e)} variant="outline" className="border-border text-foreground hover:bg-secondary h-9 text-xs font-mono rounded-none">
              Get SDK
            </Button>
          </div>
          
          <button 
            className="md:hidden p-2 text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary rounded"
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            aria-expanded={mobileMenuOpen}
            aria-controls="mobile-menu"
            aria-label="Toggle navigation menu"
          >
            {mobileMenuOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
        </div>
        
        {/* Mobile Menu */}
        {mobileMenuOpen && (
          <div id="mobile-menu" role="menu" className="md:hidden absolute top-16 left-0 right-0 bg-background border-b border-border py-4 px-6 flex flex-col gap-4 shadow-xl">
            <a href="#features" role="menuitem" onClick={(e) => scrollToSection('features', e)} className="text-sm font-medium text-muted-foreground hover:text-foreground py-2 border-b border-border/50">Features</a>
            <a href="#performance" role="menuitem" onClick={(e) => scrollToSection('performance', e)} className="text-sm font-medium text-muted-foreground hover:text-foreground py-2 border-b border-border/50">Performance</a>
            <a href="#integration" role="menuitem" onClick={(e) => scrollToSection('integration', e)} className="text-sm font-medium text-muted-foreground hover:text-foreground py-2 border-b border-border/50">Integration</a>
            <a href="#" role="menuitem" onClick={(e) => handleOpenDialog('DOCS', e)} className="text-sm font-medium text-muted-foreground hover:text-foreground py-2 border-b border-border/50">Documentation</a>
            <Button onClick={(e) => handleOpenDialog('SDK', e)} variant="outline" className="mt-2 w-full justify-center border-border text-foreground hover:bg-secondary h-10 text-sm font-mono rounded-none">
              Get SDK
            </Button>
          </div>
        )}
      </header>

      <main className="pt-24 pb-20 md:pb-32">
        
        {/* HERO SECTION */}
        <section className="container mx-auto px-6 py-6 md:py-12">
          
          <div className="mb-8 md:mb-12 max-w-5xl">
            <div className="text-[10px] font-mono text-primary tracking-widest mb-4 uppercase">Realtime Networking / Concept Model</div>
            <h1 className="text-4xl md:text-5xl lg:text-7xl font-semibold tracking-tight leading-[1.05] text-foreground">
              <span className="hidden md:block">Smooth Multiplayer.</span>
              <span className="block md:hidden">Smooth<br/>Multiplayer.</span>
              <span className="block text-muted-foreground mt-1 md:mt-2">Zero Rubberbanding.</span>
            </h1>
          </div>
          
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-10 items-start">
            <div className="lg:col-span-4 flex flex-col items-start gap-8 order-2 lg:order-1 pt-2 lg:pt-8">
              <p className="text-lg text-muted-foreground leading-relaxed">
                An engine-agnostic netcode concept designed to preserve fluid motion through packet loss — with a target of less than 0.05ms of CPU overhead.
              </p>
              
              <div className="flex flex-col w-full gap-4">
                <Button onClick={(e) => handleOpenDialog('DOCS', e)} className="w-full bg-foreground text-background hover:bg-foreground/90 rounded-none h-12 font-medium focus-visible:ring-primary">
                  Read Documentation
                </Button>
                <Button onClick={(e) => scrollToSection('simulator', e)} variant="outline" className="w-full border-border bg-transparent text-foreground hover:bg-secondary rounded-none h-12 font-medium focus-visible:ring-primary">
                  View Simulator
                </Button>
              </div>
              
              <div className="flex flex-wrap gap-x-6 gap-y-3 text-[10px] font-mono text-muted-foreground uppercase tracking-widest pt-6 border-t border-border/50 w-full">
                <span>Engine Agnostic</span>
                <span>Zero GC</span>
                <span>&lt; 0.05ms Target</span>
              </div>
            </div>
            
            <div id="simulator" className="lg:col-span-8 w-full scroll-mt-24 order-1 lg:order-2 lg:pl-6">
              <HeroSimulator />
            </div>
          </div>
        </section>

        {/* TRANSITION */}
        <div className="container mx-auto px-6 my-16 md:my-24">
          <div className="w-full h-px bg-border flex items-center justify-center relative">
            <div className="bg-background px-4 text-[10px] font-mono text-muted-foreground">01 / ARCHITECTURE</div>
          </div>
        </div>

        {/* CORE TECHNOLOGY SECTION */}
        <section id="features" className="container mx-auto px-6 scroll-mt-24">
          <div className="mb-10 md:mb-16">
            <h2 className="text-3xl md:text-4xl font-semibold tracking-tight mb-4">Core Technology</h2>
            <p className="text-muted-foreground text-lg md:text-xl max-w-2xl text-balance">
              Three mechanisms keep motion visually stable when the network does not.
            </p>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 lg:gap-8 items-stretch">
            <TechnologyCard 
              index="01" 
              title="Circular Kinematics" 
              description="Motion continues along the inferred turning trajectory using the last observed angular velocity instead of collapsing into linear dead reckoning." 
              mathLabel="ω ≠ 0"
            >
              <DiagramCircularKinematics />
            </TechnologyCard>
            
            <TechnologyCard 
              index="02" 
              title="Quintic Reconciliation" 
              description="When authoritative state returns, the visual trajectory converges through a fifth-order polynomial correction, mathematically ensuring C² continuity." 
              mathLabel="C² CONTINUOUS"
            >
              <DiagramQuinticReconciliation />
            </TechnologyCard>
            
            <TechnologyCard 
              index="03" 
              title="R(t) Uncertainty Envelope" 
              description="Prediction uncertainty expands with packet age and is expressed visually as a dynamic confidence envelope to maintain player trust." 
              mathLabel="R(t) = r₀ + age × k"
            >
              <DiagramUncertaintyEnvelope />
            </TechnologyCard>
          </div>
        </section>
        
        {/* TRANSITION */}
        <div className="container mx-auto px-6 my-16 md:my-24">
          <div className="w-full h-px bg-border flex items-center justify-center relative">
            <div className="bg-background px-4 text-[10px] font-mono text-muted-foreground">02 / BENCHMARKS</div>
          </div>
        </div>

        {/* PERFORMANCE SECTION */}
        <section id="performance" className="scroll-mt-24">
          <div className="container mx-auto px-6 mb-10 md:mb-16">
            <h2 className="text-3xl md:text-4xl font-semibold tracking-tight">Built for the frame budget.</h2>
          </div>
          <MetricSection />
        </section>
        
        {/* TRANSITION */}
        <div className="container mx-auto px-6 my-16 md:my-24">
          <div className="w-full h-px bg-border flex items-center justify-center relative">
            <div className="bg-background px-4 text-[10px] font-mono text-muted-foreground">03 / INTEGRATION</div>
          </div>
        </div>

        {/* DEVELOPER-FIRST SECTION */}
        <section id="integration" className="container mx-auto px-6 scroll-mt-24">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 lg:gap-24 items-center">
            
            <div className="flex flex-col gap-6 md:gap-8">
              <h2 className="text-3xl md:text-4xl font-semibold tracking-tight text-balance">
                Minimal integration. Maximum control.
              </h2>
              
              <div className="text-lg text-muted-foreground leading-relaxed border-l-2 border-primary/50 pl-6">
                NetGhost does not dictate your simulation architecture. It provides the prediction and reconciliation layer required to preserve visual continuity when authoritative state temporarily disappears.
              </div>
              
              <div className="flex flex-col gap-3 mt-4">
                <div className="flex items-center gap-3 text-sm font-mono text-foreground border-b border-border/50 pb-3">
                  <ChevronRight className="w-4 h-4 text-primary" /> Target: Unity / C#
                </div>
                <div className="flex items-center gap-3 text-sm font-mono text-foreground border-b border-border/50 pb-3">
                  <ChevronRight className="w-4 h-4 text-primary" /> Target: Unreal / C++
                </div>
                <div className="flex items-center gap-3 text-sm font-mono text-foreground border-b border-border/50 pb-3">
                  <ChevronRight className="w-4 h-4 text-primary" /> Target: Custom Engine
                </div>
                <div className="flex items-center gap-3 text-sm font-mono text-foreground pb-3">
                  <ChevronRight className="w-4 h-4 text-primary" /> Concept: Pure Simulation Model
                </div>
              </div>
              
              <div className="mt-6 md:mt-8 font-mono text-[10px] md:text-xs text-muted-foreground flex flex-col gap-2">
                <div className="flex items-center gap-3 md:gap-4">
                  <div className="text-foreground">AUTHORITATIVE STATE</div>
                  <ArrowRight className="w-3 h-3" />
                </div>
                <div className="flex items-center gap-3 md:gap-4 ml-4">
                  <div className="text-foreground">INERTIAL PREDICTION</div>
                  <ArrowRight className="w-3 h-3" />
                </div>
                <div className="flex items-center gap-3 md:gap-4 ml-8">
                  <div className="text-foreground">UNCERTAINTY MODEL</div>
                  <ArrowRight className="w-3 h-3" />
                </div>
                <div className="flex items-center gap-3 md:gap-4 ml-12">
                  <div className="text-primary font-medium">QUINTIC RECONCILIATION</div>
                </div>
              </div>
            </div>
            
            <div className="w-full">
              <CodePanel />
            </div>
            
          </div>
        </section>
        
        {/* TRANSITION */}
        <div className="container mx-auto px-6 my-24 md:my-32">
          <div className="w-full h-px bg-border flex items-center justify-center relative">
            <div className="w-2 h-2 rounded-full bg-border" />
          </div>
        </div>

        {/* DOCUMENTATION CTA */}
        <section className="container mx-auto px-6 pb-12 text-center flex flex-col items-center">
          <h2 className="text-3xl md:text-4xl font-semibold tracking-tight mb-4">Explore the model.</h2>
          <p className="text-lg text-muted-foreground mb-8 md:mb-10 max-w-xl mx-auto">
            Review the pure prediction mathematics and see how C² reconciliation behaves analytically.
          </p>
          
          <div className="flex flex-col items-center gap-6">
            <Button onClick={(e) => handleOpenDialog('DOCS', e)} className="bg-primary text-primary-foreground hover:bg-primary/90 rounded-none h-14 px-10 font-medium text-base">
              Read Documentation
            </Button>
            
            <div className="mt-4 md:mt-8 text-xs text-muted-foreground font-mono flex flex-col sm:flex-row items-center gap-2 sm:gap-6">
              <span className="flex items-center gap-2">
                <div className="w-1.5 h-1.5 rounded-full bg-primary" /> Concept Demonstration
              </span>
              <span className="hidden sm:inline text-border">|</span>
              <span className="flex items-center gap-2">
                <Github className="w-4 h-4" /> SDK currently in private preview
              </span>
            </div>
          </div>
        </section>

      </main>

      {/* FOOTER */}
      <footer className="border-t border-border bg-[#0A0B0D] pt-12 pb-8">
        <div className="container mx-auto px-6">
          <div className="flex flex-col md:flex-row justify-between items-start gap-10 mb-12">
            
            <div>
              <div className="font-mono font-medium text-foreground text-lg mb-2 flex items-center gap-2">
                <div className="w-2 h-2 bg-primary" /> NetGhost
              </div>
              <div className="text-sm text-muted-foreground max-w-xs mt-4">
                Engine-agnostic inertial networking concept for realtime simulation.
              </div>
            </div>
            
            <div className="flex flex-wrap gap-12 text-sm font-medium">
              <div className="flex flex-col gap-4">
                <span className="text-xs font-mono text-muted-foreground mb-1 uppercase tracking-widest">Resources</span>
                <a href="#" onClick={(e) => handleOpenDialog('DOCS', e)} className="text-muted-foreground hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary rounded">Documentation</a>
                <a href="#" onClick={(e) => handleOpenDialog('SDK', e)} className="text-muted-foreground hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-primary rounded">SDK Access</a>
              </div>
            </div>
            
          </div>
          
          <div className="border-t border-border pt-8 flex flex-col md:flex-row justify-between items-center gap-4 text-xs font-mono">
            <div className="text-muted-foreground">© {new Date().getFullYear()} NetGhost Concept Model</div>
            <div className="flex items-center gap-2 text-muted-foreground">
              MODEL STATUS: <span className="text-green-500">NOMINAL</span>
            </div>
          </div>
        </div>
      </footer>
      
      {/* INFO DIALOG */}
      <Dialog open={dialogContent !== null} onOpenChange={(open) => !open && setDialogContent(null)}>
        <DialogContent className="bg-card border-border rounded-none text-foreground max-w-md p-6">
          <DialogHeader>
            <DialogTitle className="text-xl font-semibold mb-2">
              {dialogContent === 'SDK' ? 'SDK Availability' : 'Documentation'}
            </DialogTitle>
            <DialogDescription className="text-muted-foreground text-base leading-relaxed">
              {dialogContent === 'SDK' ? (
                <>
                  NetGhost is currently a conceptual engineering demonstration of advanced prediction and reconciliation techniques. 
                  <br/><br/>
                  The production SDK is not publicly available at this time. The performance figures presented are design targets for this conceptual model.
                </>
              ) : (
                <>
                  The mathematical foundation for NetGhost is based on deterministic circular kinematics and quintic polynomial reconciliation.
                  <br/><br/>
                  In this demonstration, you can review the pure functional model in the provided source code, focusing on C² continuity preservation during packet loss events.
                </>
              )}
            </DialogDescription>
          </DialogHeader>
          <div className="mt-8 flex justify-end">
            <Button onClick={() => setDialogContent(null)} className="bg-foreground text-background hover:bg-foreground/90 rounded-none px-6">
              Acknowledge
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
