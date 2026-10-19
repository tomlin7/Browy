import React, { useState, useEffect } from "react"
import {
  Download,
  Zap,
  Layers,
  RotateCcw,
  Command,
  HardDrive,
  RefreshCw,
  Shield,
  Bot,
  Split,
  Laptop,
  ChevronDown,
  ChevronUp,
} from "lucide-react"
import { Button } from "@/components/ui/button"

function GithubSvg({ className = "size-6" }: { className?: string }) {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" className={className}>
      <path
        fill="currentColor"
        d="M12 2A10 10 0 0 0 2 12c0 4.42 2.87 8.17 6.84 9.5c.5.08.66-.23.66-.5v-1.69c-2.77.6-3.36-1.34-3.36-1.34c-.46-1.16-1.11-1.47-1.11-1.47c-.91-.62.07-.6.07-.6c1 .07 1.53 1.03 1.53 1.03c.87 1.52 2.34 1.07 2.91.83c.09-.65.35-1.09.63-1.34c-2.22-.25-4.55-1.11-4.55-4.92c0-1.11.38-2 1.03-2.71c-.1-.25-.45-1.29.1-2.64c0 0 .84-.27 2.75 1.02c.79-.22 1.65-.33 2.5-.33s1.71.11 2.5.33c1.91-1.29 2.75-1.02 2.75-1.02c.55 1.35.2 2.39.1 2.64c.65.71 1.03 1.6 1.03 2.71c0 3.82-2.34 4.66-4.57 4.91c.36.31.69.92.69 1.85V21c0 .27.16.59.67.5C19.14 20.16 22 16.42 22 12A10 10 0 0 0 12 2"
      />
    </svg>
  )
}

function BrowyAppIcon({ className = "size-8" }: { className?: string }) {
  return (
    <div className={`flex items-center justify-center rounded-[6px] bg-foreground text-background font-bold text-sm tracking-tight ${className}`}>
      B
    </div>
  )
}

export default function App() {
  const [starCount, setStarCount] = useState("")
  const [activeTabTitle, setActiveTabTitle] = useState("tomlin7/Browy · Modern Arc/Zen Browser")
  const [activeSpace, setActiveSpace] = useState<"work" | "dev" | "research">("dev")
  const [openFaq, setOpenFaq] = useState<number | null>(null)

  useEffect(() => {
    // Ensure dark theme class on documentElement like waku
    document.documentElement.classList.add("dark")
    fetch("https://api.github.com/repos/tomlin7/Browy")
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data && typeof data.stargazers_count === "number") {
          setStarCount(`${data.stargazers_count.toLocaleString()}`)
        }
      })
      .catch(() => {})
  }, [])

  const faqs = [
    {
      q: "Is this another Chromium clone or Electron app?",
      a: "No. Browy is built in .NET 9 WPF utilizing native Direct3D hardware acceleration and the Microsoft Edge WebView2 Evergreen runtime. It does not package bloated node runtimes, launching in ~240ms with 40% less memory than monolithic Chromium browsers.",
    },
    {
      q: "Is Browy completely free and open source?",
      a: "Yes, 100%. Browy is licensed under the MIT License. The complete source code, Direct3D composition pipelines, and 203 automated unit tests are hosted publicly on GitHub.",
    },
    {
      q: "How does the Autonomous AI Copilot work?",
      a: "Browy connects directly to your chosen local or cloud providers (OpenAI, Anthropic Claude, or local Ollama) using BYOK (Bring Your Own Key) stored securely with Windows DPAPI. By default, it operates in an offline heuristic mode that intercepts DOM actions locally with zero telemetry.",
    },
    {
      q: "Where does my data live?",
      a: "Everything is strictly local. Tabs, Spaces, history, bookmarks, and sessions live on your disk in AppData. No accounts, no telemetry servers, and no Browy cloud in between you and the web.",
    },
    {
      q: "What are the system requirements?",
      a: "64-bit Windows 11 or Windows 10 with Microsoft Edge WebView2 runtime (pre-installed on virtually all modern Windows machines). We publish both a lightweight standard package and a zero-dependency self-contained portable edition.",
    },
  ]

  const features = [
    {
      icon: <Zap className="size-4 text-muted-foreground" />,
      title: "Native down to the frame",
      desc: ".NET 9 WPF and Direct3D composition. Instant launch, smooth scrolling through dense documentation, no Electron overhead.",
    },
    {
      icon: <Layers className="size-4 text-muted-foreground" />,
      title: "Spaces & tab isolation",
      desc: "Cleanly compartmentalize Work, Dev, and Research contexts. Each space isolates tabs, pinned workflows, and session memory.",
    },
    {
      icon: <Bot className="size-4 text-muted-foreground" />,
      title: "Autonomous AI Copilot",
      desc: "Local DOM directive execution: TRANSLATE, CLICK, FILL, ZOOM, and READER_MODE with real-time viewport analysis and BYOK privacy.",
    },
    {
      icon: <Split className="size-4 text-muted-foreground" />,
      title: "4-way split screen",
      desc: "Tile tabs side-by-side in horizontal, vertical, or 4-way quadrant grids within a single window. Code and read docs simultaneously.",
    },
    {
      icon: <Shield className="size-4 text-muted-foreground" />,
      title: "Local & shielded by architecture",
      desc: "Aggressive tracker stripper for utm_*, fbclid, and telemetry tokens. Built-in InPrivate cache destruction on space close.",
    },
    {
      icon: <HardDrive className="size-4 text-muted-foreground" />,
      title: "Memory efficient tab suspension",
      desc: "Shared evergreen WebView2 runtime cuts resident RAM by 40% vs Chrome or Arc by automatically putting idle background tabs to sleep.",
    },
  ]

  return (
    <div className="min-h-dvh antialiased">
      {/* Centralized framed container */}
      <div className="mx-auto w-full max-w-[1100px] border-border/70 md:border-x">
        {/* Header */}
        <header className="flex h-16 items-center justify-between px-5 md:px-10">
          <a href="#" className="flex items-center gap-2.5">
            <BrowyAppIcon className="size-8" />
            <span className="text-[15px] font-semibold tracking-tight">Browy</span>
          </a>

          <div className="flex items-center gap-5">
            <a
              href="https://github.com/tomlin7/Browy"
              target="_blank"
              rel="noreferrer"
              aria-label="GitHub"
              className="flex items-center gap-1.5 rounded-full text-muted-foreground transition-colors outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/60"
            >
              <GithubSvg className="size-5" />
              {starCount && <span className="font-mono text-xs">{starCount}</span>}
            </a>

            <a href="#download">
              <Button size="sm" className="font-medium">
                Download
              </Button>
            </a>
          </div>
        </header>

        <main>
          {/* Hero Section */}
          <section className="px-5 pt-14 pb-14 md:px-10 md:pt-24">
            <div className="mb-7 inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs text-muted-foreground">
              <span className="flex size-3.5 items-center justify-center rounded-[3px] bg-[#0078d4] text-[10px] font-bold text-white">
                W
              </span>
              Engineered natively for Windows 11
            </div>

            <h1 className="max-w-4xl text-4xl font-semibold tracking-[-0.03em] text-balance md:text-[3.4rem] md:leading-[1.04]">
              The fluid, AI-Native browser for power users.
            </h1>

            <p className="mt-5 max-w-[36rem] text-[17px] leading-relaxed text-pretty text-muted-foreground">
              Browy brings Arc spaces, autonomous AI directives, 4-way split views, and aggressive tracker stripping into one fast, lightweight native window.
            </p>

            <div className="mt-8 flex flex-wrap items-center gap-x-5 gap-y-3">
              <a href="#download">
                <Button size="lg" className="gap-2">
                  <Download className="size-4" />
                  <span>Download for Windows</span>
                </Button>
              </a>

              <a
                href="https://github.com/tomlin7/Browy"
                target="_blank"
                rel="noreferrer"
                className="text-sm font-medium text-muted-foreground hover:text-foreground transition-colors"
              >
                Explore GitHub repository &rarr;
              </a>
            </div>

            {/* Sub-strip with architectural characteristics */}
            <div className="mt-16">
              <div className="font-mono text-[11px] tracking-[0.14em] text-muted-foreground/80 uppercase">
                Architecture &amp; Features
              </div>
              <div className="mt-4 flex flex-wrap items-center gap-x-7 gap-y-4 text-xs font-mono text-muted-foreground">
                <span className="flex items-center gap-1.5 hover:text-foreground transition-colors">
                  <span className="size-1.5 rounded-full bg-emerald-500"></span>
                  .NET 9 + WebView2
                </span>
                <span className="flex items-center gap-1.5 hover:text-foreground transition-colors">
                  <span className="size-1.5 rounded-full bg-blue-500"></span>
                  Arc Contextual Spaces
                </span>
                <span className="flex items-center gap-1.5 hover:text-foreground transition-colors">
                  <span className="size-1.5 rounded-full bg-purple-500"></span>
                  Autonomous AI Directives
                </span>
                <span className="flex items-center gap-1.5 hover:text-foreground transition-colors">
                  <span className="size-1.5 rounded-full bg-zinc-400"></span>
                  4-Way Split Screen
                </span>
              </div>
            </div>
          </section>

          {/* Full-width Product Screenshot / App Preview */}
          <section className="border-t">
            <div className="p-4 md:p-8 bg-muted/20">
              <div className="rounded-lg border bg-background shadow-2xl overflow-hidden">
                {/* Windows 11 Window Titlebar */}
                <div className="flex h-9 items-center justify-between border-b px-3 bg-muted/30 text-xs">
                  <div className="flex items-center gap-2">
                    <div className="flex items-center gap-1">
                      <span className="size-2 rounded-full bg-muted-foreground/30"></span>
                      <span className="size-2 rounded-full bg-muted-foreground/30"></span>
                      <span className="size-2 rounded-full bg-muted-foreground/30"></span>
                    </div>
                    <span className="text-[11px] font-mono text-muted-foreground ml-2">Browy v0.3.0</span>
                  </div>

                  <div className="flex max-w-sm flex-1 items-center justify-center mx-4">
                    <div className="flex h-6 w-full max-w-md items-center justify-between rounded border bg-background px-2 text-[11px] font-mono text-muted-foreground">
                      <span className="text-foreground truncate">https://github.com/tomlin7/Browy</span>
                      <span className="text-[9px] text-emerald-500 font-semibold">SHIELDED</span>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 font-mono text-[10px] text-muted-foreground">
                    <span>640MB RAM</span>
                  </div>
                </div>

                {/* Inner Browser Workspace */}
                <div className="grid grid-cols-1 md:grid-cols-[180px_1fr] h-[400px]">
                  {/* Left Spaces & Tab Strip */}
                  <div className="hidden md:flex flex-col border-r bg-muted/10 p-3">
                    <div className="text-[10px] font-mono text-muted-foreground uppercase tracking-wider mb-2">
                      Spaces
                    </div>
                    <div className="flex flex-col gap-1 mb-4">
                      {(["dev", "work", "research"] as const).map((space) => (
                        <button
                          key={space}
                          onClick={() => setActiveSpace(space)}
                          className={`flex items-center justify-between rounded px-2 py-1.5 text-xs font-mono transition-colors text-left ${
                            activeSpace === space
                              ? "bg-foreground text-background font-medium"
                              : "text-muted-foreground hover:bg-muted hover:text-foreground"
                          }`}
                        >
                          <span className="uppercase">{space}</span>
                          <span className="text-[9px] opacity-70">
                            {space === "dev" ? "4 TABS" : "2 TABS"}
                          </span>
                        </button>
                      ))}
                    </div>

                    <div className="text-[10px] font-mono text-muted-foreground uppercase tracking-wider mb-2 pt-2 border-t">
                      Open Tabs
                    </div>
                    <div className="space-y-1 text-xs font-mono">
                      <div
                        onClick={() => setActiveTabTitle("tomlin7/Browy · Modern Arc/Zen Browser")}
                        className="rounded px-2 py-1.5 bg-muted text-foreground truncate cursor-pointer font-medium"
                      >
                        tomlin7/Browy
                      </div>
                      <div
                        onClick={() => setActiveTabTitle("Edge WebView2 Evergreen Documentation")}
                        className="rounded px-2 py-1.5 text-muted-foreground hover:text-foreground hover:bg-muted/50 truncate cursor-pointer"
                      >
                        WebView2 Docs
                      </div>
                      <div
                        onClick={() => setActiveTabTitle("Direct3D WPF Presentation Pipeline")}
                        className="rounded px-2 py-1.5 text-muted-foreground hover:text-foreground hover:bg-muted/50 truncate cursor-pointer"
                      >
                        Direct3D Pipeline
                      </div>
                    </div>
                  </div>

                  {/* Right Viewport Content */}
                  <div className="p-6 overflow-y-auto flex flex-col justify-between bg-background">
                    <div>
                      <div className="flex items-center gap-2 text-xs font-mono text-muted-foreground mb-3">
                        <span className="rounded border px-1.5 py-0.5">PUBLIC REPO</span>
                        <span>tomlin7 / Browy</span>
                      </div>
                      <h2 className="text-xl font-semibold tracking-tight text-foreground mb-2">
                        {activeTabTitle}
                      </h2>
                      <p className="text-sm text-muted-foreground leading-relaxed max-w-xl">
                        Modern Arc/Zen-inspired Windows 11 browser built with .NET 9 WPF and Microsoft Edge WebView2. Features autonomous DOM actions protocol, tab suspension, and 4-way split views.
                      </p>
                    </div>

                    <div className="grid grid-cols-3 gap-3 border-t pt-4 font-mono text-xs mt-6">
                      <div className="border rounded p-3">
                        <div className="text-muted-foreground text-[10px] uppercase">Cold Start</div>
                        <div className="text-base font-semibold text-foreground mt-0.5">240ms</div>
                      </div>
                      <div className="border rounded p-3">
                        <div className="text-muted-foreground text-[10px] uppercase">Automated Tests</div>
                        <div className="text-base font-semibold text-foreground mt-0.5">203 Passing</div>
                      </div>
                      <div className="border rounded p-3">
                        <div className="text-muted-foreground text-[10px] uppercase">Telemetry</div>
                        <div className="text-base font-semibold text-foreground mt-0.5">0% (Zero)</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>

          {/* 1px Grid Why Native / Features Section */}
          <section className="border-t">
            <div className="px-5 pt-14 md:px-10">
              <div className="font-mono text-[11px] tracking-[0.14em] text-muted-foreground/80 uppercase">
                Why native
              </div>
            </div>

            <div className="mt-8 grid grid-cols-1 gap-px border-t bg-border/70 sm:grid-cols-2 lg:grid-cols-3">
              {features.map((item, idx) => (
                <div key={idx} className="bg-background p-6 md:p-8">
                  <div className="flex items-center gap-2.5">
                    {item.icon}
                    <h3 className="text-sm font-medium">{item.title}</h3>
                  </div>
                  <p className="mt-2.5 text-sm leading-relaxed text-muted-foreground">
                    {item.desc}
                  </p>
                </div>
              ))}
            </div>
          </section>

          {/* Download Section */}
          <section id="download" className="border-t px-5 py-16 md:px-10 md:py-20">
            <div className="font-mono text-[11px] tracking-[0.14em] text-muted-foreground/80 uppercase">
              Download
            </div>
            <h2 className="mt-3 text-2xl font-semibold tracking-tight">Get Browy</h2>
            <p className="mt-2 text-sm text-muted-foreground max-w-lg">
              Download for 64-bit Windows 11 &amp; 10. Portable, reproducible, and MIT open-source.
            </p>

            <div className="mt-6 flex flex-wrap items-center gap-x-5 gap-y-3">
              <a href="https://github.com/tomlin7/Browy/releases/latest" target="_blank" rel="noreferrer">
                <Button size="lg" className="gap-2">
                  <Download className="size-4" />
                  <span>Download Browy-v0.3.0.zip</span>
                </Button>
              </a>

              <a href="https://github.com/tomlin7/Browy/releases/latest" target="_blank" rel="noreferrer">
                <Button variant="outline" size="lg" className="gap-2">
                  <Laptop className="size-4" />
                  <span>Portable Edition</span>
                </Button>
              </a>
            </div>

            <div className="mt-6 text-xs text-muted-foreground font-mono">
              SHA256 checksums verified automatically via GitHub Actions release artifacts.
            </div>
          </section>

          {/* Accordion Questions Section */}
          <section id="faq" className="border-t px-5 py-16 md:px-10">
            <div className="flex items-center justify-between">
              <div className="font-mono text-[11px] tracking-[0.14em] text-muted-foreground/80 uppercase">
                Questions / FAQ
              </div>
              <a href="#benchmarks" className="hidden font-mono text-[11px] text-muted-foreground hover:underline">
                Benchmarks
              </a>
            </div>

            <div className="flex w-full flex-col mt-6 max-w-2xl">
              {faqs.map((faq, idx) => {
                const isOpen = openFaq === idx
                return (
                  <div key={idx} className="border-b last:border-b-0">
                    <h3>
                      <button
                        type="button"
                        onClick={() => setOpenFaq(isOpen ? null : idx)}
                        className="relative flex w-full flex-1 items-start justify-between rounded-lg py-3 text-left font-medium transition-all outline-none hover:underline text-[15px]"
                      >
                        <span>{faq.q}</span>
                        {isOpen ? (
                          <ChevronUp className="size-4 text-muted-foreground shrink-0 ml-4 mt-1" />
                        ) : (
                          <ChevronDown className="size-4 text-muted-foreground shrink-0 ml-4 mt-1" />
                        )}
                      </button>
                    </h3>
                    {isOpen && (
                      <div className="pb-3 text-sm text-muted-foreground leading-relaxed">
                        {faq.a}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          </section>
        </main>

        {/* Minimal Footer */}
        <footer className="flex items-center justify-between border-t px-5 py-10 text-xs text-muted-foreground md:px-10">
          <div className="flex items-center gap-2">
            <BrowyAppIcon className="size-4 text-[9px] rounded-[3px]" />
            <span>&copy; 2026 Browy</span>
          </div>

          <div className="flex items-center gap-5">
            <a href="https://github.com/tomlin7/Browy" target="_blank" rel="noreferrer" className="hover:text-foreground transition-colors">
              GitHub
            </a>
            <a href="https://github.com/tomlin7/Browy/releases" target="_blank" rel="noreferrer" className="hover:text-foreground transition-colors">
              Releases
            </a>
            <a href="https://github.com/tomlin7/Browy/blob/main/LICENSE" target="_blank" rel="noreferrer" className="hover:text-foreground transition-colors">
              MIT License
            </a>
          </div>
        </footer>
      </div>
    </div>
  )
}
